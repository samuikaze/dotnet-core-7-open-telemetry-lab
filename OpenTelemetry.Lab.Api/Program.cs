using Microsoft.OpenApi.Models;
using DotNet7.OpenTelemetryLab.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

CorsHandlerExtension.ConfigureCorsHeaders(builder.Services);

// Add services to the container.

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
}); ;
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

SwaggerDefinitionExtension.ConfigureSwagger(builder.Services);
ServiceMapperExtension.GetServiceProvider(builder.Services);
DatabaseExtension.AddDatabaseContext(builder.Services, builder.Configuration);
AuthorizationExtension.ConfigureAuthorization(builder.Services, builder.Configuration);
HttpClientExtension.ConfigureHttpClients(builder.Services);
// 增加 OpenTelemetryExtension
OpenTelemetryExtension.ConfigureOpenTelemetry(builder.Logging, builder.Services, builder.Configuration);

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

// 套用 Middlewares
app.ConfigureMiddlewares();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(config =>
    {
        string? path = app.Configuration.GetValue<string>("Swagger:RoutePrefix");
        if (!string.IsNullOrEmpty(path))
        {
            config.PreSerializeFilters.Add((swaggerDoc, httpRequest) =>
            {
                string httpScheme = (app.Environment.IsDevelopment()) ? httpRequest.Scheme : "https";
                swaggerDoc.Servers = new List<OpenApiServer> {
                    new OpenApiServer { Url = $"{httpScheme}://{httpRequest.Host.Value}{path}" }
                };
            });
        }
    });
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseRouting();

// 設定 OpenTelemetry 輸出 Prometheus 指標的路徑
app.UseOpenTelemetryPrometheusScrapingEndpoint(context =>
    context.Request.Path == "/metrics");

app.UseCors(CorsHandlerExtension.CorsPolicyName);

app.MapControllers();

app.Run();
