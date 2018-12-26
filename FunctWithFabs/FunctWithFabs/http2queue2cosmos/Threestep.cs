using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FunctWithFabs.http2queue2cosmos
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
            log.LogInformation("C# Fabian Http Trigger preparing to send payload to Queue.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<ColleagueAccolade>(requestBody);
            var tabledata = JsonConvert.DeserializeObject<AccoladeTable>(requestBody);
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

        [FunctionName("ProcessAccoladeQueue")]
        public static void ProcessAccoladeQueue(
            [QueueTrigger("fwfqueue-accolade")] string myQueueItem,
            ILogger log)
        {
            log.LogInformation($"C# Fabian ProcessAccolade Queue Function completded..: {myQueueItem}");
        }

    }
}
