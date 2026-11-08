# Code Review

Three independent read-only reviews (Architect, Security, QA) of the original code. Findings by severity and status.

| # | Sev | Role | Finding | Status |
|---|-----|------|---------|--------|
| 1 | High | QA | Level matched case-insensitively anywhere (`info` in prose → INFO) | Fixed (upper-case token near line start) |
| 2 | High | QA | Stack lines took levels from namespaces (`My.Debug.Helper`) and polluted inherited level | Fixed |
| 3 | High | QA | `Caused by:` never matched (`\b` after colon); any word ending `error` flagged | Fixed (case-sensitive patterns, separate Caused-by rule) |
| 4 | High | QA/Sec | Regex timeout unhandled → 500; unbounded line length | Fixed (4000-char cap, timeout caught) |
| 5 | High | Arch | No upload, persistence, ids, grouping, timestamps in model | Implemented |
| 6 | High | Sec | No 5 MB limits / upload validation / path-traversal safety | Implemented (GUID keys, bounded read) |
| 7 | High | Sec | CSV export formula injection | Implemented guard |
| 8 | Med | Sec | No security headers, no global antiforgery, status pages | Fixed (CSP, nosniff, DENY, global antiforgery) |
| 9 | Med | QA | Blank lines inflated OTHER count; level filter case-sensitive; search untrimmed | Fixed |
| 10 | Med | Arch | Unbounded rendered rows | Fixed (1000-row cap) |
| 11 | Med | Arch | Reviewer suggested EF Core + SQLite and parser abstractions | Not adopted (extra dependency; in-memory store sufficient) |
| 12 | Low | Sec | No authentication / per-user isolation (GUID is the only access control) | Open, documented in SECURITY.md |
| 13 | Low | Arch | Strings hard-coded Turkish (no localisation) | Open |
| 14 | High | Sec | Logs persisted to disk (file store; framework temp-file buffering) contrary to privacy requirement | Fixed (memory-only store, in-memory form buffering) |

Found during TDD: status-code re-execute turned antiforgery failures into 405 (`Error` action was GET-only) — fixed.
