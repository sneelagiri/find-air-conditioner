namespace FindAirConditioners.Domain.Models;

public sealed record AirConditionerSearchResult(
    Guid SearchId,
    DateTimeOffset RequestedAtUtc,
    IReadOnlyCollection<AirConditionerListing> Listings,
    string Status,
    string? Summary = null);
