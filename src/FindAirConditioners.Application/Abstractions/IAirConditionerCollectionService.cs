using FindAirConditioners.Domain.Models;

namespace FindAirConditioners.Application.Abstractions;

public interface IAirConditionerCollectionService
{
    Task<Guid> QueueCollectionAsync(AirConditionerSearchRequest request, CancellationToken cancellationToken = default);
}
