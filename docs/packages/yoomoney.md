# OpenMoney.YooMoney

Неофициальный клиент ЮKassa для **safe_deal** и выплаты физлицу.

## DI

```csharp
services.AddOpenMoneyYooMoney(o =>
{
    o.ShopId = configuration["YooMoney:ShopId"]!;
    o.SecretKey = configuration["YooMoney:SecretKey"]!;
});
```

Клиент: `IYooMoneyClient`.

## Методы

| Метод | Назначение |
|---|---|
| `CreateSafeDealAsync` | Открыть сделку |
| `CreatePaymentAsync` | Платёж с привязкой к deal |
| `GetPaymentAsync` / `GetDealAsync` | Статусы |
| `HasDealBalanceAsync` | Готовность к выплате |
| `CreatePayoutAsync` | Выплата по `PayoutToken` |

Полный процесс: [safe-deal](../processes/safe-deal.md). Пример: `-- yoomoney`.
