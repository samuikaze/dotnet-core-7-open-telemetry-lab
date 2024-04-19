using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Lab.Api.Middlewares;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

namespace OpenTelemetry.Lab.Api.Extensions
{
    public class OpenTelemetryExtension
    {
        public static IServiceCollection ConfigureOpenTelemetry(
            ILoggingBuilder loggingBuilder,
            IServiceCollection serviceCollection,
            IConfiguration config)
        {
            string? applicationName = config.GetValue<string>("Application:Name");
            if (applicationName == null)
            {
                applicationName = "OpenTelemetry.Lab";
            }

            var resource = ResourceBuilder.CreateDefault().AddService(applicationName);
            // 設定僅輸出警告日誌
            // loggingBuilder.AddFilter<OpenTelemetryLoggerProvider>("*", LogLevel.Warning);
            loggingBuilder.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(resource);
                options.AddConsoleExporter();
            });

            serviceCollection.AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(serviceName: applicationName))
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddPrometheusExporter()
                    .AddMeter(OpenTelemetryMiddleware.MeterName))
                .WithTracing(tracing => tracing
                    .AddAspNetCoreInstrumentation()
                    .AddConsoleExporter());

            return serviceCollection;
        }
    }
}
