using System.Diagnostics.Metrics;

namespace OpenTelemetry.Lab.Api.Middlewares
{
    public class OpenTelemetryMiddleware
    {
        internal const string MeterName = "dotnet7.opentelemetry.lab";
        private readonly RequestDelegate _next;
        private readonly Meter _meter;
        private Counter<int> _greetingCounter;

        public OpenTelemetryMiddleware(RequestDelegate next)
        {
            _next = next;
            _meter = new Meter(MeterName, "1.0.0");
            _greetingCounter = _meter.CreateCounter<int>(
                "greetings.count",
                description: "Counts the number of greetings.");
        }

        public async Task Invoke(HttpContext context)
        {
            await _next(context);
            _greetingCounter.Add(1);
        }
    }

    public static class  OpenTelemetryMiddlewareExtension
    {
        public static IApplicationBuilder UseOpenTelemetryMiddleware(
            this IApplicationBuilder app)
        {
            return app.UseMiddleware<OpenTelemetryMiddleware>();
        }
    }
}
