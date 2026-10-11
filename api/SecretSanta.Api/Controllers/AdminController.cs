using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretSanta.Api.Auth;
using SecretSanta.Api.Data;

namespace SecretSanta.Api.Controllers;

[ApiController]
[Route("api/admin")]
[AdminOnly]
public class AdminController(AppDbContext db) : ControllerBase
{
    // Lets the admin page check a password before storing it.
    [HttpPost("login")]
    public IActionResult Login() => NoContent();

    [HttpGet("exchanges")]
    public async Task<List<ExchangeSummary>> ListExchanges() =>
        await db.Exchanges
            .OrderBy(e => e.Date)
            .Select(e => new ExchangeSummary(e.Id, e.Name, e.Date, e.Participants.Count, e.DrawnAt != null))
            .ToListAsync();

    [HttpGet("exchanges/{id:guid}")]
    public async Task<ActionResult<ExchangeDetail>> GetExchange(Guid id) =>
        await LoadDetail(id) is { } detail ? detail : NotFound();

    [HttpPost("exchanges")]
    public async Task<ActionResult<ExchangeDetail>> CreateExchange(CreateExchangeRequest req)
    {
        if (Validate(req.Name, req.Description) is { } error)
            return error;

        var names = (req.Participants ?? []).Select(n => n.Trim()).Where(n => n.Length > 0).ToList();
        if (names.Any(n => n.Length > 100))
            return Error("Participant names must be 100 characters or fewer.");
        if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Count)
            return Error("Participant names must be unique.");

        var exchange = new Exchange
        {
            Name = req.Name.Trim(),
            Date = req.Date,
            Description = req.Description?.Trim() ?? "",
            CreatedAt = DateTimeOffset.UtcNow,
            Participants = names
                .Select(n => new Participant { Name = n, InviteToken = Draw.NewInviteToken() })
                .ToList(),
        };
        db.Exchanges.Add(exchange);
        await db.SaveChangesAsync();

        return Created($"/api/admin/exchanges/{exchange.Id}", await LoadDetail(exchange.Id));
    }

    [HttpPut("exchanges/{id:guid}")]
    public async Task<ActionResult<ExchangeDetail>> UpdateExchange(Guid id, ExchangeInput req)
    {
        if (Validate(req.Name, req.Description) is { } error)
            return error;

        var exchange = await db.Exchanges.FindAsync(id);
        if (exchange is null)
            return NotFound();

        exchange.Name = req.Name.Trim();
        exchange.Date = req.Date;
        exchange.Description = req.Description?.Trim() ?? "";
        await db.SaveChangesAsync();

        return (await LoadDetail(id))!;
    }

    [HttpDelete("exchanges/{id:guid}")]
    public async Task<IActionResult> DeleteExchange(Guid id)
    {
        var exchange = await db.Exchanges.Include(e => e.Participants).FirstOrDefaultAsync(e => e.Id == id);
        if (exchange is null)
            return NotFound();

        // Clear recipient links first so the self-referencing foreign key doesn't block the delete.
        foreach (var p in exchange.Participants)
            p.RecipientId = null;
        await db.SaveChangesAsync();

        db.Exchanges.Remove(exchange);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("exchanges/{id:guid}/participants")]
    public async Task<ActionResult<ExchangeDetail>> AddParticipant(Guid id, AddParticipantRequest req)
    {
        var exchange = await db.Exchanges.Include(e => e.Participants).FirstOrDefaultAsync(e => e.Id == id);
        if (exchange is null)
            return NotFound();
        if (exchange.DrawnAt is not null)
            return Error("Names have already been drawn. Reset the draw to change participants.", 409);

        var name = req.Name?.Trim() ?? "";
        if (name.Length is 0 or > 100)
            return Error("Name must be between 1 and 100 characters.");
        if (exchange.Participants.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
            return Error($"{name} is already in this exchange.");

        db.Participants.Add(new Participant { ExchangeId = id, Name = name, InviteToken = Draw.NewInviteToken() });
        await db.SaveChangesAsync();
        return (await LoadDetail(id))!;
    }

    [HttpDelete("exchanges/{id:guid}/participants/{participantId:guid}")]
    public async Task<ActionResult<ExchangeDetail>> RemoveParticipant(Guid id, Guid participantId)
    {
        var exchange = await db.Exchanges.FindAsync(id);
        if (exchange is null)
            return NotFound();
        if (exchange.DrawnAt is not null)
            return Error("Names have already been drawn. Reset the draw to change participants.", 409);

        var participant = await db.Participants.FirstOrDefaultAsync(p => p.Id == participantId && p.ExchangeId == id);
        if (participant is null)
            return NotFound();

        db.Participants.Remove(participant);
        await db.SaveChangesAsync();
        return (await LoadDetail(id))!;
    }

    [HttpPost("exchanges/{id:guid}/draw")]
    public async Task<ActionResult<ExchangeDetail>> DrawNames(Guid id)
    {
        var exchange = await db.Exchanges.Include(e => e.Participants).FirstOrDefaultAsync(e => e.Id == id);
        if (exchange is null)
            return NotFound();
        if (exchange.DrawnAt is not null)
            return Error("Names have already been drawn.", 409);
        if (exchange.Participants.Count < 2)
            return Error("Add at least two participants before drawing names.");

        Draw.Assign(exchange.Participants);
        exchange.DrawnAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return (await LoadDetail(id))!;
    }

    // Clears all assignments so participants can be changed and names drawn again.
    [HttpPost("exchanges/{id:guid}/reset")]
    public async Task<ActionResult<ExchangeDetail>> ResetDraw(Guid id)
    {
        var exchange = await db.Exchanges.Include(e => e.Participants).FirstOrDefaultAsync(e => e.Id == id);
        if (exchange is null)
            return NotFound();

        foreach (var p in exchange.Participants)
        {
            p.RecipientId = null;
            p.FirstViewedAt = null;
        }
        exchange.DrawnAt = null;
        await db.SaveChangesAsync();
        return (await LoadDetail(id))!;
    }

    private async Task<ExchangeDetail?> LoadDetail(Guid id) =>
        await db.Exchanges
            .Where(e => e.Id == id)
            .Select(e => new ExchangeDetail(
                e.Id, e.Name, e.Date, e.Description, e.DrawnAt,
                e.Participants
                    .OrderBy(p => p.Name)
                    .Select(p => new ParticipantView(p.Id, p.Name, p.InviteToken, p.FirstViewedAt != null))
                    .ToList()))
            .FirstOrDefaultAsync();

    private static ObjectResult? Validate(string? name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200)
            return Error("Name must be between 1 and 200 characters.");
        if (description?.Length > 4000)
            return Error("Description must be 4000 characters or fewer.");
        return null;
    }

    private static ObjectResult Error(string detail, int status = StatusCodes.Status400BadRequest) =>
        new(new ProblemDetails { Status = status, Detail = detail }) { StatusCode = status };
}
