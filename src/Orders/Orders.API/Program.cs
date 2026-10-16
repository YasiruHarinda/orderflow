using Orders.API;
using Orders.Application;
using Orders.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Telemetry, health checks, resilience: the same defaults in every service.
builder.AddServiceDefaults();

// The API is the "composition root": the one place where all layers are connected.
builder.Services.AddOrdersApplication();
builder.AddOrdersInfrastructure();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

// "dotnet Orders.API.dll --migrate" only updates the database schema and exits.
// Kubernetes runs it as a Job before a new version starts (Part 13).
if (args.Contains("--migrate"))
{
    await app.Services.MigrateOrdersDatabaseAsync();
    return;
}

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await app.Services.MigrateOrdersDatabaseAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapDefaultEndpoints();
app.MapOrderEndpoints();

await app.RunAsync();

// Makes the auto-generated Program class visible to the integration tests.
public partial class Program;
