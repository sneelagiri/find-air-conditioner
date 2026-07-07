namespace FindAirConditioners.Domain.Models;

public sealed record AnalysisJob(Guid SearchId, DateTimeOffset RequestedAtUtc);
