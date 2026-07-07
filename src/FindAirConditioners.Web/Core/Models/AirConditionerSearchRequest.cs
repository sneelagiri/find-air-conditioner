namespace FindAirConditioners.Web.Core.Models;

public sealed record AirConditionerSearchRequest(
    decimal? MaxPrice = null,
    string? NotificationEmail = null);
