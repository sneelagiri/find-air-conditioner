using FindAirConditioners.Web.Core.Abstractions;
using FindAirConditioners.Web.Core.Models;
using Microsoft.Extensions.Logging;

namespace FindAirConditioners.Web.Core.Services;

public sealed class AirConditionerAnalysisService(
    IAirConditionerRepository repository,
    ILogger<AirConditionerAnalysisService> logger) : IAirConditionerAnalysisService
{
    public async Task<AirConditionerSearchResult> AnalyzeAsync(Guid searchId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Analyzing search {SearchId}.", searchId);

        var request = await repository.GetRequestAsync(searchId, cancellationToken)
            ?? throw new InvalidOperationException($"Search request {searchId} was not found.");

        var listings = await repository.GetListingsAsync(searchId, cancellationToken);
        logger.LogInformation("Loaded {ListingCount} listings for search {SearchId}.", listings.Count, searchId);

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
        logger.LogInformation("Saved analysis result for search {SearchId} with {MatchCount} matching listings.", searchId, filtered.Length);
        return result;
    }
}
