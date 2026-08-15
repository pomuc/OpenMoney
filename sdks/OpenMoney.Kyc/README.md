# OpenMoney.Kyc

Неофициальный .NET 8 KYC‑SDK экосистемы OpenMoney. Три провайдера идентификации:

| Провайдер | Клиент | Роль |
|---|---|---|
| **Мой налог** (ФНС) | `MoyNalogKycClient` | SMS‑вход, статус НПД по ИНН, паспортные данные налогоплательщика |
| **MTS ID / RIM** | `MtsIdClient`, `MtsRimClient` | Mobile Connect SI KYC + OAuth; OCR‑сессии документов |
| **Didit.me** | `DiditClient` | Hosted OCR+FACE сессия и опрос решения |

Пакет **не аффилирован** с ФНС, МТС и Didit.

Доходы и фискальные чеки — в **OpenMoney.Fiscal**. Этот пакет только про идентификацию / KYC.

## Регистрация

```csharp
services.AddOpenMoneyKyc(
    moyNalog: o => { /* по умолчанию lknpd.nalog.ru */ },
    mtsId: o =>
    {
        o.ClientId = configuration["Kyc:MtsId:ClientId"]!;
        o.ClientSecret = configuration["Kyc:MtsId:ClientSecret"];
        o.NotificationUri = configuration["Kyc:MtsId:NotificationUri"]!;
        o.ClientNotificationToken = configuration["Kyc:MtsId:ClientNotificationToken"]!;
        o.SigningPrivateKeyPem = configuration["Kyc:MtsId:SigningPrivateKeyPem"]!;
        o.SigningKeyKid = configuration["Kyc:MtsId:SigningKeyKid"]!;
        o.RedirectUri = configuration["Kyc:MtsId:RedirectUri"];
    },
    mtsRim: o =>
    {
        o.AccessToken = configuration["Kyc:MtsRim:AccessToken"]!;
        o.DefaultRedirectUrl = configuration["Kyc:MtsRim:DefaultRedirectUrl"];
    },
    didit: o =>
    {
        o.ClientId = configuration["Kyc:Didit:ClientId"]!;
        o.ClientSecret = configuration["Kyc:Didit:ClientSecret"]!;
    });
```

Любые client secret, PEM и access token — только из конфигурации окружения.

## Пример

Полный runnable-пример: `examples/OpenMoney.SdkExamples` (`dotnet run --project examples/OpenMoney.SdkExamples -- kyc`).
