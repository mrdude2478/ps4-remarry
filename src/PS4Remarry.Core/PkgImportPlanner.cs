namespace PS4Remarry.Core;

/// <summary>
/// Pure decision logic (no file or database access): given what is already in the
/// database and a batch of PKGs, work out what to insert / append.
///
///  - Only Game (gd) and Update (gp) PKGs become rows.
///  - A row is a duplicate when Title ID + Category + Version + App Version already exist.
///  - DLC (ac) PKGs never get a row; their file name is appended (one per line) to the
///    Information text of the parent game. Already-listed names are ignored.
/// </summary>
public static class PkgImportPlanner
{
    public static PkgImportPlan Plan(IReadOnlyList<PkgRecord> existing, IEnumerable<PkgMeta> metas)
    {
        var plan = new PkgImportPlan();
        int nextId = existing.Count == 0 ? 1 : existing.Max(r => r.Id) + 1;

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var games = new Dictionary<string, PkgRecord>(StringComparer.OrdinalIgnoreCase);
        var newIds = new HashSet<int>();

        foreach (var r in existing.OrderBy(r => r.Id))
        {
            keys.Add(Key(r.TitleId, r.Category, r.Version, r.AppVersion));
            if (r.Category.Equals("Game", StringComparison.OrdinalIgnoreCase))
                games.TryAdd(r.TitleId, r);
        }

        var list = metas.ToList();

        // ---- Pass 1: games and updates -------------------------------------------------
        foreach (var m in list.Where(m => m.Kind is PkgKind.Game or PkgKind.Update)
                              .OrderBy(m => m.FileName, StringComparer.OrdinalIgnoreCase))
        {
            var category = m.Kind == PkgKind.Game ? "Game" : "Update";
            var appVer = string.IsNullOrWhiteSpace(m.AppVersion) ? "01.00" : m.AppVersion;

            if (!keys.Add(Key(m.TitleId, category, m.Version, appVer)))
            {
                plan.DuplicatesSkipped++;
                plan.Duplicates.Add(m.FileName);
                continue;
            }

            var rec = new PkgRecord
            {
                Id = nextId++,
                Title = m.Title,
                TitleId = m.TitleId,
                Category = category,
                Version = m.Version,
                AppVersion = appVer,
                ContentId = m.ContentId,
                Information = "",
            };
            plan.Inserts.Add(rec);
            newIds.Add(rec.Id);

            if (m.Kind == PkgKind.Game)
            {
                games.TryAdd(rec.TitleId, rec);
                plan.GamesAdded++;
            }
            else plan.UpdatesAdded++;
        }

        // ---- Pass 2: DLC (after all games are known, so scan order doesn't matter) -------
        var infoText = new Dictionary<int, string>();
        var changed = new HashSet<int>();

        foreach (var m in list.Where(m => m.Kind == PkgKind.Dlc)
                              .OrderBy(m => m.FileName, StringComparer.OrdinalIgnoreCase))
        {
            PkgRecord? parent = null;
            foreach (var cand in new[] { m.ParentTitleId, m.TitleId })
            {
                if (!string.IsNullOrEmpty(cand) && games.TryGetValue(cand, out parent)) break;
                parent = null;
            }

            if (parent is null)
            {
                plan.UnmatchedDlc.Add($"{m.FileName}  (parent game {m.ParentTitleId} not in database)");
                continue;
            }

            if (!infoText.TryGetValue(parent.Id, out var text))
                text = parent.Information ?? "";

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(l => l.Trim());
            if (lines.Contains(m.FileName, StringComparer.OrdinalIgnoreCase))
            {
                plan.DlcDuplicates++;
                plan.Duplicates.Add(m.FileName);
                continue;
            }

            text = text.Length == 0 ? m.FileName : text.TrimEnd('\r', '\n') + "\n" + m.FileName;
            infoText[parent.Id] = text;
            changed.Add(parent.Id);
            plan.DlcAttached++;
        }

        foreach (var id in changed)
        {
            if (newIds.Contains(id))
                plan.Inserts.First(r => r.Id == id).Information = infoText[id];
            else
                plan.InfoUpdates[id] = infoText[id];
        }

        // ---- Everything else ------------------------------------------------------------
        foreach (var m in list.Where(m => m.Kind == PkgKind.Other))
        {
            plan.OtherSkipped++;
            plan.SkippedOther.Add($"{m.FileName}  (category '{m.RawCategory}')");
        }

        return plan;
    }

    public static bool IsDuplicate(IEnumerable<PkgRecord> existing, PkgRecord candidate)
    {
        var key = Key(candidate.TitleId, candidate.Category, candidate.Version, candidate.AppVersion);
        return existing.Any(r => string.Equals(
            Key(r.TitleId, r.Category, r.Version, r.AppVersion), key, StringComparison.OrdinalIgnoreCase));
    }

    // Old databases stored versions as "V01.00" / "A01.05"; ignore that prefix when comparing.
    private static string NormVer(string v)
    {
        v = (v ?? "").Trim();
        if (v.Length > 1 && (v[0] is 'V' or 'v' or 'A' or 'a') && char.IsDigit(v[1])) v = v.Substring(1);
        return v;
    }

    private static string Key(string titleId, string category, string version, string appVersion) =>
        $"{(titleId ?? "").Trim()}|{(category ?? "").Trim()}|{NormVer(version)}|{NormVer(appVersion)}";
}
