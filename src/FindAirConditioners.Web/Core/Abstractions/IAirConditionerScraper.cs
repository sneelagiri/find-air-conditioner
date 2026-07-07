using FindAirConditioners.Web.Core.Models;

namespace FindAirConditioners.Web.Core.Abstractions;

public interface IAirConditionerScraper
{
    Task<IReadOnlyCollection<AirConditionerListing>> ScrapeAsync(CancellationToken cancellationToken = default);
}
