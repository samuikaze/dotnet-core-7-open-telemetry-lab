using DotNet7.OpenTelemetryLab.Api.Models.ServiceModels;

namespace DotNet7.OpenTelemetryLab.Api.Services
{
    public interface IWeatherService
    {
        public List<WeatherForecastServiceModel> Get();
    }
}
