using SecretSanta.Api.Data;

namespace SecretSanta.Api.Stores;

/// <summary>Simple CRUD access to the Participants table.</summary>
public interface IParticipantStore
{
    Task<List<Participant>> GetByExchangeIdAsync(Guid exchangeId);
    Task<int> CountByExchangeIdAsync(Guid exchangeId);
    Task<Participant?> GetByIdAsync(Guid id);
    Task<Participant?> GetByInviteTokenAsync(string inviteToken);
    Task AddAsync(Participant participant);
    Task AddRangeAsync(IEnumerable<Participant> participants);
    Task UpdateAsync(Participant participant);
    Task UpdateRangeAsync(IEnumerable<Participant> participants);
    Task DeleteAsync(Participant participant);
    Task DeleteRangeAsync(IEnumerable<Participant> participants);
}
