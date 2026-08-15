# OpenMoney.CloudPayments

Неофициальный .NET 8 клиент для карточных операций CloudPayments и фискальных чеков CloudKassir.

Пакет **не аффилирован** с CloudPayments.

```csharp
services.AddOpenMoneyCloudPayments(o =>
{
    o.PublicId = configuration["CloudPayments:PublicId"]!;
    o.ApiSecret = configuration["CloudPayments:ApiSecret"]!;
    o.Inn = configuration["CloudPayments:Inn"];
    o.CalculationPlace = "merchant.example";
});
```

`CloudPaymentsClient` поддерживает одностадийный charge, двухстадийную авторизацию и confirm, refund, void, произвольные чеки ККТ и helper для чека комиссии. Карточные операции принимают клиентский `CardCryptogramPacket`; сырой PAN/CVV через сервер передавать нельзя.

Credentials и реквизиты компании — только в конфигурации, в логи не пишутся.

## Пример

Полный runnable-пример: `examples/OpenMoney.SdkExamples` (`dotnet run --project examples/OpenMoney.SdkExamples -- cloudpayments`).
