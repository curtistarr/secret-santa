using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretSanta.Api.Controllers;
using SecretSanta.Api.Data;

namespace SecretSanta.Api.Tests;

public class AdminControllerTests
{
    private readonly AppDbContext _db = TestDb.Create();
    private readonly AdminController _controller;

    public AdminControllerTests() => _controller = new AdminController(_db);

    private async Task<ExchangeDetail> CreateExchange(params string[] people)
    {
        var result = await _controller.CreateExchange(
            new CreateExchangeRequest("Family Christmas", new DateOnly(2026, 12, 25), "£30 budget", [.. people]));
        return (ExchangeDetail)Assert.IsType<CreatedResult>(result.Result).Value!;
    }

    private static int StatusOf<T>(ActionResult<T> result) =>
        Assert.IsType<ObjectResult>(result.Result).StatusCode!.Value;

    [Fact]
    public async Task CreateExchange_SavesDetailsAndParticipants()
    {
        var created = await _controller.CreateExchange(new CreateExchangeRequest(
            "  Family Christmas  ", new DateOnly(2026, 12, 25), " £30 budget ", ["Alice", " Bob ", "", "Cara"]));

        var detail = (ExchangeDetail)Assert.IsType<CreatedResult>(created.Result).Value!;
        Assert.Equal("Family Christmas", detail.Name);
        Assert.Equal(new DateOnly(2026, 12, 25), detail.Date);
        Assert.Equal("£30 budget", detail.Description);
        Assert.Null(detail.DrawnAt);
        Assert.Equal(["Alice", "Bob", "Cara"], detail.Participants.Select(p => p.Name));
        Assert.Equal(3, detail.Participants.Select(p => p.InviteToken).Distinct().Count());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateExchange_WithoutName_IsRejected(string name)
    {
        var result = await _controller.CreateExchange(new CreateExchangeRequest(name, new DateOnly(2026, 12, 25), null, []));

        Assert.Equal(400, StatusOf(result));
        Assert.Empty(_db.Exchanges);
    }

    [Fact]
    public async Task CreateExchange_WithDuplicateNames_IsRejected()
    {
        var result = await _controller.CreateExchange(
            new CreateExchangeRequest("Xmas", new DateOnly(2026, 12, 25), null, ["Alice", "alice"]));

        Assert.Equal(400, StatusOf(result));
    }

    [Fact]
    public async Task ListExchanges_ReturnsSummariesOrderedByDate()
    {
        await _controller.CreateExchange(new CreateExchangeRequest("Later", new DateOnly(2027, 1, 1), null, ["A", "B"]));
        await _controller.CreateExchange(new CreateExchangeRequest("Sooner", new DateOnly(2026, 12, 1), null, ["A"]));

        var list = await _controller.ListExchanges();

        Assert.Equal(["Sooner", "Later"], list.Select(x => x.Name));
        Assert.Equal([1, 2], list.Select(x => x.ParticipantCount));
        Assert.All(list, x => Assert.False(x.IsDrawn));
    }

    [Fact]
    public async Task GetExchange_UnknownId_IsNotFound()
    {
        var result = await _controller.GetExchange(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateExchange_ChangesDetails()
    {
        var exchange = await CreateExchange("Alice", "Bob");

        var result = await _controller.UpdateExchange(exchange.Id, new ExchangeInput("New name", new DateOnly(2026, 12, 24), "New description"));

        Assert.Equal("New name", result.Value!.Name);
        Assert.Equal(new DateOnly(2026, 12, 24), result.Value.Date);
        Assert.Equal("New description", result.Value.Description);
    }

    [Fact]
    public async Task AddAndRemoveParticipant_BeforeDraw()
    {
        var exchange = await CreateExchange("Alice", "Bob");

        var added = await _controller.AddParticipant(exchange.Id, new AddParticipantRequest(" Cara "));
        Assert.Equal(["Alice", "Bob", "Cara"], added.Value!.Participants.Select(p => p.Name));

        var bob = added.Value.Participants.Single(p => p.Name == "Bob");
        var removed = await _controller.RemoveParticipant(exchange.Id, bob.Id);
        Assert.Equal(["Alice", "Cara"], removed.Value!.Participants.Select(p => p.Name));
    }

    [Fact]
    public async Task AddParticipant_DuplicateName_IsRejected()
    {
        var exchange = await CreateExchange("Alice", "Bob");

        var result = await _controller.AddParticipant(exchange.Id, new AddParticipantRequest("ALICE"));

        Assert.Equal(400, StatusOf(result));
    }

    [Fact]
    public async Task DrawNames_AssignsEveryone()
    {
        var exchange = await CreateExchange("Alice", "Bob", "Cara", "Dan");

        var result = await _controller.DrawNames(exchange.Id);

        Assert.NotNull(result.Value!.DrawnAt);
        var participants = await _db.Participants.AsNoTracking().ToListAsync();
        Assert.All(participants, p => Assert.NotNull(p.RecipientId));
        Assert.All(participants, p => Assert.NotEqual(p.Id, p.RecipientId));
        Assert.Equal(4, participants.Select(p => p.RecipientId).Distinct().Count());
    }

    [Fact]
    public async Task DrawNames_WithFewerThanTwoPeople_IsRejected()
    {
        var exchange = await CreateExchange("Alice");

        Assert.Equal(400, StatusOf(await _controller.DrawNames(exchange.Id)));
    }

    [Fact]
    public async Task DrawNames_Twice_IsConflict()
    {
        var exchange = await CreateExchange("Alice", "Bob");
        await _controller.DrawNames(exchange.Id);

        Assert.Equal(409, StatusOf(await _controller.DrawNames(exchange.Id)));
    }

    [Fact]
    public async Task ChangingParticipants_AfterDraw_IsConflict()
    {
        var exchange = await CreateExchange("Alice", "Bob");
        await _controller.DrawNames(exchange.Id);

        Assert.Equal(409, StatusOf(await _controller.AddParticipant(exchange.Id, new AddParticipantRequest("Cara"))));
        Assert.Equal(409, StatusOf(await _controller.RemoveParticipant(exchange.Id, exchange.Participants[0].Id)));
    }

    [Fact]
    public async Task ResetDraw_ClearsAssignmentsAndViews()
    {
        var exchange = await CreateExchange("Alice", "Bob", "Cara");
        await _controller.DrawNames(exchange.Id);
        await new InviteController(_db).GetInvite(exchange.Participants[0].InviteToken);

        var result = await _controller.ResetDraw(exchange.Id);

        Assert.Null(result.Value!.DrawnAt);
        Assert.All(result.Value.Participants, p => Assert.False(p.HasViewed));
        Assert.All(await _db.Participants.AsNoTracking().ToListAsync(), p => Assert.Null(p.RecipientId));
    }

    [Fact]
    public async Task DeleteExchange_RemovesExchangeAndParticipants()
    {
        var exchange = await CreateExchange("Alice", "Bob", "Cara");
        await _controller.DrawNames(exchange.Id);

        var result = await _controller.DeleteExchange(exchange.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(_db.Exchanges);
        Assert.Empty(_db.Participants);
    }

    [Fact]
    public void AdminResponses_NeverExposeWhoHasWho()
    {
        // The admin must never be able to see assignments, so the view models sent to the
        // admin page must not carry any recipient information.
        string[] adminTypes = [.. new[] { typeof(ExchangeDetail), typeof(ExchangeSummary), typeof(ParticipantView) }
            .SelectMany(t => t.GetProperties())
            .Select(p => p.Name)];

        Assert.DoesNotContain(adminTypes, name => name.Contains("Recipient", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AdminController_RequiresAdminPassword()
    {
        Assert.Contains(typeof(AdminController).GetCustomAttributes(true), a => a is Auth.AdminOnlyAttribute);
    }
}
