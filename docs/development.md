# Разработка

## Требования

- .NET SDK 8.x

## Сборка SDK

```bash
dotnet restore OpenMoney.sln
dotnet build OpenMoney.sln -c Release
dotnet pack OpenMoney.sln -c Release -o artifacts/nupkg
```

Версия пакетов — `sdks/Directory.Build.props` (сейчас `0.1.0`).

## Документация для разработчиков

Оглавление: [`docs/README.md`](README.md) — процессы, пакеты, MCP, примеры.

## Конфигурация

Копируйте [`examples/appsettings.example.json`](../examples/appsettings.example.json) и заполняйте **только плейсхолдеры** `YOUR_*`.

Не подключайте боевые терминалы и базы к локальной разработке без необходимости. См. [конфигурацию](configuration.md) и [песочницу](concepts/unofficial-and-sandbox.md).

## Примеры и MCP

```powershell
dotnet run --project examples/OpenMoney.SdkExamples -- list
dotnet build mcp\OpenMoney.Mcp\OpenMoney.Mcp.csproj -c Release
```

- [Примеры](examples.md)
- [MCP](mcp/overview.md)
- [`mcp/README.md`](../mcp/README.md)

## Структура репозитория

```
sdks/          # NuGet-пакеты SDK
examples/      # SdkExamples + appsettings.example.json
mcp/           # OpenMoney.Mcp + agent configs
docs/          # эта документация
```
