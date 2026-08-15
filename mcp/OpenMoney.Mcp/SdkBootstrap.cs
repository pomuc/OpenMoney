using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenMoney.CloudPayments;
using OpenMoney.Fiscal;
using OpenMoney.Inwizo;
using OpenMoney.Kyc;
using OpenMoney.SelfEmployed;
using OpenMoney.SelfEmployed.Models;
using OpenMoney.TBank;
using OpenMoney.Tochka;
using OpenMoney.VTB;
using OpenMoney.YooMoney;

namespace OpenMoney.Mcp;

internal static class SdkBootstrap
{
    public static IReadOnlyDictionary<string, bool> RegisterConfiguredSdks(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var enabled = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        // Fiscal / MoyNalog — публичные FNS endpoints, всегда доступны.
        services.AddOpenMoneyFiscal(o => configuration.GetSection(FiscalOptions.SectionName).Bind(o));
        services.AddOpenMoneyKycMoyNalog(o => { });
        enabled["Fiscal"] = true;
        enabled["Kyc.MoyNalog"] = true;

        var tbank = configuration.GetSection(TBankOptions.SectionName);
        if (Has(tbank, "TerminalKey", "TerminalPassword"))
        {
            services.AddTBank(o => tbank.Bind(o));
            enabled["TBank"] = true;
        }

        var tochka = configuration.GetSection(TochkaOptions.SectionName);
        if (Has(tochka, "BaseUrl", "ClientId", "KeyId", "CertificatePemPath", "PrivateKeyPemPath"))
        {
            services.AddOpenMoneyTochka(o => tochka.Bind(o));
            enabled["Tochka"] = true;
        }

        var vtb = configuration.GetSection(VtbAcquiringOptions.SectionName);
        if (Has(vtb, "Token"))
        {
            services.AddOpenMoneyVtbAcquiring(o => vtb.Bind(o));
            enabled["VTB"] = true;
        }

        var cp = configuration.GetSection(CloudPaymentsOptions.SectionName);
        if (Has(cp, "PublicId", "ApiSecret"))
        {
            services.AddOpenMoneyCloudPayments(o => cp.Bind(o));
            enabled["CloudPayments"] = true;
        }

        var inwizo = configuration.GetSection(InwizoOptions.SectionName);
        if (Has(inwizo, "BaseUrl", "Account", "ApiKey", "Operator"))
        {
            services.AddOpenMoneyInwizo(o => inwizo.Bind(o));
            enabled["Inwizo"] = true;
        }

        var yoo = configuration.GetSection(YooMoneyOptions.SectionName);
        if (Has(yoo, "ShopId", "SecretKey"))
        {
            services.AddOpenMoneyYooMoney(o => yoo.Bind(o));
            enabled["YooMoney"] = true;
        }

        var npd = configuration.GetSection(TBankNpdOptions.SectionName);
        if (Has(npd, "Token"))
        {
            services.AddSingleton<INpdRecipientStore, MemoryNpdRecipientStore>();
            services.AddSingleton<INpdReceiptStore, MemoryNpdReceiptStore>();
            services.AddOpenMoneySelfEmployed(o => npd.Bind(o));
            enabled["SelfEmployed"] = true;
        }

        var mtsId = configuration.GetSection("Kyc:MtsId");
        if (Has(mtsId, "ClientId", "SigningPrivateKeyPem", "SigningKeyKid", "NotificationUri", "ClientNotificationToken"))
        {
            services.AddOpenMoneyKycMtsId(o => mtsId.Bind(o));
            enabled["Kyc.MtsId"] = true;
        }

        var mtsRim = configuration.GetSection("Kyc:MtsRim");
        if (Has(mtsRim, "AccessToken"))
        {
            services.AddOpenMoneyKycMtsRim(o => mtsRim.Bind(o));
            enabled["Kyc.MtsRim"] = true;
        }

        var didit = configuration.GetSection("Kyc:Didit");
        if (Has(didit, "ClientId", "ClientSecret"))
        {
            services.AddOpenMoneyKycDidit(o => didit.Bind(o));
            enabled["Kyc.Didit"] = true;
        }

        services.AddSingleton<IReadOnlyDictionary<string, bool>>(enabled);
        return enabled;
    }

    private static bool Has(IConfiguration section, params string[] keys) =>
        keys.All(k => !string.IsNullOrWhiteSpace(section[k]));
}

internal sealed class MemoryNpdRecipientStore : INpdRecipientStore
{
    private readonly ConcurrentDictionary<long, SelfEmployedRecipient> _items = new();

    public Task UpsertRecipientsAsync(IReadOnlyList<SelfEmployedRecipient> recipients, CancellationToken cancellationToken = default)
    {
        foreach (var r in recipients)
            _items[r.Id] = r;
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<SelfEmployedRecipient> Snapshot() => _items.Values.ToArray();
}

internal sealed class MemoryNpdReceiptStore : INpdReceiptStore
{
    private readonly ConcurrentDictionary<string, SelfEmployedReceiptRecord> _items = new(StringComparer.Ordinal);

    public Task<bool> ExistsAsync(string externalOperationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.ContainsKey(externalOperationId));

    public Task SaveAsync(SelfEmployedReceiptRecord record, CancellationToken cancellationToken = default)
    {
        _items[record.ExternalOperationId] = record;
        return Task.CompletedTask;
    }
}
