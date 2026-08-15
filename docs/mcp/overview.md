# MCP: обзор

**OpenMoney.Mcp** — stdio MCP‑сервер, который поднимает **реальные** OpenMoney SDK и отдаёт агенту tools для платежей, НПД, фискализации и KYC.

Это не каталог подсказок: без credentials tool вернёт JSON с ошибкой и подсказкой env; с credentials — HTTP к API провайдера.

## Запуск

```powershell
dotnet build mcp\OpenMoney.Mcp\OpenMoney.Mcp.csproj -c Release
dotnet run --project mcp\OpenMoney.Mcp\OpenMoney.Mcp.csproj -c Release
```

Конфиг: `mcp/OpenMoney.Mcp/appsettings.json` или переменные `Секция__Ключ`.

## Что поднимается всегда

- **Fiscal** (`FnsClient`)
- **Kyc.MoyNalog** (`MoyNalogKycClient`)

Остальные SDK — только если в конфиге заполнены обязательные ключи (`SdkBootstrap`).

## Первые вызовы агента

1. `openmoney_status` — какие провайдеры live.
2. `openmoney_list_tools_help` — список tool‑имён.
3. Боевой tool нужного провайдера.

## Безопасность

- Ключи только в локальном конфиге / env агента.
- Payout / refund — только с human‑in‑the‑loop.
- Сначала песочница провайдера.

Далее: [справочник tools](tools-reference.md), [агенты](agents.md).
