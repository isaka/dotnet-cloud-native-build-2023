using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Orders;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IOrderService, FakeOrderService>();

builder.Services.AddOpenTelemetry()
    .WithMetrics(builder =>
    {
        builder
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddEventCountersInstrumentation(c => {
                c.AddEventSources(
                    "Microsoft.AspNetCore.Hosting",
                    "Microsoft-AspNetCore-Server-Kestrel",
                    "System.Net.Http",
                    "System.Net.Sockets",
                    "System.Net.NameResolution",
                    "System.Net.Security");
            })
            .AddView("request-duration", new ExplicitBucketHistogramConfiguration
                {
                    Boundaries = new double[] { 0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 }
                })
            .AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel")
            .AddPrometheusExporter();
    })
    .WithTracing(tracing => {
        tracing
            .AddAspNetCoreInstrumentation()
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName: "Orders", serviceVersion: "1.0"));

        tracing.AddZipkinExporter(zipkin => {
            zipkin.Endpoint = new Uri("http://zipkin:9411/api/v2/spans");
        });
    });

var app = builder.Build();

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.MapGet("/orders", async (IOrderService service) =>
    await service.GetOrdersAsync()
);

app.MapPost("/orders", async (Order order, IOrderService service) =>
    await service.SaveOrderAsync(order)
);

app.Run();

public record CartItem(string ProductId, int Quantity = 1);

public record Order(CartItem[] Cart, DateTime OrderedAt, Guid OrderId);
