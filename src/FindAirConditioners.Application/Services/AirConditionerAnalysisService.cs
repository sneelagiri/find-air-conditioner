using FindAirConditioners.Application.Abstractions;
using FindAirConditioners.Domain.Models;

namespace FindAirConditioners.Application.Services;

public sealed class AirConditionerAnalysisService(IAirConditionerRepository repository) : IAirConditionerAnalysisService
{
    public async Task<AirConditionerSearchResult> AnalyzeAsync(Guid searchId, CancellationToken cancellationToken = default)
    {
        var request = await repository.GetRequestAsync(searchId, cancellationToken)
            ?? throw new InvalidOperationException($"Search request {searchId} was not found.");

        var listings = await repository.GetListingsAsync(searchId, cancellationToken);
        var filtered = listings
            .Where(listing => request.MaxPrice is null || listing.Price <= request.MaxPrice)
            .OrderBy(listing => listing.Price)
            .ToArray();

        var result = new AirConditionerSearchResult(
            searchId,
            DateTimeOffset.UtcNow,
            filtered,
            filtered.Length == 0 ? "NoResults" : "Completed",
            filtered.Length == 0 ? "No listings matched the requested filters." : $"Found {filtered.Length} matching listings.");

        await repository.SaveResultAsync(result, cancellationToken);
        return result;
    }
}
