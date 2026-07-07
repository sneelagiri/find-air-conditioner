using FindAirConditioners.Web.Core.Models;

namespace FindAirConditioners.Web.Core.Abstractions;

public interface IAirConditionerCollectionService
{
    Task<Guid> QueueCollectionAsync(AirConditionerSearchRequest request, CancellationToken cancellationToken = default);
}
