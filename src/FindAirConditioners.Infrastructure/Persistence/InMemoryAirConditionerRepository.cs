using System.Collections.Concurrent;
using System.Text.Json;
using FindAirConditioners.Application.Abstractions;
using FindAirConditioners.Domain.Models;

namespace FindAirConditioners.Infrastructure.Persistence;

public sealed class InMemoryAirConditionerRepository : IAirConditionerRepository
{
    readonly ConcurrentDictionary<Guid, AirConditionerSearchRequest> requests = new();
    readonly ConcurrentDictionary<Guid, List<AirConditionerListing>> listings = new();
    readonly ConcurrentDictionary<Guid, AirConditionerSearchResult> results = new();

    public Task SaveRequestedSearchAsync(Guid searchId, AirConditionerSearchRequest request, CancellationToken cancellationToken = default)
    {
        requests[searchId] = request;
        return Task.CompletedTask;
    }

    public Task SaveListingsAsync(Guid searchId, IReadOnlyCollection<AirConditionerListing> newListings, CancellationToken cancellationToken = default)
    {
        listings[searchId] = newListings.ToList();
        return Task.CompletedTask;
    }

    public Task<AirConditionerSearchRequest?> GetRequestAsync(Guid searchId, CancellationToken cancellationToken = default)
    {
        requests.TryGetValue(searchId, out var request);
        return Task.FromResult(request);
    }

    public Task<IReadOnlyCollection<AirConditionerListing>> GetListingsAsync(Guid searchId, CancellationToken cancellationToken = default)
    {
        listings.TryGetValue(searchId, out var currentListings);
        return Task.FromResult<IReadOnlyCollection<AirConditionerListing>>(currentListings?.ToArray() ?? []);
    }

    public Task SaveResultAsync(AirConditionerSearchResult result, CancellationToken cancellationToken = default)
    {
        results[result.SearchId] = result;
        return Task.CompletedTask;
    }

    public Task<AirConditionerSearchResult?> GetResultAsync(Guid searchId, CancellationToken cancellationToken = default)
    {
        results.TryGetValue(searchId, out var result);
        return Task.FromResult(result);
    }
}
