# OpenMoney.VTB

Неофициальный клиент эквайринга ВТБ RBS: регистрация оплаты, СБП QR, разбор callback.

## DI

```csharp
services.AddOpenMoneyVtbAcquiring(configuration.GetSection(VtbAcquiringOptions.SectionName));
services.AddSingleton<IVtbCallbackVerifier, MerchantVtbCallbackVerifier>();
```

Клиент: `VtbAcquiringClient`.

## Методы

- `RegisterCardPaymentAsync`
- `CreateSbpQrAsync`
- `StartPaymentAsync(orderNumber, amountMinorUnits, byCard)` → `(redirectOrPayload, bankOrderId)`
- Callback: `VtbCallbackParser` + `IVtbCallbackVerifier`

## BaseUrl

| Среда | URL |
|---|---|
| UAT | `https://vtb.rbsuat.com/payment` |
| Prod | `https://platezh.vtb24.ru/payment` |

См. [pay-in](../processes/pay-in.md), [sbp-qr](../processes/sbp-qr.md). Пример: `-- vtb`.
