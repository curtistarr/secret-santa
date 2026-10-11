using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretSanta.Api.Data;

namespace SecretSanta.Api.Controllers;

[ApiController]
[Route("api/invite")]
public class InviteController(AppDbContext db) : ControllerBase
{
    [HttpGet("{token}")]
    public async Task<ActionResult<InviteView>> GetInvite(string token)
    {
        var participant = await db.Participants
            .Include(p => p.Exchange).ThenInclude(e => e.Participants)
            .Include(p => p.Recipient)
            .FirstOrDefaultAsync(p => p.InviteToken == token);
        if (participant is null)
            return NotFound();

        if (participant.Recipient is not null && participant.FirstViewedAt is null)
        {
            participant.FirstViewedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        var exchange = participant.Exchange;
        return new InviteView(
            exchange.Name, exchange.Date, exchange.Description, exchange.Participants.Count,
            participant.Name, participant.Recipient?.Name);
    }
}
