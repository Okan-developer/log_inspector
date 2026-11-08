# Architecture

```
Browser ─ HomeController ─┬─ UploadValidator   (extension / size / UTF-8 / binary checks)
                          ├─ LogStore          (in-memory, TTL 60 min, max 20)
                          ├─ LogParser         (lines → LogEntry; GroupExceptions → ExceptionGroup)
                          ├─ LogFilter         (level, search, date range)
                          ├─ CsvExporter       (RFC 4180 + formula-injection guard)
                          └─ ErrorKnowledge / SampleLogs
```

Routes: `GET /` (upload form, recent in-memory analyses), `POST Home/Upload`, `POST Home/Sample`, `GET Home/Analysis/{id}` (filters via query string), `GET Home/Export/{id}`, `POST Home/Delete`.

Decisions
- Logs are held in memory only (never on disk) and re-parsed on each request (5000 lines ≈ 15 ms, see PERFORMANCE.md); no database or extra dependency.
- Entries are keyed by server-generated GUIDs; user file names are only display metadata.
- The page renders at most 1000 rows; CSV export contains all filtered rows.
- Antiforgery is validated globally for all unsafe methods.
