using FindAirConditioners.Web.Core.Models;

namespace FindAirConditioners.Web.Core.Abstractions;

public interface IAirConditionerRepository
{
    Task SaveRequestedSearchAsync(Guid searchId, AirConditionerSearchRequest request, CancellationToken cancellationToken = default);
    Task SaveListingsAsync(Guid searchId, IReadOnlyCollection<AirConditionerListing> listings, CancellationToken cancellationToken = default);
    Task<AirConditionerSearchRequest?> GetRequestAsync(Guid searchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AirConditionerListing>> GetListingsAsync(Guid searchId, CancellationToken cancellationToken = default);
    Task SaveResultAsync(AirConditionerSearchResult result, CancellationToken cancellationToken = default);
    Task<AirConditionerSearchResult?> GetResultAsync(Guid searchId, CancellationToken cancellationToken = default);
}
