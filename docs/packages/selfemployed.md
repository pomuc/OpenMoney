# OpenMoney.SelfEmployed

Клиент Т‑Банк Бизнес для **самозанятых**: получатели, реестры выплат, чеки. Неофициальный.

## DI

```csharp
services.AddScoped<INpdRecipientStore, AppRecipientStore>();
services.AddScoped<INpdReceiptStore, AppReceiptStore>();
services.AddOpenMoneySelfEmployed(
    configuration.GetSection(TBankNpdOptions.SectionName),
    addBackgroundStatusChecker: true,
    addBackgroundReceiptSync: true);
```

Клиент: `NpdClient`. Секция: `TBankNpd`.

## Методы

| Группа | Методы |
|---|---|
| Получатели | `ListRecipientsAsync`, `CheckNpdAsync`, `AddRecipientsByRequisitesAsync`, `GetAddResultAsync` |
| Реестр | `CreatePaymentRegistryAsync` → create result → `Submit*` → `Pay*` |
| Чеки | `RequestRegistryReceiptsAsync`, `GetRegistryReceiptsResultAsync`, `SyncRegistryReceiptsAsync` |

Submit/pay/receipts идут по **mTLS**.

Процесс: [npd-registry](../processes/npd-registry.md). Пример: `-- npd`.
