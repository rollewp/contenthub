using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Globalization;
using Stylelabs.M.Framework.Essentials.LoadConfigurations;
using Stylelabs.M.Framework.Essentials.LoadOptions;
using RDA.ContentHub.AzureFunctions.Models;

namespace RDA.ContentHub.AzureFunctions
{
    public static class PostToCHContent
    {
        [FunctionName("PostToCHContent")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            //string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            CHPostRequest data = JsonConvert.DeserializeObject<CHPostRequest>(requestBody);
            log.LogInformation(requestBody);
            
            var MClient = MConnector.Client;
            //int contentId = Int32.Parse(req.Query["contentid"], NumberStyles.Integer | NumberStyles.AllowThousands, new CultureInfo("en-US"));
            int contentId = int.Parse(data.SaveEntityMessage.TargetId.ToString());
            var loadConfig = new EntityLoadConfiguration
            {
                CultureLoadOption = CultureLoadOption.None,
                RelationLoadOption = new RelationLoadOption("ChannelAccountToContent"),
                PropertyLoadOption = PropertyLoadOption.All
            };
            var eventEntity = await MClient.Entities.GetAsync(contentId, loadConfig).ConfigureAwait(false);
            string messageBody = eventEntity.GetPropertyValue<string>("SocialMediaMessage_Body");
            eventEntity.SetPropertyValue("SocialMediaMessage_Body", messageBody + " - Appended Ticks: " + DateTime.Now.Ticks.ToString());

            MClient.Entities.SaveAsync(eventEntity).Wait();

            string responseMessage = "Event Updated successfully from Azure Function";

            return new OkObjectResult(responseMessage);
        }
    }
}
