namespace PS4Remarry.Core;

/// <summary>What kind of PKG a param.sfo CATEGORY describes.</summary>
public enum PkgKind
{
    Game,    // gd
    Update,  // gp
    Dlc,     // ac
    Other,   // gde (homebrew), gdo (PS2 classic), anything unknown
}

/// <summary>One row of the PKG table in pkg.db.</summary>
public sealed class PkgRecord
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string TitleId { get; set; } = "";
    public string Category { get; set; } = "";
    public string Version { get; set; } = "";
    public string AppVersion { get; set; } = "";
    public string ContentId { get; set; } = "";
    public string Information { get; set; } = "";
}

/// <summary>Everything we need from one PKG file, read straight from its param.sfo.</summary>
public sealed record PkgMeta(
    string FullPath,
    string FileName,
    string Title,
    string TitleId,
    string ContentId,
    string RawCategory,
    PkgKind Kind,
    string Version,
    string AppVersion,
    string ParentTitleId);

public sealed record PkgScanProgress(int Done, int Total, string CurrentFile);

public sealed class PkgScanResult
{
    public List<PkgMeta> Metas { get; } = new();
    public List<string> Failed { get; } = new();
    public int FilesFound { get; set; }
    public bool Cancelled { get; set; }
}

/// <summary>What importing a batch of PKGs would do to the database.</summary>
public sealed class PkgImportPlan
{
    public List<PkgRecord> Inserts { get; } = new();
    /// <summary>Existing game rows whose Information text changed (id -> new text).</summary>
    public Dictionary<int, string> InfoUpdates { get; } = new();

    public int GamesAdded { get; set; }
    public int UpdatesAdded { get; set; }
    public int DlcAttached { get; set; }
    public int DuplicatesSkipped { get; set; }
    public int DlcDuplicates { get; set; }
    public int OtherSkipped { get; set; }

    public List<string> Duplicates { get; } = new();
    public List<string> UnmatchedDlc { get; } = new();
    public List<string> SkippedOther { get; } = new();

    public bool HasChanges => Inserts.Count > 0 || InfoUpdates.Count > 0;
}
