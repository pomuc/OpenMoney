# Примеры кода (SdkExamples)

Консольное приложение со сценариями по каждому SDK:

```powershell
dotnet run --project examples/OpenMoney.SdkExamples -- list
dotnet run --project examples/OpenMoney.SdkExamples -- tbank
dotnet run --project examples/OpenMoney.SdkExamples -- yoomoney
```

Исходники: [`examples/OpenMoney.SdkExamples/Samples/`](../examples/OpenMoney.SdkExamples/Samples/).

| Аргумент | Файл | Что делает |
|---|---|---|
| `tbank` | `TBankSample.cs` | Init pay-in → GetStatus |
| `yoomoney` / `yookassa` | `YooMoneySample.cs` | safe_deal → payment |
| `vtb` | `VtbSample.cs` | StartPayment RBS |
| `cloudpayments` / `cp` | `CloudPaymentsSample.cs` | Confirm/Refund/Void |
| `inwizo` | `InwizoSample.cs` | Hosted payment URL |
| `tochka` | `TochkaSample.cs` | Create recipient |
| `fiscal` / `fns` | `FiscalSample.cs` | Статус НПД (`NPD_INN`) |
| `npd` / `selfemployed` | `SelfEmployedSample.cs` | Список получателей |
| `kyc` | `KycSample.cs` | MoyNalog / Didit / MTS |

Конфиг: `examples/OpenMoney.SdkExamples/appsettings.json` или env.  
Шаблон всех секций: [`examples/appsettings.example.json`](../examples/appsettings.example.json).

SDK регистрируются **только** при наличии ключей — иначе sample сообщит, что провайдер не сконфигурирован.
