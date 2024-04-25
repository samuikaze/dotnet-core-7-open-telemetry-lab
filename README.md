# DotNet Core 7 OpenTelemetry Lab

此專案用以了解如何在 .Net Core 7 中透過 OpenTelemetry 建立日誌、指標與追蹤，並暴露到 Prometheus 中。

> 請注意，OpenTelemetry 雖然目前在開源社群非常活躍，但對於 .Net Core 來說，他的相關 SDK 有需多都還是搶鮮版，因此若要應用到生產環境中，請審慎評估。

## 前置準備

- NuGet 套件
  - OpenTelemetry
  - OpenTelemetry.Exporter.Console
    > 將日誌輸出到終端機
  - OpenTelemetry.Exporter.Prometheus.AspNetCore (搶鮮版)
    > 將指標輸出到指定的路徑，並使用 Prometheus 格式輸出
  - OpenTelemetry.Exporter.OpenTelemetryProtocol
  - OpenTelemetry.Extensions.Hosting
  - OpenTelemetry.Instruumentation.AspNetCore
  - OpenTelemetry.Instrumentation.Http
  - OpenTelemetry.Instrumentation.Process
    > 輸出 CPU、記憶體等指標資料
  - OpenTelemetry.Instrumentation.Runtime
    > 輸出 GC 等資料

並在 Extensions 資料夾中新增 `OpenTelemetryExtension.cs` 檔案，寫入如下的內容

```csharp
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
```

最後到 `Program.cs` 中加入以下程式碼：

```csharp
OpenTelemetryExtension.ConfigureOpenTelemetry(builder.Logging, builder.Services, builder.Configuration);
```

## 日誌撰寫

請注意，所有的日誌輸出都應使用結構化日誌進行輸出，這類格式的日誌對於後續系統自動化分析才有意義。

1. 在 Extensions 資料夾新增 `ILoggerExtension.cs`，並加入以下程式碼：

    > 這個主要在做結構化日誌的設定，不一定要透過這種方式輸出日誌

    ```csharp
    internal static partial class LoggerExtensions
    {
        /// <summary>
        /// 啟動應用程式的日誌
        /// </summary>
        /// <param name="logger"></param>
        [LoggerMessage(LogLevel.Information, "Starting the app...")]
        public static partial void StartingApp(this ILogger logger);

        /// <summary>
        /// 取得天氣預報的日誌，其中包含請求的使用者名稱
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="userName">使用者名稱</param>
        [LoggerMessage(LogLevel.Information, "Requesting all weather forcast results, user name is `{userName}`.")]
        public static partial void RetrievingWeatherForecast(this ILogger logger, string userName);
    }
    ```

2. 在專案任意地方注入 ILogger 介面後，透過 ILogger 去呼叫第一步中定義的方法輸入日誌。
3. 若需要調整日誌輸出的嚴重性設定，可以到 `OpenTelemetryExtension` 中設定只輸出哪些嚴重性的日誌。
4. 若需要針對日誌做後處理或前處理，可以自行新增一 `LogProcessor`，如下範例是一個日誌後處理，可以在結束時輸出 `Hello - OnEnd` 日誌：
    1. 新增 LogProcessor

        ```csharp
        public class LogProcessor : BaseProcessor<LogRecord>
        {
            public override void OnEnd(LogRecord data)
            {
                Console.WriteLine("Hello - OnEnd");
                base.OnEnd(data);
            }
        }
        ```

    2. 調整 `OpenTelemetryExtension.cs` 中日誌的設定，加入以下邏輯：

        ```csharp
        loggingBuilder.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(resource);
            options.AddConsoleExporter();
            options.AddProcessor(new LogProcessor());
            options.IncludeFormattedMessage = true;
            options.ParseStateValues = true;
        });
        ```

    3. 重新啟動應用程式，並試著打看看 API，就會看到終端機輸出 `Hello - OnEnd` 字樣的日誌了。

## 指標撰寫

