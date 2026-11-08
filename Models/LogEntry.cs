namespace LogInspector.Models;

public sealed record LogEntry(int LineNumber, string Level, string Message, bool IsException, bool IsStackTrace)
{
    public DateTime? Timestamp { get; init; }
    public string? ExceptionType { get; init; }
}

public sealed class ExceptionOccurrence
{
    public int LineNumber { get; init; }
    public DateTime? Timestamp { get; init; }
    public string Message { get; init; } = "";
    public List<string> StackTrace { get; init; } = [];
}

public sealed class ExceptionGroup
{
    public string Type { get; init; } = "";
    public int Count { get; set; }
    public string? Explanation { get; set; }
    public List<ExceptionOccurrence> Occurrences { get; } = [];
}

public sealed record StoredLogInfo(Guid Id, string Name, DateTime CreatedUtc, long SizeBytes);

public sealed class FilterCriteria
{
    public string Level { get; set; } = "ALL";
    public string Search { get; set; } = "";
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public sealed class AnalysisViewModel
{
    public StoredLogInfo? Info { get; set; }
    public FilterCriteria Filter { get; set; } = new();
    public List<LogEntry> Entries { get; set; } = [];
    public int MatchCount { get; set; }
    public bool Truncated { get; set; }
    public Dictionary<string, int> Counts { get; set; } = new();
    public List<ExceptionGroup> Groups { get; set; } = [];
    public int TotalLines { get; set; }
}

public sealed class IndexViewModel
{
    public string Input { get; set; } = "";
    public string? Error { get; set; }
    public List<StoredLogInfo> Saved { get; set; } = [];
}
