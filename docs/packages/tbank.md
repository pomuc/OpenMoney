# OpenMoney.TBank

Неофициальный .NET 8 SDK эквайринга и E2C Т‑Банка.

## DI

```csharp
services.AddTBank(o =>
{
    o.TerminalKey = configuration["TBank:TerminalKey"]!;
    o.TerminalPassword = configuration["TBank:TerminalPassword"]!;
    o.PayoutTerminalKey = configuration["TBank:PayoutTerminalKey"];
    o.PayoutTerminalPassword = configuration["TBank:PayoutTerminalPassword"];
});
```

Клиент: `ITBankAcquiringClient`.

## Методы (основные)

| Группа | Методы |
|---|---|
| Pay-in | `InitPayInAsync`, `ChargeAsync`, `ConfirmAsync`, `CancelAsync`, `GetStatusAsync`, `CheckOrderAsync` |
| Payout | `InitPayoutAsync`, `PaymentAsync`, `InitMomentPayoutAsync`, `MomentPaymentAsync` |
| Карты pay-in | `AddCardAsync`, `RemoveCardAsync`, `GetCardListAsync` |
| Карты payout | `AddPayoutCustomerAsync`, `GetPayoutCustomerAsync`, `AddPayoutCardAsync`, `GetPayoutCardsAsync`, … |
| Прочее | `CreateSecureDealAsync`, `CreateQrAsync` |
| Чеки | `MakePaycheckAsync`, `MakeReturnPaycheckAsync`, `MakeAgentPaycheckAsync`, … |

Токен подписи запросов SDK считает сам (SHA‑256).

## Процессы

- [Приём оплаты](../processes/pay-in.md)
- [Выплаты](../processes/payout.md)
- [Безопасная сделка / Мультирасчёты](../processes/safe-deal.md)
- [СБП QR](../processes/sbp-qr.md)
- [Фискализация (paycheck / агентский чек)](../processes/fiscal-income.md)

Пример: `-- tbank`. README пакета: `sdks/OpenMoney.TBank/README.md`.
