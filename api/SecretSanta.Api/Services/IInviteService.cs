using SecretSanta.Api.Contracts;

namespace SecretSanta.Api.Services;

/// <summary>What a participant sees when they open their personal invite link.</summary>
public interface IInviteService
{
    Task<ServiceResult<InviteView>> GetInviteAsync(string inviteToken);
}
