namespace PS4Remarry.Core;

/// <summary>
/// Reads and writes the metadata sidecar file that we drop into an extract
/// folder during extraction. This tells the repack step what kind of PKG
/// the folder came from, whether a keystone is expected, and where the
/// original came from.
/// </summary>
public sealed class RepackExtractLog
{
    public const string FileName = ".ps4repack.log";

    public string SourcePkg { get; set; } = "";
    public long SourceFileSize { get; set; }
    public string TitleId { get; set; } = "";
    public string ContentId { get; set; } = "";
    public string Category { get; set; } = "";
    public string CategoryLabel { get; set; } = "";
    public bool KeystoneNeeded { get; set; }
    public bool KeystoneFound { get; set; }
    public DateTime ExtractedAt { get; set; } = DateTime.Now;
    public string ExtractTool { get; set; } = "";

    public static bool Exists(string extractFolder) =>
        File.Exists(Path.Combine(extractFolder, FileName));

    public static RepackExtractLog? TryRead(string extractFolder)
    {
        var path = Path.Combine(extractFolder, FileName);
        if (!File.Exists(path)) return null;

        try
        {
            var lines = File.ReadAllLines(path);
            var log = new RepackExtractLog();

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                int colon = line.IndexOf(':');
                if (colon <= 0) continue;

                var key = line.Substring(0, colon).Trim();
                var value = line.Substring(colon + 1).Trim();

                switch (key)
                {
                    case "Source PKG":       log.SourcePkg = value; break;
                    case "Source file size":
                        if (long.TryParse(value.Split(' ')[0], out var sz)) log.SourceFileSize = sz;
                        break;
                    case "TITLE_ID":         log.TitleId = value; break;
                    case "CONTENT_ID":       log.ContentId = value; break;
                    case "CATEGORY":         log.Category = value; break;
                    case "Category label":   log.CategoryLabel = value; break;
                    case "Keystone needed":  log.KeystoneNeeded = ParseYesNo(value); break;
                    case "Keystone found":   log.KeystoneFound = ParseYesNo(value); break;
                    case "Extracted at":
                        if (DateTime.TryParse(value, out var dt)) log.ExtractedAt = dt;
                        break;
                    case "Extract tool":     log.ExtractTool = value; break;
                }
            }
            return log;
        }
        catch
        {
            return null;
        }
    }

    public void Write(string extractFolder)
    {
        var path = Path.Combine(extractFolder, FileName);
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("PS4 Repack Extract Log");
        sb.AppendLine("======================");
        sb.AppendLine($"Source PKG:       {SourcePkg}");
        sb.AppendLine($"Source file size: {SourceFileSize} bytes");
        sb.AppendLine($"TITLE_ID:         {TitleId}");
        sb.AppendLine($"CONTENT_ID:       {ContentId}");
        sb.AppendLine($"CATEGORY:         {Category}");
        sb.AppendLine($"Category label:   {CategoryLabel}");
        sb.AppendLine($"Keystone needed:  {(KeystoneNeeded ? "yes" : "no")}");
        sb.AppendLine($"Keystone found:   {(KeystoneFound ? "yes" : "no")}");
        sb.AppendLine($"Extracted at:     {ExtractedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Extract tool:     {ExtractTool}");

        File.WriteAllText(path, sb.ToString());
    }

    private static bool ParseYesNo(string value) =>
        value.Equals("yes", StringComparison.OrdinalIgnoreCase);
}