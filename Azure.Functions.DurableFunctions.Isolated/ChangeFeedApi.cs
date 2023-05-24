using System;
using System.Collections.Generic;
using Azure.Core;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading;

namespace Azure.Functions.DurableFunctions.Isolated
{
    public class ChangeFeedApi
    {
        [Function("ChangeFeedApiHttpRequest")]
        [CosmosDBOutput(databaseName: "ApiRequests",
                       collectionName: "Outbox",
                       ConnectionStringSetting = "CosmosConnection")]
        public async Task<HttpResponseData> ChangeFeedApiHttpRequest([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("ChangeFeedApi");
            var correlationId = Guid.NewGuid().ToString();
            logger.LogInformation("Received Request for CorrleationId = '{instanceId}'.", correlationId);

            var payload = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(payload);

            // Output to Cosmos DB
            var cosmosData = new { id = Guid.NewGuid().ToString(), Payload = data };

            // Create response
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteStringAsync("This HTTP triggered function executed successfully.");

            return response;
        }       

        [Function("ChangeFeedApiTrigger")]
        public void ChangeFeedApiTrigger([CosmosDBTrigger(
            databaseName: "ApiRequests",
            collectionName: "Outbox",
            ConnectionStringSetting = "CosmosConnection",
            LeaseCollectionName = "Outboxleases", CreateLeaseCollectionIfNotExists = true)] IReadOnlyList<MyDocument> input, FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("ChangeFeedApi");
            if (input != null && input.Count > 0)
            {
                logger.LogInformation("Documents modified: " + input.Count);
                logger.LogInformation("First document Id: " + input[0].Id);
            }
        }
    }

    public class MyDocument
    {
        public string Id { get; set; }

        public string Text { get; set; }

        public int Number { get; set; }

        public bool Boolean { get; set; }
    }
}
