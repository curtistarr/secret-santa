using Microsoft.EntityFrameworkCore;
using SecretSanta.Api.Data;

namespace SecretSanta.Api.Endpoints;

public record ExchangeInput(string Name, DateOnly Date, string? Description);
public record CreateExchangeRequest(string Name, DateOnly Date, string? Description, List<string>? Participants);
public record AddParticipantRequest(string Name);

public record ExchangeSummary(Guid Id, string Name, DateOnly Date, int ParticipantCount, bool IsDrawn);

// Note: deliberately has no recipient information, so the admin never sees who has who.
public record ParticipantView(Guid Id, string Name, string InviteToken, bool HasViewed);

public record ExchangeDetail(
    Guid Id, string Name, DateOnly Date, string Description, DateTimeOffset? DrawnAt,
    List<ParticipantView> Participants);

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/admin").AddEndpointFilter<AdminAuthFilter>();

        // Lets the admin page check a password before storing it.
        admin.MapPost("/login", () => Results.NoContent());

        admin.MapGet("/exchanges", async (AppDbContext db) =>
            await db.Exchanges
                .OrderBy(e => e.Date)
                .Select(e => new ExchangeSummary(e.Id, e.Name, e.Date, e.Participants.Count, e.DrawnAt != null))
                .ToListAsync());

        admin.MapGet("/exchanges/{id:guid}", async (Guid id, AppDbContext db) =>
            await LoadDetail(db, id) is { } detail ? Results.Ok(detail) : Results.NotFound());

        admin.MapPost("/exchanges", async (CreateExchangeRequest req, AppDbContext db) =>
        {
            if (Validate(req.Name, req.Description) is { } error)
                return error;

            var names = (req.Participants ?? []).Select(n => n.Trim()).Where(n => n.Length > 0).ToList();
            if (names.Any(n => n.Length > 100))
                return Problem("Participant names must be 100 characters or fewer.");
            if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Count)
                return Problem("Participant names must be unique.");

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

            return Results.Created($"/api/admin/exchanges/{exchange.Id}", await LoadDetail(db, exchange.Id));
        });

        admin.MapPut("/exchanges/{id:guid}", async (Guid id, ExchangeInput req, AppDbContext db) =>
        {
            if (Validate(req.Name, req.Description) is { } error)
                return error;

            var exchange = await db.Exchanges.FindAsync(id);
            if (exchange is null)
                return Results.NotFound();

            exchange.Name = req.Name.Trim();
            exchange.Date = req.Date;
            exchange.Description = req.Description?.Trim() ?? "";
            await db.SaveChangesAsync();

            return Results.Ok(await LoadDetail(db, id));
        });

        admin.MapDelete("/exchanges/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var exchange = await db.Exchanges.Include(e => e.Participants).FirstOrDefaultAsync(e => e.Id == id);
            if (exchange is null)
                return Results.NotFound();

            // Clear recipient links first so the self-referencing foreign key doesn't block the delete.
            foreach (var p in exchange.Participants)
                p.RecipientId = null;
            await db.SaveChangesAsync();

            db.Exchanges.Remove(exchange);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        admin.MapPost("/exchanges/{id:guid}/participants", async (Guid id, AddParticipantRequest req, AppDbContext db) =>
        {
            var exchange = await db.Exchanges.Include(e => e.Participants).FirstOrDefaultAsync(e => e.Id == id);
            if (exchange is null)
                return Results.NotFound();
            if (exchange.DrawnAt is not null)
                return Problem("Names have already been drawn. Reset the draw to change participants.", 409);

            var name = req.Name?.Trim() ?? "";
            if (name.Length is 0 or > 100)
                return Problem("Name must be between 1 and 100 characters.");
            if (exchange.Participants.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
                return Problem($"{name} is already in this exchange.");

            db.Participants.Add(new Participant { ExchangeId = id, Name = name, InviteToken = Draw.NewInviteToken() });
            await db.SaveChangesAsync();
            return Results.Ok(await LoadDetail(db, id));
        });

        admin.MapDelete("/exchanges/{id:guid}/participants/{participantId:guid}", async (Guid id, Guid participantId, AppDbContext db) =>
        {
            var exchange = await db.Exchanges.FindAsync(id);
            if (exchange is null)
                return Results.NotFound();
            if (exchange.DrawnAt is not null)
                return Problem("Names have already been drawn. Reset the draw to change participants.", 409);

            var participant = await db.Participants.FirstOrDefaultAsync(p => p.Id == participantId && p.ExchangeId == id);
            if (participant is null)
                return Results.NotFound();

            db.Participants.Remove(participant);
            await db.SaveChangesAsync();
            return Results.Ok(await LoadDetail(db, id));
        });

        admin.MapPost("/exchanges/{id:guid}/draw", async (Guid id, AppDbContext db) =>
        {
            var exchange = await db.Exchanges.Include(e => e.Participants).FirstOrDefaultAsync(e => e.Id == id);
            if (exchange is null)
                return Results.NotFound();
            if (exchange.DrawnAt is not null)
                return Problem("Names have already been drawn.", 409);
            if (exchange.Participants.Count < 2)
                return Problem("Add at least two participants before drawing names.");

            Draw.Assign(exchange.Participants);
            exchange.DrawnAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(await LoadDetail(db, id));
        });

        // Clears all assignments so participants can be changed and names drawn again.
        admin.MapPost("/exchanges/{id:guid}/reset", async (Guid id, AppDbContext db) =>
        {
            var exchange = await db.Exchanges.Include(e => e.Participants).FirstOrDefaultAsync(e => e.Id == id);
            if (exchange is null)
                return Results.NotFound();

            foreach (var p in exchange.Participants)
            {
                p.RecipientId = null;
                p.FirstViewedAt = null;
            }
            exchange.DrawnAt = null;
            await db.SaveChangesAsync();
            return Results.Ok(await LoadDetail(db, id));
        });
    }

    private static async Task<ExchangeDetail?> LoadDetail(AppDbContext db, Guid id) =>
        await db.Exchanges
            .Where(e => e.Id == id)
            .Select(e => new ExchangeDetail(
                e.Id, e.Name, e.Date, e.Description, e.DrawnAt,
                e.Participants
                    .OrderBy(p => p.Name)
                    .Select(p => new ParticipantView(p.Id, p.Name, p.InviteToken, p.FirstViewedAt != null))
                    .ToList()))
            .FirstOrDefaultAsync();

    private static IResult? Validate(string? name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200)
            return Problem("Name must be between 1 and 200 characters.");
        if (description?.Length > 4000)
            return Problem("Description must be 4000 characters or fewer.");
        return null;
    }

    private static IResult Problem(string detail, int status = 400) =>
        Results.Problem(detail: detail, statusCode: status);
}
