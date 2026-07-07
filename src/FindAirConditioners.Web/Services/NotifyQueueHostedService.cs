using FindAirConditioners.Web.Core.Abstractions;
using FindAirConditioners.Web.Core.Models;
using FindAirConditioners.Web.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FindAirConditioners.Web.Services;

public sealed class NotifyQueueHostedService(
    IMessageQueue<NotifyJob> queue,
    IServiceScopeFactory scopeFactory,
    ILogger<NotifyQueueHostedService> logger) : BackgroundService
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

                logger.LogInformation("Dequeued notify job for search {SearchId} and recipient {RecipientEmail}.", job.SearchId, job.RecipientEmail);
                await using var scope = scopeFactory.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<IAirConditionerRepository>();
                var sender = scope.ServiceProvider.GetRequiredService<IEmailNotificationSender>();

                var result = await repository.GetResultAsync(job.SearchId, stoppingToken);
                if (result is null)
                {
                    logger.LogWarning("Skipping notify job for search {SearchId} because no analysis result exists yet.", job.SearchId);
                    continue;
                }

                var request = await repository.GetRequestAsync(job.SearchId, stoppingToken);
                var subject = "Air conditioner search results";
                var body = NotificationEmailComposer.BuildBody(request, result);
                await sender.SendAsync(job.RecipientEmail, subject, body, stoppingToken);
                logger.LogInformation("Completed notify job for search {SearchId}.", job.SearchId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Notify worker failed.");
            }
        }
    }

}
