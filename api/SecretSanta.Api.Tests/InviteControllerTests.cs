using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretSanta.Api.Controllers;
using SecretSanta.Api.Data;

namespace SecretSanta.Api.Tests;

public class InviteControllerTests
{
    private readonly AppDbContext _db = TestDb.Create();
    private readonly AdminController _admin;
    private readonly InviteController _controller;

    public InviteControllerTests()
    {
        _admin = new AdminController(_db);
        _controller = new InviteController(_db);
    }

    private async Task<ExchangeDetail> CreateExchange(params string[] people)
    {
        var result = await _admin.CreateExchange(
            new CreateExchangeRequest("Family Christmas", new DateOnly(2026, 12, 25), "£30 budget", [.. people]));
        return (ExchangeDetail)Assert.IsType<CreatedResult>(result.Result).Value!;
    }

    [Fact]
    public async Task UnknownToken_IsNotFound()
    {
        var result = await _controller.GetInvite("not-a-real-token");

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task BeforeDraw_ShowsExchangeWithoutRecipient()
    {
        var exchange = await CreateExchange("Alice", "Bob", "Cara");
        var alice = exchange.Participants.Single(p => p.Name == "Alice");

        var invite = (await _controller.GetInvite(alice.InviteToken)).Value!;

        Assert.Equal("Family Christmas", invite.ExchangeName);
        Assert.Equal(new DateOnly(2026, 12, 25), invite.Date);
        Assert.Equal("£30 budget", invite.Description);
        Assert.Equal(3, invite.ParticipantCount);
        Assert.Equal("Alice", invite.YourName);
        Assert.Null(invite.RecipientName);
        Assert.False((await _admin.GetExchange(exchange.Id)).Value!.Participants.Single(p => p.Id == alice.Id).HasViewed);
    }

    [Fact]
    public async Task AfterDraw_ShowsOwnRecipientAndRecordsView()
    {
        var exchange = await CreateExchange("Alice", "Bob", "Cara");
        await _admin.DrawNames(exchange.Id);
        var alice = await _db.Participants.AsNoTracking().Include(p => p.Recipient).SingleAsync(p => p.Name == "Alice");

        var invite = (await _controller.GetInvite(alice.InviteToken)).Value!;

        Assert.Equal("Alice", invite.YourName);
        Assert.Equal(alice.Recipient!.Name, invite.RecipientName);
        Assert.NotEqual("Alice", invite.RecipientName);

        var views = (await _admin.GetExchange(exchange.Id)).Value!.Participants.ToDictionary(p => p.Name, p => p.HasViewed);
        Assert.True(views["Alice"]);
        Assert.False(views["Bob"]);
        Assert.False(views["Cara"]);
    }
}
