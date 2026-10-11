namespace SecretSanta.Api.Data;

public class Exchange
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DateOnly Date { get; set; }
    public string Description { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DrawnAt { get; set; }

    public List<Participant> Participants { get; set; } = [];
}

public class Participant
{
    public Guid Id { get; set; }
    public Guid ExchangeId { get; set; }
    public Exchange Exchange { get; set; } = null!;
    public required string Name { get; set; }

    /// <summary>Secret, unguessable token used in the participant's personal invite link.</summary>
    public required string InviteToken { get; set; }

    /// <summary>Who this participant buys a gift for. Never exposed through the admin API.</summary>
    public Guid? RecipientId { get; set; }
    public Participant? Recipient { get; set; }

    public DateTimeOffset? FirstViewedAt { get; set; }
}
