namespace FindAirConditioners.Domain.Models;

public sealed record CollectionJob(Guid SearchId, AirConditionerSearchRequest Request, DateTimeOffset RequestedAtUtc);
