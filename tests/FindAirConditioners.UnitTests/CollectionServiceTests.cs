using FluentAssertions;
using FindAirConditioners.Application.Abstractions;
using FindAirConditioners.Application.Services;
using FindAirConditioners.Domain.Models;

namespace FindAirConditioners.UnitTests;

public sealed class CollectionServiceTests
{
    [Fact]
    public async Task QueueCollectionAsync_persists_request_and_enqueues_job()
    {
        var queue = new FakeQueue();
        var repository = new FakeRepository();
        var service = new AirConditionerCollectionService(queue, repository);

        var searchId = await service.QueueCollectionAsync(new AirConditionerSearchRequest());

        searchId.Should().NotBe(Guid.Empty);
        repository.SavedRequest.Should().NotBeNull();
        queue.EnqueuedJob.Should().NotBeNull();
        queue.EnqueuedJob!.SearchId.Should().Be(searchId);
    }

    sealed class FakeQueue : IMessageQueue<CollectionJob>
    {
        public CollectionJob? EnqueuedJob { get; private set; }
        public Task EnqueueAsync(CollectionJob message, CancellationToken cancellationToken = default)
        {
            EnqueuedJob = message;
            return Task.CompletedTask;
        }
        public Task<CollectionJob?> DequeueAsync(TimeSpan timeout, CancellationToken cancellationToken = default) => Task.FromResult<CollectionJob?>(EnqueuedJob);
    }

    sealed class FakeRepository : IAirConditionerRepository
    {
        public AirConditionerSearchRequest? SavedRequest { get; private set; }
        public Task SaveRequestedSearchAsync(Guid searchId, AirConditionerSearchRequest request, CancellationToken cancellationToken = default)
        {
            SavedRequest = request;
            return Task.CompletedTask;
        }
        public Task SaveListingsAsync(Guid searchId, IReadOnlyCollection<AirConditionerListing> listings, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<AirConditionerSearchRequest?> GetRequestAsync(Guid searchId, CancellationToken cancellationToken = default) => Task.FromResult<AirConditionerSearchRequest?>(SavedRequest);
        public Task<IReadOnlyCollection<AirConditionerListing>> GetListingsAsync(Guid searchId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<AirConditionerListing>>([]);
        public Task SaveResultAsync(AirConditionerSearchResult result, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<AirConditionerSearchResult?> GetResultAsync(Guid searchId, CancellationToken cancellationToken = default) => Task.FromResult<AirConditionerSearchResult?>(null);
    }
}
