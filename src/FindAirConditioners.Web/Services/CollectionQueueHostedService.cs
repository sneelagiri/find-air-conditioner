using FindAirConditioners.Web.Core.Abstractions;
using FindAirConditioners.Web.Core.Models;
using FindAirConditioners.Web.Core.Scraping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FindAirConditioners.Web.Services;

public sealed class CollectionQueueHostedService(
    IMessageQueue<CollectionJob> queue,
    IMessageQueue<AnalysisJob> analysisQueue,
    IAirConditionerScraper scraper,
    IServiceScopeFactory scopeFactory,
    ILogger<CollectionQueueHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await queue.DequeueAsync(TimeSpan.FromSeconds(1), stoppingToken);
                if (job is null)
                {
                    continue;
                }

                logger.LogInformation("Dequeued collection job {SearchId}.", job.SearchId);
                await using var scope = scopeFactory.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<IAirConditionerRepository>();
                var listings = await scraper.ScrapeAsync(stoppingToken);
                logger.LogInformation("Found {ListingCount} scraped listings for search {SearchId}.", listings.Count, job.SearchId);
                await repository.SaveListingsAsync(job.SearchId, listings, stoppingToken);
                logger.LogInformation("Stored {ListingCount} listings for search {SearchId}.", listings.Count, job.SearchId);
                await analysisQueue.EnqueueAsync(new AnalysisJob(job.SearchId, DateTimeOffset.UtcNow), stoppingToken);
                logger.LogInformation("Enqueued analysis job for search {SearchId}.", job.SearchId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Collection worker failed.");
            }
        }
    }
}
