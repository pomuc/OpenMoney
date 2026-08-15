# OpenMoney.YooMoney

Неофициальный .NET 8 клиент **ЮKassa** (`api.yookassa.ru`) для сценария безопасной сделки и выплаты физлицу.

Пакет **не аффилирован** с ЮMoney / ЮKassa.

## Возможности

1. **Создать безопасную сделку** — `POST /v3/deals` (`type=safe_deal`)
2. **Принять оплату** с settlement на payout — `POST /v3/payments` + `deal.id=dl-{guid}`
3. **Проверить баланс сделки** — `GET /v3/deals/dl-{guid}`
4. **Выплатить физлицу** по `payout_token` — `POST /v3/payouts` в рамках сделки

## Регистрация

```csharp
services.AddOpenMoneyYooMoney(o =>
{
    o.ShopId = configuration["YooMoney:ShopId"]!;
    o.SecretKey = configuration["YooMoney:SecretKey"]!;
    // опционально несколько магазинов:
    // o.Shops["YOUR_SECOND_SHOP_ID"] = configuration["YooMoney:Shops:YOUR_SECOND_SHOP_ID"]!;
});
```

## Типовой поток

```csharp
var deal = await yoo.CreateSafeDealAsync(new YooCreateDealRequest());
if (!deal.Success) throw new InvalidOperationException(deal.Status);

var pay = await yoo.CreatePaymentAsync(new YooCreatePaymentRequest(
    AmountMinorUnits: 100_000,          // 1000.00 ₽ от плательщика
    PayoutAmountMinorUnits: 80_000,     // 800.00 ₽ уйдёт на выплату
    DealId: deal.ExternalDealId,
    ReturnUrl: "https://merchant.example/pay/return",
    Description: "Оплата заказа"));

// … пользователь оплачивает ConfirmationUrl …

if (await yoo.HasDealBalanceAsync(deal.ExternalDealId))
{
    var payout = await yoo.CreatePayoutAsync(new YooCreatePayoutRequest(
        AmountMinorUnits: 80_000,
        DealId: deal.ExternalDealId,
        PayoutToken: configuration["YooMoney:PayoutToken"]!, // токен карты/кошелька
        OrderId: "order-123"));
}
```

ShopId / SecretKey / payout_token — только из конфигурации. Не коммитьте боевые ключи.

## Пример

Полный runnable-пример: `examples/OpenMoney.SdkExamples` (`dotnet run --project examples/OpenMoney.SdkExamples -- yoomoney`).
