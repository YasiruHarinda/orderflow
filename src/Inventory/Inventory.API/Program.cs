using Inventory.API;
using Inventory.Application;
using Inventory.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddInventoryApplication();
builder.AddInventoryInfrastructure();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

if (args.Contains("--migrate"))
{
    await app.Services.MigrateInventoryDatabaseAsync();
    return;
}

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await app.Services.MigrateInventoryDatabaseAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapDefaultEndpoints();
app.MapProductEndpoints();

await app.RunAsync();

public partial class Program;
