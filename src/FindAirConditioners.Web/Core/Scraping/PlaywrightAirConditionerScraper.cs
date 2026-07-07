using System.Globalization;
using System.Text.RegularExpressions;
using FindAirConditioners.Web.Core.Abstractions;
using FindAirConditioners.Web.Core.Models;
using FindAirConditioners.Web.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace FindAirConditioners.Web.Core.Scraping;

public sealed partial class PlaywrightAirConditionerScraper(
    IOptions<ScraperOptions> options,
    SeededAirConditionerCatalog fallbackCatalog,
    ILogger<PlaywrightAirConditionerScraper> logger) : IAirConditionerScraper
{
    public async Task<IReadOnlyCollection<AirConditionerListing>> ScrapeAsync(CancellationToken cancellationToken = default)
    {
        var sources = options.Value.Sources;
        if (sources.Count == 0)
        {
            logger.LogWarning("No scraper sources configured. Falling back to the seeded catalog.");
            return fallbackCatalog.GetListings();
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize
            {
                Width = 1280,
                Height = 1600
            }
        });

        var listings = new List<AirConditionerListing>();

        foreach (var source in sources)
        {
            var url = BuildSourceUrl(source.UrlTemplate, postalCode);
            logger.LogInformation("Scraping source {SourceName} from {Url}.", source.Name, url);

            var page = await context.NewPageAsync();
            try
            {
                await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 60000
                });

                var parsedListings = await ParseSourceListingsAsync(page, source, new Uri(url, UriKind.Absolute), cancellationToken);
                logger.LogInformation("Source {SourceName} yielded {ListingCount} listings.", source.Name, parsedListings.Count);
                listings.AddRange(parsedListings);
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        return listings.OrderBy(listing => listing.Price).ToArray();
    }

    async Task<IReadOnlyCollection<AirConditionerListing>> ParseSourceListingsAsync(
        IPage page,
        ScraperSourceOptions source,
        Uri pageUri,
        CancellationToken cancellationToken)
    {
        var cards = page.Locator(source.ListingSelector);
        var count = await cards.CountAsync();
        var results = new List<AirConditionerListing>();

        for (var index = 0; index < count; index++)
        {
            var card = cards.Nth(index);
            var availabilityText = await ResolveAvailabilityTextAsync(card, source);

            if (IsUnavailable(availabilityText, source.UnavailableKeywords))
            {
                logger.LogInformation("Skipping unavailable listing from source {SourceName}: {AvailabilityText}", source.Name, availabilityText.Trim());
                continue;
            }

            var title = await ResolveTitleAsync(card, source);
            var priceText = await ResolvePriceTextAsync(card, source);
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(priceText))
            {
                continue;
            }

            if (!TryParsePrice(priceText, out var price))
            {
                logger.LogDebug("Skipping listing from source {SourceName} because price '{PriceText}' could not be parsed.", source.Name, priceText);
                continue;
            }

            var hrefTarget = await ResolveLinkTargetAsync(card, source);
            var href = await hrefTarget.GetAttributeAsync("href");
            var url = string.IsNullOrWhiteSpace(href) ? pageUri.ToString() : new Uri(pageUri, href).ToString();

            string? imageUrl = null;
            if (!string.IsNullOrWhiteSpace(source.ImageSelector))
            {
                var imageLocator = card.Locator(source.ImageSelector);
                if (await imageLocator.CountAsync() > 0)
                {
                    var src = await imageLocator.First.GetAttributeAsync("src");
                    imageUrl = string.IsNullOrWhiteSpace(src) ? null : new Uri(pageUri, src).ToString();
                }
            }

            string? notes = null;
            if (!string.IsNullOrWhiteSpace(source.NotesSelector))
            {
                var notesLocator = card.Locator(source.NotesSelector);
                if (await notesLocator.CountAsync() > 0)
                {
                    notes = (await notesLocator.First.InnerTextAsync()).Trim();
                }
            }

            results.Add(new AirConditionerListing(
                source.Name,
                title,
                price,
                url,
                imageUrl,
                notes));
        }

        return results;
    }

    static async Task<string> ResolveAvailabilityTextAsync(ILocator card, ScraperSourceOptions source)
    {
        if (!string.IsNullOrWhiteSpace(source.AvailabilitySelector))
        {
            var locator = card.Locator(source.AvailabilitySelector);
            if (await locator.CountAsync() > 0)
            {
                return await locator.First.InnerTextAsync();
            }

            return string.Empty;
        }

        return await card.InnerTextAsync();
    }

    static async Task<ILocator> ResolveLinkTargetAsync(ILocator card, ScraperSourceOptions source)
    {
        if (!string.IsNullOrWhiteSpace(source.LinkSelector))
        {
            var locator = card.Locator(source.LinkSelector);
            if (await locator.CountAsync() > 0)
            {
                return locator.First;
            }
        }

        var anchors = card.Locator("a");
        if (await anchors.CountAsync() > 0)
        {
            return anchors.First;
        }

        return card;
    }

    static async Task<string> ResolveTitleAsync(ILocator card, ScraperSourceOptions source)
    {
        if (!string.IsNullOrWhiteSpace(source.TitleSelector))
        {
            var locator = card.Locator(source.TitleSelector);
            if (await locator.CountAsync() > 0)
            {
                return (await locator.First.InnerTextAsync()).Trim();
            }

            return string.Empty;
        }

        var anchors = card.Locator("a");
        if (await anchors.CountAsync() > 0)
        {
            var text = (await anchors.First.InnerTextAsync()).Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        var lines = (await card.InnerTextAsync())
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var banned = new[]
        {
            "out of stock",
            "sold out",
            "unavailable",
            "not available",
            "niet beschikbaar",
            "niet beschibaar",
            "niet op voorraad",
            "uitverkocht"
        };

        foreach (var line in lines)
        {
            var normalized = line.ToLowerInvariant();
            if (normalized.StartsWith("£", StringComparison.Ordinal) ||
                normalized.StartsWith("€", StringComparison.Ordinal) ||
                normalized.StartsWith("$", StringComparison.Ordinal) ||
                banned.Any(keyword => normalized.Contains(keyword, StringComparison.Ordinal)))
            {
                continue;
            }

            return line;
        }

        return string.Empty;
    }

    static async Task<string> ResolvePriceTextAsync(ILocator card, ScraperSourceOptions source)
    {
        if (!string.IsNullOrWhiteSpace(source.PriceSelector))
        {
            var locator = card.Locator(source.PriceSelector);
            if (await locator.CountAsync() > 0)
            {
                return (await locator.First.InnerTextAsync()).Trim();
            }

            return string.Empty;
        }

        var text = await card.InnerTextAsync();
        var match = Regex.Match(text, @"[£€$]\s*\d[\d.,]*");
        return match.Success ? match.Value : string.Empty;
    }

    static string BuildSourceUrl(string urlTemplate, string postalCode)
        => urlTemplate.Replace("{postalCode}", Uri.EscapeDataString(postalCode), StringComparison.OrdinalIgnoreCase);

    static bool IsUnavailable(string? text, IReadOnlyCollection<string> unavailableKeywords)
    {
        if (string.IsNullOrWhiteSpace(text) || unavailableKeywords.Count == 0)
        {
            return false;
        }

        var normalizedText = text.ToLowerInvariant();
        foreach (var keyword in unavailableKeywords)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            if (normalizedText.Contains(keyword.Trim().ToLowerInvariant(), StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    [GeneratedRegex(@"[^\d,.\-]")]
    private static partial Regex PriceCleanupRegex();

    static bool TryParsePrice(string rawPrice, out decimal price)
    {
        var cleaned = PriceCleanupRegex().Replace(rawPrice, string.Empty);
        cleaned = cleaned.Replace(" ", string.Empty);

        if (decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out price))
        {
            return true;
        }

        var normalized = cleaned.Replace(".", string.Empty).Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out price);
    }
}
