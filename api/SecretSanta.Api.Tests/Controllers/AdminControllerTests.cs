using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using NSubstitute;
using SecretSanta.Api.Auth;
using SecretSanta.Api.Contracts;
using SecretSanta.Api.Controllers;
using SecretSanta.Api.Services;

namespace SecretSanta.Api.Tests.Controllers;

public class AdminControllerTests
{
    private static readonly Guid ExchangeId = Guid.NewGuid();
    private static readonly ExchangeDetail Detail = new(
        ExchangeId, "Family Christmas", new DateOnly(2026, 12, 25), "£30 budget", null,
        [new ParticipantView(Guid.NewGuid(), "Alice", "token-alice", false)]);

    private IExchangeService _service = null!;
    private AdminController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _service = Substitute.For<IExchangeService>();
        _controller = new AdminController(_service);
    }

    private static void AssertProblem(IConvertToActionResult result, int status, string detail)
    {
        var objectResult = result.Convert() as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);
        Assert.That(objectResult!.StatusCode, Is.EqualTo(status));
        Assert.That(((ProblemDetails)objectResult.Value!).Detail, Is.EqualTo(detail));
    }

    [Test]
    public void RequiresAdminPassword()
    {
        Assert.That(typeof(AdminController).GetCustomAttributes(true), Has.Some.InstanceOf<AdminOnlyAttribute>());
    }

    [Test]
    public void Login_ReturnsNoContent()
    {
        Assert.That(_controller.Login(), Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task ListExchanges_ReturnsServiceResult()
    {
        List<ExchangeSummary> summaries = [new(ExchangeId, "Family Christmas", new DateOnly(2026, 12, 25), 1, false)];
        _service.ListAsync().Returns(summaries);

        Assert.That(await _controller.ListExchanges(), Is.SameAs(summaries));
    }

    [Test]
    public async Task GetExchange_Found_ReturnsDetail()
    {
        _service.GetAsync(ExchangeId).Returns(ServiceResult<ExchangeDetail>.Ok(Detail));

        var result = await _controller.GetExchange(ExchangeId);

        Assert.That(((OkObjectResult)result.Result!).Value, Is.SameAs(Detail));
    }

    [Test]
    public async Task GetExchange_NotFound_Returns404()
    {
        _service.GetAsync(ExchangeId).Returns(ServiceResult<ExchangeDetail>.NotFound());

        var result = await _controller.GetExchange(ExchangeId);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task CreateExchange_Success_Returns201WithLocation()
    {
        var request = new CreateExchangeRequest("Family Christmas", new DateOnly(2026, 12, 25), null, ["Alice"]);
        _service.CreateAsync(request).Returns(ServiceResult<ExchangeDetail>.Ok(Detail));

        var result = await _controller.CreateExchange(request);

        var created = result.Result as CreatedResult;
        Assert.That(created, Is.Not.Null);
        Assert.That(created!.Location, Is.EqualTo($"/api/admin/exchanges/{ExchangeId}"));
        Assert.That(created.Value, Is.SameAs(Detail));
    }

    [Test]
    public async Task CreateExchange_Invalid_Returns400WithDetail()
    {
        var request = new CreateExchangeRequest("", new DateOnly(2026, 12, 25), null, []);
        _service.CreateAsync(request).Returns(ServiceResult<ExchangeDetail>.Invalid("Name is required."));

        AssertProblem(await _controller.CreateExchange(request), 400, "Name is required.");
    }

    [Test]
    public async Task UpdateExchange_PassesInputToService()
    {
        var input = new ExchangeInput("New name", new DateOnly(2026, 12, 24), null);
        _service.UpdateAsync(ExchangeId, input).Returns(ServiceResult<ExchangeDetail>.Ok(Detail));

        var result = await _controller.UpdateExchange(ExchangeId, input);

        Assert.That(((OkObjectResult)result.Result!).Value, Is.SameAs(Detail));
    }

    [Test]
    public async Task DeleteExchange_Success_ReturnsNoContent()
    {
        _service.DeleteAsync(ExchangeId).Returns(ServiceResult.Ok());

        Assert.That(await _controller.DeleteExchange(ExchangeId), Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeleteExchange_NotFound_Returns404()
    {
        _service.DeleteAsync(ExchangeId).Returns(ServiceResult.NotFound());

        Assert.That(await _controller.DeleteExchange(ExchangeId), Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task AddParticipant_PassesNameToService()
    {
        _service.AddParticipantAsync(ExchangeId, "Bob").Returns(ServiceResult<ExchangeDetail>.Ok(Detail));

        var result = await _controller.AddParticipant(ExchangeId, new AddParticipantRequest("Bob"));

        Assert.That(((OkObjectResult)result.Result!).Value, Is.SameAs(Detail));
    }

    [Test]
    public async Task AddParticipant_Conflict_Returns409WithDetail()
    {
        _service.AddParticipantAsync(ExchangeId, "Bob").Returns(ServiceResult<ExchangeDetail>.Conflict("Already drawn."));

        AssertProblem(await _controller.AddParticipant(ExchangeId, new AddParticipantRequest("Bob")), 409, "Already drawn.");
    }

    [Test]
    public async Task RemoveParticipant_PassesIdsToService()
    {
        var participantId = Guid.NewGuid();
        _service.RemoveParticipantAsync(ExchangeId, participantId).Returns(ServiceResult<ExchangeDetail>.Ok(Detail));

        var result = await _controller.RemoveParticipant(ExchangeId, participantId);

        Assert.That(((OkObjectResult)result.Result!).Value, Is.SameAs(Detail));
    }

    [Test]
    public async Task DrawNames_ReturnsDetail()
    {
        _service.DrawAsync(ExchangeId).Returns(ServiceResult<ExchangeDetail>.Ok(Detail));

        var result = await _controller.DrawNames(ExchangeId);

        Assert.That(((OkObjectResult)result.Result!).Value, Is.SameAs(Detail));
    }

    [Test]
    public async Task DrawNames_Conflict_Returns409()
    {
        _service.DrawAsync(ExchangeId).Returns(ServiceResult<ExchangeDetail>.Conflict("Names have already been drawn."));

        AssertProblem(await _controller.DrawNames(ExchangeId), 409, "Names have already been drawn.");
    }

    [Test]
    public async Task ResetDraw_ReturnsDetail()
    {
        _service.ResetDrawAsync(ExchangeId).Returns(ServiceResult<ExchangeDetail>.Ok(Detail));

        var result = await _controller.ResetDraw(ExchangeId);

        Assert.That(((OkObjectResult)result.Result!).Value, Is.SameAs(Detail));
    }
}
