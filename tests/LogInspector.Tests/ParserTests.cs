using LogInspector.Services;
using Xunit;

namespace LogInspector.Tests;

public class ParserTests
{
    private readonly LogParser _parser = new();

    [Fact]
    public void Empty_And_Null_Input_Yield_No_Entries()
    {
        Assert.Empty(_parser.Parse(null));
        Assert.Empty(_parser.Parse(""));
    }

    [Theory]
    [InlineData("2026-01-01 10:00:00 ERROR boom", "ERROR")]
    [InlineData("2026-01-01 10:00:00 WARNING careful", "WARN")]
    [InlineData("2026-01-01 10:00:00 FATAL dead", "ERROR")]
    [InlineData("2026-01-01 10:00:00 TRACE t", "DEBUG")]
    [InlineData("[INFO] started", "INFO")]
    public void Level_Detected(string line, string expected) =>
        Assert.Equal(expected, _parser.Parse(line)[0].Level);

    [Fact]
    public void Lowercase_Words_In_Message_Are_Not_Levels()
    {
        var e = _parser.Parse("Sending info to server, no error here")[0];
        Assert.Equal("OTHER", e.Level);
    }

    [Fact]
    public void CausedBy_Line_Is_Exception()
    {
        var e = _parser.Parse("Caused by: java.io.IOException: disk")[0];
        Assert.True(e.IsException);
    }

    [Fact]
    public void Word_Ending_In_Error_Lowercase_Is_Not_Exception()
    {
        Assert.False(_parser.Parse("INFO no syntaxerror found")[0].IsException);
    }

    [Fact]
    public void Stack_Lines_Inherit_Level_And_Are_Flagged()
    {
        var list = _parser.Parse("ERROR java.lang.NullPointerException: x\n\tat com.acme.A.run(A.java:10)\n\tat com.acme.B.run(B.java:20)");
        Assert.Equal(3, list.Count);
        Assert.All(list, e => Assert.Equal("ERROR", e.Level));
        Assert.True(list[1].IsStackTrace && list[2].IsStackTrace);
    }

    [Fact]
    public void Crlf_Lines_Are_Split()
    {
        Assert.Equal(2, _parser.Parse("INFO a\r\nINFO b\r\n").Count);
    }

    [Fact]
    public void Binary_Garbage_Does_Not_Throw()
    {
        var text = new string('\0', 50) + "��" + new string('x', 200_000);
        var list = _parser.Parse(text);
        Assert.Single(list);
    }
}
