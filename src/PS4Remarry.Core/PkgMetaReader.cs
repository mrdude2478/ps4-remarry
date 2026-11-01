using System.Globalization;
using System.Text.RegularExpressions;

namespace PS4Remarry.Core;

/// <summary>
/// Reads the fields the PKG database needs from a PS4 PKG (or a bare param.sfo).
/// Replaces the old external sfo.exe + text scraping.
/// </summary>
public static class PkgMetaReader
{
    // e.g. UP0001-CUSA12345_00-ABCDEFGHIJKLMNOP  -> CUSA12345
    private static readonly Regex ContentIdRx =
        new(@"^[A-Z]{2}\d{4}-(?<tid>[A-Z]{4}\d{5})_\d{2}-", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static PkgMeta Read(string path)
    {
        var sfo = SfoParser.ParseToDictionary(path);

        string Get(string key) =>
            sfo.TryGetValue(key, out var v) ? v.Trim('\0', ' ', '\r', '\n') : "";

        var raw = Get("CATEGORY");
        var contentId = Get("CONTENT_ID");

        var titleId = Get("TITLE_ID");
        if (titleId.Length == 0)
        {
            try { titleId = PkgTitleIdReader.ReadTitleId(path); } catch { }
        }

        // For DLC the CUSA id inside the CONTENT_ID is the parent game's id.
        var parent = titleId;
        var m = ContentIdRx.Match(contentId);
        if (m.Success) parent = m.Groups["tid"].Value.ToUpperInvariant();

        return new PkgMeta(
            FullPath: path,
            FileName: Path.GetFileName(path),
            Title: CleanTitle(Get("TITLE")),
            TitleId: titleId.ToUpperInvariant(),
            ContentId: contentId,
            RawCategory: raw,
            Kind: KindFromRaw(raw),
            Version: Get("VERSION"),
            AppVersion: Get("APP_VER"),
            ParentTitleId: parent);
    }

    public static PkgKind KindFromRaw(string raw) => raw.Trim().ToLowerInvariant() switch
    {
        "gd" => PkgKind.Game,
        "gp" => PkgKind.Update,
        "ac" => PkgKind.Dlc,
        _ => PkgKind.Other,
    };

    /// <summary>gd -> Game, gp -> Update ... (anything else is returned unchanged).</summary>
    public static string DisplayCategory(string raw) => raw.Trim().ToLowerInvariant() switch
    {
        "gd" => "Game",
        "gp" => "Update",
        "ac" => "DLC",
        "gde" => "Homebrew",
        "gdo" => "PS2 Classic",
        _ => raw.Trim(),
    };

    /// <summary>Kind for text typed in the Category box ("Game", "gd", "DLC" ...).</summary>
    public static PkgKind KindFromDisplay(string text) => text.Trim().ToLowerInvariant() switch
    {
        "game" or "gd" => PkgKind.Game,
        "update" or "gp" => PkgKind.Update,
        "dlc" or "ac" => PkgKind.Dlc,
        _ => PkgKind.Other,
    };

    /// <summary>Same tidy-up the old tool did: strip (R)/TM, underscores, Title Case.</summary>
    public static string CleanTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "";
        var t = title.Replace("®", "").Replace("™", "").Replace('_', ' ');
        t = Regex.Replace(t, @"\s+", " ").Trim();
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(t.ToLowerInvariant());
    }
}
