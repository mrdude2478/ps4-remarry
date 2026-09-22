using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace PS4Remarry.Core;

/// <summary>
/// Watches a folder for the .pkg file that img_create is writing, and
/// reports progress based on the file's growing size relative to an
/// expected final size.
/// </summary>
public sealed class PkgBuildProgressMonitor : IDisposable
{
    private readonly string   _outputDir;
    private readonly long     _expectedSize;
    private readonly IProgress<ProgressReport> _progress;
    private readonly CancellationToken _ct;
    private readonly Timer    _timer;
    private readonly HashSet<string> _ignoreFiles;

    private long _lastReportedBytes = -1;

    public PkgBuildProgressMonitor(
        string outputDir,
        long expectedSize,
        IProgress<ProgressReport> progress,
        CancellationToken ct,
        IEnumerable<string>? ignoreFiles = null)
    {
        _outputDir    = outputDir;
        _expectedSize = expectedSize > 0 ? expectedSize : 1;
        _progress     = progress;
        _ct           = ct;
        _ignoreFiles  = new HashSet<string>(
            ignoreFiles ?? Enumerable.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);

        _timer = new Timer(Poll, state: null, dueTime: 250, period: 250);
    }

    private void Poll(object? _)
    {
        try
        {
            if (_ct.IsCancellationRequested) return;

            FileInfo? newest = new DirectoryInfo(_outputDir)
                .EnumerateFiles("*.pkg")
                .Where(f => !_ignoreFiles.Contains(f.FullName))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();

            if (newest is null) return;

            long size = newest.Length;
            if (size == _lastReportedBytes) return;
            _lastReportedBytes = size;

            double pct = 100.0 * size / _expectedSize;
            if (pct > 99.0) pct = 99.0;

            double mapped = 75.0 + (pct * 0.24);

            _progress.Report(ProgressReport.Pct(mapped,
                $"Building: {pct:0.0}%  ({size / 1024 / 1024} MB written)"));
        }
        catch
        {
            // Best-effort. Never throw from a timer callback.
        }
    }

    public void Dispose()
    {
        try { _timer.Dispose(); } catch { }
    }
}