using SecretSanta.Api.Contracts;
using SecretSanta.Api.Data;
using SecretSanta.Api.Stores;

namespace SecretSanta.Api.Services;

public class ExchangeService(IExchangeStore exchanges, IParticipantStore participants, TimeProvider time) : IExchangeService
{
    private const string AlreadyDrawn = "Names have already been drawn. Reset the draw to change participants.";

    public async Task<List<ExchangeSummary>> ListAsync()
    {
        var result = new List<ExchangeSummary>();
        foreach (var e in await exchanges.GetAllAsync())
        {
            var count = await participants.CountByExchangeIdAsync(e.Id);
            result.Add(new ExchangeSummary(e.Id, e.Name, e.Date, count, e.DrawnAt is not null));
        }
        return result;
    }

    public async Task<ServiceResult<ExchangeDetail>> GetAsync(Guid id)
    {
        var exchange = await exchanges.GetByIdAsync(id);
        return exchange is null
            ? ServiceResult<ExchangeDetail>.NotFound()
            : ServiceResult<ExchangeDetail>.Ok(await ToDetail(exchange));
    }

    public async Task<ServiceResult<ExchangeDetail>> CreateAsync(CreateExchangeRequest request)
    {
        if (ValidateDetails(request.Name, request.Description) is { } error)
            return ServiceResult<ExchangeDetail>.Invalid(error);

        var names = (request.Participants ?? []).Select(n => n.Trim()).Where(n => n.Length > 0).ToList();
        if (names.Any(n => n.Length > 100))
            return ServiceResult<ExchangeDetail>.Invalid("Participant names must be 100 characters or fewer.");
        if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Count)
            return ServiceResult<ExchangeDetail>.Invalid("Participant names must be unique.");

        var exchange = new Exchange
        {
            Id = Guid.CreateVersion7(),
            Name = request.Name.Trim(),
            Date = request.Date,
            Description = request.Description?.Trim() ?? "",
            CreatedAt = time.GetUtcNow(),
        };
        var people = names.Select(n => NewParticipant(exchange.Id, n)).ToList();
        await exchanges.AddAsync(exchange);
        await participants.AddRangeAsync(people);

