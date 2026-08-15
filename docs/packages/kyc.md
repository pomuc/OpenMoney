# OpenMoney.Kyc

Идентификация: Мой налог, MTS ID/RIM, Didit.me. Неофициальный.

Доходы и чеки НПД — в **OpenMoney.Fiscal**, не здесь.

## DI

```csharp
services.AddOpenMoneyKyc(
    moyNalog: _ => { },
    mtsId: o => { /* ClientId, SigningPrivateKeyPem, NotificationUri, … */ },
    mtsRim: o => { /* AccessToken, DefaultRedirectUrl */ },
    didit: o => { /* ClientId, ClientSecret */ });
```

Или по отдельности: `AddOpenMoneyKycMoyNalog`, `AddOpenMoneyKycMtsId`, `AddOpenMoneyKycMtsRim`, `AddOpenMoneyKycDidit`.

## Клиенты

| Клиент | Ключевые методы |
|---|---|
| `MoyNalogKycClient` | SMS auth, `CheckTaxpayerStatusAsync`, identity |
| `MtsIdClient` | `StartSiAuthorizeAsync`, `SubmitOtpAsync`, OAuth |
| `MtsRimClient` | `CreateApplicantAsync`, `StartIdentificationAsync`, `GetIdentificationAsync` |
| `DiditClient` | `CreateSessionAsync`, `GetDecisionAsync` |

Процесс: [kyc-session](../processes/kyc-session.md). Пример: `-- kyc`.
