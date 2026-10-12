using Microsoft.EntityFrameworkCore;
using SecretSanta.Api.Data;

namespace SecretSanta.Api.Stores;

public class ExchangeStore(AppDbContext db) : IExchangeStore
{
    public Task<List<Exchange>> GetAllAsync() =>
        db.Exchanges.OrderBy(e => e.Date).ThenBy(e => e.Name).ToListAsync();

    public Task<Exchange?> GetByIdAsync(Guid id) =>
        db.Exchanges.FirstOrDefaultAsync(e => e.Id == id);

    public async Task AddAsync(Exchange exchange)
    {
        db.Exchanges.Add(exchange);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Exchange exchange)
    {
        db.Exchanges.Update(exchange);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Exchange exchange)
    {
        db.Exchanges.Remove(exchange);
        await db.SaveChangesAsync();
    }
}
