# CI/CD

Workflow: `.github/workflows/ci.yml` — on push to `main` and on pull requests: checkout → setup .NET 10 → `dotnet restore` → `dotnet build -c Release` → `dotnet test -c Release` (perf benchmarks excluded).

Status: **the workflow has not been run on GitHub.** The same restore/build/test sequence was verified locally in Release (0 warnings, 0 errors, 61 tests passed). No deployment stage is defined.
