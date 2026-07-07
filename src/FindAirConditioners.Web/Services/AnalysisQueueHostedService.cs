using FindAirConditioners.Web.Core.Abstractions;
using FindAirConditioners.Web.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FindAirConditioners.Web.Services;

public sealed class AnalysisQueueHostedService(
    IMessageQueue<AnalysisJob> queue,
    IMessageQueue<NotifyJob> notifyQueue,
    IServiceScopeFactory scopeFactory,
    ILogger<AnalysisQueueHostedService> logger) : BackgroundService
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

                logger.LogInformation("Dequeued analysis job for search {SearchId}.", job.SearchId);
                await using var scope = scopeFactory.CreateAsyncScope();
                var analysisService = scope.ServiceProvider.GetRequiredService<IAirConditionerAnalysisService>();
                var repository = scope.ServiceProvider.GetRequiredService<IAirConditionerRepository>();
                await analysisService.AnalyzeAsync(job.SearchId, stoppingToken);
                var request = await repository.GetRequestAsync(job.SearchId, stoppingToken);
                if (!string.IsNullOrWhiteSpace(request?.NotificationEmail))
                {
                    await notifyQueue.EnqueueAsync(new NotifyJob(job.SearchId, request.NotificationEmail, DateTimeOffset.UtcNow), stoppingToken);
                    logger.LogInformation("Enqueued notify job for search {SearchId} and recipient {RecipientEmail}.", job.SearchId, request.NotificationEmail);
                }
                logger.LogInformation("Completed analysis job for search {SearchId}.", job.SearchId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Analysis worker failed.");
            }
        }
    }
}
