using FindAirConditioners.Application.Abstractions;
using FindAirConditioners.Domain.Models;

namespace FindAirConditioners.Infrastructure.Services;

public sealed class AnalysisWorker(
    IMessageQueue<AnalysisJob> queue,
    IAirConditionerAnalysisService analysisService)
{
    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        var job = await queue.DequeueAsync(TimeSpan.FromSeconds(1), cancellationToken);
        if (job is null)
        {
            return;
        }

        await analysisService.AnalyzeAsync(job.SearchId, cancellationToken);
    }
}
