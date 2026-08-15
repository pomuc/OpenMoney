# OpenMoney.VTB

Неофициальный .NET 8 клиент эквайринга ВТБ RBS:

- регистрация карточного заказа и URL платёжной формы;
- динамический QR СБП C2B;
- строгий разбор form-urlencoded callback;
- контракт проверки контрольной суммы для интеграции на стороне мерчанта.

Пакет **не аффилирован** с ВТБ.

## Регистрация

```csharp
services.AddOpenMoneyVtbAcquiring(
    configuration.GetSection(VtbAcquiringOptions.SectionName));
services.AddSingleton<IVtbCallbackVerifier, MerchantVtbCallbackVerifier>();
```

См. `examples/appsettings.example.json`. Токен мерчанта храните в secret manager.

Базы, использовавшиеся в исходном flow:

- `https://platezh.vtb24.ru/payment`
- `https://vtb.rbsuat.com/payment`

## Старт оплаты

Суммы — целые минорные единицы (копейки).

```csharp
var (redirectOrPayload, bankOrderId) = await client.StartPaymentAsync(
    orderNumber: Guid.NewGuid(),
    amountMinorUnits: 50000,
    byCard: false,
    cancellationToken);
```

Для карты `redirectOrPayload` — URL хостед‑формы. Для СБП — payload URL QR НСПК. Сохраняйте merchant order id, bank order id и сумму до обработки callback.

## Пример

Полный runnable-пример: `examples/OpenMoney.SdkExamples` (`dotnet run --project examples/OpenMoney.SdkExamples -- vtb`).
