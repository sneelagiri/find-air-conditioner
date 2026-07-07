using FindAirConditioners.Web.Core.Models;

namespace FindAirConditioners.Web.Core.Services;

public static class NotificationEmailComposer
{
    public static string BuildBody(AirConditionerSearchRequest? request, AirConditionerSearchResult result)
    {
        var lines = new List<string>
        {
            $"Search ID: {result.SearchId}",
            $"Status: {result.Status}",
            $"Summary: {result.Summary}",
            $"Requested at: {result.RequestedAtUtc:u}",
            string.Empty
        };

        if (request is not null)
        {
            lines.Add($"Max price: {(request.MaxPrice is null ? "any" : request.MaxPrice.Value.ToString("C"))}");
            lines.Add(string.Empty);
        }

        if (result.Listings.Count == 0)
        {
            lines.Add("No listings matched the requested filters.");
            return string.Join(Environment.NewLine, lines);
        }

        lines.Add("Matching listings:");
        foreach (var listing in result.Listings)
        {
            lines.Add($"- {listing.Title} | {listing.Price:C} | {listing.Source}");
            lines.Add($"  {listing.Url}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
