using DotNet7.OpenTelemetryLab.Api.HttpClients;

namespace DotNet7.OpenTelemetryLab.Api.Extensions
{
    public class HttpClientExtension
    {
        public static IServiceCollection ConfigureHttpClients(IServiceCollection serviceCollection)
        {
            serviceCollection.AddHttpClient<SingleSignOnClient>();

            return serviceCollection;
        }
    }
}
