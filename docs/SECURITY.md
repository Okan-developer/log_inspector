# Security

| Area | Measure |
|------|---------|
| Persistence | Log content is never written to disk by the application: `LogStore` is an in-memory dictionary (max 20 entries, 60-minute TTL, manual delete, cleared on restart). `FormOptions.MemoryBufferThreshold` is set above the upload limit so ASP.NET Core does not spill multipart bodies to temp files. The app does not log request bodies. |
| Upload | Allow-list `.log`/`.txt`, 5 MB cap enforced on actual bytes read (not the declared length), strict UTF-8, NUL/control-character rejection, Kestrel + form limits (5 MB + 1 MB overhead) |
| Identifiers | Entries are keyed by random GUIDs; the user file name is sanitised (`Path.GetFileName`, control chars stripped) and used for display only; `Guid` route binding rejects traversal |
| XSS | Razor encoding everywhere, no `Html.Raw`; CSP `default-src 'self'` (no inline script/style); tests post `<script>` payloads in log text and file names |
| CSRF | Global `AutoValidateAntiforgeryToken`; all state changes are POST |
| CSV | Cells starting with `= + - @ TAB CR` are prefixed with `'`; quotes escaped; attachment + `nosniff` |
| DoS | Regex timeouts (200 ms, caught), 4000-char line cap, 1000 rendered rows, max 20 stored logs (worst case ≈ 100 MB RAM) |
| Headers | CSP, `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, HSTS outside Development |
| Errors | Generic error page, no stack traces exposed |

Verified by tests: `Form_Buffering_Stays_In_Memory_For_Max_Upload` (fails if the threshold setting is removed), `Source_Has_No_Filesystem_Writes` (source scan of Services/Controllers/Models), `Large_Upload_Leaves_No_Persistent_Copy_On_Disk` (only detects copies that remain after the request; temporary framework files are covered by the buffering test).

Known limitations
- No authentication or per-user separation: the recent-analyses list is global, and a GUID is the only access control.
- Framework-level disk use outside application control is not covered: the OS may swap process memory, and ASP.NET Core Data Protection keys (antiforgery) may be written to the user profile; these contain no log content.
- Deploy only locally or on a trusted network.
