using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace Azure.Functions.DurableFunctions.Isolated
{
    public class HttpApi
    {
        private ILogger Logger;
        private string correlationId = string.Empty;

        private static ConcurrentDictionary<string, TaskCompletionSource<SampleResponse>> requests = new ConcurrentDictionary<string, TaskCompletionSource<SampleResponse>>();

        [Function("HttpRequest")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, FunctionContext executionContext)
        {
            Logger = executionContext.GetLogger("Function1_HttpStart");
            correlationId = Guid.NewGuid().ToString();
            var payload = new StreamReader(req.Body).ReadToEnd();

            Logger.LogInformation("Received Request for CorrleationId = '{instanceId}'.", correlationId);

            TaskCompletionSource<SampleResponse> tcs = new TaskCompletionSource<SampleResponse>(correlationId);
            requests.TryAdd(correlationId, tcs);
            var result = await tcs.Task;
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;            
        }

        [Function(nameof(HttpApiResponse))]
        public async Task HttpApiResponse([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            var payload = new StreamReader(req.Body).ReadToEnd(); // payload is correlationId
            if (requests.TryGetValue(payload, out TaskCompletionSource<SampleResponse> tcs))
                tcs?.SetResult(new SampleResponse($"I am now complete for {payload}"));
            else
                tcs?.SetException(new Exception("Invalid request"));
        }

        public record SampleResponse(string Message);
    }
}
