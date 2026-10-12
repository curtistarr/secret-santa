using SecretSanta.Api.Data;

namespace SecretSanta.Api.Stores;

/// <summary>Simple CRUD access to the Exchanges table.</summary>
public interface IExchangeStore
{
    Task<List<Exchange>> GetAllAsync();
    Task<Exchange?> GetByIdAsync(Guid id);
    Task AddAsync(Exchange exchange);
    Task UpdateAsync(Exchange exchange);
    Task DeleteAsync(Exchange exchange);
}
