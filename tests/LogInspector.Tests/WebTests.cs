using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogInspector.Tests;

public sealed class WebTests : IClassFixture<WebTests.Factory>
{
    public sealed class Factory : WebApplicationFactory<Program> { }

    [Fact]
    public void Form_Buffering_Stays_In_Memory_For_Max_Upload()
    {
        var o = _f.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Http.Features.FormOptions>>().Value;
        Assert.True(o.MemoryBufferThreshold >= LogInspector.Services.UploadValidator.MaxBytes + 1_048_576, "multipart bodies above the threshold are buffered to temp files");
    }

    [Fact]
    public async Task Large_Upload_Leaves_No_Persistent_Copy_On_Disk()
    {
        var marker = "MARKER-" + Guid.NewGuid().ToString("N");
        var started = DateTime.UtcNow.AddSeconds(-2);
        var body = new System.Text.StringBuilder();
        for (var i = 0; i < 40_000; i++) body.Append("INFO line ").Append(i).Append(' ').Append(marker).Append('\n');
        var (c, t) = await ClientWithToken();
        var r = await c.PostAsync("/Home/Upload", Form(t, null, "big.log", System.Text.Encoding.UTF8.GetBytes(body.ToString())));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        var r2 = await c.PostAsync("/Home/Upload", Form(t, body.ToString()));
        Assert.Equal(HttpStatusCode.Redirect, r2.StatusCode);

        var needle = System.Text.Encoding.UTF8.GetBytes(marker);
        var roots = new[] { Path.GetTempPath(), AppContext.BaseDirectory, Directory.GetCurrentDirectory() };
        foreach (var root in roots.Distinct())
        {
            var opt = root == Path.GetTempPath() ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;
            foreach (var f in Directory.EnumerateFiles(root, "*", opt))
            {
                try
                {
                    var fi = new FileInfo(f);
                    if (fi.LastWriteTimeUtc < started || fi.Length > 50_000_000 || fi.Length < needle.Length) continue;
                    Assert.False(File.ReadAllBytes(f).AsSpan().IndexOf(needle) >= 0, $"log content found on disk: {f}");
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }

    [Fact]
    public void Source_Has_No_Filesystem_Writes()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "LogInspector.csproj"))) dir = dir.Parent;
        if (dir == null) return; // source tree not available
        var pattern = new Regex(@"File\.(Write|Append|Create|Open|Copy|Move)|FileStream|StreamWriter|Directory\.CreateDirectory|SaveAs\(|CopyTo\(");
        foreach (var sub in new[] { "Services", "Controllers", "Models" })
            foreach (var f in Directory.EnumerateFiles(Path.Combine(dir.FullName, sub), "*.cs", SearchOption.AllDirectories))
                Assert.DoesNotMatch(pattern, File.ReadAllText(f));
        Assert.DoesNotContain("App_Data", File.ReadAllText(Path.Combine(dir.FullName, "Program.cs")));
    }

    private readonly Factory _f;
    public WebTests(Factory f) => _f = f;

    private async Task<(HttpClient, string)> ClientWithToken()
    {
        var c = _f.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await c.GetStringAsync("/");
        var m = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        return (c, m.Groups[1].Value);
    }

    private static MultipartFormDataContent Form(string token, string? input = null, string? fileName = null, byte[]? file = null)
    {
        var f = new MultipartFormDataContent { { new StringContent(token), "__RequestVerificationToken" } };
        if (input != null) f.Add(new StringContent(input), "input");
        if (file != null) f.Add(new ByteArrayContent(file), "file", fileName!);
        return f;
    }

    [Fact]
    public async Task Post_Without_Antiforgery_Token_Is_Rejected()
    {
        var c = _f.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var r = await c.PostAsync("/Home/Upload", new MultipartFormDataContent { { new StringContent("INFO x"), "input" } });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Xss_Payload_Is_Encoded_In_Output()
    {
        var (c, t) = await ClientWithToken();
        var r = await c.PostAsync("/Home/Upload", Form(t, "ERROR <script>alert(1)</script> java.lang.RuntimeException: <img src=x onerror=alert(2)>"));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        var body = await c.GetStringAsync(r.Headers.Location);
        Assert.DoesNotContain("<script>alert(1)", body);
        Assert.DoesNotContain("<img src=x", body);
        Assert.Contains("&lt;script&gt;", body);
    }

    [Fact]
    public async Task Xss_In_File_Name_Is_Encoded()
    {
        var (c, t) = await ClientWithToken();
        await c.PostAsync("/Home/Upload", Form(t, null, "<b>x</b>.log", "INFO hi"u8.ToArray()));
        var home = await c.GetStringAsync("/");
        Assert.DoesNotContain("<b>x</b>", home);
    }

    [Fact]
    public async Task Bad_Extension_Returns_400()
    {
        var (c, t) = await ClientWithToken();
        var r = await c.PostAsync("/Home/Upload", Form(t, null, "x.exe", "INFO hi"u8.ToArray()));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Empty_Submit_Returns_400()
    {
        var (c, t) = await ClientWithToken();
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync("/Home/Upload", Form(t))).StatusCode);
    }

    [Fact]
    public async Task Sample_Then_Filter_And_Csv_Export()
    {
        var (c, t) = await ClientWithToken();
        var r = await c.PostAsync("/Home/Sample", new FormUrlEncodedContent(new Dictionary<string, string> { ["kind"] = "java", ["__RequestVerificationToken"] = t }));
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        var url = r.Headers.Location!.ToString();
        var page = await c.GetStringAsync(url + "?level=ERROR&search=OrderController");
        Assert.Contains("NullPointerException", page);
        var csv = await c.GetAsync(url.Replace("Analysis", "Export") + "?level=ERROR");
        Assert.Equal("text/csv", csv.Content.Headers.ContentType!.MediaType);
        Assert.Contains("attachment", csv.Content.Headers.ContentDisposition!.ToString());
        Assert.Contains("ERROR", await csv.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Unknown_Id_Is_404_And_Security_Headers_Present()
    {
        var c = _f.CreateClient();
        var r = await c.GetAsync($"/Home/Analysis/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        Assert.True(r.Headers.Contains("Content-Security-Policy"));
        Assert.True(r.Headers.Contains("X-Content-Type-Options"));
    }

    [Fact]
    public async Task Non_Guid_Id_Is_Rejected_Not_Path_Traversal()
    {
        var c = _f.CreateClient();
        var r = await c.GetAsync("/Home/Analysis/..%2F..%2Fsecret");
        Assert.NotEqual(HttpStatusCode.OK, r.StatusCode);
    }
}
