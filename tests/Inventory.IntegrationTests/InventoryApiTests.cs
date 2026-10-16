using System.Net;
using System.Net.Http.Json;
using BuildingBlocks.Application;
using Inventory.Application.Products;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Contracts;

namespace Inventory.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class InventoryApiTests(InventoryApiFactory factory) : IClassFixture<InventoryApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private Task<ProductResponse?> GetProductAsync(string sku) =>
        _client.GetFromJsonAsync<ProductResponse>($"/api/v1/products/{sku}");

    private async Task PublishAsync<T>(T message)
        where T : class
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IBus>().Publish(message);
    }

    private async Task WaitForAvailableAsync(string sku, int expected)
    {
        for (var i = 0; i < 60; i++)
        {
            if ((await GetProductAsync(sku))!.QuantityAvailable == expected)
            {
                return;
            }

            await Task.Delay(250);
        }

        (await GetProductAsync(sku))!.QuantityAvailable.ShouldBe(expected);
    }

    [Fact]
    public async Task Seeded_products_are_listed()
    {
        var page = await _client.GetFromJsonAsync<PagedResult<ProductResponse>>("/api/v1/products?pageSize=50");

        page!.TotalCount.ShouldBe(6);
        page.Items.Single(p => p.Sku == "LT-001").QuantityAvailable.ShouldBe(0);
    }

    [Fact]
    public async Task Unknown_product_returns_404()
    {
        var response = await _client.GetAsync("/api/v1/products/NOPE-999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Updating_stock_is_visible_immediately_despite_the_cache()
    {
        (await GetProductAsync("HS-001"))!.QuantityAvailable.ShouldBe(30); // now cached

        var response = await _client.PutAsJsonAsync("/api/v1/products/HS-001/stock", new { quantityOnHand = 42 });

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await GetProductAsync("HS-001"))!.QuantityAvailable.ShouldBe(42); // cache was invalidated
    }

    [Fact]
    public async Task OrderSubmitted_reserves_stock_and_OrderRejected_releases_it()
    {
        var orderId = Guid.NewGuid();

        await PublishAsync(new OrderSubmitted(orderId, Guid.NewGuid(), [new OrderSubmittedLine("MN-001", 3)], DateTimeOffset.UtcNow));
        await WaitForAvailableAsync("MN-001", 17); // 20 - 3

        await PublishAsync(new OrderRejected(orderId, Guid.NewGuid(), "Customer cancelled", DateTimeOffset.UtcNow));
        await WaitForAvailableAsync("MN-001", 20); // compensation gave the stock back
    }
}
