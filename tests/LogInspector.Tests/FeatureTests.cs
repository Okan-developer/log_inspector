using System.Text;
using LogInspector.Models;
using LogInspector.Services;
using Xunit;

namespace LogInspector.Tests;

public class GroupingTests
{
    private readonly LogParser _p = new();
    private const string Log = """
2026-03-10 09:00:07 ERROR Query failed
java.sql.SQLException: ORA-00942: table or view does not exist
	at a.B.c(B.java:1)
2026-03-10 09:00:09 ERROR boom
java.lang.NullPointerException: x
	at a.B.c(B.java:1)
Caused by: java.lang.IllegalStateException: s
	at a.B.d(B.java:2)
2026-03-10 09:01:09 ERROR boom
java.lang.NullPointerException: x
	at a.B.c(B.java:1)
""";

    [Fact]
    public void Groups_Count_And_Attach_Stack()
    {
        var groups = _p.GroupExceptions(_p.Parse(Log));
        var npe = Assert.Single(groups, g => g.Type == "java.lang.NullPointerException");
        Assert.Equal(2, npe.Count);
        Assert.Equal(3, npe.Occurrences[0].StackTrace.Count);
        Assert.NotNull(npe.Explanation);
    }

    [Fact]
    public void Caused_By_Is_Not_Counted_As_Separate_Group() =>
        Assert.DoesNotContain(_p.GroupExceptions(_p.Parse(Log)), g => g.Type.Contains("IllegalState"));

    [Fact]
    public void Ora_Code_Detected_With_Explanation()
    {
        var g = Assert.Single(_p.GroupExceptions(_p.Parse("ERROR ORA-00942: table or view does not exist")));
        Assert.Equal("ORA-00942", g.Type);
        Assert.Contains("Tablo", g.Explanation);
    }

    [Fact]
    public void Dotnet_Exception_Detected()
    {
        var e = _p.Parse("System.InvalidOperationException: bad")[0];
        Assert.True(e.IsException);
        Assert.Equal("System.InvalidOperationException", e.ExceptionType);
    }

    [Fact]
    public void Timestamp_Parsed_And_Inherited_By_Stack()
    {
        var l = _p.Parse("2026-03-10 09:00:09 ERROR x\n\tat a.B.c(B.java:1)");
        Assert.Equal(new DateTime(2026, 3, 10, 9, 0, 9), l[0].Timestamp);
        Assert.Equal(l[0].Timestamp, l[1].Timestamp);
    }

    [Fact]
    public void Stack_Line_With_Level_Word_In_Namespace_Keeps_Level()
    {
        var l = _p.Parse("WARN x\n   at My.Debug.Helper.Run()");
        Assert.Equal("WARN", l[1].Level);
    }

    [Fact]
    public void Prose_Starting_With_At_Is_Not_Stack() =>
        Assert.False(_p.Parse("At least 3 retries")[0].IsStackTrace);

    [Fact]
    public void Samples_Parse_With_Exceptions()
    {
        foreach (var k in new[] { "java", "dotnet" })
            Assert.NotEmpty(_p.GroupExceptions(_p.Parse(SampleLogs.Get(k))));
        Assert.Null(SampleLogs.Get("x"));
    }
}

public class FilterTests
{
    private static readonly List<LogEntry> Entries = new LogParser().Parse(
        "2026-03-10 09:00:00 INFO a\n2026-03-10 10:00:00 ERROR Boom\n2026-03-10 11:00:00 WARN c\nno timestamp line");

    [Fact] public void Level_Filter_Case_Insensitive() =>
        Assert.Single(LogFilter.Apply(Entries, new FilterCriteria { Level = "error" }));

    [Fact] public void Search_Is_Case_Insensitive_And_Trimmed() =>
        Assert.Single(LogFilter.Apply(Entries, new FilterCriteria { Search = " boom " }));

    [Fact] public void Date_Range_Filters_And_Drops_Untimed()
    {
        var r = LogFilter.Apply(Entries, new FilterCriteria { From = new DateTime(2026, 3, 10, 9, 30, 0), To = new DateTime(2026, 3, 10, 10, 30, 0) }).ToList();
        Assert.Single(r);
        Assert.Equal("ERROR", r[0].Level);
    }

