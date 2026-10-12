using SecretSanta.Api.Contracts;

namespace SecretSanta.Api.Services;

/// <summary>Organiser operations on gift exchanges. Results never include who has who.</summary>
public interface IExchangeService
{
    Task<List<ExchangeSummary>> ListAsync();
    Task<ServiceResult<ExchangeDetail>> GetAsync(Guid id);
    Task<ServiceResult<ExchangeDetail>> CreateAsync(CreateExchangeRequest request);
    Task<ServiceResult<ExchangeDetail>> UpdateAsync(Guid id, ExchangeInput input);
    Task<ServiceResult> DeleteAsync(Guid id);
    Task<ServiceResult<ExchangeDetail>> AddParticipantAsync(Guid id, string name);
    Task<ServiceResult<ExchangeDetail>> RemoveParticipantAsync(Guid id, Guid participantId);
    Task<ServiceResult<ExchangeDetail>> DrawAsync(Guid id);
    Task<ServiceResult<ExchangeDetail>> ResetDrawAsync(Guid id);
}
