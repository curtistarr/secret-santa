using Microsoft.EntityFrameworkCore;
using SecretSanta.Api.Data;

namespace SecretSanta.Api.Endpoints;

public record InviteView(
    string ExchangeName, DateOnly Date, string Description, int ParticipantCount,
    string YourName, string? RecipientName);

public static class InviteEndpoints
{
    public static void MapInviteEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/invite/{token}", async (string token, AppDbContext db) =>
        {
            var participant = await db.Participants
                .Include(p => p.Exchange).ThenInclude(e => e.Participants)
                .Include(p => p.Recipient)
                .FirstOrDefaultAsync(p => p.InviteToken == token);
            if (participant is null)
                return Results.NotFound();

            if (participant.Recipient is not null && participant.FirstViewedAt is null)
            {
                participant.FirstViewedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync();
            }

            var exchange = participant.Exchange;
            return Results.Ok(new InviteView(
                exchange.Name, exchange.Date, exchange.Description, exchange.Participants.Count,
                participant.Name, participant.Recipient?.Name));
        });
    }
}
