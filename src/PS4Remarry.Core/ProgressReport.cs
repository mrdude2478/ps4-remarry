namespace PS4Remarry.Core;

/// <summary>
/// A single progress tick: an optional percentage and a log line.
/// Either field can be null/empty.
/// </summary>
public sealed record ProgressReport(string? Message, double? Percent = null)
{
    public static ProgressReport Log(string msg) => new(msg, null);
    public static ProgressReport Pct(double p, string? msg = null) => new(msg, p);
}