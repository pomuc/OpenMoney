using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OpenMoney.VTB;

/// <summary>Dependency injection registration helpers for VTB acquiring.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers <see cref="VtbAcquiringClient"/> with typed HttpClient.</summary>
    public static IServiceCollection AddOpenMoneyVtbAcquiring(
        this IServiceCollection services,
        Action<VtbAcquiringOptions> configure)
    {
        services.Configure(configure);
        services.AddHttpClient<VtbAcquiringClient>();
        return services;
    }

    /// <summary>Registers <see cref="VtbAcquiringClient"/> from a configuration section.</summary>
    public static IServiceCollection AddOpenMoneyVtbAcquiring(
        this IServiceCollection services,
        IConfigurationSection configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<VtbAcquiringOptions>(configuration);
        services.AddHttpClient<VtbAcquiringClient>();
        return services;
    }
}
