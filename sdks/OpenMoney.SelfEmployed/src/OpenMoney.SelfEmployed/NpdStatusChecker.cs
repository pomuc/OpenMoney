using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenMoney.SelfEmployed.Models;

namespace OpenMoney.SelfEmployed;

/// <summary>Host persistence adapter used by recipient synchronization.</summary>
public interface INpdRecipientStore
{
    /// <summary>Inserts or updates a page of recipients atomically.</summary>
    Task UpsertRecipientsAsync(
        IReadOnlyList<SelfEmployedRecipient> recipients,
        CancellationToken cancellationToken = default);
}

/// <summary>Periodically synchronizes recipient NPD statuses.</summary>
public sealed class NpdStatusChecker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<TBankNpdOptions> _options;
    private readonly ILogger<NpdStatusChecker> _logger;

    /// <summary>Creates the optional background synchronization service.</summary>
    public NpdStatusChecker(
        IServiceScopeFactory scopeFactory,
        IOptions<TBankNpdOptions> options,
        ILogger<NpdStatusChecker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.Value.PollInterval);
        do
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var client = scope.ServiceProvider.GetRequiredService<NpdClient>();
                var count = await client.CheckNpdAsync(stoppingToken).ConfigureAwait(false);
                _logger.LogInformation("Synchronized {RecipientCount} T-Bank NPD recipients.", count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "T-Bank NPD recipient synchronization failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }
}

internal static class JsonDefaults
{
    internal static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
}
