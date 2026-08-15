# CI

GitHub Actions: [`.github/workflows/sdk-ci.yml`](../.github/workflows/sdk-ci.yml)

- `dotnet restore`
- `dotnet build -c Release`
- `dotnet pack` → артефакты `artifacts/nupkg`

Автопубликация в NuGet не выполняется.
