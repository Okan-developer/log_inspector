using LogInspector.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
const long limit = UploadValidator.MaxBytes + 1_048_576;
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = limit);
// MemoryBufferThreshold = limit: uploads are buffered in memory and never spill to temp files on disk.
builder.Services.Configure<FormOptions>(o => { o.MultipartBodyLengthLimit = limit; o.ValueLengthLimit = (int)limit; o.MemoryBufferThreshold = (int)limit; });
builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddSingleton<LogParser>();
builder.Services.AddSingleton<LogStore>();

var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.UseStatusCodePagesWithReExecute("/Home/Error");
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["Content-Security-Policy"] = "default-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "no-referrer";
    await next();
});
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

public partial class Program;
