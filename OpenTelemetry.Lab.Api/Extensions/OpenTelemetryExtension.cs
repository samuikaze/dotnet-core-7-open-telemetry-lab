using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Exporter;

namespace DotNet7.OpenTelemetryLab.Api.Extensions
{
    public static class OpenTelemetryExtension
    {
        public static Meter openTelemetryMeter = new Meter("dotnet7.core.telemetry", "1.0.0");
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

            string? otlpTargetUri = config.GetValue(
                "OpenTelemetry:OtlpUri",
                "http://localhost:4318");
            if (otlpTargetUri == null)
            {
                otlpTargetUri = "http://localhost:4318";
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
                    .AddMeter(openTelemetryMeter.Name)
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddPrometheusExporter()
                    .AddConsoleExporter()
                    .AddPrometheusExporter())
                .WithTracing(tracing => tracing
                    .AddAspNetCoreInstrumentation()
                    .AddConsoleExporter())
                .UseOtlpExporter(
                    OtlpExportProtocol.HttpProtobuf,
                    new Uri(otlpTargetUri));

            return serviceCollection;
        }
    }
}
