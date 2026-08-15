# OpenMoney.Tochka

Неофициальный клиент API «Точки» (Medusa): получатели, карты, заказы, решения. Запросы подписываются RSA‑SHA256 по PEM.

## DI

```csharp
services.AddOpenMoneyTochka(o =>
{
    o.BaseUrl = configuration["Tochka:BaseUrl"]!;
    o.ClientId = configuration["Tochka:ClientId"]!;
    o.KeyId = configuration["Tochka:KeyId"]!;
    o.CertificatePemPath = configuration["Tochka:CertificatePemPath"]!;
    o.PrivateKeyPemPath = configuration["Tochka:PrivateKeyPemPath"]!;
    o.SuccessRedirectUrl = "https://merchant.example/pay/success";
    o.FailureRedirectUrl = "https://merchant.example/pay/failure";
    o.EnableSandboxOperations = false; // в проде
});
```

Клиент: `TochkaClient`.

## Методы

- Получатели/карты: `CreateRecipientAsync`, `GetRecipientAsync`, `GetRecipientCardsAsync`, `CreateCardAsync`
- Заказы: `CreateOrderAsync`, `GetOrderAsync`, `SetOrderDecisionAsync`, `ConfirmAllServicesAsync`
- UAT: `RunSandboxOperationAsync` (только при `EnableSandboxOperations`)

## Процесс

1. Recipient → Card form → Order → оплата → Confirm services.  
Полный разбор: [безопасная сделка / Medusa](../processes/safe-deal.md).  
См. также [pay-in](../processes/pay-in.md). Пример: `-- tochka`.
