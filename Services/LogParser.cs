using System.Globalization;
using System.Text.RegularExpressions;
using LogInspector.Models;
namespace LogInspector.Services;

public sealed class LogParser
{
    public const int MaxLineLength = 4000;
    private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(200);
    private const RegexOptions Opts = RegexOptions.Compiled | RegexOptions.CultureInvariant;

    private static readonly Regex LevelPattern = new(@"(?<![\w.])(ERROR|ERR|FATAL|WARN|WARNING|INFO|DEBUG|TRACE)(?![\w.])", Opts, Timeout);
    private static readonly Regex DotnetLevel = new(@"^(info|warn|fail|crit|dbug|trce):", Opts, Timeout);
    private static readonly Regex ExceptionPattern = new(@"ORA-\d{5}|\b(?:[A-Za-z_][\w$]*\.)+[A-Z][\w$]*(?:Exception|Error)\b|\b[A-Z][\w$]*Exception\b", Opts, Timeout);
    private static readonly Regex CausedBy = new(@"^\s*Caused by:", Opts, Timeout);
    private static readonly Regex StackPattern = new(@"^\s*(?:at\s+[^\s(]+\(|--- End of|\.\.\. \d+ (?:more|common frames omitted)|--->)", Opts, Timeout);
    private static readonly Regex TimestampPattern = new(@"(\d{4}-\d{2}-\d{2})[ T](\d{2}:\d{2}:\d{2})", Opts, Timeout);

    public List<LogEntry> Parse(string? text)
    {
        var entries = new List<LogEntry>();
        if (string.IsNullOrEmpty(text)) return entries;
        using var reader = new StringReader(text);
        string? line;
        var lineNo = 0;
        var previousLevel = "OTHER";
        DateTime? lastTs = null;
        while ((line = reader.ReadLine()) != null)
        {
            lineNo++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.Length > MaxLineLength) line = line[..MaxLineLength] + "…";
            try
            {
                var isStack = StackPattern.IsMatch(line);
                var causedBy = CausedBy.IsMatch(line);
                string? exType = null;
                if (!isStack)
                {
                    var m = ExceptionPattern.Match(line);
                    if (m.Success) exType = m.Value;
                }
                var isException = exType != null || causedBy;

                string? levelRaw = null;
                var head = line.Length > 120 ? line[..120] : line;
                if (!isStack)
                {
                    var lm = LevelPattern.Match(head);
                    if (lm.Success) levelRaw = lm.Value;
                    else
                    {
                        var dm = DotnetLevel.Match(line);
                        if (dm.Success) levelRaw = dm.Groups[1].Value;
                    }
                }
                var level = levelRaw != null ? Normalize(levelRaw)
                    : (isStack || isException ? previousLevel : "OTHER");
                if (levelRaw != null) previousLevel = level;

                DateTime? ts = null;
                var tm = TimestampPattern.Match(head);
                if (tm.Success && DateTime.TryParse($"{tm.Groups[1].Value} {tm.Groups[2].Value}", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                { ts = parsed; lastTs = parsed; }
                else if (isStack || causedBy || exType != null) ts = lastTs;

                entries.Add(new LogEntry(lineNo, level, line, isException, isStack) { Timestamp = ts, ExceptionType = exType });
            }
            catch (RegexMatchTimeoutException)
            {
                entries.Add(new LogEntry(lineNo, "OTHER", line, false, false));
            }
        }
        return entries;
    }

    /// <summary>Groups top-level exception lines by type and attaches the stack trace that follows.</summary>
    public List<ExceptionGroup> GroupExceptions(IReadOnlyList<LogEntry> entries, int maxOccurrences = 5)
    {
        var groups = new Dictionary<string, ExceptionGroup>();
        for (var i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e.ExceptionType == null || CausedBy.IsMatch(e.Message)) continue;
            if (!groups.TryGetValue(e.ExceptionType, out var g))
            {
                g = new ExceptionGroup { Type = e.ExceptionType, Explanation = ErrorKnowledge.Explain(e.ExceptionType) };
                groups[e.ExceptionType] = g;
            }
            g.Count++;
            var trace = new List<string>();
            for (var j = i + 1; j < entries.Count && (entries[j].IsStackTrace || CausedBy.IsMatch(entries[j].Message)); j++)
            {
                trace.Add(entries[j].Message);
                if (g.Explanation == null && entries[j].ExceptionType is { } inner) g.Explanation = ErrorKnowledge.Explain(inner);
            }
            if (g.Occurrences.Count < maxOccurrences)
                g.Occurrences.Add(new ExceptionOccurrence { LineNumber = e.LineNumber, Timestamp = e.Timestamp, Message = e.Message, StackTrace = trace });
        }
        return groups.Values.OrderByDescending(x => x.Count).ThenBy(x => x.Type, StringComparer.Ordinal).ToList();
    }

    private static string Normalize(string level) => level.ToUpperInvariant() switch
    {
        "ERR" or "FATAL" or "FAIL" or "CRIT" => "ERROR",
        "WARNING" => "WARN",
        "TRACE" or "TRCE" or "DBUG" => "DEBUG",
        var value => value
    };
}
