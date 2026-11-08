# Testing

`dotnet test LogInspector.slnx -c Release` — xUnit, 61 tests (+ 3 perf benchmarks excluded in CI via `Category!=Perf`).

| File | Covers |
|------|--------|
| `ParserTests` | empty/null, levels, false positives, Caused by, stack inheritance, CRLF, binary garbage |
| `FeatureTests` | exception grouping/stack attach, ORA, .NET types, timestamps, filters, CSV escaping, upload validation (extension, empty, binary, invalid UTF-8, 5 MB boundary, spoofed length), in-memory store (TTL, cap), samples |
| `WebTests` | integration via `WebApplicationFactory`: missing antiforgery, XSS in log text and file name, bad extension, empty submit, no-disk-write checks (buffering config, source scan), sample → filter → CSV, 404, security headers, traversal id |

TDD record: parser tests were written first against the original code and failed 2 of 12
(`Lowercase_Words_In_Message_Are_Not_Levels`, `Word_Ending_In_Error_Lowercase_Is_Not_Exception`); the rest of the suite was written together with the new features, and one more defect (405 instead of 400 on antiforgery failure) was caught by `WebTests` and fixed.

Last local result (Release): **61 passed, 0 failed**.
