namespace FindAirConditioners.Domain.Models;

public sealed record AirConditionerSearchRequest(
    decimal? MaxPrice = null,
    string? NotificationEmail = null);
