# Процесс: KYC / идентификация

Пакет: **OpenMoney.Kyc**.

Цель: подтвердить личность или статус НПД до допуска к выплатам / сделкам.

## Выбор провайдера

| Задача | Клиент | Поток |
|---|---|---|
| Статус НПД по ИНН | `MoyNalogKycClient` | `CheckTaxpayerStatusAsync` |
| SMS‑сессия Мой налог + профиль | `MoyNalogKycClient` | challenge → verify → identity |
| Mobile Connect / SI | `MtsIdClient` | `StartSiAuthorizeAsync` → OTP → match / OAuth |
| OCR паспорт + selfie (MTS) | `MtsRimClient` | applicant → identification URL |
| Hosted OCR+FACE | `DiditClient` | session URL → poll decision |

## Мой налог (KYC)

```csharp
var status = await moy.CheckTaxpayerStatusAsync(inn, ct: ct);
// SMS-путь аналогичен Fiscal, но для identity/KYC
```

## Didit.me

1. `CreateSessionAsync(callbackUrl, vendorData, features?)`.
2. Пользователь проходит `Url`.
3. `GetDecisionAsync(sessionId)`.

## MTS ID

1. `StartSiAuthorizeAsync(phoneMsisdn)` — без `+`.
2. `SubmitOtpAsync(smsOtpEndpoint, code)`.
3. Далее match даты рождения / OAuth (`ExchangeAuthorizationCodeAsync`, `GetUserInfoAsync`) по сценарию кабинета.

## MTS RIM

1. `CreateApplicantAsync(externalId)`.
2. `StartIdentificationAsync(externalId, redirectUrl?)` → `IdentificationUrl`.
3. `GetIdentificationAsync(externalId, identificationId)`.

## Пример

`dotnet run --project examples/OpenMoney.SdkExamples -- kyc`  
Пакет: [kyc.md](../packages/kyc.md).

## Практика

- Не смешивайте Fiscal‑доход и KYC‑статус в одном use‑case без нужды.
- Callback URL должен быть HTTPS и идемпотентным.
- PEM / access token — только secret store.
