using System.Text;
namespace LogInspector.Services;

public sealed record UploadResult(string? Text, string? Error);

public static class UploadValidator
{
    public const int MaxBytes = 5 * 1024 * 1024;
    private static readonly string[] Extensions = [".log", ".txt"];

    public static UploadResult Validate(string? fileName, long length, Stream content)
    {
        var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
        if (!Extensions.Contains(ext)) return Fail("Yalnızca .log ve .txt dosyaları kabul edilir.");
        if (length <= 0) return Fail("Dosya boş.");
        if (length > MaxBytes) return Fail("Dosya en fazla 5 MB olabilir.");

        var buffer = new byte[MaxBytes + 1];
        var read = 0;
        int n;
        while (read < buffer.Length && (n = content.Read(buffer, read, buffer.Length - read)) > 0) read += n;
        if (read > MaxBytes) return Fail("Dosya en fazla 5 MB olabilir.");
        if (read == 0) return Fail("Dosya boş.");

        string text;
        try { text = new UTF8Encoding(false, true).GetString(buffer, 0, read); }
        catch (DecoderFallbackException) { return Fail("Dosya geçerli UTF-8 metin değil."); }
        return ValidateText(text);
    }

    public static UploadResult ValidateText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Fail("Log içeriği boş.");
        if (text.Length > MaxBytes) return Fail("Log metni en fazla 5 MB olabilir.");
        text = text.TrimStart('﻿');
        var control = 0;
        foreach (var ch in text)
        {
            if (ch == '\0') return Fail("Dosya ikili (binary) içerik barındırıyor.");
            if (char.IsControl(ch) && ch is not ('\t' or '\r' or '\n' or '\f')) control++;
        }
        if (control > text.Length / 20 + 1) return Fail("Dosya metin log gibi görünmüyor.");
        return new UploadResult(text, null);
    }

    private static UploadResult Fail(string error) => new(null, error);
}
