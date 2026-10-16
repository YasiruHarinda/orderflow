using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// YARP reads its routes from configuration (appsettings.json, "ReverseProxy").
// Destinations like "http://orders-api" are resolved by service discovery: Aspire
// provides the real address locally; in Kubernetes the name is the Service's DNS name.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

// Rate limiting: each client IP may send PermitLimit requests per Window. More gets
// "429 Too Many Requests". This protects the services from abuse and runaway clients.
var rateLimit = builder.Configuration.GetSection("RateLimiting");
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("per-client", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = rateLimit.GetValue("PermitLimit", 100),
            Window = TimeSpan.FromSeconds(rateLimit.GetValue("WindowSeconds", 10)),
            QueueLimit = 0,
        }));
});

// In Kubernetes the gateway sits behind the ingress controller. These headers carry the
// real client IP, so rate limiting counts per client instead of per ingress pod.
// Any proxy is trusted because only the ingress can reach this pod (NetworkPolicy, Part 13).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseRateLimiter();

app.MapDefaultEndpoints();
app.MapReverseProxy();

await app.RunAsync();
