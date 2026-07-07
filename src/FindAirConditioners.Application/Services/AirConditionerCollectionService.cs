using FindAirConditioners.Application.Abstractions;
using FindAirConditioners.Domain.Models;

namespace FindAirConditioners.Application.Services;

public sealed class AirConditionerCollectionService(
    IMessageQueue<CollectionJob> collectQueue,
    IAirConditionerRepository repository) : IAirConditionerCollectionService
{
    public async Task<Guid> QueueCollectionAsync(AirConditionerSearchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var searchId = Guid.NewGuid();
        var job = new CollectionJob(searchId, request, DateTimeOffset.UtcNow);

        await repository.SaveRequestedSearchAsync(searchId, request, cancellationToken);
        await collectQueue.EnqueueAsync(job, cancellationToken);

        return searchId;
    }
}
