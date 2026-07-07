namespace FindAirConditioners.Web.Core.Models;

public sealed record SchedulerSubscription(
    decimal? MaxPrice = null,
    string? NotificationEmail = null);
