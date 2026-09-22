namespace PS4Remarry.Core;

public sealed class ToolLocator
{
    public string ToolsDir    { get; }
    public string WorkDir     { get; }
    public string OrbisPubCmd { get; }
    public string Gengp4Patch { get; }
    public string Gengp4App   { get; }
    public string ExtDir      { get; }

    public ToolLocator(string toolsDir)
    {
        ToolsDir    = Path.GetFullPath(toolsDir);
        WorkDir     = Path.Combine(ToolsDir, "Work");
        ExtDir      = Path.Combine(ToolsDir, "ext");
        OrbisPubCmd = Path.Combine(ToolsDir, "orbis-pub-cmd.exe");
        Gengp4Patch = Path.Combine(ToolsDir, "gengp4_patch.exe");
        Gengp4App   = Path.Combine(ToolsDir, "gengp4_app.exe");

        Directory.CreateDirectory(WorkDir);
    }

    public void EnsureToolsExist()
    {
        var missing = new List<string>();

        RequireFile(OrbisPubCmd, "orbis-pub-cmd.exe", missing);
        RequireFile(Gengp4Patch, "gengp4_patch.exe", missing);
        RequireFile(Gengp4App,   "gengp4_app.exe",   missing);

        if (!Directory.Exists(ExtDir))
        {
            missing.Add($"ext\\ folder  (expected at: {ExtDir})");
        }
        else
        {
            RequireFile(Path.Combine(ExtDir, "sc.exe"), @"ext\sc.exe", missing);
            RequireFile(Path.Combine(ExtDir, "di.exe"), @"ext\di.exe", missing);
        }

        if (missing.Count > 0)
        {
            var msg =
                "Missing required tool files. The remarry step will fail without them." +
                Environment.NewLine + Environment.NewLine +
                "Missing:" + Environment.NewLine +
                "  - " + string.Join(Environment.NewLine + "  - ", missing) +
                Environment.NewLine + Environment.NewLine +
                "Tools folder:" + Environment.NewLine +
                "  " + ToolsDir + Environment.NewLine + Environment.NewLine +
                "Copy the FULL contents of PS4-Fake-PKG-Tools-3.87-main" +
                Environment.NewLine +
                "(including the ext\\ subfolder) into the tools folder, then retry.";

            throw new FileNotFoundException(msg);
        }
    }

    private static void RequireFile(string path, string displayName, List<string> missing)
    {
        if (!File.Exists(path))
            missing.Add($"{displayName}  (expected at: {path})");
    }

    /// <summary>
    /// Resolves the tools folder. Preference order:
    ///   1. tools\ next to the running exe — the normal case.
    ///   2. A few levels up from the exe (dev-time bin\Debug layout).
    ///
    /// The result must be a valid tools folder (contains orbis-pub-cmd.exe),
    /// and must not live inside a protected Windows folder. Both checks are
    /// enforced here so the app fails loudly at startup rather than deriving
    /// a WorkDir somewhere dangerous.
    /// </summary>
    public static ToolLocator Default()
    {
        var exeDir = AppContext.BaseDirectory;
        var beside = Path.Combine(exeDir, "tools");

        if (LooksLikeToolsFolder(beside))
        {
            AssertToolsPathSafe(beside);
            return new ToolLocator(beside);
        }

        var dir = new DirectoryInfo(exeDir);
        for (int i = 0; i < 6 && dir is not null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "tools");
            if (LooksLikeToolsFolder(candidate))
            {
                AssertToolsPathSafe(candidate);
                return new ToolLocator(candidate);
            }
        }

        return new ToolLocator(beside);
    }

    /// <summary>
    /// Refuses to accept a tools folder that lives inside a protected Windows
    /// folder, unless that folder contains a marker file indicating the user
    /// has explicitly trusted it.
    /// </summary>
    private static void AssertToolsPathSafe(string toolsPath)
    {
        // Marker file opt-in. If the user has created a file called
        // ".ps4remarry-trusted" inside the tools folder, they're saying
        // "yes, I know this is inside the Desktop / Documents / whatever,
        // and I want the tool to use it anyway."
        var marker = Path.Combine(toolsPath, ".ps4remarry-trusted");
        if (File.Exists(marker))
            return;

        SafePathGuard.AssertNotProtected(toolsPath);
    }

    private static bool LooksLikeToolsFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return false;

        if (!File.Exists(Path.Combine(path, "orbis-pub-cmd.exe")))
            return false;

        return true;
    }
}