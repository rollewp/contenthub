using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using System;
using Stylelabs.M.Base.Querying;
using System.Threading.Tasks;
using System.Globalization;
using Stylelabs.M.Sdk.Contracts.Base;
using Stylelabs.M.Base.Querying.Filters;
using Stylelabs.M.Framework.Essentials.LoadConfigurations;
using Stylelabs.M.Framework.Essentials.LoadOptions;
using System.Linq;
using System.Text.RegularExpressions;
using Stylelabs.M.Base.Querying.Linq;
using System.Collections.Generic;
using RDA.ContentHub.AzureFunctions.Models;

namespace RDA.ContentHub.AzureFunctions
{
    public static class GetSocialMediaContentDetails
    {
        //Test Comment
        [FunctionName("GetSocialMediaContentDetails")]
        public static async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            SocialMediaContentResponse ret = new SocialMediaContentResponse();
            try
            {
                var MClient = MConnector.Client;
                int contentId = Int32.Parse(req.Query["contentid"], NumberStyles.Integer | NumberStyles.AllowThousands, new CultureInfo("en-US"));
                
                var loadConfig = new EntityLoadConfiguration
                {
                    CultureLoadOption = CultureLoadOption.None,
                    RelationLoadOption = new RelationLoadOption("ChannelAccountToContent"),
                    PropertyLoadOption = PropertyLoadOption.All
                };
                var eventEntity = await MClient.Entities.GetAsync(contentId, loadConfig).ConfigureAwait(false);

                IRelation ca = eventEntity.Relations.First(r => r.Name == "ChannelAccountToContent");
                var caIds = ca.GetIds();

                List<string> channels = new List<string>();
                if (caIds.Count > 0)
                {
                    var channelEnt = await MClient.Entities.GetAsync(caIds[0]).ConfigureAwait(false);
                    channels.Add(channelEnt.GetPropertyValue<string>("ChannelAccount.Name"));
                    for (int i = 1; i < caIds.Count; ++i)
                    {
                        channelEnt = await MClient.Entities.GetAsync(caIds[i]).ConfigureAwait(false);
                        channels.Add(channelEnt.GetPropertyValue<string>("ChannelAccount.Name"));
                    }
                }

                Regex rx = new Regex(@"(http|ftp|https)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                string text = eventEntity.GetPropertyValue<string>("SocialMediaMessage_Body");
                MatchCollection matches = rx.Matches(text);
                string match = null;
                if (matches.Count > 0)
                {
                    match = matches[0].Value;
                }

                string publicLink = null;

                if (match != null)
                {
                    publicLink = match;
                }
                else
                {
                    //get linked asset
                    var query = Query.CreateQuery(entities =>
                        from e in entities
                        where e.Parent("CmpContentToLinkedAsset") == contentId
                        select e);
                    query.Take = 1;

                    // Execute the query
                    var assetPublicLinks = await MClient.Querying.QueryIdsAsync(query);

                    if (assetPublicLinks == null || !(assetPublicLinks.Items.Count > 0))
                    {
                        //get brief asset if there is no linked asset
                        query = Query.CreateQuery(entities =>
                            from e in entities
                            where e.Parent("CmpContentToBriefAsset") == contentId
                            select e);
                        query.Take = 1;
                        assetPublicLinks = await MClient.Querying.QueryIdsAsync(query);
                    }

                    if (assetPublicLinks != null || assetPublicLinks.Items.Count > 0)
                    {
                        var publicLinkAssetID = assetPublicLinks.Items[0];
                        MClient.Logger.Info("POC publicLinkAssetID: " + publicLinkAssetID);
                        var pubLinkQuery = new Query()
                        {
                            Filter = new CompositeQueryFilter()
                            {
                                Children = new QueryFilter[] {
                    new DefinitionQueryFilter() {
                        Name = "M.PublicLink"
                    },
                    new RelationQueryFilter() {
                        ParentId = publicLinkAssetID,
                        Relation = "AssetToPublicLink"
                    }
                }
                            }
                        };
                        var realPublicLinks = await MClient.Querying.QueryIdsAsync(pubLinkQuery);
                        var realPublicLinkID = realPublicLinks.Items[0];
                        var publicLinkEnt = await MClient.Entities.GetAsync(realPublicLinkID).ConfigureAwait(false);
                        var linkRelativeUrlGuid = publicLinkEnt.GetPropertyValue<string>("RelativeUrl");
                        string rel = publicLinkEnt.GetPropertyValue<string>("RelativeUrl");
                        publicLink = "REDACTED" + rel;
                    }
                }

                ret.Message = eventEntity.GetPropertyValue<string>("SocialMediaMessage_Body");
                ret.Tags = eventEntity.GetPropertyValue<string>("SocialMediaMessage_Footer");
                ret.ImageUrl = publicLink;
                ret.BlogUrl = eventEntity.GetPropertyValue<string>("SocialMediaMessage_BlogUrl");
                ret.Channels = channels.ToArray();
            }
            catch (Exception ex)
            {
                ret.Message = ex.Message;
            }

            return (ActionResult)new OkObjectResult(ret);
        }
    }
}

