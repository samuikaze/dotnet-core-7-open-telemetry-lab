using DotNet7.OpenTelemetryLab.Api.Middlewares;

namespace DotNet7.OpenTelemetryLab.Api.Extensions
{
    public static class MiddlewareExtension
    {
        public static IApplicationBuilder ConfigureMiddlewares(this IApplicationBuilder builder)
        {
            // 套用 OpenTelemetryMiddleware
            builder.UseMiddleware<OpenTelemetryMiddleware>();

            return builder;
        }
    }
}
