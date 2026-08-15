# OpenMoney.Fiscal

Модели фискализации и неофициальный клиент ФНС «Мой налог» (НПД) для .NET 8.

Пакет **не аффилирован** с ФНС и вендорами ККТ.

```csharp
services.AddOpenMoneyFiscal(o =>
{
    o.LegalEntityInn = configuration["Fiscal:LegalEntityInn"];
    o.LegalEntityName = configuration["Fiscal:LegalEntityName"];
});
```

`FnsClient`: старт/проверка SMS‑challenge, refresh токена, статус налогоплательщика, профиль и активный статус, регистрация дохода и отмена. Аутентифицированные методы один раз повторяют запрос после refresh и возвращают актуальную пару токенов вызывающему коду.

`FiscalReceipt` моделирует чеки дохода, возврата, расхода и агентские. `CloudKassirReceiptFactory.CreatePayload` собирает payload, совместимый с CloudKassir (включая данные поставщика и признак агента).

Телефоны, токены, паспортные/профильные данные, ИНН и содержимое чеков — PII или секреты. Храните безопасно, в логи не включайте.

## Пример

Полный runnable-пример: `examples/OpenMoney.SdkExamples` (`dotnet run --project examples/OpenMoney.SdkExamples -- fiscal`).