1. 預期指標都會透過 Middleware 進行記錄，因此需建立 `TelemetryMiddleware`，並在檔案中鍵入以下程式碼：

    > Meter 的名稱可以自行決定，但請務必透過常數將之暴露出去，否則在 Extension 終將無法把此自訂的 Meter 加入

    ```csharp
    using System.Diagnostics.Metrics;

    namespace DotNet7.Template.Api.Middlewares
    {
        public class TelemetryMiddleware
        {
            internal const string MeterName = "dotnet7.template.opentelemetry";
            private readonly RequestDelegate _next;
            private readonly Meter _meter;
            private Counter<int> _greetingCounter;

            public TelemetryMiddleware(RequestDelegate next)
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

        public static class OpenTelemetryMiddlewareExtensions
        {
            public static IApplicationBuilder UseOpenTelemetryMiddleware(
                this IApplicationBuilder app)
            {
                return app.UseMiddleware<TelemetryMiddleware>();
            }
        }
    }
    ```

2. 在 `Program.cs` 中加入以下兩行程式碼：

    ```csharp
    app.UseOpenTelemetryMiddleware();
    // 設定指標輸出的路徑
    app.UseOpenTelemetryPrometheusScrapingEndpoint(context =>
        context.Request.Path == "/metrics");
    ```

3. 啟動應用程式，存取 `/metrics` 就可以看到 Prometheus 格式的指標被輸出在頁面上

    > 若沒看到自訂的指標，請先試著打幾次 API，當流量經過 Middleware 後，這些指標就會被暴露出來了

## 撰寫追蹤

OpenTelemetry 在 .Net Core 中的追蹤實作是依賴於 .Net Core 內建的 `System.Diagnostics` API，因此若希望使用 OpenTelemetry API 實作追蹤，請參閱 [OpenTelemetry API Shim 文件](https://opentelemetry.io/docs/languages/net/shim)。

## 參考資料

- [Documentation - OpenTelemetry](https://opentelemetry.io/docs/)
- [open-telemetry / opentelemetry-dotnet](https://github.com/open-telemetry/opentelemetry-dotnet)
- [初探 OpenTelemetry 工具組：蒐集遙測數據的新標準](https://www.youtube.com/watch?v=PT-Bjs6iCug)
- [[鐵人賽 Day03] ASP.NET Core 2 系列 - Middleware](https://blog.johnwu.cc/article/ironman-day03-asp-net-core-middleware.html)
- [OpenTelemetry 的 .NET 可檢視性](https://learn.microsoft.com/zh-tw/dotnet/core/diagnostics/observability-with-otel)
- [OpenTelemetry .NET Logs](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/logs/README.md#best-practices)
- [Getting Started with OpenTelemetry .NET Logs in 5 Minutes - Console Application](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/logs/getting-started-console/README.md)
- [Prometheus Exporter AspNetCore for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Exporter.Prometheus.AspNetCore)
- [Customizing OpenTelemetry .NET SDK for Metrics](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/metrics/customizing-the-sdk/README.md)
- [opentelemetry-dotnet - Program.cs](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/examples/AspNetCore/Program.cs)
- [How to Setup OpenTelemetry Logging in .NET](https://www.youtube.com/watch?v=QU_o24OZeIw)
- [Collector](https://opentelemetry.io/docs/collector/)
- [OTLP Exporter Configuration](https://opentelemetry.io/docs/languages/sdk-configuration/otlp-exporter/#otel_exporter_otlp_endpoint)
- [OTLP Exporter for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/README.md)
- [Getting started with OpenTelemetry Metrics in .NET 8. Part 2: Instrumenting the BookStore API](https://www.mytechramblings.com/posts/getting-started-with-opentelemetry-metrics-and-dotnet-part-2/)
- [在 ASP .NET Core 中使用 OpenTelemetry，為應用程式埋下觀測點](https://hackmd.io/@ZamHsu/BkzYWpiao)
- [ASP.NET Core Instrumentation for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Instrumentation.AspNetCore/README.md)
- [建立計量](https://learn.microsoft.com/zh-tw/dotnet/core/diagnostics/metrics-instrumentation)
- [System.Diagnostics.Metrics 命名空間](https://learn.microsoft.com/zh-tw/dotnet/api/system.diagnostics.metrics?view=net-8.0)
- [podman build with relative path to dockerfile in subdirectory fails with "no such file or directory" on Mac #12841](https://github.com/containers/podman/issues/12841)
