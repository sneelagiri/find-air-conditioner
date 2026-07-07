using FindAirConditioners.Application.Abstractions;
using FindAirConditioners.Application.Services;
using FindAirConditioners.Domain.Models;

namespace FindAirConditioners.Infrastructure.Services;

public sealed class CollectionWorker(
    IMessageQueue<CollectionJob> queue,
    IMessageQueue<AnalysisJob> analysisQueue,
    IAirConditionerRepository repository,
    SeededAirConditionerCatalog catalog)
{
    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        var job = await queue.DequeueAsync(TimeSpan.FromSeconds(1), cancellationToken);
        if (job is null)
        {
            return;
        }

        var listings = catalog.GetListings();
        await repository.SaveListingsAsync(job.SearchId, listings, cancellationToken);
        await analysisQueue.EnqueueAsync(new AnalysisJob(job.SearchId, DateTimeOffset.UtcNow), cancellationToken);
    }
}
