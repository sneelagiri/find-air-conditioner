namespace FindAirConditioners.Web.Core.Models;

public sealed record AnalysisJob(Guid SearchId, DateTimeOffset RequestedAtUtc);
