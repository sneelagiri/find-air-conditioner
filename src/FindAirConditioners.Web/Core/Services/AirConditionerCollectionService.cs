using FindAirConditioners.Web.Core.Abstractions;
using FindAirConditioners.Web.Core.Models;
using Microsoft.Extensions.Logging;

namespace FindAirConditioners.Web.Core.Services;

public sealed class AirConditionerCollectionService(
    IMessageQueue<CollectionJob> collectQueue,
    IAirConditionerRepository repository,
    ILogger<AirConditionerCollectionService> logger) : IAirConditionerCollectionService
{
    public async Task<Guid> QueueCollectionAsync(AirConditionerSearchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var searchId = Guid.NewGuid();
        var job = new CollectionJob(searchId, request, DateTimeOffset.UtcNow);

        logger.LogInformation("Queueing collection job {SearchId} with max price {MaxPrice}.", searchId, request.MaxPrice);
        await repository.SaveRequestedSearchAsync(searchId, request, cancellationToken);
        await collectQueue.EnqueueAsync(job, cancellationToken);
        logger.LogInformation("Collection job {SearchId} enqueued.", searchId);

        return searchId;
    }
}
