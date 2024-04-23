using DotNet7.OpenTelemetryLab.Api.Services;

namespace DotNet7.OpenTelemetryLab.Api.Extensions
{
    public class ServiceMapperExtension
    {
        public static IServiceCollection? GetServiceProvider(IServiceCollection? serviceCollection)
        {
            if (serviceCollection != null)
            {
                serviceCollection.AddScoped<IWeatherService, WeatherService>();
            }

            return serviceCollection;
        }
    }
}
