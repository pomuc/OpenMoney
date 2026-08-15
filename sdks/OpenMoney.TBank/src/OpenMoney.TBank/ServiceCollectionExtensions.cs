using Microsoft.Extensions.DependencyInjection;
using OpenMoney.TBank.Client;

namespace OpenMoney.TBank;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddTBank(
        this IServiceCollection services,
        Action<TBankOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        return services.AddHttpClient<ITBankAcquiringClient, TBankAcquiringClient>();
    }
}
