using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace PS4Remarry.Core;

/// <summary>
/// Async wrapper around System.Diagnostics.Process with live stdout/stderr
/// streaming, cancellation support, and console-window hiding.
///
/// The hide is necessary because orbis-pub-cmd.exe and friends are console
/// applications that (in some builds) call AllocConsole() themselves, which
/// bypasses CreateNoWindow. We hide the resulting window explicitly with
/// ShowWindow(SW_HIDE) right after the process starts.
/// </summary>
public sealed class ProcessRunner
{
    // ---------- Win32 interop for hiding the console window ----------

    private const int SW_HIDE = 0;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    // ---------- public API ----------

    public async Task<int> RunAsync(
        string exe,
        IEnumerable<string> args,
        string workingDir,
        IProgress<ProgressReport>? progress = null,
        CancellationToken ct = default)
    {
        if (!File.Exists(exe))
            throw new FileNotFoundException($"Tool not found: {exe}");

        var psi = new ProcessStartInfo
        {
            FileName               = exe,
            WorkingDirectory       = workingDir,
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding  = Encoding.UTF8,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        p.Exited += (_, _) => tcs.TrySetResult(p.ExitCode);

        progress?.Report(ProgressReport.Log($"> {Path.GetFileName(exe)} {string.Join(' ', args)}"));

        if (!p.Start())
            throw new InvalidOperationException($"Failed to start {exe}");

        _ = HideConsoleWhenReadyAsync(p, ct);

        using var reg = ct.Register(() =>
        {
            try { if (!p.HasExited) p.Kill(entireProcessTree: true); }
            catch { /* best effort */ }
            tcs.TrySetCanceled(ct);
        });

        var stdoutTask = PumpStreamAsync(p.StandardOutput, progress, ct);
        var stderrTask = PumpStreamAsync(p.StandardError,  progress, ct);

        var exitCode = await tcs.Task.ConfigureAwait(false);

        try { await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false); }
        catch { /* best effort */ }

        return exitCode;
    }

    // ---------- console hiding ----------

    private static async Task HideConsoleWhenReadyAsync(Process p, CancellationToken ct)
    {
        try
        {
            await Task.Delay(50, ct).ConfigureAwait(false);
            if (!p.HasExited) HideProcessWindows(p);

            await Task.Delay(250, ct).ConfigureAwait(false);
            if (!p.HasExited) HideProcessWindows(p);
        }
        catch (OperationCanceledException) { /* cancelled */ }
        catch { /* best effort */ }
    }

    private static void HideProcessWindows(Process p)
    {
        try
        {
            if (p.MainWindowHandle != IntPtr.Zero)
                ShowWindow(p.MainWindowHandle, SW_HIDE);

            int pid = p.Id;
            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out uint windowPid);
                if (windowPid == (uint)pid && IsWindowVisible(hWnd))
                    ShowWindow(hWnd, SW_HIDE);
                return true;
            }, IntPtr.Zero);
        }
        catch { /* best effort */ }
    }

    // ---------- output streaming ----------

    private static async Task PumpStreamAsync(
        StreamReader reader,
        IProgress<ProgressReport>? progress,
        CancellationToken ct)
    {
        var buffer = new char[1024];
        var line   = new StringBuilder(128);

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            int read;
            try
            {
                read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)
                                   .ConfigureAwait(false);
            }
            catch (OperationCanceledException) { break; }
            if (read == 0) break;

            for (int i = 0; i < read; i++)
            {
                char c = buffer[i];

                if (c == '\r' || c == '\n')
                {
                    if (line.Length > 0)
                    {
                        progress?.Report(ProgressReport.Log(line.ToString()));
                        line.Clear();
                    }
                }
                else
                {
                    line.Append(c);
                }
            }
        }

        if (line.Length > 0)
            progress?.Report(ProgressReport.Log(line.ToString()));
    }
}