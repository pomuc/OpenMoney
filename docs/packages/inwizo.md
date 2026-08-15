# OpenMoney.Inwizo

Неофициальный клиент hosted‑оплаты и выплат Inwizo. Подпись legacy MD5 (uppercase).

## DI

```csharp
services.AddOpenMoneyInwizo(o =>
{
    o.BaseUrl = configuration["Inwizo:BaseUrl"]!; // именной host от Inwizo
    o.Account = configuration["Inwizo:Account"]!;
    o.ApiKey = configuration["Inwizo:ApiKey"]!;
    o.Operator = configuration["Inwizo:Operator"]!;
    o.HostedPaymentUrl = "https://merchant.example/pay/inwizo";
    o.HostedCardUrl = "https://merchant.example/cards/inwizo/add";
});
```

Клиент: `InwizoClient`. `BaseUrl` обязателен (именной адрес клиента, без дефолта в SDK).

## Методы

- `InitializeHostedPayment` → `PaymentUrl`
- `CreateCardRegistrationUrl`
- `GetPaymentStatusAsync`
- `InitializePayoutAsync`, `GetPayoutStatusAsync`

См. [pay-in](../processes/pay-in.md), [payout](../processes/payout.md), [sbp-qr](../processes/sbp-qr.md). Пример: `-- inwizo`.
