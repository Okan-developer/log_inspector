# Performance

Release build, median of 5 runs after warm-up (ms). Synthetic log, ~10 % errors with stack traces. Reproduce:
`dotnet test tests/LogInspector.Tests -c Release --filter Category=Perf --logger "console;verbosity=detailed"`

| Lines | Old parser (baseline) | New parse | Group exceptions | Filter (level+search+date) | CSV export |
|------:|------:|------:|------:|------:|------:|
| 100 | 0.60 | 0.29 | 0.01 | 0.00 | 0.05 |
| 1000 | 6.17 | 2.78 | 0.05 | 0.05 | 0.32 |
| 5000 | 32.56 | 14.68 | 0.10 | 0.26 | 1.43 |

Conclusion: no real bottleneck; every operation is far below the rendering/network cost, so no further optimisation was done. The new parser is faster than the baseline because level search is limited to the first 120 characters and regexes are case-sensitive. Baseline only runs the three regexes (no timestamp parsing), so the comparison is conservative. A 5 MB file (~50k lines) extrapolates to roughly 150 ms parse time; the page renders at most 1000 rows.
