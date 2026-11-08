using LogInspector.Models;
namespace LogInspector.Services;

public static class LogFilter
{
    public static readonly string[] Levels = ["ERROR", "WARN", "INFO", "DEBUG", "OTHER"];

    public static IEnumerable<LogEntry> Apply(IEnumerable<LogEntry> entries, FilterCriteria c)
    {
        var level = (c.Level ?? "").ToUpperInvariant();
        if (!Levels.Contains(level)) level = "ALL";
        var search = c.Search?.Trim();
        var dateActive = c.From != null || c.To != null;
        foreach (var e in entries)
        {
            if (level != "ALL" && e.Level != level) continue;
            if (!string.IsNullOrEmpty(search) && !e.Message.Contains(search, StringComparison.OrdinalIgnoreCase)) continue;
            if (dateActive)
            {
                if (e.Timestamp is not { } ts) continue;
                if (c.From is { } f && ts < f) continue;
                if (c.To is { } t && ts > t) continue;
            }
            yield return e;
        }
    }
}
