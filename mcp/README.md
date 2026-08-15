# OpenMoney MCP

MCP‑сервер, который **подключает реальные OpenMoney SDK** и отдаёт агенту боевые tools: платежи, выплаты, НПД, фискализацию, KYC.

Агенты (Claude Desktop, Cursor, Codex, VS Code; мосты к GigaChat / Алисе) вызывают провайдеров через stdio MCP — те же клиенты, что в NuGet‑пакетах.

Без ключей в конфиге tool вернёт JSON с `ok: false` и подсказкой по `appsettings` / env (`Секция__Ключ`). С ключами — реальный HTTP к API провайдера.

Подробности для разработчиков — в [`docs/README.md`](../docs/README.md) (процессы, конфигурация, MCP).

## Быстрый старт

```powershell
cd YOUR_OPENMONEY_ROOT
dotnet build mcp\OpenMoney.Mcp\OpenMoney.Mcp.csproj -c Release
```

Заполните `mcp/OpenMoney.Mcp/appsettings.json` или переменные окружения, например:

```powershell
$env:TBank__TerminalKey = "..."
$env:TBank__TerminalPassword = "..."
$env:YooMoney__ShopId = "..."
$env:YooMoney__SecretKey = "..."
```

Запуск (stdio):

```powershell
dotnet run --project mcp\OpenMoney.Mcp\OpenMoney.Mcp.csproj -c Release
```

Сначала вызовите `openmoney_status` — увидите, какие SDK реально подняты.

## Инструменты

| Группа | Tools |
|---|---|
| Статус | `openmoney_status`, `openmoney_list_tools_help` |
| Т‑Банк | `tbank_init_payin`, `tbank_get_status`, `tbank_cancel`, `tbank_create_qr`, `tbank_init_payout` |
| ЮKassa | `yoomoney_create_safe_deal`, `yoomoney_create_payment`, `yoomoney_get_payment`, `yoomoney_get_deal`, `yoomoney_create_payout` |
| ВТБ | `vtb_start_payment` |
| CloudPayments | `cloudpayments_confirm`, `cloudpayments_refund`, `cloudpayments_void` |
| Inwizo | `inwizo_init_hosted_payment`, `inwizo_payment_status`, `inwizo_payout`, `inwizo_payout_status` |
| Точка | `tochka_create_recipient`, `tochka_create_order`, `tochka_get_order`, `tochka_confirm_services` |
| ФНС | `fiscal_check_taxpayer_status`, `fiscal_start_sms`, `fiscal_verify_sms` |
| Самозанятые | `npd_list_recipients`, `npd_sync_recipients` |
| KYC | `kyc_moynalog_check_status`, `kyc_didit_*`, `kyc_mts_*`, `kyc_mts_rim_*` |

Fiscal / MoyNalog поднимаются всегда (публичные endpoints). Остальные — только при наличии credentials.

## Конфигурация

Секции в `appsettings.json` (или env с `__`):

| Секция | Минимум ключей |
|---|---|
| `TBank` | `TerminalKey`, `TerminalPassword` |
| `YooMoney` | `ShopId`, `SecretKey` |
| `VtbAcquiring` | `Token` |
| `CloudPayments` | `PublicId`, `ApiSecret` |
| `Inwizo` | `Account`, `ApiKey`, `Operator` |
| `Tochka` | `BaseUrl`, `ClientId`, `KeyId`, `CertificatePemPath`, `PrivateKeyPemPath` |
| `TBankNpd` | `Token` |
| `Kyc:Didit` | `ClientId`, `ClientSecret` |
| `Kyc:MtsId` | `ClientId`, PEM/signing + notification |
| `Kyc:MtsRim` | `AccessToken` |

Пример env для Cursor — в [`examples/cursor_mcp.json`](examples/cursor_mcp.json).

## Подключение к агентам

| Агент | Файл |
|---|---|
| Claude Desktop | [`examples/claude_desktop_config.json`](examples/claude_desktop_config.json) |
| Cursor | [`examples/cursor_mcp.json`](examples/cursor_mcp.json) |
| Codex | [`examples/codex_config.toml`](examples/codex_config.toml) |
| VS Code / Copilot | [`examples/vscode_mcp.json`](examples/vscode_mcp.json) |
| GigaChat | [`examples/gigachat_bridge.md`](examples/gigachat_bridge.md) |
| Алиса | [`examples/alice_skill.md`](examples/alice_skill.md) |

## Безопасность

- Ключи только в локальном `appsettings` / secret store / env агента — не в git.
- Payout и возвраты — только с human‑in‑the‑loop у агента.
- Для боя сначала песочница провайдера.
