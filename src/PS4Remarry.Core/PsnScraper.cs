using System.Text.Json.Serialization;
using Microsoft.Playwright;

namespace PS4Remarry.Core;

public sealed class PsnProduct
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("href")] public string? Href { get; set; }
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("titleId")] public string? TitleId { get; set; }
}

/// <summary>
/// Thrown when the store page loads but the expected DOM is missing —
/// which almost always means Sony changed the store layout.
/// Distinguishes "scraper is broken" from "no results found".
/// </summary>
public sealed class PsnScrapeException : Exception
{
    public string? PageSnippet { get; }

    public PsnScrapeException(string message, string? pageSnippet = null)
        : base(message)
    {
        PageSnippet = pageSnippet;
    }
}

/// <summary>
/// Scrapes the public PlayStation Store search page for products matching
/// a query. Returns name / href / id / titleId for each product tile.
///
/// Requires a Playwright IBrowser supplied by the caller (see
/// PsnSearchService for lifecycle management).
/// </summary>
public static class PsnScraper
{
    private const string TileSelector = "a[data-track=\"web:store:product-tile\"]";
    private const int SelectorTimeoutMs = 20_000;

    public static async Task<List<PsnProduct>> SearchAsync(
        IBrowser browser,
        string query,
        string region = "en-us",
        IProgress<string>? progress = null,
        int scrollPasses = 3)
    {
        var page = await browser.NewPageAsync();
        try
        {
            var url = $"https://store.playstation.com/{region}/search/{Uri.EscapeDataString(query)}";

            progress?.Report("Opening store page...");
            await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });

            progress?.Report("Waiting for product tiles...");
            try
            {
                await page.WaitForSelectorAsync(
                    TileSelector,
                    new() { Timeout = SelectorTimeoutMs });
            }
            catch (TimeoutException)
            {
                // Distinguish "scraper broken" from "no results". This is
                // the failure path — the store layout has probably changed.
                var snippet = await TryReadPageSnippetAsync(page);
                throw new PsnScrapeException(
                    "Store layout may have changed: no product tiles appeared " +
                    $"within {SelectorTimeoutMs / 1000}s.",
                    snippet);
            }

            progress?.Report($"Scrolling to load more results ({scrollPasses} pass(es))...");
            for (var i = 0; i < scrollPasses; i++)
            {
                await page.Mouse.WheelAsync(0, 4000);
                await page.WaitForTimeoutAsync(1200);
            }

            progress?.Report("Extracting product data...");
            const string js = @"
    els => JSON.stringify(els.map(a => {
        let meta = {};
        try { meta = JSON.parse(a.getAttribute('data-telemetry-meta') || '{}'); } catch(e) {}
        const nameEl = a.querySelector('[data-qa*=""product-name""]');
        return {
            name: nameEl ? nameEl.innerText.trim() : a.innerText.trim(),
            href: a.href,
            id: meta.id || null,
            titleId: meta.titleId || null
        };
    }))
";

            var json = await page.EvalOnSelectorAllAsync<string>(TileSelector, js);
            var items = System.Text.Json.JsonSerializer.Deserialize<List<PsnProduct>>(json ?? "[]")
                        ?? new List<PsnProduct>();

            // De-duplicate by product id (store sometimes repeats tiles).
            var seen = new HashSet<string>();
            var unique = new List<PsnProduct>();
            foreach (var item in items)
            {
                var key = item.Id ?? item.Href ?? Guid.NewGuid().ToString();
                if (seen.Add(key)) unique.Add(item);
            }
            return unique;
        }
        finally
        {
            try { await page.CloseAsync(); } catch { }
        }
    }

    private static async Task<string?> TryReadPageSnippetAsync(IPage page)
    {
        try
        {
            var text = await page.Locator("body").InnerTextAsync(
                new() { Timeout = 2000 });
            if (string.IsNullOrWhiteSpace(text)) return null;
            text = text.Trim();
            return text.Length > 500 ? text.Substring(0, 500) + "…" : text;
        }
        catch { return null; }
    }

    /// <summary>
    /// Attempts to identify the base game in a set of results.
    /// Heuristics are tried in order and the one that fired is returned
    /// in <paramref name="reason"/> for logging.
    ///
    ///   1. Tile id ending in GAME000001 — classic base-game marker.
    ///   2. Tile id containing BUNDLE — a bundle, not a base game, but
    ///      usually the closest thing when no plain game exists. Flagged
    ///      so the caller can surface it.
    ///   3. Exact name match on the query.
    ///   4. Name contains the query.
    ///   5. First result (fallback).
    /// </summary>
    public static PsnProduct? FindBaseGame(
        IReadOnlyList<PsnProduct> results,
        string query,
        out string reason)
    {
        reason = "(no results)";
        if (results.Count == 0) return null;

        foreach (var r in results)
        {
            if (r.Id?.EndsWith("GAME000001", StringComparison.OrdinalIgnoreCase) == true)
            {
                reason = "id ends with GAME000001";
                return r;
            }
        }

        foreach (var r in results)
        {
            if (r.Id?.Contains("BUNDLE", StringComparison.OrdinalIgnoreCase) == true)
            {
                reason = "id contains BUNDLE (no plain base game found)";
                return r;
            }
        }

        var q = query.ToLowerInvariant().Trim();
        if (!string.IsNullOrEmpty(q))
        {
            foreach (var r in results)
            {
                if (string.Equals(r.Name?.ToLowerInvariant().Trim(), q,
                                  StringComparison.Ordinal))
                {
                    reason = "exact name match";
                    return r;
                }
            }

            foreach (var r in results)
            {
                if (r.Name?.ToLowerInvariant().Contains(q) == true)
                {
                    reason = "name contains query";
                    return r;
                }
            }
        }

        reason = "fallback: first result";
        return results[0];
    }

    public static PsnProduct? FindBaseGame(
        IReadOnlyList<PsnProduct> results,
        string query) => FindBaseGame(results, query, out _);

    public static List<PsnProduct> GetDlc(
        IReadOnlyList<PsnProduct> results,
        string query)
    {
        var baseGame = FindBaseGame(results, query);
        if (baseGame?.TitleId is null) return new List<PsnProduct>();

        return results
            .Where(r => r.TitleId == baseGame.TitleId && r.Id != baseGame.Id)
            .ToList();
    }

    public static Dictionary<string, List<PsnProduct>> GroupByTitle(
        IReadOnlyList<PsnProduct> results)
    {
        var groups = new Dictionary<string, List<PsnProduct>>();
        foreach (var r in results)
        {
            var key = string.IsNullOrEmpty(r.TitleId) ? "_unknown" : r.TitleId!;
            if (!groups.TryGetValue(key, out var list))
            {
                list = new List<PsnProduct>();
                groups[key] = list;
            }
            list.Add(r);
        }
        return groups;
    }
}