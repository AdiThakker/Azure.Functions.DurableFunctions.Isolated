using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Hosting;

var hostBuilder = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services => services.AddDurableTaskClient(builder =>
    {
        // Configure options for this builder. Can be omitted if no options customization is needed.
        builder.Configure(opt => { });
        builder.UseGrpc(); // multiple overloads available for providing gRPC information

        // AddDurableTaskClient allows for multiple named clients by passing in a name as the first argument.
        // When using a non-default named client, you will need to make this call below to have the
        // DurableTaskClient added directly to the DI container. Otherwise IDurableTaskClientProvider must be used
        // to retrieve DurableTaskClients by name from the DI container. In this case, we are using the default
        // name, so the line below is NOT required as it was already called for us.
        builder.RegisterDirectly();
    }));
    
var host =hostBuilder.Build();
host.Run();
