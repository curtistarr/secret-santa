using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SecretSanta.Api.Auth;
using SecretSanta.Api.Contracts;
using SecretSanta.Api.Controllers;
using SecretSanta.Api.Services;

namespace SecretSanta.Api.Tests.Controllers;

public class InviteControllerTests
{
    private IInviteService _service = null!;
    private InviteController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _service = Substitute.For<IInviteService>();
        _controller = new InviteController(_service);
    }

    [Test]
    public void DoesNotRequireAdminPassword()
    {
        Assert.That(typeof(InviteController).GetCustomAttributes(true), Has.None.InstanceOf<AdminOnlyAttribute>());
    }

    [Test]
    public async Task GetInvite_Found_ReturnsInvite()
    {
        var invite = new InviteView("Family Christmas", new DateOnly(2026, 12, 25), "£30 budget", 3, "Alice", "Bob");
        _service.GetInviteAsync("token-alice").Returns(ServiceResult<InviteView>.Ok(invite));

        var result = await _controller.GetInvite("token-alice");

        Assert.That(((OkObjectResult)result.Result!).Value, Is.SameAs(invite));
    }

    [Test]
    public async Task GetInvite_UnknownToken_Returns404()
    {
        _service.GetInviteAsync("nope").Returns(ServiceResult<InviteView>.NotFound());

        var result = await _controller.GetInvite("nope");

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }
}
