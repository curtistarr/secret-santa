using SecretSanta.Api.Data;
using SecretSanta.Api.Stores;
using SecretSanta.Api.Tests.Support;

namespace SecretSanta.Api.Tests.Stores;

public class ParticipantStoreTests
{
    private AppDbContext _db = null!;
    private ParticipantStore _store = null!;
    private Exchange _exchange = null!;
    private Exchange _otherExchange = null!;

    [SetUp]
    public async Task SetUp()
    {
        _db = TestDb.Create();
        _store = new ParticipantStore(_db);
        _exchange = TestData.Exchange();
        _otherExchange = TestData.Exchange("Other");
        _db.Exchanges.AddRange(_exchange, _otherExchange);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task GetByExchangeIdAsync_ReturnsOnlyThatExchangeOrderedByName()
    {
        await _store.AddRangeAsync(TestData.Participants(_exchange.Id, "Cara", "Alice", "Bob"));
        await _store.AddAsync(TestData.Participant(_otherExchange.Id, "Zed"));

        var people = await _store.GetByExchangeIdAsync(_exchange.Id);

        Assert.That(people.Select(p => p.Name), Is.EqualTo(new[] { "Alice", "Bob", "Cara" }));
    }

    [Test]
    public async Task CountByExchangeIdAsync_CountsOnlyThatExchange()
    {
        await _store.AddRangeAsync(TestData.Participants(_exchange.Id, "Alice", "Bob"));
        await _store.AddAsync(TestData.Participant(_otherExchange.Id, "Zed"));

        Assert.That(await _store.CountByExchangeIdAsync(_exchange.Id), Is.EqualTo(2));
    }

    [Test]
    public async Task GetByIdAsync_And_GetByInviteTokenAsync_FindParticipant()
    {
        var alice = TestData.Participant(_exchange.Id, "Alice");
        await _store.AddAsync(alice);

        Assert.That(await _store.GetByIdAsync(alice.Id), Is.SameAs(alice));
        Assert.That(await _store.GetByInviteTokenAsync(alice.InviteToken), Is.SameAs(alice));
        Assert.That(await _store.GetByInviteTokenAsync("nope"), Is.Null);
    }

    [Test]
    public async Task UpdateRangeAsync_SavesChanges()
    {
        var people = TestData.Participants(_exchange.Id, "Alice", "Bob");
        await _store.AddRangeAsync(people);

        people[0].RecipientId = people[1].Id;
        people[1].RecipientId = people[0].Id;
        await _store.UpdateRangeAsync(people);

        _db.ChangeTracker.Clear();
        var saved = await _store.GetByExchangeIdAsync(_exchange.Id);
        Assert.That(saved.Select(p => p.RecipientId), Is.EqualTo(new Guid?[] { people[1].Id, people[0].Id }));
    }

    [Test]
    public async Task DeleteAsync_And_DeleteRangeAsync_RemoveParticipants()
    {
        var people = TestData.Participants(_exchange.Id, "Alice", "Bob", "Cara");
        await _store.AddRangeAsync(people);

        await _store.DeleteAsync(people[0]);
        Assert.That(await _store.CountByExchangeIdAsync(_exchange.Id), Is.EqualTo(2));

        await _store.DeleteRangeAsync(people.Skip(1));
        Assert.That(await _store.CountByExchangeIdAsync(_exchange.Id), Is.Zero);
    }
}
