namespace FindAirConditioners.Web.Core.Scraping;

public sealed class ScraperSourceOptions
{
    public string Name { get; set; } = string.Empty;

    public string UrlTemplate { get; set; } = string.Empty;

    public string ListingSelector { get; set; } = string.Empty;

    public string TitleSelector { get; set; } = string.Empty;

    public string PriceSelector { get; set; } = string.Empty;

    public string? LinkSelector { get; set; }

    public string? ImageSelector { get; set; }

    public string? NotesSelector { get; set; }

    public string? AvailabilitySelector { get; set; }

    public List<string> UnavailableKeywords { get; set; } = [];
}
