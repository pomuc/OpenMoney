using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenMoney.CloudPayments;
using OpenMoney.Fiscal;
using OpenMoney.Inwizo;
using OpenMoney.Kyc;
using OpenMoney.SdkExamples;
using OpenMoney.SdkExamples.Samples;
using OpenMoney.SelfEmployed;
using OpenMoney.SelfEmployed.Models;
using OpenMoney.TBank;
using OpenMoney.Tochka;
using OpenMoney.VTB;
using OpenMoney.YooMoney;

// Примеры использования OpenMoney SDK.
//
//   dotnet run --project examples/OpenMoney.SdkExamples -- list
//   dotnet run --project examples/OpenMoney.SdkExamples -- tbank
//   dotnet run --project examples/OpenMoney.SdkExamples -- yoomoney
//
// Ключи: appsettings.json рядом с проектом или env (TBank__TerminalKey, YooMoney__ShopId, …).

var sample = args.FirstOrDefault()?.Trim().ToLowerInvariant() ?? "list";

if (sample is "list" or "-h" or "--help" or "help")
{
    PrintHelp();
    return;
}

var builder = Host.CreateApplicationBuilder(args);
var cfg = builder.Configuration;

RegisterIfConfigured(builder.Services, cfg);
var host = builder.Build();
using var scope = host.Services.CreateScope();
var sp = scope.ServiceProvider;

try
{
    await (sample switch
    {
        "tbank" => TBankSample.RunAsync(sp),
        "yoomoney" or "yookassa" => YooMoneySample.RunAsync(sp),
        "vtb" => VtbSample.RunAsync(sp),
        "cloudpayments" or "cp" => CloudPaymentsSample.RunAsync(sp),
        "inwizo" => InwizoSample.RunAsync(sp),
        "tochka" => TochkaSample.RunAsync(sp),
        "fiscal" or "fns" => FiscalSample.RunAsync(sp),
        "npd" or "selfemployed" => SelfEmployedSample.RunAsync(sp),
        "kyc" => KycSample.RunAsync(sp),
        _ => Task.Run(() =>
        {
            Console.Error.WriteLine($"Неизвестный пример: {sample}");
            PrintHelp();
            Environment.ExitCode = 1;
        })
    }).ConfigureAwait(false);
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 2;
}

static void PrintHelp()
{
    Console.WriteLine("""
        OpenMoney SDK examples

        Usage:
          dotnet run --project examples/OpenMoney.SdkExamples -- <sample>

        Samples:
          tbank           Init + GetStatus (эквайринг Т‑Банка)
          yoomoney        safe_deal → payment (ЮKassa)
          vtb             Старт оплаты RBS (карта / СБП)
          cloudpayments   Confirm / Refund / Void
          inwizo          Hosted payment URL + status
          tochka          Create recipient + get order sketch
          fiscal          Статус НПД по ИНН (ФНС)
          npd             Список получателей самозанятых
          kyc             MoyNalog status + Didit session (если ключи есть)

        Config: examples/OpenMoney.SdkExamples/appsettings.json
                or env like TBank__TerminalKey / YooMoney__ShopId
        """);
}

static void RegisterIfConfigured(IServiceCollection services, IConfiguration configuration)
{
    var tbank = configuration.GetSection(TBankOptions.SectionName);
    if (Has(tbank, "TerminalKey", "TerminalPassword"))
        services.AddTBank(o => tbank.Bind(o));

    var yoo = configuration.GetSection(YooMoneyOptions.SectionName);
    if (Has(yoo, "ShopId", "SecretKey"))
        services.AddOpenMoneyYooMoney(o => yoo.Bind(o));

    var vtb = configuration.GetSection(VtbAcquiringOptions.SectionName);
    if (Has(vtb, "Token"))
        services.AddOpenMoneyVtbAcquiring(o => vtb.Bind(o));

    var cp = configuration.GetSection(CloudPaymentsOptions.SectionName);
    if (Has(cp, "PublicId", "ApiSecret"))
        services.AddOpenMoneyCloudPayments(o => cp.Bind(o));

    var inwizo = configuration.GetSection(InwizoOptions.SectionName);
    if (Has(inwizo, "BaseUrl", "Account", "ApiKey", "Operator"))
        services.AddOpenMoneyInwizo(o => inwizo.Bind(o));

    var tochka = configuration.GetSection(TochkaOptions.SectionName);
    if (Has(tochka, "BaseUrl", "ClientId", "KeyId", "CertificatePemPath", "PrivateKeyPemPath"))
        services.AddOpenMoneyTochka(o => tochka.Bind(o));

    services.AddOpenMoneyFiscal(o => configuration.GetSection(FiscalOptions.SectionName).Bind(o));
    services.AddOpenMoneyKycMoyNalog(_ => { });

    var npd = configuration.GetSection(TBankNpdOptions.SectionName);
    if (Has(npd, "Token"))
    {
        services.AddSingleton<INpdRecipientStore, MemoryNpdRecipientStore>();
        services.AddSingleton<INpdReceiptStore, MemoryNpdReceiptStore>();
        services.AddOpenMoneySelfEmployed(o => npd.Bind(o));
    }

    var didit = configuration.GetSection("Kyc:Didit");
    if (Has(didit, "ClientId", "ClientSecret"))
        services.AddOpenMoneyKycDidit(o => didit.Bind(o));

    var mtsId = configuration.GetSection("Kyc:MtsId");
    if (Has(mtsId, "ClientId", "SigningPrivateKeyPem", "SigningKeyKid", "NotificationUri", "ClientNotificationToken"))
        services.AddOpenMoneyKycMtsId(o => mtsId.Bind(o));

    var mtsRim = configuration.GetSection("Kyc:MtsRim");
    if (Has(mtsRim, "AccessToken"))
        services.AddOpenMoneyKycMtsRim(o => mtsRim.Bind(o));
}

static bool Has(IConfiguration section, params string[] keys) =>
    keys.All(k => !string.IsNullOrWhiteSpace(section[k]));
