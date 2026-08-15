# OpenMoney.Inwizo

Неофициальный .NET 8 клиент платёжных и выплатных сценариев Inwizo.

Пакет **не аффилирован** с Inwizo.

```csharp
services.AddOpenMoneyInwizo(o =>
{
    // Именной host от Inwizo — только из env/конфига, без хардкода в коде.
    o.BaseUrl = configuration["Inwizo:BaseUrl"]!;
    o.Account = configuration["Inwizo:Account"]!;
    o.ApiKey = configuration["Inwizo:ApiKey"]!;
    o.Operator = configuration["Inwizo:Operator"]!;
    o.HostedPaymentUrl = "https://merchant.example/pay/inwizo";
    o.HostedCardUrl = "https://merchant.example/cards/inwizo/add";
});
```

`InwizoClient` формирует URL hosted‑оплаты и регистрации карты, проверяет статус оплаты картой/СБП, выполняет выплаты на карту и проверяет статус выплаты. Формы URL‑encoded и подписываются legacy MD5 (uppercase) по схеме провайдера.

`BaseUrl` обязателен: Inwizo выдаёт клиентам **именные** API‑адреса. Задавайте через `Inwizo__BaseUrl` / `Inwizo:BaseUrl`.

Account, operator, API key, токены карт и подписи считайте секретами. В проде не логируйте тела запросов/ответов.

## Пример

Полный runnable-пример: `examples/OpenMoney.SdkExamples` (`dotnet run --project examples/OpenMoney.SdkExamples -- inwizo`).
