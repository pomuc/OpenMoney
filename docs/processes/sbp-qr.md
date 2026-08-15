# Процесс: СБП и QR

Цель: принять оплату через Систему быстрых платежей (QR / payload).

## Т‑Банк

1. Создайте платёж: `InitPayInAsync`.
2. `CreateQrAsync(PaymentId, dataType)` — например `PAYLOAD`.
3. Покажите QR пользователю; статус — `GetStatusAsync`.

## ВТБ

```csharp
var (payload, bankOrderId) = await client.StartPaymentAsync(
    orderNumber: Guid.NewGuid(),
    amountMinorUnits: 50_000,
    byCard: false, // СБП
    ct);
```

`payload` — данные для QR. Дальше — callback как в [pay-in](pay-in.md).

Либо напрямую `CreateSbpQrAsync`.

## Inwizo

`InitializeHostedPayment(..., Method: InwizoPaymentMethod.Sbp)` — hosted СБП. При отдельных credentials используйте `SbpAccount` / `SbpApiKey`.

## Практика

- Сумма и OrderId фиксируются **до** генерации QR.
- TTL QR ограничен провайдером — обновляйте при истечении.
- Не путайте «payload строки QR» с URL карточной формы.
