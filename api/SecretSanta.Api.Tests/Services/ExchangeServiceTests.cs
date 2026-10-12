using NSubstitute;
using SecretSanta.Api.Contracts;
using SecretSanta.Api.Data;
using SecretSanta.Api.Services;
using SecretSanta.Api.Stores;
using SecretSanta.Api.Tests.Support;

namespace SecretSanta.Api.Tests.Services;

public class ExchangeServiceTests
{
    private IExchangeStore _exchanges = null!;
    private IParticipantStore _participants = null!;
    private ExchangeService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _exchanges = Substitute.For<IExchangeStore>();
        _participants = Substitute.For<IParticipantStore>();
        _service = new ExchangeService(_exchanges, _participants, new FixedTimeProvider());
    }

    /// <summary>Makes the mocked stores return the given exchange and its participants.</summary>
    private (Exchange Exchange, List<Participant> People) Given(DateTimeOffset? drawnAt = null, params string[] names)
    {
        var exchange = TestData.Exchange(drawnAt: drawnAt);
        var people = TestData.Participants(exchange.Id, names);
        _exchanges.GetByIdAsync(exchange.Id).Returns(exchange);
        _participants.GetByExchangeIdAsync(exchange.Id).Returns(_ => people.ToList());
        foreach (var p in people)
            _participants.GetByIdAsync(p.Id).Returns(p);
        return (exchange, people);
    }

    [Test]
    public async Task ListAsync_ReturnsSummariesWithCounts()
    {
        var open = TestData.Exchange("Open");
        var drawn = TestData.Exchange("Drawn", drawnAt: FixedTimeProvider.DefaultNow);
        _exchanges.GetAllAsync().Returns([open, drawn]);
        _participants.CountByExchangeIdAsync(open.Id).Returns(3);
        _participants.CountByExchangeIdAsync(drawn.Id).Returns(5);

        var list = await _service.ListAsync();

        Assert.That(list, Is.EqualTo(new[]
        {
            new ExchangeSummary(open.Id, "Open", open.Date, 3, false),
            new ExchangeSummary(drawn.Id, "Drawn", drawn.Date, 5, true),
        }));
    }

    [Test]
    public async Task GetAsync_UnknownExchange_IsNotFound()
    {
        var result = await _service.GetAsync(Guid.NewGuid());

        Assert.That(result.Status, Is.EqualTo(ServiceStatus.NotFound));
    }

    [Test]
    public async Task GetAsync_ReturnsDetailWithParticipantsSortedByName()
    {
        var (exchange, people) = Given(null, "Cara", "alice", "Bob");
        people[0].FirstViewedAt = FixedTimeProvider.DefaultNow;

        var result = await _service.GetAsync(exchange.Id);

        Assert.That(result.Status, Is.EqualTo(ServiceStatus.Ok));
        Assert.That(result.Value!.Name, Is.EqualTo(exchange.Name));
        Assert.That(result.Value.Participants.Select(p => p.Name), Is.EqualTo(new[] { "alice", "Bob", "Cara" }));
        Assert.That(result.Value.Participants.Single(p => p.Name == "Cara").HasViewed, Is.True);
    }

    [Test]
    public async Task CreateAsync_SavesTrimmedExchangeAndParticipants()
    {
        Exchange? saved = null;
        List<Participant>? savedPeople = null;
        await _exchanges.AddAsync(Arg.Do<Exchange>(e => saved = e));
        await _participants.AddRangeAsync(Arg.Do<IEnumerable<Participant>>(p => savedPeople = p.ToList()));

        var result = await _service.CreateAsync(new CreateExchangeRequest(
            "  Family Christmas  ", new DateOnly(2026, 12, 25), " £30 budget ", ["Alice", " Bob ", "", "Cara"]));

        Assert.That(result.Status, Is.EqualTo(ServiceStatus.Ok));
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.Name, Is.EqualTo("Family Christmas"));
        Assert.That(saved.Description, Is.EqualTo("£30 budget"));
        Assert.That(saved.CreatedAt, Is.EqualTo(FixedTimeProvider.DefaultNow));
        Assert.That(savedPeople!.Select(p => p.Name), Is.EqualTo(new[] { "Alice", "Bob", "Cara" }));
        Assert.That(savedPeople, Has.All.Matches<Participant>(p => p.ExchangeId == saved.Id && p.RecipientId == null));
        Assert.That(savedPeople.Select(p => p.InviteToken), Is.Unique);
    }

    [TestCase("", "Name must be between 1 and 200 characters.")]
    [TestCase("   ", "Name must be between 1 and 200 characters.")]
    public async Task CreateAsync_WithoutName_IsInvalid(string name, string error)
    {
        var result = await _service.CreateAsync(new CreateExchangeRequest(name, new DateOnly(2026, 12, 25), null, []));

        Assert.That(result.Status, Is.EqualTo(ServiceStatus.Invalid));
        Assert.That(result.Error, Is.EqualTo(error));
        await _exchanges.DidNotReceiveWithAnyArgs().AddAsync(default!);
    }

    [Test]
    public async Task CreateAsync_WithDuplicateNames_IsInvalid()
    {
        var result = await _service.CreateAsync(
            new CreateExchangeRequest("Xmas", new DateOnly(2026, 12, 25), null, ["Alice", "alice"]));

        Assert.That(result.Status, Is.EqualTo(ServiceStatus.Invalid));
        await _exchanges.DidNotReceiveWithAnyArgs().AddAsync(default!);
    }

    [Test]
    public async Task CreateAsync_WithLongDescription_IsInvalid()
    {
        var result = await _service.CreateAsync(
            new CreateExchangeRequest("Xmas", new DateOnly(2026, 12, 25), new string('x', 4001), []));

        Assert.That(result.Status, Is.EqualTo(ServiceStatus.Invalid));
    }

    [Test]
    public async Task UpdateAsync_ChangesDetails()
    {
        var (exchange, _) = Given(null, "Alice", "Bob");

        var result = await _service.UpdateAsync(exchange.Id, new ExchangeInput(" New name ", new DateOnly(2026, 12, 24), "New description"));

        Assert.That(result.Status, Is.EqualTo(ServiceStatus.Ok));
        Assert.That(exchange.Name, Is.EqualTo("New name"));
        Assert.That(exchange.Date, Is.EqualTo(new DateOnly(2026, 12, 24)));
        await _exchanges.Received(1).UpdateAsync(exchange);
    }

    [Test]
    public async Task UpdateAsync_UnknownExchange_IsNotFound()
    {
        var result = await _service.UpdateAsync(Guid.NewGuid(), new ExchangeInput("Name", new DateOnly(2026, 12, 24), null));

        Assert.That(result.Status, Is.EqualTo(ServiceStatus.NotFound));
    }

    [Test]
    public async Task DeleteAsync_ClearsAssignmentsThenDeletesParticipantsAndExchange()
    {
        var (exchange, people) = Given(FixedTimeProvider.DefaultNow, "Alice", "Bob");
        people[0].RecipientId = people[1].Id;
        people[1].RecipientId = people[0].Id;

        var result = await _service.DeleteAsync(exchange.Id);

        Assert.That(result.Status, Is.EqualTo(ServiceStatus.Ok));
        Assert.That(people, Has.All.Matches<Participant>(p => p.RecipientId == null));
        Received.InOrder(() =>
        {
            _participants.UpdateRangeAsync(Arg.Any<IEnumerable<Participant>>());
            _participants.DeleteRangeAsync(Arg.Any<IEnumerable<Participant>>());
            _exchanges.DeleteAsync(exchange);
        });
    }

    [Test]
    public async Task DeleteAsync_UnknownExchange_IsNotFound()
    {
        var result = await _service.DeleteAsync(Guid.NewGuid());

        Assert.That(result.Status, Is.EqualTo(ServiceStatus.NotFound));
        await _exchanges.DidNotReceiveWithAnyArgs().DeleteAsync(default!);
    }

    [Test]
    public async Task AddParticipantAsync_AddsTrimmedName()
    {
        var (exchange, _) = Given(null, "Alice");

        var result = await _service.AddParticipantAsync(exchange.Id, " Bob ");

        Assert.That(result.Status, Is.EqualTo(ServiceStatus.Ok));
        await _participants.Received(1).AddAsync(Arg.Is<Participant>(p => p.Name == "Bob" && p.ExchangeId == exchange.Id));
    }

    [Test]
    public async Task AddParticipantAsync_DuplicateName_IsInvalid()
    {
        var (exchange, _) = Given(null, "Alice");

        var result = await _service.AddParticipantAsync(exchange.Id, "ALICE");

        Assert.That(result.Status, Is.EqualTo(ServiceStatus.Invalid));
        await _participants.DidNotReceiveWithAnyArgs().AddAsync(default!);
    }

    [Test]
    public async Task AddParticipantAsync_AfterDraw_IsConflict()
    {
        var (exchange, _) = Given(FixedTimeProvider.DefaultNow, "Alice", "Bob");

        var result = await _service.AddParticipantAsync(exchange.Id, "Cara");

        Assert.That(result.Status, Is.EqualTo(ServiceStatus.Conflict));
    }

    [Test]
    public async Task RemoveParticipantAsync_DeletesParticipant()
    {
        var (exchange, people) = Given(null, "Alice", "Bob");

        var result = await _service.RemoveParticipantAsync(exchange.Id, people[1].Id);

        Assert.That(result.Status, Is.EqualTo(ServiceStatus.Ok));
        await _participants.Received(1).DeleteAsync(people[1]);
    }

    [Test]
    public async Task RemoveParticipantAsync_FromAnotherExchange_IsNotFound()
    {
        var (exchange, _) = Given(null, "Alice");
        var (_, otherPeople) = Given(null, "Zed");

        var result = await _service.RemoveParticipantAsync(exchange.Id, otherPeople[0].Id);

        Assert.That(result.Status, Is.EqualTo(ServiceStatus.NotFound));
        await _participants.DidNotReceiveWithAnyArgs().DeleteAsync(default!);
    }

    [Test]
    public async Task RemoveParticipantAsync_AfterDraw_IsConflict()
    {
        var (exchange, people) = Given(FixedTimeProvider.DefaultNow, "Alice", "Bob");

        var result = await _service.RemoveParticipantAsync(exchange.Id, people[0].Id);

        Assert.That(result.Status, Is.EqualTo(ServiceStatus.Conflict));
    }

    [Test]
    public async Task DrawAsync_AssignsEveryoneAndMarksExchangeDrawn()
    {
        var (exchange, people) = Given(null, "Alice", "Bob", "Cara", "Dan");

        var result = await _service.DrawAsync(exchange.Id);

        Assert.That(result.Status, Is.EqualTo(ServiceStatus.Ok));
        Assert.That(result.Value!.DrawnAt, Is.EqualTo(FixedTimeProvider.DefaultNow));
        Assert.That(people, Has.All.Matches<Participant>(p => p.RecipientId != null && p.RecipientId != p.Id));
        Assert.That(people.Select(p => p.RecipientId), Is.Unique);
        await _participants.Received(1).UpdateRangeAsync(Arg.Any<IEnumerable<Participant>>());
        await _exchanges.Received(1).UpdateAsync(exchange);
    }

    [Test]
    public async Task DrawAsync_WithFewerThanTwoPeople_IsInvalid()
    {
        var (exchange, _) = Given(null, "Alice");

        var result = await _service.DrawAsync(exchange.Id);

        Assert.That(result.Status, Is.EqualTo(ServiceStatus.Invalid));
        await _exchanges.DidNotReceiveWithAnyArgs().UpdateAsync(default!);
    }

    [Test]
    public async Task DrawAsync_AlreadyDrawn_IsConflict()
    {
        var (exchange, _) = Given(FixedTimeProvider.DefaultNow, "Alice", "Bob");

        var result = await _service.DrawAsync(exchange.Id);

        Assert.That(result.Status, Is.EqualTo(ServiceStatus.Conflict));
    }

    [Test]
    public async Task ResetDrawAsync_ClearsAssignmentsAndViews()
    {
        var (exchange, people) = Given(FixedTimeProvider.DefaultNow, "Alice", "Bob");
        people[0].RecipientId = people[1].Id;
        people[0].FirstViewedAt = FixedTimeProvider.DefaultNow;
        people[1].RecipientId = people[0].Id;

        var result = await _service.ResetDrawAsync(exchange.Id);

        Assert.That(result.Value!.DrawnAt, Is.Null);
        Assert.That(result.Value.Participants, Has.All.Matches<ParticipantView>(p => !p.HasViewed));
        Assert.That(people, Has.All.Matches<Participant>(p => p.RecipientId == null && p.FirstViewedAt == null));
        await _participants.Received(1).UpdateRangeAsync(Arg.Any<IEnumerable<Participant>>());
        await _exchanges.Received(1).UpdateAsync(exchange);
    }
}
