using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FunctWithFabs.httpgen
{
    public static class Fabshttp1
    {
        [FunctionName("fabshttp1")]
        public static async Task<IActionResult> FabsHttp1(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", 
            Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Simple HTTP Trigger that echos a Name for now. This is raw scaffolding..");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }
    }
}
