using FluentAssertions;
using FindAirConditioners.Application.Abstractions;
using FindAirConditioners.Application.Services;
using FindAirConditioners.Domain.Models;

namespace FindAirConditioners.UnitTests;

public sealed class AnalysisServiceTests
{
    [Fact]
    public async Task AnalyzeAsync_filters_by_price_and_sorts_listings()
    {
        var repository = new FakeRepository(
            new AirConditionerSearchRequest(1500m),
            [
                new("Other", "Budget Unit", 999m, "u1"),
                new("Store", "Daikin Premium", 1400m, "u2"),
                new("Store", "Daikin Expensive", 1800m, "u3")
            ]);

        var service = new AirConditionerAnalysisService(repository);

        var result = await service.AnalyzeAsync(Guid.NewGuid());

        result.Status.Should().Be("Completed");
        result.Listings.Should().HaveCount(2);
        result.Listings.Select(listing => listing.Title).Should().ContainInOrder("Budget Unit", "Daikin Premium");
        repository.SavedResult.Should().NotBeNull();
    }

    sealed class FakeRepository(AirConditionerSearchRequest request, IReadOnlyCollection<AirConditionerListing> listings) : IAirConditionerRepository
    {
        public AirConditionerSearchResult? SavedResult { get; private set; }

        public Task<AirConditionerSearchRequest?> GetRequestAsync(Guid searchId, CancellationToken cancellationToken = default) => Task.FromResult<AirConditionerSearchRequest?>(request);
        public Task<IReadOnlyCollection<AirConditionerListing>> GetListingsAsync(Guid searchId, CancellationToken cancellationToken = default) => Task.FromResult(listings);
        public Task SaveResultAsync(AirConditionerSearchResult result, CancellationToken cancellationToken = default)
        {
            SavedResult = result;
            return Task.CompletedTask;
        }

        public Task SaveRequestedSearchAsync(Guid searchId, AirConditionerSearchRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveListingsAsync(Guid searchId, IReadOnlyCollection<AirConditionerListing> listings, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<AirConditionerSearchResult?> GetResultAsync(Guid searchId, CancellationToken cancellationToken = default) => Task.FromResult(SavedResult);
    }
}
