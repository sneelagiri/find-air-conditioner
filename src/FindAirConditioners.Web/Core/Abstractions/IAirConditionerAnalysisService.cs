using FindAirConditioners.Web.Core.Models;

namespace FindAirConditioners.Web.Core.Abstractions;

public interface IAirConditionerAnalysisService
{
    Task<AirConditionerSearchResult> AnalyzeAsync(Guid searchId, CancellationToken cancellationToken = default);
}
