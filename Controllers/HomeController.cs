using LogInspector.Models;
using LogInspector.Services;
using Microsoft.AspNetCore.Mvc;
namespace LogInspector.Controllers;

public sealed class HomeController(LogParser parser, LogStore store) : Controller
{
    private const int MaxRows = 1000;
    private const long RequestLimit = UploadValidator.MaxBytes + 1_048_576;

    [HttpGet]
    public IActionResult Index() => View(new IndexViewModel { Saved = store.List() });

    [HttpPost, RequestSizeLimit(RequestLimit), RequestFormLimits(MultipartBodyLengthLimit = RequestLimit, ValueLengthLimit = (int)RequestLimit)]
    public IActionResult Upload(IFormFile? file, string? input)
    {
        UploadResult result;
        string? name = null;
        if (file is { Length: > 0 })
        {
            name = file.FileName;
            using var stream = file.OpenReadStream();
            result = UploadValidator.Validate(file.FileName, file.Length, stream);
        }
        else if (!string.IsNullOrWhiteSpace(input)) result = UploadValidator.ValidateText(input);
        else result = new UploadResult(null, "Bir .log/.txt dosyası seçin veya log metni yapıştırın.");

        if (result.Text == null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", new IndexViewModel { Error = result.Error, Saved = store.List() });
        }
        var info = store.Save(name, result.Text);
        return RedirectToAction(nameof(Analysis), new { id = info.Id });
    }

    [HttpPost]
    public IActionResult Sample(string? kind)
    {
        var text = SampleLogs.Get(kind);
        if (text == null) return BadRequest();
        var info = store.Save($"sample-{kind}.log", text);
        return RedirectToAction(nameof(Analysis), new { id = info.Id });
    }

    [HttpGet]
    public IActionResult Analysis(Guid id, string? level, string? search, DateTime? from, DateTime? to)
    {
        var stored = store.Get(id);
        if (stored == null) return NotFound();
        var all = parser.Parse(stored.Value.Content);
        var filter = new FilterCriteria { Level = level ?? "ALL", Search = search ?? "", From = from, To = to };
        var matches = LogFilter.Apply(all, filter).ToList();
        return View(new AnalysisViewModel
        {
            Info = stored.Value.Info,
            Filter = filter,
            TotalLines = all.Count,
            Counts = LogFilter.Levels.ToDictionary(l => l, l => all.Count(e => e.Level == l)),
            Groups = parser.GroupExceptions(all),
            MatchCount = matches.Count,
            Truncated = matches.Count > MaxRows,
            Entries = matches.Take(MaxRows).ToList()
        });
    }

    [HttpGet]
    public IActionResult Export(Guid id, string? level, string? search, DateTime? from, DateTime? to)
    {
        var stored = store.Get(id);
        if (stored == null) return NotFound();
        var filter = new FilterCriteria { Level = level ?? "ALL", Search = search ?? "", From = from, To = to };
        var csv = CsvExporter.Export(LogFilter.Apply(parser.Parse(stored.Value.Content), filter));
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv)).ToArray(), "text/csv; charset=utf-8", "log-export.csv");
    }

    [HttpPost]
    public IActionResult Delete(Guid id)
    {
        store.Delete(id);
        return RedirectToAction(nameof(Index));
    }

    [AcceptVerbs("GET", "POST"), IgnoreAntiforgeryToken, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Error() => View("Error");
}
