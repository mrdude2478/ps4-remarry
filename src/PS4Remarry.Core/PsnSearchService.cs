using Microsoft.Playwright;

namespace PS4Remarry.Core;

/// <summary>
/// Owns a single Chromium instance for the app's lifetime and provides a
/// self-healing install path for the Playwright browser bundle.
///
/// Thread-safety: all public methods are guarded by <see cref="_gate"/>.
/// Call <see cref="DisposeAsync"/> when the owning window closes.
/// </summary>
public sealed class PsnSearchService : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Action<string>? _log;

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private bool _installChecked;

    public PsnSearchService(Action<string>? log = null)
    {
        _log = log;
    }

    /// <summary>
    /// Runs a PSN store search with per-phase progress reporting and a
    /// region fallback: if the requested region yields no results, the
    /// search is retried once against en-us.
    /// </summary>
    public async Task<IReadOnlyList<PsnProduct>> SearchAsync(
        string query,
        string region,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var browser = await GetBrowserAsync(ct).ConfigureAwait(false);

        progress?.Report($"Searching region '{region}'...");
        var results = await PsnScraper.SearchAsync(
            browser, query, region, progress).ConfigureAwait(false);

        if (results.Count == 0 && !region.Equals("en-us", StringComparison.OrdinalIgnoreCase))
        {
            progress?.Report($"No results in '{region}', trying 'en-us'...");
            results = await PsnScraper.SearchAsync(
                browser, query, "en-us", progress).ConfigureAwait(false);
        }

        return results;
    }

    private async Task<IBrowser> GetBrowserAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_browser is not null) return _browser;

            await EnsureInstalledAsync(ct).ConfigureAwait(false);

            _playwright ??= await Playwright.CreateAsync().ConfigureAwait(false);
            _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true })
                .ConfigureAwait(false);
            return _browser;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task EnsureInstalledAsync(CancellationToken ct)
    {
        if (_installChecked) return;
        _installChecked = true;

        var cachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ms-playwright");

        if (Directory.Exists(cachePath) && Directory.EnumerateDirectories(cachePath).Any())
            return;

        _log?.Invoke("First run: downloading Playwright browser (~150 MB, one time only)...");

        Exception? installError = null;
        await Task.Run(() =>
        {
            try { Microsoft.Playwright.Program.Main(new[] { "install" }); }
            catch (Exception ex) { installError = ex; }
        }, ct).ConfigureAwait(false);

        if (installError is not null)
        {
            // Surface the real cause instead of letting LaunchAsync throw
            // a generic "executable not found" error later.
            _log?.Invoke("[playwright] install failed: " + installError.Message);
            throw new InvalidOperationException(
                "Playwright browser install failed. " +
                "Underlying error: " + installError.Message,
                installError);
        }

        _log?.Invoke("Playwright browser ready.");
    }

    public async ValueTask DisposeAsync()
    {
        try { if (_browser is not null) await _browser.CloseAsync().ConfigureAwait(false); }
        catch { }
        try { _playwright?.Dispose(); } catch { }
        _browser = null;
        _playwright = null;
        _gate.Dispose();
    }
}