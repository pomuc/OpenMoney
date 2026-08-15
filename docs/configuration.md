# Конфигурация

Единый шаблон: [`examples/appsettings.example.json`](../examples/appsettings.example.json).

Переменные окружения: `Секция__Ключ` (двойное подчёркивание), вложенность через `__`:

```text
TBank__TerminalKey
YooMoney__ShopId
Kyc__Didit__ClientId
```

## Секции

### TBank

| Ключ | Назначение |
|---|---|
| `TerminalKey` / `TerminalPassword` | Эквайринг pay-in |
| `PayoutTerminalKey` / `PayoutTerminalPassword` | E2C payout |
| `MomentPayoutTerminalKey` | Moment payout (signed) |
| `SigningCertificatePem` / `SigningPrivateKeyPem` | Подпись moment payout |
| `CloudPaymentsLogin` / `CloudPaymentsPassword` / `Inn` | Paycheck CloudKassir |

### Tochka

| Ключ | Назначение |
|---|---|
| `BaseUrl`, `ClientId`, `KeyId` | API Medusa |
| `CertificatePemPath`, `PrivateKeyPemPath` | RSA‑подпись запросов |
| `SuccessRedirectUrl`, `FailureRedirectUrl` | Редиректы после оплаты |
| `EnableSandboxOperations` | Только UAT; в проде `false` |

### VtbAcquiring

| Ключ | Назначение |
|---|---|
| `BaseUrl` | UAT: `https://vtb.rbsuat.com/payment`; бой: `https://platezh.vtb24.ru/payment` |
| `Token` | Токен мерчанта |
| `ReturnUrl` | Возврат после оплаты |

### CloudPayments

| Ключ | Назначение |
|---|---|
| `PublicId`, `ApiSecret` | API |
| `Inn`, `CalculationPlace` | Чеки CloudKassir |

### Inwizo

| Ключ | Назначение |
|---|---|
| `BaseUrl` | Именной API host Inwizo (обязателен; `Inwizo__BaseUrl`) |
| `Account`, `ApiKey`, `Operator` | Hosted / payout |
| `SbpAccount`, `SbpApiKey` | СБП (если отдельно) |
| `HostedPaymentUrl`, `HostedCardUrl` | Базовые URL формы мерчанта |

### YooMoney

| Ключ | Назначение |
|---|---|
| `ShopId`, `SecretKey` | ЮKassa |
| `Shops` | Доп. магазины (словарь ShopId → SecretKey) |

`payout_token` в options **не хранится** — передаётся в `CreatePayoutAsync`.

### Fiscal

| Ключ | Назначение |
|---|---|
| `LegalEntityInn`, `LegalEntityName` | Юрлицо платформы |
| `FnsBaseUrl`, `FnsStatusBaseUrl` | По умолчанию lknpd / statusnpd |

### TBankNpd (SelfEmployed)

| Ключ | Назначение |
|---|---|
| `Token` | OpenAPI Т‑Банк Бизнес |
| `UseSandbox` | Песочница |
| `ClientCertificatePemPath` / `ClientPrivateKeyPemPath` | mTLS для submit/pay/receipts |

Также нужны реализации `INpdRecipientStore` и `INpdReceiptStore`.

### Kyc

| Секция | Ключи |
|---|---|
| `Kyc:MoyNalog` | базовые URL (часто достаточно defaults) |
| `Kyc:MtsId` | `ClientId`, PEM/`SigningKeyKid`, `NotificationUri`, `ClientNotificationToken`, … |
| `Kyc:MtsRim` | `AccessToken`, `DefaultRedirectUrl` |
| `Kyc:Didit` | `ClientId`, `ClientSecret` |

## MCP

Те же секции читает `mcp/OpenMoney.Mcp/appsettings.json`. См. [MCP: обзор](mcp/overview.md).
