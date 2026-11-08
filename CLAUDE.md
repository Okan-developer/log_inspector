# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

LogInspector is an ASP.NET Core MVC app (.NET 10, C#, nullable + implicit usings) that analyzes Java/Spring Boot, .NET and Oracle log files in the browser. It is **memory-only**: no database, no disk writes, no client-side JavaScript, no third-party runtime dependencies. UI text and the README are in Turkish; `docs/` is in English.

## Commands

```bash
dotnet restore LogInspector.slnx
dotnet build LogInspector.slnx -c Release
dotnet run --project LogInspector.csproj
dotnet test LogInspector.slnx -c Release --filter "Category!=Perf"   # what CI runs

# Single test class / test
dotnet test tests/LogInspector.Tests --filter "FullyQualifiedName~ParserTests"
dotnet test tests/LogInspector.Tests --filter "FullyQualifiedName~ParserTests.Lowercase_Words_In_Message_Are_Not_Levels"

# Benchmarks (excluded from CI; [Trait("Category","Perf")])
dotnet test tests/LogInspector.Tests -c Release --filter Category=Perf --logger "console;verbosity=detailed"
```

The web project lives at the repo root and the test project under `tests/`, so `LogInspector.csproj` explicitly removes `tests/**` from compilation — keep that when restructuring.

## Architecture

Single controller (`Controllers/HomeController.cs`) orchestrating stateless services:

- `UploadValidator` — `.log`/`.txt` allow-list, 5 MB limit enforced on bytes actually read (not the declared length), strict UTF-8, rejects NUL/control chars. `MaxBytes` also drives Kestrel/form limits in `Program.cs` and the controller's `RequestLimit`.
- `LogStore` (singleton) — `ConcurrentDictionary` keyed by server-generated GUID; max 20 entries, 60 min TTL, evicted lazily on each call. Takes a `Func<DateTime>` clock for tests. User file names are display-only and sanitized via `SafeName`.
- `LogParser` (singleton) — **logs are re-parsed on every request** (Analysis and Export); nothing parsed is cached. `Parse` produces `LogEntry` per non-blank line; `GroupExceptions` groups top-level exception lines by type and attaches the following stack/`Caused by:` lines (max 5 occurrences per group).
- `LogFilter` (level / case-insensitive search / date range), `CsvExporter` (RFC 4180 + formula-injection guard for cells starting with `= + - @ TAB CR`), `ErrorKnowledge` (Turkish explanations for exception types and ORA codes), `SampleLogs` (synthetic `java`/`dotnet` samples).

Analysis page renders at most 1000 rows; CSV export contains all filtered rows. Filters are passed via query string to both `Analysis` and `Export`.

### Parser rules (heuristic, tested heavily in `ParserTests`)

- Level = first **upper-case** token (`ERROR|ERR|FATAL|WARN|WARNING|INFO|DEBUG|TRACE`) within the first 120 chars, not adjacent to word chars or dots (so `My.Debug.Helper` / prose `info` don't count); fallback to .NET `info:/warn:/fail:/crit:/dbug:/trce:` prefixes. Normalized to ERROR/WARN/INFO/DEBUG, else OTHER.
- Stack lines are never level-matched or exception-matched; stack and exception lines inherit the previous level and timestamp.
- Exception type = first of `ORA-NNNNN`, fully-qualified `...Exception|...Error`, or short `XxxException` on the line (case-sensitive).
- Timestamp: `yyyy-MM-dd HH:mm:ss` (space or `T`). Lines longer than 4000 chars are truncated; all regexes have a 200 ms timeout and `RegexMatchTimeoutException` degrades the line to OTHER instead of failing.

## Security/privacy invariants (enforced by tests)

- **No filesystem writes.** `WebTests.Source_Has_No_Filesystem_Writes` scans `*.cs` source for `File.Write/Create/Open...`, `FileStream`, `StreamWriter`, `SaveAs(`, `CopyTo(` etc. and fails the build if found; another test scans temp/output dirs for uploaded content. `FormOptions.MemoryBufferThreshold` is set to the upload limit so multipart uploads never spill to temp files. Do not log log content.
- Global `AutoValidateAntiforgeryToken`; all state-changing actions are POST. The `Error` action accepts GET+POST and ignores antiforgery so that status-code re-execution of a failed POST returns 400, not 405.
- Security headers (CSP `default-src 'self'`, nosniff, `X-Frame-Options: DENY`, `no-referrer`) are set in middleware in `Program.cs`. No `Html.Raw` in views; no inline scripts/styles (CSP would block them).
- `public partial class Program;` at the bottom of `Program.cs` exists for `WebApplicationFactory<Program>` in `WebTests`.

Deliberately rejected: EF Core/SQLite persistence and parser abstractions (see `docs/CODE_REVIEW.md`). Known open items: no auth/per-user isolation, hard-coded Turkish strings.

## Docs

`docs/` holds ARCHITECTURE, SECURITY, TESTING, PERFORMANCE, CI_CD and CODE_REVIEW notes; README contains test counts and benchmark tables — update them when changing behavior covered there. Note: README/CI_CD.md reference `.github/workflows/ci.yml`, but that file is not present in this checkout.
