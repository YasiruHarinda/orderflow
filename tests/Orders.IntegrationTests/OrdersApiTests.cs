using System.Net;
using System.Net.Http.Json;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Contracts;
using Orders.Application.GetOrder;

namespace Orders.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class OrdersApiTests(OrdersApiFactory factory) : IClassFixture<OrdersApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static object ValidOrder(Guid? customerId = null) => new
    {
        customerId = customerId ?? Guid.NewGuid(),
        currency = "USD",
        lines = new[] { new { sku = "KB-001", productName = "Mechanical Keyboard", unitPrice = 89.99m, quantity = 2 } },
    };

    private async Task<Guid> PlaceOrderAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/orders", ValidOrder());
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var body = await response.Content.ReadFromJsonAsync<PlaceOrderBody>();
        return body!.OrderId;
    }

    private async Task<OrderResponse> WaitForStatusAsync(Guid orderId, string status, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        OrderResponse? order;
        do
        {
            order = await _client.GetFromJsonAsync<OrderResponse>($"/api/v1/orders/{orderId}");
            if (order!.Status == status)
            {
                return order;
            }

            await Task.Delay(250);
        }
        while (DateTime.UtcNow < deadline);

        throw new ShouldAssertException($"Order {orderId} is '{order.Status}', expected '{status}' within {timeout}.");
    }

    [Fact]
    public async Task Placed_order_is_saved_and_waits_for_stock()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/orders", ValidOrder());

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        response.Headers.Location.ShouldNotBeNull();

        var order = await _client.GetFromJsonAsync<OrderResponse>(response.Headers.Location);
        order!.Status.ShouldBe("AwaitingStock");
        order.Total.ShouldBe(179.98m);
        order.Lines.ShouldHaveSingleItem().Sku.ShouldBe("KB-001");
    }

    [Fact]
    public async Task Invalid_order_returns_400_with_every_field_error()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/orders", new { customerId = Guid.Empty, currency = "usd", lines = Array.Empty<object>() });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Keys.ShouldBe(["CustomerId", "Currency", "Lines"], ignoreOrder: true);
    }

    [Fact]
    public async Task Unknown_order_returns_404_problem()
    {
        var response = await _client.GetAsync($"/api/v1/orders/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Title.ShouldBe("Order.NotFound");
    }

    [Fact]
    public async Task List_returns_only_the_customers_orders()
    {
        var customerId = Guid.NewGuid();
        await _client.PostAsJsonAsync("/api/v1/orders", ValidOrder(customerId));
        await _client.PostAsJsonAsync("/api/v1/orders", ValidOrder(customerId));
        await _client.PostAsJsonAsync("/api/v1/orders", ValidOrder());

        var page = await _client.GetFromJsonAsync<PageBody>($"/api/v1/orders?customerId={customerId}");

        page!.TotalCount.ShouldBe(2);
        page.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task StockReserved_message_confirms_the_order()
    {
        var orderId = await PlaceOrderAsync();

        // Pretend to be Inventory: publish the reply over the real RabbitMQ.
        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IBus>().Publish(new StockReserved(orderId, DateTimeOffset.UtcNow));
        }

        await WaitForStatusAsync(orderId, "Confirmed", TimeSpan.FromSeconds(15));
    }

    [Fact]
    public async Task StockReservationFailed_message_rejects_the_order()
    {
        var orderId = await PlaceOrderAsync();

        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IBus>().Publish(new StockReservationFailed(orderId, "Not enough stock", DateTimeOffset.UtcNow));
        }

        var order = await WaitForStatusAsync(orderId, "Rejected", TimeSpan.FromSeconds(15));
        order.RejectionReason.ShouldBe("Not enough stock");
    }

    [Fact]
    public async Task Order_with_no_reply_is_rejected_by_the_timeout()
    {
        var orderId = await PlaceOrderAsync();

        // Nobody answers. The StuckOrderMonitor must reject it after the (short) timeout.
        var order = await WaitForStatusAsync(orderId, "Rejected", OrdersApiFactory.StuckOrderTimeout + TimeSpan.FromSeconds(15));
        order.RejectionReason!.ShouldContain("Timed out");
    }

    private sealed record PlaceOrderBody(Guid OrderId);

    private sealed record PageBody(List<object> Items, int TotalCount);
}
