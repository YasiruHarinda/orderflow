using Inventory.Application.Abstractions;
using Inventory.Application.Reservations;
using Inventory.Domain;

namespace Inventory.UnitTests;

public class ReservationHandlerTests
{
    private readonly FakeProducts _products = new();
    private readonly FakeReservations _reservations = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeCache _cache = new();

    public ReservationHandlerTests()
    {
        _products.Add("KB-001", onHand: 10);
        _products.Add("MS-001", onHand: 1);
    }

    private ReserveStockCommandHandler Reserve() => new(_products, _reservations, _unitOfWork, _cache, TimeProvider.System);

    private ReleaseStockCommandHandler Release() => new(_products, _reservations, _unitOfWork, _cache, TimeProvider.System);

    [Fact]
    public async Task Reserves_every_line_when_all_stock_is_available()
    {
        var orderId = Guid.NewGuid();

        var result = await Reserve().Handle(
            new ReserveStockCommand(orderId, [new("KB-001", 2), new("MS-001", 1)]), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _products["KB-001"].QuantityReserved.ShouldBe(2);
        _products["MS-001"].QuantityReserved.ShouldBe(1);
        _reservations.Items.ShouldHaveSingleItem().OrderId.ShouldBe(orderId);
        _cache.InvalidateCount.ShouldBe(1);
    }

    [Fact]
    public async Task Reserves_nothing_when_one_line_is_short()
    {
        // KB-001 is fine but MS-001 only has 1: the whole order must fail, KB-001 untouched.
        var result = await Reserve().Handle(
            new ReserveStockCommand(Guid.NewGuid(), [new("KB-001", 2), new("MS-001", 5)]), CancellationToken.None);

        result.Error.Code.ShouldBe("Inventory.InsufficientStock");
        _products["KB-001"].QuantityReserved.ShouldBe(0);
        _reservations.Items.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Same_message_twice_reserves_only_once()
    {
        var command = new ReserveStockCommand(Guid.NewGuid(), [new("KB-001", 3)]);

        await Reserve().Handle(command, CancellationToken.None);
        var second = await Reserve().Handle(command, CancellationToken.None);

        second.IsSuccess.ShouldBeTrue();
        _products["KB-001"].QuantityReserved.ShouldBe(3);
    }

    [Fact]
    public async Task Release_gives_stock_back()
    {
        var orderId = Guid.NewGuid();
        await Reserve().Handle(new ReserveStockCommand(orderId, [new("KB-001", 3)]), CancellationToken.None);

        await Release().Handle(new ReleaseStockCommand(orderId), CancellationToken.None);

        _products["KB-001"].QuantityReserved.ShouldBe(0);
        _reservations.Items.Single().Status.ShouldBe(ReservationStatus.Released);
    }

    [Fact]
    public async Task Release_arriving_before_reserve_blocks_the_late_reservation()
    {
        var orderId = Guid.NewGuid();

        await Release().Handle(new ReleaseStockCommand(orderId), CancellationToken.None);
        var late = await Reserve().Handle(new ReserveStockCommand(orderId, [new("KB-001", 1)]), CancellationToken.None);

        late.Error.Code.ShouldBe("Inventory.OrderCancelled");
        _products["KB-001"].QuantityReserved.ShouldBe(0);
    }

    // ---------- Fakes ----------

    private sealed class FakeProducts : IProductRepository
    {
        private readonly Dictionary<string, Product> _bySku = [];

        public Product this[string sku] => _bySku[sku];

        public void Add(string sku, int onHand) => _bySku[sku] = Product.Create(sku, sku, 10m, "USD", onHand).Value;

        public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken) =>
            Task.FromResult(_bySku.GetValueOrDefault(sku));

        public Task<IReadOnlyDictionary<string, Product>> GetBySkusAsync(IReadOnlyCollection<string> skus, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, Product>>(
                _bySku.Where(p => skus.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value));
    }

    private sealed class FakeReservations : IStockReservationRepository
    {
        public List<StockReservation> Items { get; } = [];

        public Task<StockReservation?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken) =>
            Task.FromResult(Items.SingleOrDefault(r => r.OrderId == orderId));

        public void Add(StockReservation reservation) => Items.Add(reservation);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCache : IProductCache
    {
        public int InvalidateCount { get; private set; }

        public Task InvalidateAsync(CancellationToken cancellationToken)
        {
            InvalidateCount++;
            return Task.CompletedTask;
        }
    }
}
