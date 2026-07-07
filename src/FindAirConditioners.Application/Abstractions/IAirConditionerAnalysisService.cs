using FindAirConditioners.Domain.Models;

namespace FindAirConditioners.Application.Abstractions;

public interface IAirConditionerAnalysisService
{
    Task<AirConditionerSearchResult> AnalyzeAsync(Guid searchId, CancellationToken cancellationToken = default);
}
