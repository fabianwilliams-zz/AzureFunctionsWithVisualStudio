using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;

namespace FunctWithFabs.http2queue2TableStor
{
    [StorageAccount("AzureWebJobsStorage")]
    public static class Threestep
    {
        [FunctionName("Pass3Hands")]
        public static async Task<IActionResult> Pass3Hands(
            [HttpTrigger(AuthorizationLevel.Function, "post", 
            Route = null)] HttpRequest req,
            ILogger log,
            [Queue("fwfqueue-accolade", Connection = "AzureWebJobsStorage")] IAsyncCollector<ColleagueAccolade> outputQueue,
            [Table("fwftableaccolade", Connection = "AzureWebJobsStorage")] IAsyncCollector<AccoladeTable> outputTable)
        {
            log.LogInformation("C# Fabian Http Trigger preparing to send payload to Queue and a Table as well.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<ColleagueAccolade>(requestBody);
            var tabledata = JsonConvert.DeserializeObject<AccoladeTable>(requestBody);
            //the below is needed to be able to write to Table Storage
            //Partition Key is just a distinguqising name
            //RowKey needs to be unique
            tabledata.PartitionKey = "Accolades";
            tabledata.RowKey = tabledata.Id;

            //write to Azure Queue
            await outputQueue.AddAsync(data);
            //now write to Azure Table Storage
            await outputTable.AddAsync(tabledata);

            //log.LogInformation($"C# Fabian Http Trigger queue added {requestBody}");

            return data != null
                ? (ActionResult)new OkObjectResult($"Hello, {requestBody}")
                : new BadRequestObjectResult("Please pass a valid colleague Accolade to the request body");
        }

        private static Lazy<HttpClient> HttpClient = new Lazy<HttpClient>(() => new HttpClient());

        [FunctionName("ProcessAccoladeQueue")]
        public static async Task ProcessAccoladeQueueAsync(
            [QueueTrigger("fwfqueue-accolade")] string myQueueItem,
            ILogger log)
        {
            var data = JsonConvert.DeserializeObject<ColleagueAccolade>(myQueueItem);

            string WebhookUrl = Environment.GetEnvironmentVariable("FWorldAccoladeTeamWebHook");
            log.LogInformation("Sending to Microsoft Teams Channel Now");
            var teamsResult = await HttpClient.Value.PostAsync(WebhookUrl, 
                new StringContent($"{{\"@type\": \"MessageCard\",\"@context\": \"http://schema.org/extensions\",\"summary\": \"TeamMember Accolade\",\"themeColor\": \"0075FF\",\"sections\": [{{\"startGroup\": true,\"title\": \"**Your Colleagure said:**\",\"text\": \"![Text]({data.AccoladeStatement})\"}}]}}"));

            teamsResult.EnsureSuccessStatusCode();
            log.LogInformation($"Result is {teamsResult.StatusCode}");
            log.LogInformation($"C# Fabian ProcessAccolade Queue Function completded..: {myQueueItem}");
        }

    }
}
