using AutoMapper;
using DotNet7.OpenTelemetryLab.Api.Models.ServiceModels;
using DotNet7.OpenTelemetryLab.Api.Models.ViewModels;

namespace DotNet7.OpenTelemetryLab.Api.AutoMapperProfiles
{
    public class WeatherProfile : Profile
    {
        public WeatherProfile()
        {
            CreateMap<WeatherForecastServiceModel, WeatherForecastViewModel>().ReverseMap();
        }
    }
}
