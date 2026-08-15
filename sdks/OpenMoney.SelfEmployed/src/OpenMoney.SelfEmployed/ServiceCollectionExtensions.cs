using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OpenMoney.SelfEmployed;

/// <summary>Dependency injection registration helpers.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the NPD client using an options delegate.</summary>
    public static IServiceCollection AddOpenMoneySelfEmployed(
        this IServiceCollection services,
        Action<TBankNpdOptions> configure,
        bool addBackgroundStatusChecker = false,
        bool addBackgroundReceiptSync = false)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        return AddServices(services, addBackgroundStatusChecker, addBackgroundReceiptSync);
    }

    /// <summary>Registers the NPD client from a configuration section.</summary>
    public static IServiceCollection AddOpenMoneySelfEmployed(
        this IServiceCollection services,
        IConfigurationSection configuration,
        bool addBackgroundStatusChecker = false,
        bool addBackgroundReceiptSync = false)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<TBankNpdOptions>(configuration);
        return AddServices(services, addBackgroundStatusChecker, addBackgroundReceiptSync);
    }

    private static IServiceCollection AddServices(
        IServiceCollection services,
        bool addBackgroundStatusChecker,
        bool addBackgroundReceiptSync)
    {
        services.AddHttpClient<NpdClient>();
        if (addBackgroundStatusChecker)
        {
            services.AddHostedService<NpdStatusChecker>();
        }

        if (addBackgroundReceiptSync)
        {
            services.AddHostedService<NpdReceiptSyncHostedService>();
        }

        return services;
    }
}
