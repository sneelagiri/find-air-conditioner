using FluentAssertions;
using FindAirConditioners.Web.Core.Models;
using FindAirConditioners.Web.Core.Services;

namespace FindAirConditioners.UnitTests;

public sealed class NotificationEmailComposerTests
{
    [Fact]
    public void BuildBody_includes_request_and_listings()
    {
        var request = new AirConditionerSearchRequest(700m, "person@example.com");
        var result = new AirConditionerSearchResult(
            Guid.NewGuid(),
            DateTimeOffset.Parse("2026-07-07T12:00:00Z"),
            [
                new("Joybuy", "AC One", 499m, "https://example.com/1"),
                new("Joybuy", "AC Two", 699m, "https://example.com/2")
            ],
            "Completed",
            "Found 2 matching listings.");

        var body = NotificationEmailComposer.BuildBody(request, result);

        body.Should().Contain("Max price:");
        body.Should().Contain("Matching listings:");
        body.Should().Contain("AC One");
        body.Should().Contain("AC Two");
    }
}
