namespace PS4Remarry.Core;

public static class Formatting
{
    public static string FormatBytes(long bytes)
    {
        const long KB = 1024, MB = KB * 1024, GB = MB * 1024;
        if (bytes >= GB) return $"{bytes / (double)GB:0.00} GB";
        if (bytes >= MB) return $"{bytes / (double)MB:0.00} MB";
        if (bytes >= KB) return $"{bytes / (double)KB:0.00} KB";
        return $"{bytes} bytes";
    }
}