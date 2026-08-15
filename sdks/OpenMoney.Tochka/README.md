# OpenMoney.Tochka

Неофициальный .NET 8 SDK для API банка «Точка» (Medusa): получатели, карты как способ выплаты, заказы, решения и sandbox‑операции.

Пакет **не аффилирован** с банком «Точка».

## Подключение

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
});
```

`TochkaClient` предоставляет: `CreateRecipientAsync`, `GetRecipientAsync`, `GetRecipientCardsAsync`, `CreateCardAsync`, `CreateOrderAsync`, `GetOrderAsync`, `SetOrderDecisionAsync`, `ConfirmAllServicesAsync` и явно ограниченные sandbox‑методы. Запросы подписываются RSA‑SHA256/PKCS#1 по PEM‑файлам.

Не коммитьте PEM. В проде держите `EnableSandboxOperations = false`.

## Пример

Полный runnable-пример: `examples/OpenMoney.SdkExamples` (`dotnet run --project examples/OpenMoney.SdkExamples -- tochka`).
