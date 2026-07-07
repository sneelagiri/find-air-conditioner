using FindAirConditioners.Web.Core.Abstractions;
using FindAirConditioners.Web.Core.Models;
using FindAirConditioners.Web.Core.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FindAirConditioners.Web.Services;

public sealed class HourlySearchSchedulerHostedService(
    IOptions<SchedulerOptions> options,
    IAirConditionerCollectionService collectionService,
    ILogger<HourlySearchSchedulerHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var schedulerOptions = options.Value;
        if (!schedulerOptions.Enabled || schedulerOptions.Subscriptions.Count == 0)
        {
            logger.LogInformation("Hourly search scheduler is disabled or has no subscriptions configured.");
            return;
        }

        await RunOnceAsync(schedulerOptions, stoppingToken);

        var interval = TimeSpan.FromMinutes(Math.Max(1, schedulerOptions.IntervalMinutes));
        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnceAsync(schedulerOptions, stoppingToken);
        }
    }

    async Task RunOnceAsync(SchedulerOptions schedulerOptions, CancellationToken cancellationToken)
    {
        foreach (var subscription in schedulerOptions.Subscriptions)
        {
            var request = new AirConditionerSearchRequest(
                subscription.MaxPrice,
                subscription.NotificationEmail);

            try
            {
                var searchId = await collectionService.QueueCollectionAsync(request, cancellationToken);
                logger.LogInformation("Queued scheduled search {SearchId}.", searchId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to queue scheduled search.");
            }
        }
    }
}
