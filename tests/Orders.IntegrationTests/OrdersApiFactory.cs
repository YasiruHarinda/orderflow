using Microsoft.AspNetCore.Hosting;
using MassTransit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Orders.IntegrationTests;

/// <summary>
/// Starts the REAL Orders API in memory, connected to REAL PostgreSQL and RabbitMQ running
/// in throwaway Docker containers. Nothing is mocked, so these tests prove that EF mappings,
/// migrations, the outbox and the message consumers actually work together.
/// One instance is shared by all tests in a class (IClassFixture).
/// </summary>
public sealed class OrdersApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Short, so the timeout test does not wait two minutes; long enough that a slow
    // CI machine still confirms orders before the timeout fires.
    public static readonly TimeSpan StuckOrderTimeout = TimeSpan.FromSeconds(8);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:4.1-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync());
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ordersdb", _postgres.GetConnectionString());
        builder.UseSetting("ConnectionStrings:rabbitmq", _rabbitMq.GetConnectionString());
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Orders:StuckOrders:Timeout", StuckOrderTimeout.ToString());
        builder.UseSetting("Orders:StuckOrders:CheckInterval", "00:00:01");

        // Start the host only once the bus has created its queues and bindings. Otherwise a
        // test can publish before the consumer's queue exists, and RabbitMQ drops the message.
        builder.ConfigureServices(services => services.Configure<MassTransitHostOptions>(options =>
        {
            options.WaitUntilStarted = true;
            options.StartTimeout = TimeSpan.FromSeconds(60);
        }));
    }
}
