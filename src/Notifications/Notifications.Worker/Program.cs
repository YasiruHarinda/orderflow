using BuildingBlocks.Messaging;
using Notifications.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddMessaging("notifications", typeof(Program).Assembly);

builder.Services.AddSingleton<INotificationSender, LoggingNotificationSender>();

var app = builder.Build();

app.MapDefaultEndpoints();

await app.RunAsync();
