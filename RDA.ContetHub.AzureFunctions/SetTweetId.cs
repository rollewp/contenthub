using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Stylelabs.M.Framework.Essentials.LoadConfigurations;
using System.Threading.Tasks;
using RDA.ContentHub.AzureFunctions.Models;

namespace RDA.ContentHub.AzureFunctions
{
    class SetTweetId
    {
        [FunctionName("SetTweetId")]
        public static async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req, ILogger log)
        {
            // Extract message from request body
            var postedData = await req.Content.ReadAsAsync<CHContent>();
            log.LogInformation($"Message is {postedData.TweetId}.");

            // Extract target id from request header
            //var targetId = req.Query["id"];
            
            log.LogInformation($"Loading entity {postedData.ContentId}.");

            // Get entity
            var entity = await MConnector.Client.Entities.GetAsync(postedData.ContentId, EntityLoadConfiguration.DefaultCultureFull);
            if (entity == null) return new NotFoundResult();

            // Set the property
            entity.SetPropertyValue("TweetId", postedData.TweetId);

            // Update entity
            log.LogInformation($"Updating entity {postedData.TweetId}.");
            await MConnector.Client.Entities.SaveAsync(entity);

            // Create response
            return new OkResult();
        }
    }
}
