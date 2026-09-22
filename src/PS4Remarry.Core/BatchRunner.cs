namespace PS4Remarry.Core;

/// <summary>
/// Runs a set of BatchJob items with a maximum degree of parallelism.
/// Each job runs its own RemarryPipeline in its own work sub-directory so
/// they don't collide. Progress reports are tagged with the job Id.
/// </summary>
public sealed class BatchRunner
{
    private readonly ProcessRunner _runner;
    private readonly ToolLocator  _tools;

    public BatchRunner(ProcessRunner runner, ToolLocator tools)
    {
        _runner = runner;
        _tools  = tools;
    }

    public async Task RunAllAsync(
        IEnumerable<BatchJob> jobs,
        int maxParallel,
        Action<int, ProgressReport> perJobProgress,
        CancellationToken ct = default)
    {
        maxParallel = Math.Max(1, maxParallel);

        using var gate = new SemaphoreSlim(maxParallel, maxParallel);

        var tasks = jobs.Select(job => RunOneAsync(job, gate, perJobProgress, ct)).ToArray();
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task RunOneAsync(
        BatchJob job,
        SemaphoreSlim gate,
        Action<int, ProgressReport> perJobProgress,
        CancellationToken ct)
    {
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (ct.IsCancellationRequested)
            {
                job.State = BatchJobState.Cancelled;
                job.Status = "Cancelled before start";
                return;
            }

            job.State = BatchJobState.Running;
            job.Status = "Starting...";
            job.Progress = 0;
            job.DigestStatus = DigestStatus.Unknown;
            job.BuiltPkgPath = "";

            var jobWorkDir = Path.Combine(_tools.WorkDir, $"job{job.Id}");
            Directory.CreateDirectory(jobWorkDir);

            var before = new HashSet<string>(
                Directory.Exists(job.OutputDir)
                    ? Directory.GetFiles(job.OutputDir, "*.pkg")
                    : Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            var pipeline = new RemarryPipeline(_runner, _tools);
            var opts = new RemarryOptions(
                job.GamePkg, job.UpdatePkg, job.OutputDir, jobWorkDir);

            var progress = new Progress<ProgressReport>(r =>
            {
                if (r.Percent is double p)
                    job.Progress = (int)Math.Clamp(p, 0, 100);
                if (!string.IsNullOrEmpty(r.Message))
                    job.Status = r.Message!.Length > 80
                        ? r.Message.Substring(0, 77) + "..."
                        : r.Message!;
                perJobProgress(job.Id, r);
            });

            await pipeline.RunAsync(opts, progress, ct).ConfigureAwait(false);

            job.Progress = 100;

            string? newPkg = null;
            if (Directory.Exists(job.OutputDir))
            {
                newPkg = Directory.GetFiles(job.OutputDir, "*.pkg")
                    .FirstOrDefault(f => !before.Contains(f));
            }

            if (newPkg is null)
            {
                job.DigestStatus = DigestStatus.Unreadable;
                job.State = BatchJobState.Completed;
                job.Status = "Built (couldn't identify output to verify)";
                perJobProgress(job.Id, ProgressReport.Log(
                    "[verify] couldn't identify newly created PKG in output folder."));
                return;
            }

            job.BuiltPkgPath = newPkg;
            job.Status = "Verifying...";
            perJobProgress(job.Id, ProgressReport.Log(
                $"[verify] checking {Path.GetFileName(newPkg)}..."));

            try
            {
                var gameDigest = MarryDigestReader.GetChecksum(job.GamePkg);
                var outDigest  = MarryDigestReader.GetChecksum(newPkg);

                if (gameDigest == "-" || outDigest == "-")
                {
                    job.DigestStatus = DigestStatus.Unreadable;
                    job.Status = "Built (digest unreadable)";
                    perJobProgress(job.Id, ProgressReport.Log(
                        $"[verify] unreadable: game={gameDigest}, output={outDigest}"));
                }
                else if (string.Equals(gameDigest, outDigest, StringComparison.OrdinalIgnoreCase))
                {
                    job.DigestStatus = DigestStatus.Match;
                    job.State = BatchJobState.Completed;
                    job.Status = "Completed + verified";
                    perJobProgress(job.Id, ProgressReport.Log(
                        $"[verify] MATCH  {gameDigest}"));
                }
                else
                {
                    job.DigestStatus = DigestStatus.Mismatch;
                    job.State = BatchJobState.Completed;
                    job.Status = "Built but digest MISMATCH";
                    perJobProgress(job.Id, ProgressReport.Log(
                        $"[verify] MISMATCH  game={gameDigest}  output={outDigest}"));
                }
            }
            catch (Exception ex)
            {
                job.DigestStatus = DigestStatus.Unreadable;
                job.Status = "Built (verify errored)";
                perJobProgress(job.Id, ProgressReport.Log(
                    $"[verify] error: {ex.Message}"));
            }
        }
        catch (OperationCanceledException)
        {
            job.State = BatchJobState.Cancelled;
            job.Status = "Cancelled";
        }
        catch (Exception ex)
        {
            job.State = BatchJobState.Failed;
            job.Status = "Failed: " + ex.Message;
        }
        finally
        {
            try { gate.Release(); } catch { }
        }
    }
}