        return ServiceResult<ExchangeDetail>.Ok(ToDetail(exchange, people));
    }

    public async Task<ServiceResult<ExchangeDetail>> UpdateAsync(Guid id, ExchangeInput input)
    {
        if (ValidateDetails(input.Name, input.Description) is { } error)
            return ServiceResult<ExchangeDetail>.Invalid(error);

        var exchange = await exchanges.GetByIdAsync(id);
        if (exchange is null)
            return ServiceResult<ExchangeDetail>.NotFound();

        exchange.Name = input.Name.Trim();
        exchange.Date = input.Date;
        exchange.Description = input.Description?.Trim() ?? "";
        await exchanges.UpdateAsync(exchange);

        return ServiceResult<ExchangeDetail>.Ok(await ToDetail(exchange));
    }

    public async Task<ServiceResult> DeleteAsync(Guid id)
    {
        var exchange = await exchanges.GetByIdAsync(id);
        if (exchange is null)
            return ServiceResult.NotFound();

        var people = await participants.GetByExchangeIdAsync(id);
        // Clear recipient links first so the self-referencing foreign key doesn't block the delete.
        if (ClearAssignments(people))
            await participants.UpdateRangeAsync(people);
        await participants.DeleteRangeAsync(people);
        await exchanges.DeleteAsync(exchange);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<ExchangeDetail>> AddParticipantAsync(Guid id, string name)
    {
        var exchange = await exchanges.GetByIdAsync(id);
        if (exchange is null)
            return ServiceResult<ExchangeDetail>.NotFound();
        if (exchange.DrawnAt is not null)
            return ServiceResult<ExchangeDetail>.Conflict(AlreadyDrawn);

        name = name?.Trim() ?? "";
        if (name.Length is 0 or > 100)
            return ServiceResult<ExchangeDetail>.Invalid("Name must be between 1 and 100 characters.");

        var people = await participants.GetByExchangeIdAsync(id);
        if (people.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
            return ServiceResult<ExchangeDetail>.Invalid($"{name} is already in this exchange.");

        await participants.AddAsync(NewParticipant(id, name));
        return ServiceResult<ExchangeDetail>.Ok(await ToDetail(exchange));
    }

    public async Task<ServiceResult<ExchangeDetail>> RemoveParticipantAsync(Guid id, Guid participantId)
    {
        var exchange = await exchanges.GetByIdAsync(id);
        if (exchange is null)
            return ServiceResult<ExchangeDetail>.NotFound();
        if (exchange.DrawnAt is not null)
            return ServiceResult<ExchangeDetail>.Conflict(AlreadyDrawn);

        var participant = await participants.GetByIdAsync(participantId);
        if (participant is null || participant.ExchangeId != id)
            return ServiceResult<ExchangeDetail>.NotFound();

        await participants.DeleteAsync(participant);
        return ServiceResult<ExchangeDetail>.Ok(await ToDetail(exchange));
    }

    public async Task<ServiceResult<ExchangeDetail>> DrawAsync(Guid id)
    {
        var exchange = await exchanges.GetByIdAsync(id);
        if (exchange is null)
            return ServiceResult<ExchangeDetail>.NotFound();
        if (exchange.DrawnAt is not null)
            return ServiceResult<ExchangeDetail>.Conflict("Names have already been drawn.");

        var people = await participants.GetByExchangeIdAsync(id);
        if (people.Count < 2)
            return ServiceResult<ExchangeDetail>.Invalid("Add at least two participants before drawing names.");

        Draw.Assign(people);
        await participants.UpdateRangeAsync(people);
        exchange.DrawnAt = time.GetUtcNow();
        await exchanges.UpdateAsync(exchange);

        return ServiceResult<ExchangeDetail>.Ok(ToDetail(exchange, people));
    }

    // Clears all assignments so participants can be changed and names drawn again.
    public async Task<ServiceResult<ExchangeDetail>> ResetDrawAsync(Guid id)
    {
        var exchange = await exchanges.GetByIdAsync(id);
        if (exchange is null)
            return ServiceResult<ExchangeDetail>.NotFound();

        var people = await participants.GetByExchangeIdAsync(id);
        ClearAssignments(people);
        await participants.UpdateRangeAsync(people);
        exchange.DrawnAt = null;
        await exchanges.UpdateAsync(exchange);

        return ServiceResult<ExchangeDetail>.Ok(ToDetail(exchange, people));
    }

    private async Task<ExchangeDetail> ToDetail(Exchange exchange) =>
        ToDetail(exchange, await participants.GetByExchangeIdAsync(exchange.Id));

    private static ExchangeDetail ToDetail(Exchange exchange, IEnumerable<Participant> people) =>
        new(exchange.Id, exchange.Name, exchange.Date, exchange.Description, exchange.DrawnAt,
            people
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .Select(p => new ParticipantView(p.Id, p.Name, p.InviteToken, p.FirstViewedAt is not null))
                .ToList());

    private static Participant NewParticipant(Guid exchangeId, string name) =>
        new() { Id = Guid.CreateVersion7(), ExchangeId = exchangeId, Name = name, InviteToken = Draw.NewInviteToken() };

    /// <summary>Removes every assignment and view record. Returns whether anything changed.</summary>
    private static bool ClearAssignments(List<Participant> people)
    {
        var changed = false;
        foreach (var p in people.Where(p => p.RecipientId is not null || p.FirstViewedAt is not null))
        {
            p.RecipientId = null;
            p.FirstViewedAt = null;
            changed = true;
        }
        return changed;
    }

    private static string? ValidateDetails(string? name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200)
            return "Name must be between 1 and 200 characters.";
        if (description?.Length > 4000)
            return "Description must be 4000 characters or fewer.";
        return null;
    }
}
