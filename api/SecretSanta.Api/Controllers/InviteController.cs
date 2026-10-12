using Microsoft.AspNetCore.Mvc;
using SecretSanta.Api.Contracts;
using SecretSanta.Api.Services;

namespace SecretSanta.Api.Controllers;

[ApiController]
[Route("api/invite")]
public class InviteController(IInviteService invites) : ApiControllerBase
{
    [HttpGet("{token}")]
    public async Task<ActionResult<InviteView>> GetInvite(string token) =>
        FromResult(await invites.GetInviteAsync(token));
}
