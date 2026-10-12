namespace SecretSanta.Api.Contracts;

public record ExchangeInput(string Name, DateOnly Date, string? Description);
public record CreateExchangeRequest(string Name, DateOnly Date, string? Description, List<string>? Participants);
public record AddParticipantRequest(string Name);

public record ExchangeSummary(Guid Id, string Name, DateOnly Date, int ParticipantCount, bool IsDrawn);

// Note: deliberately has no recipient information, so the admin never sees who has who.
public record ParticipantView(Guid Id, string Name, string InviteToken, bool HasViewed);

public record ExchangeDetail(
    Guid Id, string Name, DateOnly Date, string Description, DateTimeOffset? DrawnAt,
    List<ParticipantView> Participants);

public record InviteView(
    string ExchangeName, DateOnly Date, string Description, int ParticipantCount,
    string YourName, string? RecipientName);
