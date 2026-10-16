// The Aspire app host describes the whole system for local development.
// "dotnet run --project src/AppHost" starts the containers (PostgreSQL, Redis, ...) in Docker,
// starts every service, connects them with connection strings, and opens a dashboard
// with logs, traces and metrics. Kubernetes (Part 13) does the same job in production.

var builder = DistributedApplication.CreateBuilder(args);

// ---------- Infrastructure (containers) ----------

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent); // keep the container between runs: faster start, data survives

// Database per service: each service gets its own database and never reads the other's.
var ordersDb = postgres.AddDatabase("ordersdb");
var inventoryDb = postgres.AddDatabase("inventorydb");

var redis = builder.AddRedis("redis");

// The management plugin adds a web UI to see queues and messages (link in the dashboard).
var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent);

// ---------- Services ----------

builder.AddProject<Projects.Orders_API>("orders-api")
    .WithReference(ordersDb)
    .WithReference(rabbitmq)
    .WaitFor(ordersDb)
    .WaitFor(rabbitmq)
    .WithHttpHealthCheck("/health/ready");

builder.AddProject<Projects.Inventory_API>("inventory-api")
    .WithReference(inventoryDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(inventoryDb)
    .WaitFor(redis)
    .WaitFor(rabbitmq)
    .WithHttpHealthCheck("/health/ready");

builder.AddProject<Projects.Notifications_Worker>("notifications-worker")
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq)
    .WithHttpHealthCheck("/health/ready");

await builder.Build().RunAsync();
