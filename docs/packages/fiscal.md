# OpenMoney.Fiscal

Клиент «Мой налог» (ФНС НПД): SMS‑auth, статус, доход, отмена. Неофициальный.

## DI

```csharp
services.AddOpenMoneyFiscal(o =>
{
    o.LegalEntityInn = configuration["Fiscal:LegalEntityInn"];
    o.LegalEntityName = configuration["Fiscal:LegalEntityName"];
});
```

Клиент: `FnsClient`.

## Методы

- Auth: `StartSmsChallengeAsync`, `VerifySmsChallengeAsync`, `RefreshTokenAsync`
- Статус: `CheckTaxpayerStatusAsync`, `GetActiveStatusAsync`, `GetProfileAsync`
- Доход: `IssueIncomeAsync`, `CancelIncomeAsync`
- Helpers: `GenerateDeviceId`, модели `FiscalReceipt` / `CloudKassirReceiptFactory` (агентский `AgentSign = 6`)

Процесс: [фискализация — НПД + CloudKassir](../processes/fiscal-income.md). Пример НПД: `-- fiscal` + `NPD_INN`.
