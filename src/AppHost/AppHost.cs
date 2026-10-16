// The Aspire app host describes the whole system for local development.
// "dotnet run --project src/AppHost" starts the containers (PostgreSQL, ...) in Docker,
// starts every service, connects them with connection strings, and opens a dashboard
// with logs, traces and metrics. Kubernetes (Part 13) does the same job in production.

var builder = DistributedApplication.CreateBuilder(args);

// ---------- Infrastructure (containers) ----------

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent); // keep the container between runs: faster start, data survives

var ordersDb = postgres.AddDatabase("ordersdb");

// ---------- Services ----------

builder.AddProject<Projects.Orders_API>("orders-api")
    .WithReference(ordersDb)
    .WaitFor(ordersDb)
    .WithHttpHealthCheck("/health/ready");

await builder.Build().RunAsync();
