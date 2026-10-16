using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// One call, AddServiceDefaults(), gives every service the same production basics:
/// OpenTelemetry (logs, metrics, traces), health checks, service discovery and
/// resilient HTTP calls. Based on the .NET Aspire service defaults template.
/// </summary>
public static class Extensions
{
    public const string LivenessPath = "/health/live";
    public const string ReadinessPath = "/health/ready";

    // Our own ActivitySources and Meters are named "OrderFlow.<Service>"; the wildcard picks them all up.
    private const string OrderFlowTelemetry = "OrderFlow.*";
    private const string MassTransitTelemetry = "MassTransit";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Retries, timeouts and a circuit breaker on every outgoing HTTP call.
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(OrderFlowTelemetry, MassTransitTelemetry))
            .WithTracing(tracing => tracing
                .AddSource(builder.Environment.ApplicationName, OrderFlowTelemetry, MassTransitTelemetry)
                .AddAspNetCoreInstrumentation(options =>
                    // Kubernetes calls the health endpoints every few seconds; tracing them is noise.
                    options.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
                .AddHttpClientInstrumentation());

        // Send telemetry over OTLP when an endpoint is configured: the Aspire dashboard
        // locally, the OpenTelemetry Collector in Kubernetes.
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        // "live" = the process is running and responding. Database and broker checks are
        // added by their integrations and only count towards "ready".
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// Liveness: "should Kubernetes restart me?" Only checks the process itself.
    /// Readiness: "should Kubernetes send me traffic?" Also checks database, broker, cache.
    /// These paths are for the cluster only; the gateway never routes them.
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks(LivenessPath, new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
        });

        app.MapHealthChecks(ReadinessPath);

        return app;
    }
}
