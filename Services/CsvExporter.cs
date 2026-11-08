using System.Text;
using LogInspector.Models;
namespace LogInspector.Services;

public static class CsvExporter
{
    public static string Export(IEnumerable<LogEntry> entries)
    {
        var sb = new StringBuilder("Line,Timestamp,Level,ExceptionType,Message\r\n");
        foreach (var e in entries)
            sb.Append(e.LineNumber).Append(',')
              .Append(e.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss") ?? "").Append(',')
              .Append(e.Level).Append(',')
              .Append(Cell(e.ExceptionType)).Append(',')
              .Append(Cell(e.Message)).Append("\r\n");
        return sb.ToString();
    }

    public static string Cell(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        // Neutralise spreadsheet formula injection.
        if ("=+-@\t\r".Contains(value[0])) value = "'" + value;
        return value.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
    }
}
