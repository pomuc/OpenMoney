# Примеры OpenMoney

## Код SDK (главное)

Сборка и запуск сценариев по каждому провайдеру:

```powershell
dotnet run --project examples/OpenMoney.SdkExamples -- list
dotnet run --project examples/OpenMoney.SdkExamples -- tbank
dotnet run --project examples/OpenMoney.SdkExamples -- yoomoney
```

Исходники: [`OpenMoney.SdkExamples/Samples/`](OpenMoney.SdkExamples/Samples/)

| Sample | Файл | Что делает |
|---|---|---|
| `tbank` | `TBankSample.cs` | Init pay-in → GetStatus |
| `yoomoney` | `YooMoneySample.cs` | safe_deal → payment |
| `vtb` | `VtbSample.cs` | Старт оплаты RBS |
| `cloudpayments` | `CloudPaymentsSample.cs` | Confirm / Refund / Void |
| `inwizo` | `InwizoSample.cs` | Hosted payment URL |
| `tochka` | `TochkaSample.cs` | Create recipient |
| `fiscal` | `FiscalSample.cs` | Статус НПД по ИНН |
| `npd` | `SelfEmployedSample.cs` | Список получателей |
| `kyc` | `KycSample.cs` | MoyNalog / Didit / MTS |

Ключи: [`OpenMoney.SdkExamples/appsettings.json`](OpenMoney.SdkExamples/appsettings.json) или env (`TBank__TerminalKey`, `YooMoney__ShopId`, …).

Шаблон всех секций: [`appsettings.example.json`](appsettings.example.json).

## MCP (агенты)

Конфиги Claude / Cursor / Codex / VS Code — в [`../mcp/examples/`](../mcp/examples/).
