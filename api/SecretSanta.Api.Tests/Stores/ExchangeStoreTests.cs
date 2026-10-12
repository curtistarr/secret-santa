using SecretSanta.Api.Data;
using SecretSanta.Api.Stores;
using SecretSanta.Api.Tests.Support;

namespace SecretSanta.Api.Tests.Stores;

public class ExchangeStoreTests
{
    private AppDbContext _db = null!;
    private ExchangeStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        _db = TestDb.Create();
        _store = new ExchangeStore(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsExchange()
    {
        var exchange = TestData.Exchange();

        await _store.AddAsync(exchange);

        Assert.That(await _store.GetByIdAsync(exchange.Id), Is.SameAs(exchange));
    }

    [Test]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        Assert.That(await _store.GetByIdAsync(Guid.NewGuid()), Is.Null);
    }

    [Test]
    public async Task GetAllAsync_OrdersByDate()
    {
        var later = TestData.Exchange("Later");
        later.Date = new DateOnly(2027, 1, 1);
        var sooner = TestData.Exchange("Sooner");
        sooner.Date = new DateOnly(2026, 12, 1);
        await _store.AddAsync(later);
        await _store.AddAsync(sooner);

        var all = await _store.GetAllAsync();

        Assert.That(all.Select(e => e.Name), Is.EqualTo(new[] { "Sooner", "Later" }));
    }

    [Test]
    public async Task UpdateAsync_SavesChanges()
    {
        var exchange = TestData.Exchange();
        await _store.AddAsync(exchange);

        exchange.Name = "Renamed";
        await _store.UpdateAsync(exchange);

        _db.ChangeTracker.Clear();
        Assert.That((await _store.GetByIdAsync(exchange.Id))!.Name, Is.EqualTo("Renamed"));
    }

    [Test]
    public async Task DeleteAsync_RemovesExchange()
    {
        var exchange = TestData.Exchange();
        await _store.AddAsync(exchange);

        await _store.DeleteAsync(exchange);

        Assert.That(await _store.GetByIdAsync(exchange.Id), Is.Null);
    }
}
