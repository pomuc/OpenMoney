# Суммы и идентификаторы

## Суммы

В большинстве SDK суммы передаются в **копейках** (`long`, minor units):

| Значение | Смысл |
|---|---|
| `100` | 1,00 ₽ |
| `10_000` | 100,00 ₽ |
| `100_000` | 1 000,00 ₽ |

Исключения:

- **CloudPayments** — `decimal` в рублях (`10.00m`) для Confirm/Refund/Charge.
- **Tochka** — внутри клиента копейки конвертируются в строку денег API.

Не смешивайте единицы в одном заказе: храните в БД копейки и конвертируйте на границе провайдера.

## Идентификаторы заказа

| Провайдер | Рекомендация |
|---|---|
| TBank | Свой `OrderId` (строка); банк вернёт `PaymentId` |
| VTB | Свой `Guid` merchant order + `bankOrderId` (`mdOrder`) из ответа |
| YooMoney | Свои GUID для deal/payment; payout — ещё `OrderId` строки |
| Tochka | `Guid` для recipient / order / service / card |
| Inwizo | Свой `OrderId` + `ExternalPaymentId` (Guid) |

Правила:

1. **Идемпотентность.** Повторный Init с тем же `OrderId` у провайдера может вернуть уже созданный платёж или ошибку — зафиксируйте контракт в своей БД до вызова API.
2. **Сохраняйте оба id** (merchant + provider) до финального статуса / callback.
3. Не используйте PII (ИНН, телефон, email) как единственный `OrderId`.

## Callback и webhook

- VTB: form-urlencoded callback + checksum (`IVtbCallbackVerifier`).
- TBank / YooMoney / Inwizo: обрабатывайте notification URL идемпотентно (повторные доставки нормальны).
- Всегда сверяйте сумму и статус с локальной записью платежа.
