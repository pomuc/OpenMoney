# OpenMoney.SelfEmployed

Неофициальный .NET 8 клиент для сценариев Т‑Банка Business вокруг **самозанятых (НПД)**: получатели, **реестры выплат организации**, синхронизация **чеков реестра**.

Пакет **не аффилирован** с Т‑Банком.

## API

### Получатели
- `recipients/list` (+ синхронизация страниц через `INpdRecipientStore` / `CheckNpdAsync`)
- `recipients/add/by-requisites` и результат

### Реестры выплат (организация → самозанятые)
- `payment-registry/create` и результат создания
- mTLS `submit` / `submit/result` / `pay` / `pay/result`
- `salary|self-employed/payment-registry/list`

### Чеки
- mTLS `payment-registry/receipts` + `receipts/result`
- `SyncRegistryReceiptsAsync` и опциональный `NpdReceiptSyncHostedService` (хранилище через `INpdReceiptStore`)

## Регистрация

```csharp
services.AddScoped<INpdRecipientStore, AppRecipientStore>();
services.AddScoped<INpdReceiptStore, AppReceiptStore>();
services.AddOpenMoneySelfEmployed(
    configuration.GetSection(TBankNpdOptions.SectionName),
    addBackgroundStatusChecker: true,
    addBackgroundReceiptSync: true);
```

## Типовой поток реестра

1. `CreatePaymentRegistryAsync` — черновик со счётом компании и платежами  
2. `GetPaymentRegistryCreateResultAsync` — получить `paymentRegistryId`  
3. `SubmitPaymentRegistryAsync` (mTLS) → `GetSubmitResultAsync`  
4. `PayPaymentRegistryAsync` (mTLS) → `GetPayResultAsync`  

Сертификаты mTLS и OpenAPI‑токены — только из секрет‑хранилища, не из исходников.

## Пример

Полный runnable-пример: `examples/OpenMoney.SdkExamples` (`dotnet run --project examples/OpenMoney.SdkExamples -- npd`).
