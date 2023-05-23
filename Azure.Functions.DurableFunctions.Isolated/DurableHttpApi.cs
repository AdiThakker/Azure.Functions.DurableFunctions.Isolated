using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.DurableTask;

namespace Azure.Functions.DurableFunctions.Isolated
{
    public class DurableHttpApi
    {
        private ILogger Logger;
        private string eventName = "DurableHttpApivent";
        private string correlationId =string.Empty;

        [Function(nameof(DurableHttpRequest))]
        public async Task<HttpResponseData> DurableHttpRequest([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req, 
                                                                [DurableClient] DurableTaskClient client, FunctionContext executionContext)
        {
            try
            {
                Logger = executionContext.GetLogger("Function1_HttpStart");
                correlationId = Guid.NewGuid().ToString();
                var payload = new StreamReader(req.Body).ReadToEnd();
                var status = await client.ScheduleNewOrchestrationInstanceAsync(nameof(RunOrchestrator), payload, new StartOrchestrationOptions(correlationId));
                Logger.LogInformation("Started orchestration with ID = '{instanceId}'.", correlationId);

                var result = await client.WaitForInstanceCompletionAsync(correlationId);
                if (result.IsCompleted)
                {
                    return req.CreateResponse(System.Net.HttpStatusCode.OK);
                }
                else
                {
                    await client.TerminateInstanceAsync(correlationId);
                    return req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
            }

            return req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
        }

        [Function(nameof(RunOrchestrator))]
        public async Task<string> RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context) => await context.WaitForExternalEvent<string>(eventName);

        [Function(nameof(DurableHttpApiResponse))]
        public async Task DurableHttpApiResponse([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req, [DurableClient] DurableTaskClient client)
        {
            await client.RaiseEventAsync(eventName, correlationId, $"I am complete CorrelationID: {correlationId}");
        }
    }
}
