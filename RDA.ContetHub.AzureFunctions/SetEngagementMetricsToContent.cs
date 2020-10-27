using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RDA.ContentHub.AzureFunctions.Models;
using Stylelabs.M.Base.Querying;
using Stylelabs.M.Base.Querying.Filters;
using Stylelabs.M.Framework.Essentials.LoadConfigurations;
using Stylelabs.M.Framework.Essentials.LoadOptions;
using Stylelabs.M.Sdk.Contracts.Base;

namespace RDA.ContentHub.AzureFunctions
{
    public static class SetEngagementMetricsToContent
    {
        [FunctionName("SetEngagementMetricsToContent_Orchestrator")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();
            var postedData = context.GetInput<CHMetrics>();
            if (postedData != null)
            {
                await context.CallActivityAsync("UpdateContentMetrics", postedData);
            }

            return outputs;
        }

        [FunctionName("UpdateContentMetrics")]
        public static async Task<string> UpdateContentMetrics([ActivityTrigger] CHMetrics chMetrics, ILogger log)
        {
            var MClient = MConnector.Client;

            MClient.Logger.Info("Script Execution Started");

            long? twitterChannelAccountId = 0;

            // Find Channel Account ID
            var channelAccountQuery = Query.CreateQuery(entities => from e in entities where e.DefinitionName == "M.ChannelAccount" select e);
            var channelAccountIterator = MClient.Querying.CreateEntityIterator(channelAccountQuery,
                    new EntityLoadConfiguration(CultureLoadOption.None, new PropertyLoadOption("ChannelAccount.Name"), RelationLoadOption.None));

            var channelAccountFound = false;
            while (await channelAccountIterator.MoveNextAsync().ConfigureAwait(false) && channelAccountFound == false)
            {
                foreach (var channelAccount in channelAccountIterator.Current.Items)
                {
                    var name = channelAccount.GetPropertyValue<string>("ChannelAccount.Name");
                    if (name == chMetrics.ChannelAccount)
                    {
                        twitterChannelAccountId = channelAccount.Id;
                        channelAccountFound = true;
                        break;
                    }
                }
            }

            if (twitterChannelAccountId == null || twitterChannelAccountId == 0)
            {
                MClient.Logger.Info($"Channel Account {chMetrics.ChannelAccount} not found");
                return $"Channel Account {chMetrics.ChannelAccount} not found";
            }


            // Get All Where USed entities associated to Channel Account
            var whereUsedQuery = new Query()
            {
                Filter = new CompositeQueryFilter()
                {
                    Children = new QueryFilter[] 
                                {
                                    new DefinitionQueryFilter() {
                                        Name = "M.WhereUsed"
                                    },
                                    new RelationQueryFilter() {
                                        ParentId = twitterChannelAccountId,
                                        Relation = "ChannelAccountToWhereUsed"
                                    }
                                }
                }
            };

            var whereUsedEntities = await MClient.Querying.QueryIdsAsync(whereUsedQuery);
            if (whereUsedEntities?.Items?.Count == 0)
            {
                MClient.Logger.Info($"No Where used entities found for Channel Account {chMetrics.ChannelAccount}");
                return $"No Where used entities found for Channel Account {chMetrics.ChannelAccount}";
            }


            // Find content based on tweet id
            var contentQuery = Query.CreateQuery(entities => from e in entities where e.DefinitionName == "M.Content" select e);
            var iterator = MClient.Querying.CreateEntityIterator(contentQuery,
                    new EntityLoadConfiguration(CultureLoadOption.None, new PropertyLoadOption("ContentToWhereUsed", "TweetId"), RelationLoadOption.None));

            var metricsUpdated = false;
            while (await iterator.MoveNextAsync().ConfigureAwait(false) && metricsUpdated == false)
            {
                foreach (var content in iterator.Current.Items)
                {
                    // Check if the metrics for the right content is being updated
                    var contentTweetId = content.GetPropertyValue<String>("TweetId");
                    if (string.IsNullOrEmpty(contentTweetId) || contentTweetId != chMetrics.PostIdFromChannel)
                        continue;

                    // Build the query to retrieve the Where Used
                    var usageQuery = new Query()
                    {
                        Filter = new CompositeQueryFilter()
                        {
                            Children = new QueryFilter[] {
                                new DefinitionQueryFilter() {
                                    Name = "M.WhereUsed"
                                },
                                new RelationQueryFilter() {
                                    ParentId = content.Id,
                                    Relation = "ContentToWhereUsed"
                                }
                            }
                        }
                    };

                    // Get Usage entities for Where Used for the Content
                    var usageItems = await MClient.Querying.QueryIdsAsync(usageQuery);
                    if (usageItems?.Items?.Count > 0)
                    {
                        foreach (var usageItem in usageItems.Items)
                        {
                            foreach (var whereUsedItem in whereUsedEntities.Items)
                            {
                                if (whereUsedItem == usageItem)
                                {
                                    // find usage metrics that's associated to content
                                    var usageLoadConfiguration = new EntityLoadConfiguration(
                                        CultureLoadOption.None,
                                        new PropertyLoadOption("WhereUsed.Impact", "WhereUsed.Metrics"),
                                        RelationLoadOption.None);

                                    IEntity usageEntity = await MClient.Entities.GetAsync(whereUsedItem, usageLoadConfiguration);
                                    if (usageEntity == null)
                                    {
                                        MClient.Logger.Info($"Couldn't find entity with id: {usageItem}");
                                        return $"Couldn't find entity with id: {usageItem}";
                                    }

                                    var metrics = usageEntity.GetPropertyValue<JToken>("WhereUsed.Metrics");
                                    if (metrics == null)
                                    {
                                        MClient.Logger.Info("Metrics property not found");
                                        return "Metrics property not found";
                                    }

                                    var metricsProperties = metrics.Children().ToList();
                                    bool updateUsageEntity = false;
                                    foreach (var prop in metricsProperties)
                                    {
                                        switch (((Newtonsoft.Json.Linq.JProperty)prop).Name.ToLower())
                                        {
                                            case "clicks":
                                                ((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)prop).Value).Value = chMetrics.Clicks;
                                                updateUsageEntity = true;
                                                break;
                                            case "mentions":
                                                ((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)prop).Value).Value = chMetrics.Mentions;
                                                updateUsageEntity = true;
                                                break;
                                            case "likes":
                                                ((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)prop).Value).Value = chMetrics.Likes;
                                                updateUsageEntity = true;
                                                break;
                                            case "comments":
                                                ((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)prop).Value).Value = chMetrics.Comments;
                                                updateUsageEntity = true;
                                                break;
                                            case "reach":
                                                ((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)prop).Value).Value = chMetrics.Reach;
                                                updateUsageEntity = true;
                                                break;
                                            case "favorites":
                                                ((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)prop).Value).Value = chMetrics.Favorites;
                                                updateUsageEntity = true;
                                                break;
                                            case "plusOnes":
                                                ((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)prop).Value).Value = chMetrics.PlusOnes;
                                                updateUsageEntity = true;
                                                break;
                                            case "replies":
                                                ((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)prop).Value).Value = chMetrics.Replies;
                                                updateUsageEntity = true;
                                                break;
                                            case "retweets":
                                                ((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)prop).Value).Value = chMetrics.Retweets;
                                                updateUsageEntity = true;
                                                break;
                                            case "reshares":
                                                ((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)prop).Value).Value = chMetrics.Reshares;
                                                updateUsageEntity = true;
                                                break;
                                            case "repins":
                                                ((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)prop).Value).Value = chMetrics.Repins;
                                                updateUsageEntity = true;
                                                break;
                                            case "connections":
                                                ((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)prop).Value).Value = chMetrics.Connections;
                                                updateUsageEntity = true;
                                                break;
                                        }
                                    }

                                    if (updateUsageEntity == true)
                                    {
                                        await MClient.Entities.SaveAsync(usageEntity).ConfigureAwait(false);
                                    }
                                    metricsUpdated = true;
                                    break;
                                }
                            }

                            if (metricsUpdated)
                            {
                                break;
                            }
                        }
                    }

                    if (metricsUpdated)
                    {
                        break;
                    }
                }
            }

            MClient.Logger.Info("Script Execution Completed");

            return $"Metrics Updated: ${metricsUpdated}. Script Execution Completed.";
        }

        [FunctionName("SetEngagementMetricsToContent")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var postedData = await req.Content.ReadAsAsync<CHMetrics>();

            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("SetEngagementMetricsToContent_Orchestrator", null, postedData);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}