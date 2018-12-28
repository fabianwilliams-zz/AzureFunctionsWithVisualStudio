using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FunctWithFabs.queue2Blob
{
    public static class B64PicStreamToQueueToBlob
    {

        [FunctionName("B64PicStreamToQueueToBlob")]
        public static void AddSelfieToStorage(
            [QueueTrigger("blobqueue-selfiepic", Connection = "AzureWebJobsStorage")]string myQueueItem,
            [Blob("fwf-selfie-stor/{queueTrigger}", System.IO.FileAccess.Write)] Stream myBlob,                                   
            ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
        }

        [FunctionName("WebHookToPushB64Image")]
        public static async Task<IActionResult> PassImageToPicQueue(
            [HttpTrigger(AuthorizationLevel.Function, "post",
            Route = null)] HttpRequest req,
            ILogger log,
            [Queue("blobqueue-selfiepic", Connection = "AzureWebJobsStorage")] IAsyncCollector<String> outputQueue)
        {
            log.LogInformation("Taking the Base64 Encoded File and putting it to Queue which will send to Blob..");


            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Selfie data = JsonConvert.DeserializeObject<Selfie>(requestBody);

            //write to Azure Queue
            await outputQueue.AddAsync(data.B64Payload);

            return new OkObjectResult($"{data}");
        }

    }
}
