using FindAirConditioners.Web.Core.Models;

namespace FindAirConditioners.Web.Core.Options;

public sealed class SchedulerOptions
{
    public bool Enabled { get; set; } = true;

    public List<SchedulerSubscription> Subscriptions { get; set; } = [];

    public int IntervalMinutes { get; set; } = 60;
}
