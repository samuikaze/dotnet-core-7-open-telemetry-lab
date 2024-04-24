using System.Diagnostics.Metrics;
using DotNet7.OpenTelemetryLab.Api.Extensions;

namespace DotNet7.OpenTelemetryLab.Api.Middlewares
{
    public class OpenTelemetryMiddleware
    {
        private readonly RequestDelegate _next;
        private Counter<int> _greetingCounter;

        public OpenTelemetryMiddleware(RequestDelegate next)
        {
            _next = next;
            _greetingCounter = OpenTelemetryExtension.openTelemetryMeter.CreateCounter<int>(
                "greetings.count",
                description: "Counts the number of greetings.");
        }

        public async Task Invoke(HttpContext context)
        {
            await _next(context);
            _greetingCounter.Add(1);
        }
    }
}
