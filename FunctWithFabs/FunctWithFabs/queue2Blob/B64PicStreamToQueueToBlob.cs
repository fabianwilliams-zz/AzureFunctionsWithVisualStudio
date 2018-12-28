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
        //NOT WORKING YET - So far abondoned in favor of WEbJobs SDK version called PictureToBLob in http2BlobFolder
        [FunctionName("B64PicStreamToQueueToBlob")]
        public static async Task AddSelfieToStorageAsync(
            [QueueTrigger("blobqueue-selfiepic", Connection = "AzureWebJobsStorage")]Stream myQueueItem,
            [Blob("fwf-selfie-stor/{id}", FileAccess.Write, Connection = "AzureWebJobsStorage")] Stream myBlob,                                   
            ILogger log)
        {
            //log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");

            using (MemoryStream ms = new MemoryStream())
            {
                myQueueItem.CopyTo(ms);
                var byteArray = ms.ToArray();
                await myBlob.WriteAsync(byteArray, 0, byteArray.Length);
            }

        }

        [FunctionName("WebHookToPushB64Image")]
        public static async Task<IActionResult> PassImageToPicQueue(
            [HttpTrigger(AuthorizationLevel.Function, "post",
            Route = null)] HttpRequest req,
            ILogger log,
            [Queue("blobqueue-selfiepic", Connection = "AzureWebJobsStorage")] IAsyncCollector<Selfie> outputQueue)
        {
            log.LogInformation("Taking the Base64 Encoded File and putting it to Queue which will send to Blob..");


            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Selfie data = JsonConvert.DeserializeObject<Selfie>(requestBody);

            string serData = JsonConvert.SerializeObject(data);

            //write to Azure Queue
            await outputQueue.AddAsync(data);

            return new OkObjectResult($"{data}");
        }

    }
}
