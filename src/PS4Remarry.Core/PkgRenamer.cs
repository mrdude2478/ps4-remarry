using System.Globalization;
using System.Text;

namespace PS4Remarry.Core;

/// <summary>
/// Renames a PS4 PKG file based on its SFO metadata, using the naming
/// convention the original batch scripts used:
///
///     &lt;Title&gt;-&lt;TitleID&gt;-&lt;Game|Update|DLC&gt;-(A&lt;AppVer&gt;-V&lt;Version&gt;).pkg
///
/// For example:
///     Call of Duty Black Ops-CUSA57547-Game-(A01.00-V01.08).pkg
/// </summary>
public static class PkgRenamer
{
    public sealed record RenameResult(
        bool Success,
        string OldPath,
        string NewPath,
        string Message);

    public static RenameResult Rename(string pkgPath)
    {
        if (!File.Exists(pkgPath))
            return new RenameResult(false, pkgPath, pkgPath, "File not found.");

        Dictionary<string, string> sfo;
        try
        {
            sfo = SfoParser.ParseToDictionary(pkgPath);
        }
        catch (Exception ex)
        {
            return new RenameResult(false, pkgPath, pkgPath,
                "Could not read PKG metadata: " + ex.Message);
        }

        string title = sfo.TryGetValue("TITLE", out var t) && !string.IsNullOrWhiteSpace(t)
                        ? t
                        : Path.GetFileNameWithoutExtension(pkgPath);

        string titleId = sfo.TryGetValue("TITLE_ID", out var id) && !string.IsNullOrWhiteSpace(id)
                        ? id
                        : TryReadTitleIdFromHeader(pkgPath);

        string appVer   = sfo.TryGetValue("APP_VER",  out var av) ? av : "";
        string version  = sfo.TryGetValue("VERSION",  out var v)  ? v  : "";
        string category = sfo.TryGetValue("CATEGORY", out var c)  ? c  : "";

        if (string.IsNullOrWhiteSpace(titleId))
            return new RenameResult(false, pkgPath, pkgPath,
                "Could not determine TITLE_ID from the PKG.");

        string label = CategoryToLabel(category);
        string safeTitle = SanitizeForFilename(title);

        string appVerClean  = StripLeading(appVer,  'A');
        string versionClean = StripLeading(version, 'V');

        var sb = new StringBuilder();
        sb.Append(safeTitle);
        sb.Append('-').Append(titleId);
        sb.Append('-').Append(label);
        if (!string.IsNullOrWhiteSpace(appVerClean) || !string.IsNullOrWhiteSpace(versionClean))
        {
            sb.Append("-(A").Append(appVerClean);
            sb.Append("-V").Append(versionClean).Append(')');
        }
        sb.Append(".pkg");

        string newFileName = sb.ToString();

        var dir = Path.GetDirectoryName(pkgPath) ?? ".";
        var newPath = Path.Combine(dir, newFileName);

        if (string.Equals(Path.GetFullPath(newPath),
                          Path.GetFullPath(pkgPath),
                          StringComparison.OrdinalIgnoreCase))
        {
            return new RenameResult(true, pkgPath, pkgPath,
                "Filename already matches the convention - nothing to rename.");
        }

        newPath = EnsureUniquePath(newPath);
        newFileName = Path.GetFileName(newPath);

        try
        {
            File.Move(pkgPath, newPath);
            return new RenameResult(true, pkgPath, newPath,
                $"Renamed to: {newFileName}");
        }
        catch (Exception ex)
        {
            return new RenameResult(false, pkgPath, pkgPath,
                "Rename failed: " + ex.Message);
        }
    }

    private static string TryReadTitleIdFromHeader(string pkgPath)
    {
        try { return PkgTitleIdReader.ReadTitleId(pkgPath); }
        catch { return ""; }
    }

    private static string CategoryToLabel(string category)
    {
        return category?.ToLowerInvariant() switch
        {
            "gd"        => "Game",
            "gp"        => "Update",
            "gde"       => "DLC",
            "gdk"       => "DLC",
            "gda"       => "DLC",
            "ac"        => "DLC",
            null or ""  => "PKG",
            _           => category
        };
    }

    private static string StripLeading(string value, char prefix)
    {
        if (string.IsNullOrEmpty(value)) return "";
        value = value.Trim();
        if (value.Length > 0 && (value[0] == prefix || value[0] == char.ToLowerInvariant(prefix)))
            value = value.Substring(1);
        return value;
    }

    private static string SanitizeForFilename(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "Untitled";

        var sb = new StringBuilder(input.Length);

        foreach (var ch in input)
        {
            if (Path.GetInvalidFileNameChars().Contains(ch) || char.IsControl(ch))
            {
                sb.Append(' ');
                continue;
            }

            switch (ch)
            {
                case '®': case '©': case '™': case '℠':
                    continue;
            }

            var cat = char.GetUnicodeCategory(ch);
            if (cat == UnicodeCategory.OtherSymbol
                || cat == UnicodeCategory.MathSymbol
                || cat == UnicodeCategory.CurrencySymbol
                || cat == UnicodeCategory.ModifierSymbol)
            {
                continue;
            }

            sb.Append(ch);
        }

        var collapsed = new StringBuilder(sb.Length);
        bool lastWasSpace = false;
        foreach (var ch in sb.ToString())
        {
            bool isSpace = ch == ' ';
            if (isSpace && lastWasSpace) continue;
            collapsed.Append(ch);
            lastWasSpace = isSpace;
        }

        var result = collapsed.ToString().Trim().TrimEnd('.');

        if (string.IsNullOrEmpty(result)) result = "Untitled";

        return result;
    }

    private static string EnsureUniquePath(string desiredPath)
    {
        if (!File.Exists(desiredPath)) return desiredPath;

        var dir  = Path.GetDirectoryName(desiredPath) ?? ".";
        var stem = Path.GetFileNameWithoutExtension(desiredPath);
        var ext  = Path.GetExtension(desiredPath);

        for (int i = 1; i < 1000; i++)
        {
            var candidate = Path.Combine(dir, $"{stem} ({i}){ext}");
            if (!File.Exists(candidate)) return candidate;
        }

        return Path.Combine(dir, $"{stem}-{Guid.NewGuid():N}{ext}");
    }
}