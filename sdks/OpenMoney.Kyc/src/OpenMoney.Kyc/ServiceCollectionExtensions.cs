using Microsoft.Extensions.DependencyInjection;
using OpenMoney.Kyc.Didit;
using OpenMoney.Kyc.MoyNalog;
using OpenMoney.Kyc.Mts;

namespace OpenMoney.Kyc;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenMoneyKycMoyNalog(
        this IServiceCollection services, Action<MoyNalogOptions> configure)
    {
        services.AddOptions<MoyNalogOptions>().Configure(configure);
        services.AddHttpClient<MoyNalogKycClient>((sp, http) =>
        {
            var o = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MoyNalogOptions>>().Value;
            http.BaseAddress = new Uri(o.FnsBaseUrl.EndsWith('/') ? o.FnsBaseUrl : o.FnsBaseUrl + "/");
        });
        return services;
    }

    public static IServiceCollection AddOpenMoneyKycMtsId(
        this IServiceCollection services, Action<MtsIdOptions> configure)
    {
        services.AddOptions<MtsIdOptions>().Configure(configure);
        services.AddHttpClient<MtsIdClient>();
        return services;
    }

    public static IServiceCollection AddOpenMoneyKycMtsRim(
        this IServiceCollection services, Action<MtsRimOptions> configure)
    {
        services.AddOptions<MtsRimOptions>().Configure(configure);
        services.AddHttpClient<MtsRimClient>((sp, http) =>
        {
            var o = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MtsRimOptions>>().Value;
            http.BaseAddress = new Uri(o.BaseUrl.EndsWith('/') ? o.BaseUrl : o.BaseUrl + "/");
        });
        return services;
    }

    public static IServiceCollection AddOpenMoneyKycDidit(
        this IServiceCollection services, Action<DiditOptions> configure)
    {
        services.AddOptions<DiditOptions>().Configure(configure);
        services.AddHttpClient<DiditClient>();
        return services;
    }

    /// <summary>Registers MoyNalog + MTS ID + MTS RIM + Didit KYC clients.</summary>
    public static IServiceCollection AddOpenMoneyKyc(
        this IServiceCollection services,
        Action<MoyNalogOptions>? moyNalog = null,
        Action<MtsIdOptions>? mtsId = null,
        Action<MtsRimOptions>? mtsRim = null,
        Action<DiditOptions>? didit = null)
    {
        if (moyNalog is not null) services.AddOpenMoneyKycMoyNalog(moyNalog);
        if (mtsId is not null) services.AddOpenMoneyKycMtsId(mtsId);
        if (mtsRim is not null) services.AddOpenMoneyKycMtsRim(mtsRim);
        if (didit is not null) services.AddOpenMoneyKycDidit(didit);
        return services;
    }
}
