# OpenMoney.CloudPayments

Неофициальный клиент CloudPayments + чеки CloudKassir.

## DI

```csharp
services.AddOpenMoneyCloudPayments(o =>
{
    o.PublicId = configuration["CloudPayments:PublicId"]!;
    o.ApiSecret = configuration["CloudPayments:ApiSecret"]!;
    o.Inn = configuration["CloudPayments:Inn"];
    o.CalculationPlace = "merchant.example";
});
```

Клиент: `CloudPaymentsClient`.

## Методы

- Оплата: `ChargeCryptogramAsync`, `AuthorizeCryptogramAsync`
- После auth: `ConfirmAsync`, `VoidAsync`, `RefundAsync`
- Чеки: `IssueReceiptAsync`, `IssueCommissionReceiptAsync`

На сервер передаётся только `CardCryptogramPacket` из виджета.

См. [pay-in](../processes/pay-in.md), [фискализация / CloudKassir](../processes/fiscal-income.md).  
Пример: `-- cloudpayments` (нужен `TRANSACTION_ID`).
