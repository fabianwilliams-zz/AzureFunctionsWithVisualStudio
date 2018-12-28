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
using System.Text;
using Microsoft.Rest;
using System.Threading;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using System.Collections.Generic;

namespace FunctWithFabs.httpText
{


    class ApiKeyServiceClientCredentials : ServiceClientCredentials
    {
        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string keyText = Environment.GetEnvironmentVariable("TextCognitiveServicesKey1");
            request.Headers.Add("Ocp-Apim-Subscription-Key", keyText);
            return base.ProcessHttpRequestAsync(request, cancellationToken);
        }
    }

    public static class WhatDidYouSayAboutMe
    {
        //No longer needed as i am using the native SDK Client
        /*
        public static async Task<Quote[]> GetTextAnalysisResults(string txtPayload)
        {

            string textUrlBase = Environment.GetEnvironmentVariable("TextCognitiveServicesUrlBase");
            string keyText = Environment.GetEnvironmentVariable("TextCognitiveServicesKey1");

            string reqParams = "?returnFaceLandmarks=true&returnFaceAttributes=emotion";

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", keyText);

            //var json = "{\"url\":\"" + $"{txtPayload}" + "\"}";
            var json = "{\"documents\": [{\"language\": \"en\", \"id\": \"1\",\"text\": \"" + $"{txtPayload}" + "\"}}]";

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, textUrlBase);
            request.Content = new StringContent(json,
                                                Encoding.UTF8,
                                                "application/json");

            var resp = await client.SendAsync(request);
            var jsonResponse = await resp.Content.ReadAsStringAsync();


            var quotes = JsonConvert.DeserializeObject<Quote[]>(jsonResponse);

            return quotes;

        }
        */
        public static IncomingText GetTextInfo(HttpRequest req)
        {
            string txtInfo = req.Query["Text"];

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject<IncomingText>(requestBody);


            if (string.IsNullOrWhiteSpace(txtInfo))
            {
                return data;
            }
            else
            {
                //the below is used to allow the GET method which only takes the raw text to conform
                //or cast to a IncomingText object. Be aware this hard codes English as the language
                txtInfo = "{\"Language\": \"en\", \"Id\": \"1\",\"Text\": \"" + $"{txtInfo}" + "\"}";
                IncomingText txtData = JsonConvert.DeserializeObject<IncomingText>(txtInfo);
                return txtData;
            }
        }

        [FunctionName("WhatDidYouSay")]
        public static async Task<IActionResult> WhatDidYouSay(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", 
            Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Functions with Fabian Text Analyisi Cog Svcs about to begin...");
            IncomingText input = GetTextInfo(req);

            ITextAnalyticsClient client = new TextAnalyticsClient(new ApiKeyServiceClientCredentials())
            {
                Endpoint = "https://eastus2.api.cognitive.microsoft.com"
            };

            if (string.IsNullOrWhiteSpace(input.Text))
            {
                return new BadRequestObjectResult("Please pass an image URL on the query string or in the request body");
            }
            else
            {
                SentimentBatchResult result = client.SentimentAsync(
                new MultiLanguageBatchInput(
                new List<MultiLanguageInput>()
                {
                    new MultiLanguageInput(input.Language, input.Id, input.Text),
                })).Result;

                var outputItem = JsonConvert.SerializeObject(result.Documents[0].Score);
                log.LogInformation($"Evauated Sentiment of thet text: \"{input.Text}\" is: {outputItem}");
                return new OkObjectResult($"{outputItem}");
            }
        }
    }
}
