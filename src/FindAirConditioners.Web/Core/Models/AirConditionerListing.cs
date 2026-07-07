namespace FindAirConditioners.Web.Core.Models;

public sealed record AirConditionerListing(
    string Source,
    string Title,
    decimal Price,
    string Url,
    string? ImageUrl = null,
    string? Notes = null);
