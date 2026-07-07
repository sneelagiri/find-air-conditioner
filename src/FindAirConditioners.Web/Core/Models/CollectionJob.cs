namespace FindAirConditioners.Web.Core.Models;

public sealed record CollectionJob(Guid SearchId, AirConditionerSearchRequest Request, DateTimeOffset RequestedAtUtc);
