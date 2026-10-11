using SecretSanta.Api.Contracts;
using SecretSanta.Api.Stores;

namespace SecretSanta.Api.Services;

public class InviteService(IExchangeStore exchanges, IParticipantStore participants, TimeProvider time) : IInviteService
{
    public async Task<ServiceResult<InviteView>> GetInviteAsync(string inviteToken)
    {
        var participant = await participants.GetByInviteTokenAsync(inviteToken);
        if (participant is null)
            return ServiceResult<InviteView>.NotFound();

        var exchange = await exchanges.GetByIdAsync(participant.ExchangeId);
        if (exchange is null)
            return ServiceResult<InviteView>.NotFound();

        var recipient = participant.RecipientId is { } recipientId
            ? await participants.GetByIdAsync(recipientId)
            : null;

        // Record the first time someone sees who they're buying for, so the organiser knows they've looked.
        if (recipient is not null && participant.FirstViewedAt is null)
        {
            participant.FirstViewedAt = time.GetUtcNow();
            await participants.UpdateAsync(participant);
        }

        var participantCount = await participants.CountByExchangeIdAsync(exchange.Id);
        return ServiceResult<InviteView>.Ok(new InviteView(
            exchange.Name, exchange.Date, exchange.Description, participantCount,
            participant.Name, recipient?.Name));
    }
}
