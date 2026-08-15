# OpenMoney.TBank

Неофициальный .NET 8 SDK для эквайринга и E2C API Т‑Банка. Поддерживает DI/`HttpClient`, `CancellationToken`, расчёт Token (SHA‑256), опциональную подпись сертификатом, pay-in/pay-out, карты, безопасную сделку, СБП QR и вызовы чеков CloudPayments.

Пакет **не аффилирован** с Т‑Банком.

## Регистрация

```csharp
services.AddTBank(options =>
{
    options.TerminalKey = configuration["TBank:TerminalKey"]!;
    options.TerminalPassword = configuration["TBank:TerminalPassword"]!;
    options.PayoutTerminalKey = configuration["TBank:PayoutTerminalKey"];
    options.PayoutTerminalPassword = configuration["TBank:PayoutTerminalPassword"];
});
```

Далее резолвите `ITBankAcquiringClient`:

```csharp
var response = await client.InitPayInAsync(
    new RequestInitPaymentContext
    {
        Amount = 10_000,
        OrderId = "order-123",
        Description = "Оплата заказа"
    },
    cancellationToken);
```

Клиент подставляет реквизиты терминала и считает `Token`; секреты в JSON не сериализуются. Для подписанных E2C нужны `SigningCertificatePem` и `SigningPrivateKeyPem`. Для чеков — credentials CloudPayments и `Inn`.

Храните секреты в vault / переменных окружения / конфигурации вне git.

## Ошибки

Неуспешный HTTP → `TBankApiException` (статус и тело ответа). Ошибки уровня API при HTTP 200 смотрите в полях `Success`, `ErrorCode`, `Message`, `Details` модели ответа.

## Пример

Полный runnable-пример: `examples/OpenMoney.SdkExamples` (`dotnet run --project examples/OpenMoney.SdkExamples -- tbank`).
