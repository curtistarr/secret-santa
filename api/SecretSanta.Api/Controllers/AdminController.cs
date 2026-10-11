using Microsoft.AspNetCore.Mvc;
using SecretSanta.Api.Auth;
using SecretSanta.Api.Contracts;
using SecretSanta.Api.Services;

namespace SecretSanta.Api.Controllers;

[ApiController]
[Route("api/admin")]
[AdminOnly]
public class AdminController(IExchangeService exchanges) : ApiControllerBase
{
    // Lets the admin page check a password before storing it.
    [HttpPost("login")]
    public IActionResult Login() => NoContent();

    [HttpGet("exchanges")]
    public Task<List<ExchangeSummary>> ListExchanges() => exchanges.ListAsync();

    [HttpGet("exchanges/{id:guid}")]
    public async Task<ActionResult<ExchangeDetail>> GetExchange(Guid id) =>
        FromResult(await exchanges.GetAsync(id));

    [HttpPost("exchanges")]
    public async Task<ActionResult<ExchangeDetail>> CreateExchange(CreateExchangeRequest request) =>
        FromResult(await exchanges.CreateAsync(request),
            detail => Created($"/api/admin/exchanges/{detail.Id}", detail));

    [HttpPut("exchanges/{id:guid}")]
    public async Task<ActionResult<ExchangeDetail>> UpdateExchange(Guid id, ExchangeInput input) =>
        FromResult(await exchanges.UpdateAsync(id, input));

    [HttpDelete("exchanges/{id:guid}")]
    public async Task<IActionResult> DeleteExchange(Guid id) =>
        FromResult(await exchanges.DeleteAsync(id));

    [HttpPost("exchanges/{id:guid}/participants")]
    public async Task<ActionResult<ExchangeDetail>> AddParticipant(Guid id, AddParticipantRequest request) =>
        FromResult(await exchanges.AddParticipantAsync(id, request.Name));

    [HttpDelete("exchanges/{id:guid}/participants/{participantId:guid}")]
    public async Task<ActionResult<ExchangeDetail>> RemoveParticipant(Guid id, Guid participantId) =>
        FromResult(await exchanges.RemoveParticipantAsync(id, participantId));

    [HttpPost("exchanges/{id:guid}/draw")]
    public async Task<ActionResult<ExchangeDetail>> DrawNames(Guid id) =>
        FromResult(await exchanges.DrawAsync(id));

    [HttpPost("exchanges/{id:guid}/reset")]
    public async Task<ActionResult<ExchangeDetail>> ResetDraw(Guid id) =>
        FromResult(await exchanges.ResetDrawAsync(id));
}
