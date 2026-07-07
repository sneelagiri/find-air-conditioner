namespace FindAirConditioners.Web.Core.Models;

public sealed record NotifyJob(
    Guid SearchId,
    string RecipientEmail,
    DateTimeOffset RequestedAtUtc);
