using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using LogInspector.Models;
using LogInspector.Services;
using Xunit;
using Xunit.Abstractions;

namespace LogInspector.Tests;

/// <summary>Benchmarks (5 repeats). Run: dotnet test --filter Category=Perf --logger "console;verbosity=detailed"</summary>
[Trait("Category", "Perf")]
public class PerfTests(ITestOutputHelper output)
{
    // Original (pre-refactor) per-line parser, kept only as a baseline.
    private static readonly Regex OldLevel = new(@"\b(ERROR|ERR|FATAL|WARN(?:ING)?|INFO|DEBUG|TRACE)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex OldEx = new(@"\b(?:[\w.$]+Exception|[\w.$]+Error|System\.[\w.]+Exception|Caused by:)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex OldStack = new(@"^\s*(?:at\s+\S+|--- End of|\.\.\. \d+ more|--->|in\s+\S+\s*:line\s+\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static int OldParse(string text)
    {
        var n = 0;
        using var r = new StringReader(text);
        string? l;
        while ((l = r.ReadLine()) != null) { OldStack.IsMatch(l); OldEx.IsMatch(l); OldLevel.Match(l); n++; }
        return n;
    }

    private static string Generate(int lines)
    {
        var sb = new StringBuilder();
        var i = 0;
        while (sb.Length < 0 || i < lines)
        {
            var t = new DateTime(2026, 3, 10, 9, 0, 0).AddSeconds(i);
            switch (i % 10)
            {
                case 3:
                    sb.Append($"{t:yyyy-MM-dd HH:mm:ss} ERROR Request {i} failed\njava.lang.NullPointerException: x{i % 7}\n\tat com.acme.A.run(A.java:10)\n\tat com.acme.B.run(B.java:20)\n");
                    i += 4; break;
                case 6: sb.Append($"{t:yyyy-MM-dd HH:mm:ss} WARN slow call {i}\n"); i++; break;
                default: sb.Append($"{t:yyyy-MM-dd HH:mm:ss} INFO processed item {i} ok\n"); i++; break;
            }
        }
        return sb.ToString();
    }

    private static double[] Time(Action a)
    {
        a(); // warm-up
        var r = new double[5];
        for (var k = 0; k < 5; k++) { var sw = Stopwatch.StartNew(); a(); r[k] = sw.Elapsed.TotalMilliseconds; }
        return r;
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(5000)]
    public void Measure(int lines)
    {
        var text = Generate(lines);
        var p = new LogParser();
        List<LogEntry> entries = [];
        var crit = new FilterCriteria { Level = "ERROR", Search = "request", From = new DateTime(2026, 3, 10, 9, 0, 0) };
        void Report(string name, double[] t) => output.WriteLine($"{lines,5} lines | {name,-22} | median {t.Order().ElementAt(2):F2} ms | min {t.Min():F2} | max {t.Max():F2}");

        Report("baseline old parse", Time(() => OldParse(text)));
        Report("parse", Time(() => entries = p.Parse(text)));
        Report("group exceptions", Time(() => p.GroupExceptions(entries)));
        Report("filter (lvl+search+date)", Time(() => LogFilter.Apply(entries, crit).ToList()));
        Report("csv export", Time(() => CsvExporter.Export(entries)));
    }
}