    [Fact] public void Invalid_Level_Means_All() =>
        Assert.Equal(4, LogFilter.Apply(Entries, new FilterCriteria { Level = "bogus" }).Count());
}

public class CsvTests
{
    [Theory]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData("say \"hi\"", "\"say \"\"hi\"\"\"")]
    [InlineData("plain", "plain")]
    public void Cell_Escaping(string input, string expected) => Assert.Equal(expected, CsvExporter.Cell(input));

    [Fact]
    public void Export_Has_Header_And_Rows()
    {
        var csv = CsvExporter.Export(new LogParser().Parse("2026-03-10 10:00:00 ERROR Boom"));
        var lines = csv.TrimEnd().Split("\r\n");
        Assert.Equal(2, lines.Length);
        Assert.StartsWith("1,2026-03-10 10:00:00,ERROR,", lines[1]);
    }
}

public class UploadTests
{
    private static UploadResult V(string name, byte[] data) => UploadValidator.Validate(name, data.Length, new MemoryStream(data));
    private static byte[] B(string s) => Encoding.UTF8.GetBytes(s);
    private static byte[] A(int n) => Enumerable.Repeat((byte)97, n).ToArray();

    [Fact] public void Valid_Log_Accepted() => Assert.Null(V("a.LOG", B("INFO ok")).Error);
    [Fact] public void Valid_Txt_Accepted() => Assert.Null(V("a.txt", B("INFO ok")).Error);
    [Theory] [InlineData("a.exe")] [InlineData("a.log.exe")] [InlineData("a")] [InlineData("a.html")]
    public void Bad_Extension_Rejected(string n) => Assert.NotNull(V(n, B("INFO ok")).Error);
    [Fact] public void Empty_Rejected() => Assert.NotNull(V("a.log", []).Error);
    [Fact] public void Whitespace_Rejected() => Assert.NotNull(V("a.log", B("  \n ")).Error);
    [Fact] public void Binary_Rejected() => Assert.NotNull(V("a.log", [0x4D, 0x5A, 0, 0, 1, 2]).Error);
    [Fact] public void Invalid_Utf8_Rejected() => Assert.NotNull(V("a.log", [0xFF, 0xFE, 0xFA, 0x41]).Error);
    [Fact] public void Over_5MB_Rejected() => Assert.NotNull(V("a.log", A(UploadValidator.MaxBytes + 1)).Error);
    [Fact] public void Exactly_5MB_Accepted() => Assert.Null(V("a.log", A(UploadValidator.MaxBytes)).Error);
    [Fact] public void Lying_Length_Header_Cannot_Bypass_Limit() =>
        Assert.NotNull(UploadValidator.Validate("a.log", 100, new MemoryStream(A(UploadValidator.MaxBytes + 10))).Error);
}

public class StoreTests : IDisposable
{
    public void Dispose() { }

    [Fact]
    public void Entries_Expire_After_Ttl()
    {
        var now = DateTime.UtcNow;
        var s = new LogStore(() => now);
        var info = s.Save("x.log", "INFO hi");
        now += LogStore.Ttl + TimeSpan.FromSeconds(1);
        Assert.Null(s.Get(info.Id));
        Assert.Empty(s.List());
    }

    [Fact]
    public void Store_Is_Capped()
    {
        var s = new LogStore();
        for (var i = 0; i < LogStore.MaxStored + 5; i++) s.Save("x.log", "INFO " + i);
        Assert.Equal(LogStore.MaxStored, s.List().Count);
    }

    [Fact]
    public void Save_Get_List_Delete()
    {
        var s = new LogStore();
        var info = s.Save("x.log", "INFO hi");
        Assert.Equal("INFO hi", s.Get(info.Id)!.Value.Content);
        Assert.Single(s.List());
        s.Delete(info.Id);
        Assert.Null(s.Get(info.Id));
    }

    [Theory]
    [InlineData("..\\..\\evil.log", "evil.log")]
    [InlineData("../../evil.log", "evil.log")]
    [InlineData("", "pasted-log")]
    public void SafeName_Strips_Paths(string input, string expected) => Assert.Equal(expected, LogStore.SafeName(input));

    [Fact] public void Unknown_Id_Returns_Null() => Assert.Null(new LogStore().Get(Guid.NewGuid()));
}
