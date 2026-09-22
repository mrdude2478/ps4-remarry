namespace PS4Remarry.Core;

public enum IntegrityLevel { Ok, Warn, Error }

public sealed record IntegrityItem(IntegrityLevel Level, string Message);

public sealed record IntegrityReport(IReadOnlyList<IntegrityItem> Items)
{
    public bool HasErrors   => Items.Any(i => i.Level == IntegrityLevel.Error);
    public bool HasWarnings => Items.Any(i => i.Level == IntegrityLevel.Warn);
    public int  ErrorCount  => Items.Count(i => i.Level == IntegrityLevel.Error);
    public int  WarnCount   => Items.Count(i => i.Level == IntegrityLevel.Warn);
    public int  OkCount     => Items.Count(i => i.Level == IntegrityLevel.Ok);
}

/// <summary>
/// Walks an extract folder and reports on whether it looks like a valid
/// PS4 PKG source tree. Used as a pre-repack sanity check.
/// </summary>
public static class PkgIntegrityChecker
{
    public static IntegrityReport Check(string extractFolder)
    {
        var items = new List<IntegrityItem>();

        if (!Directory.Exists(extractFolder))
        {
            items.Add(new(IntegrityLevel.Error, "Extract folder does not exist."));
            return new IntegrityReport(items);
        }

        var folderName = new DirectoryInfo(extractFolder).Name;
        var sceSys     = Path.Combine(extractFolder, "sce_sys");
        var paramSfo   = Path.Combine(sceSys, "param.sfo");
        var icon0      = Path.Combine(sceSys, "icon0.png");
        var keystone   = Path.Combine(sceSys, "keystone");
        var eboot      = Path.Combine(extractFolder, "eboot.bin");
        var sceModule  = Path.Combine(extractFolder, "sce_module");
        var sc0        = Path.Combine(extractFolder, "Sc0");
        var nestedImg  = Path.Combine(extractFolder, "Image0");

        // ---- 1. sce_sys/param.sfo ----
        Dictionary<string, string>? sfo = null;
        string category = "";
        string titleIdInSfo = "";

        if (!File.Exists(paramSfo))
        {
            items.Add(new(IntegrityLevel.Error,
                "sce_sys\\param.sfo is missing. The PKG cannot be built without it."));
        }
        else
        {
            items.Add(new(IntegrityLevel.Ok, "sce_sys\\param.sfo present."));

            try
            {
                sfo = SfoParser.ParseToDictionary(paramSfo);
                items.Add(new(IntegrityLevel.Ok, "param.sfo parsed successfully."));
            }
            catch (Exception ex)
            {
                items.Add(new(IntegrityLevel.Error,
                    "param.sfo is not readable: " + ex.Message));
            }

            if (sfo is not null)
            {
                if (sfo.TryGetValue("CATEGORY", out var c)) category = c.Trim();
                if (sfo.TryGetValue("TITLE_ID", out var t)) titleIdInSfo = t.Trim();
            }

            bool keystonePresent = File.Exists(keystone);
            bool isBaseGame = category.Equals("gd", StringComparison.OrdinalIgnoreCase);

            if (isBaseGame && !keystonePresent)
            {
                items.Add(new(IntegrityLevel.Error,
                    "Base game (CATEGORY=gd) but sce_sys\\keystone is missing. " +
                    "Saves will be broken by the resulting PKG."));
            }
            else if (isBaseGame && keystonePresent)
            {
                items.Add(new(IntegrityLevel.Ok, "sce_sys\\keystone present (required for base game)."));
            }
            else if (!isBaseGame && keystonePresent)
            {
                items.Add(new(IntegrityLevel.Warn,
                    $"sce_sys\\keystone present but CATEGORY='{category}' is not a base game. " +
                    "Unusual but usually harmless."));
            }
            else
            {
                items.Add(new(IntegrityLevel.Ok,
                    $"sce_sys\\keystone not present (not required for CATEGORY='{category}')."));
            }

            var log = RepackExtractLog.TryRead(extractFolder);
            if (log is not null)
            {
                if (!string.IsNullOrWhiteSpace(log.Category) &&
                    !string.Equals(log.Category, category, StringComparison.OrdinalIgnoreCase))
                {
                    items.Add(new(IntegrityLevel.Warn,
                        $"Sidecar log says CATEGORY='{log.Category}' but param.sfo says '{category}'. " +
                        "The param.sfo may have been edited."));
                }

                if (log.KeystoneNeeded && !keystonePresent)
                {
                    items.Add(new(IntegrityLevel.Warn,
                        "Sidecar log says this is a base game, but keystone is now missing."));
                }
            }

            if (!string.IsNullOrEmpty(titleIdInSfo) && !string.IsNullOrEmpty(folderName))
            {
                if (folderName.IndexOf(titleIdInSfo, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    items.Add(new(IntegrityLevel.Warn,
                        $"Extract folder name '{folderName}' does not contain the TITLE_ID " +
                        $"'{titleIdInSfo}' from param.sfo. Renaming the folder may confuse " +
                        "the GP4 generator."));
                }
                else
                {
                    items.Add(new(IntegrityLevel.Ok,
                        $"Folder name contains TITLE_ID '{titleIdInSfo}'."));
                }
            }
        }

        // ---- eboot.bin ----
        if (File.Exists(eboot))
        {
            var size = new FileInfo(eboot).Length;
            items.Add(new(IntegrityLevel.Ok, $"eboot.bin present ({FormatBytes(size)})."));
        }
        else
        {
            var log = RepackExtractLog.TryRead(extractFolder);
            bool likelyGameOrUpdate = log is null
                || log.Category.Equals("gd", StringComparison.OrdinalIgnoreCase)
                || log.Category.Equals("gp", StringComparison.OrdinalIgnoreCase);

            items.Add(new(
                likelyGameOrUpdate ? IntegrityLevel.Error : IntegrityLevel.Warn,
                "eboot.bin not found in the extract folder root."));
        }

        // ---- sce_module/ ----
        if (Directory.Exists(sceModule))
        {
            var count = Directory.GetFiles(sceModule, "*.prx", SearchOption.TopDirectoryOnly).Length;
            items.Add(new(IntegrityLevel.Ok, $"sce_module\\ present ({count} .prx files)."));
        }
        else
        {
            items.Add(new(IntegrityLevel.Warn,
                "sce_module\\ folder not found. Some games need it; a few don't."));
        }

        // ---- Leftover Sc0 / Image0 ----
        if (Directory.Exists(sc0))
        {
            items.Add(new(IntegrityLevel.Error,
                "Leftover Sc0\\ folder found in the extract root. The extraction " +
                "was not normalised — repack would produce a broken PKG."));
        }

        if (Directory.Exists(nestedImg))
        {
            items.Add(new(IntegrityLevel.Error,
                "Nested Image0\\ folder found in the extract root. Repack expects " +
                "the contents of Image0 directly in the extract folder."));
        }

        // ---- icon0.png ----
        if (File.Exists(icon0))
        {
            var size = new FileInfo(icon0).Length;
            items.Add(new(IntegrityLevel.Ok, $"sce_sys\\icon0.png present ({FormatBytes(size)})."));
        }
        else
        {
            items.Add(new(IntegrityLevel.Warn,
                "sce_sys\\icon0.png not found. Optional, but most games have one."));
        }

        return new IntegrityReport(items);
    }

    private static string FormatBytes(long bytes)
    {
        const long KB = 1024, MB = KB * 1024, GB = MB * 1024;
        if (bytes >= GB) return $"{bytes / (double)GB:0.00} GB";
        if (bytes >= MB) return $"{bytes / (double)MB:0.00} MB";
        if (bytes >= KB) return $"{bytes / (double)KB:0.00} KB";
        return $"{bytes} bytes";
    }
}