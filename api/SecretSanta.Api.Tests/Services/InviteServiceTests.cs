using NSubstitute;
using SecretSanta.Api.Data;
using SecretSanta.Api.Services;
using SecretSanta.Api.Stores;
using SecretSanta.Api.Tests.Support;

namespace SecretSanta.Api.Tests.Services;

public class InviteServiceTests
{
    private IExchangeStore _exchanges = null!;
    private IParticipantStore _participants = null!;
    private InviteService _service = null!;
    private Exchange _exchange = null!;
    private Participant _alice = null!;
    private Participant _bob = null!;

    [SetUp]
    public void SetUp()
    {
        _exchanges = Substitute.For<IExchangeStore>();
        _participants = Substitute.For<IParticipantStore>();
        _service = new InviteService(_exchanges, _participants, new FixedTimeProvider());

        _exchange = TestData.Exchange();
        _alice = TestData.Participant(_exchange.Id, "Alice");
        _bob = TestData.Participant(_exchange.Id, "Bob");
        _exchanges.GetByIdAsync(_exchange.Id).Returns(_exchange);
        _participants.GetByInviteTokenAsync(_alice.InviteToken).Returns(_alice);
        _participants.GetByIdAsync(_bob.Id).Returns(_bob);
        _participants.CountByExchangeIdAsync(_exchange.Id).Returns(3);
    }

    [Test]
    public async Task UnknownToken_IsNotFound()
    {
        var result = await _service.GetInviteAsync("not-a-real-token");

        Assert.That(result.Status, Is.EqualTo(ServiceStatus.NotFound));
    }

    [Test]
    public async Task BeforeDraw_ShowsExchangeWithoutRecipient()
    {
        var result = await _service.GetInviteAsync(_alice.InviteToken);

        Assert.That(result.Status, Is.EqualTo(ServiceStatus.Ok));
        var invite = result.Value!;
        Assert.That(invite.ExchangeName, Is.EqualTo(_exchange.Name));
        Assert.That(invite.Date, Is.EqualTo(_exchange.Date));
        Assert.That(invite.Description, Is.EqualTo(_exchange.Description));
        Assert.That(invite.ParticipantCount, Is.EqualTo(3));
        Assert.That(invite.YourName, Is.EqualTo("Alice"));
        Assert.That(invite.RecipientName, Is.Null);
        Assert.That(_alice.FirstViewedAt, Is.Null);
        await _participants.DidNotReceiveWithAnyArgs().UpdateAsync(default!);
    }

    [Test]
    public async Task AfterDraw_ShowsRecipientAndRecordsFirstView()
    {
        _alice.RecipientId = _bob.Id;

        var result = await _service.GetInviteAsync(_alice.InviteToken);

        Assert.That(result.Value!.RecipientName, Is.EqualTo("Bob"));
        Assert.That(_alice.FirstViewedAt, Is.EqualTo(FixedTimeProvider.DefaultNow));
        await _participants.Received(1).UpdateAsync(_alice);
    }

    [Test]
    public async Task AlreadyViewed_DoesNotUpdateAgain()
    {
        var firstView = FixedTimeProvider.DefaultNow.AddDays(-1);
        _alice.RecipientId = _bob.Id;
        _alice.FirstViewedAt = firstView;

        await _service.GetInviteAsync(_alice.InviteToken);

        Assert.That(_alice.FirstViewedAt, Is.EqualTo(firstView));
        await _participants.DidNotReceiveWithAnyArgs().UpdateAsync(default!);
    }
}
