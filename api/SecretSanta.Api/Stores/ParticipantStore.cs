using Microsoft.EntityFrameworkCore;
using SecretSanta.Api.Data;

namespace SecretSanta.Api.Stores;

public class ParticipantStore(AppDbContext db) : IParticipantStore
{
    public Task<List<Participant>> GetByExchangeIdAsync(Guid exchangeId) =>
        db.Participants.Where(p => p.ExchangeId == exchangeId).OrderBy(p => p.Name).ToListAsync();

    public Task<int> CountByExchangeIdAsync(Guid exchangeId) =>
        db.Participants.CountAsync(p => p.ExchangeId == exchangeId);

    public Task<Participant?> GetByIdAsync(Guid id) =>
        db.Participants.FirstOrDefaultAsync(p => p.Id == id);

    public Task<Participant?> GetByInviteTokenAsync(string inviteToken) =>
        db.Participants.FirstOrDefaultAsync(p => p.InviteToken == inviteToken);

    public async Task AddAsync(Participant participant)
    {
        db.Participants.Add(participant);
        await db.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Participant> participants)
    {
        db.Participants.AddRange(participants);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Participant participant)
    {
        db.Participants.Update(participant);
        await db.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(IEnumerable<Participant> participants)
    {
        db.Participants.UpdateRange(participants);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Participant participant)
    {
        db.Participants.Remove(participant);
        await db.SaveChangesAsync();
    }

    public async Task DeleteRangeAsync(IEnumerable<Participant> participants)
    {
        db.Participants.RemoveRange(participants);
        await db.SaveChangesAsync();
    }
}
