using PS4Remarry.Core;

namespace PS4Remarry.WinForms;

/// <summary>
/// Shared helper for the "work folder has content, wipe it?" confirmation
/// shown before a pipeline run. Kept in the WinForms project because it
/// needs an IWin32Window owner for the message box.
/// </summary>
internal static class WorkFolderPrompt
{
    /// <summary>
    /// Returns true if the work folder is safe to wipe (empty, missing, or
    /// the user confirmed). Returns false if the user cancelled.
    /// </summary>
    public static bool ConfirmWipe(IWin32Window owner, string workDir)
    {
        try
        {
            if (!Directory.Exists(workDir)) return true;

            var (files, dirs, bytes) = SafePathGuard.MeasureDirectory(workDir);
            if (files == 0 && dirs == 0) return true;

            var answer = MessageBox.Show(owner,
                "The tool's work folder currently contains data:" + Environment.NewLine + Environment.NewLine +
                workDir + Environment.NewLine + Environment.NewLine +
                $"{files} file(s), {dirs} folder(s), {Formatting.FormatBytes(bytes)} total." + Environment.NewLine + Environment.NewLine +
                "This will be deleted before the next operation runs. Continue?",
                "Work folder will be cleared",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            return answer == DialogResult.Yes;
        }
        catch
        {
            return true;
        }
    }
}