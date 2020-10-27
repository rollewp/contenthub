using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using RDA.ContentHub.AzureFunctions.Models;

namespace RDA.ContentHub.AzureFunctions
{
    public static class GetEngagementMetricsFromTwitter
    {
        [FunctionName("GetEngagementMetricsFromTwitter_Orchestrator")]
        public static async Task<IActionResult> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var twitterAnalytics = await context.CallActivityAsync<CHMetrics>("GetEngagementMetrics", "");
            
            return (ActionResult)new OkObjectResult(twitterAnalytics); ;
        }

        [FunctionName("GetEngagementMetrics")]
        public static async Task<CHMetrics> Get([ActivityTrigger] ILogger log)
        {
            // TODO: Get actual Analytics from Twitter here
            var twitterAnalytics = new CHMetrics()
            {
                ChannelAccount = "REDACTED",
                PostIdFromChannel = "REDACTED",
                Clicks = "22",
                Likes = "12",
                Retweets = "8"
            };

            await SetEngagementMetricsToContent.UpdateContentMetrics(twitterAnalytics, log);

            return twitterAnalytics;
        }

        [FunctionName("GetEngagementMetricsFromTwitter")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("GetEngagementMetricsFromTwitter_Orchestrator", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}