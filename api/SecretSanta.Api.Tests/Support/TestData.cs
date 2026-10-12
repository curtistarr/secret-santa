using SecretSanta.Api.Data;

namespace SecretSanta.Api.Tests.Support;

public static class TestData
{
    public static Exchange Exchange(string name = "Family Christmas", DateTimeOffset? drawnAt = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Date = new DateOnly(2026, 12, 25),
        Description = "£30 budget",
        CreatedAt = FixedTimeProvider.DefaultNow,
        DrawnAt = drawnAt,
    };

    public static Participant Participant(Guid exchangeId, string name) => new()
    {
        Id = Guid.NewGuid(),
        ExchangeId = exchangeId,
        Name = name,
        InviteToken = $"token-{name.ToLowerInvariant()}-{Guid.NewGuid():N}",
    };

    public static List<Participant> Participants(Guid exchangeId, params string[] names) =>
        names.Select(n => Participant(exchangeId, n)).ToList();
}
