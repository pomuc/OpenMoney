using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenMoney.SelfEmployed.Models;

namespace OpenMoney.SelfEmployed;

/// <summary>
/// Background sync of self-employed payment-registry receipts from T-Bank OpenAPI.
/// Port of production <c>TinkoffReceiptSyncHostedService</c> without hard-wired DB/S3.
/// </summary>
public sealed class NpdReceiptSyncHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IOptions<TBankNpdOptions> _options;
    private readonly ILogger<NpdReceiptSyncHostedService> _logger;

    /// <summary>Creates the hosted service.</summary>
    public NpdReceiptSyncHostedService(
        IServiceProvider services,
        IOptions<TBankNpdOptions> options,
        ILogger<NpdReceiptSyncHostedService> logger)
    {
        _services = services;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = _options.Value.ReceiptSyncInterval;
        if (interval <= TimeSpan.Zero)
        {
            interval = TimeSpan.FromHours(1);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var client = scope.ServiceProvider.GetRequiredService<NpdClient>();
                var store = scope.ServiceProvider.GetRequiredService<INpdReceiptStore>();
                var saved = await client.SyncRegistryReceiptsAsync(store, cancellationToken: stoppingToken)
                    .ConfigureAwait(false);
                _logger.LogInformation("NPD receipt sync saved {Count} new receipts", saved);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NPD receipt sync failed");
            }

            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
