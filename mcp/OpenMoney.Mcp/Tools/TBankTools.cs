using System.ComponentModel;
using ModelContextProtocol.Server;
using OpenMoney.TBank.Client;
using OpenMoney.TBank.Models;

namespace OpenMoney.Mcp.Tools;

[McpServerToolType]
public sealed class TBankTools(IServiceProvider services)
{
    [McpServerTool(Name = "tbank_init_payin")]
    [Description("Т‑Банк: инициализация оплаты (Init). Сумма в копейках.")]
    public Task<string> InitPayIn(
        long amountMinorUnits,
        string orderId,
        string? description = null,
        string? successUrl = null,
        string? failUrl = null,
        string? notificationUrl = null,
        CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<ITBankAcquiringClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("TBank", "TBank__TerminalKey", "TBank__TerminalPassword"));

        return McpJson.RunAsync(async () =>
        {
            var response = await client.InitPayInAsync(new RequestInitPaymentContext
            {
                Amount = amountMinorUnits,
                OrderId = orderId,
                Description = description,
                SuccessURL = successUrl,
                FailURL = failUrl,
                NotificationURL = notificationUrl
            }, cancellationToken).ConfigureAwait(false);
            return (object)response;
        });
    }

    [McpServerTool(Name = "tbank_get_status")]
    [Description("Т‑Банк: статус платежа по PaymentId.")]
    public Task<string> GetStatus(string paymentId, CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<ITBankAcquiringClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("TBank", "TBank__TerminalKey", "TBank__TerminalPassword"));

        return McpJson.RunAsync(async () =>
        {
            var response = await client.GetStatusAsync(new RequestGetStatePaymentContext
            {
                PaymentId = paymentId
            }, cancellationToken).ConfigureAwait(false);
            return (object)response;
        });
    }

    [McpServerTool(Name = "tbank_cancel")]
    [Description("Т‑Банк: отмена/возврат платежа. amountMinorUnits опционален.")]
    public Task<string> Cancel(string paymentId, long? amountMinorUnits = null, CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<ITBankAcquiringClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("TBank", "TBank__TerminalKey", "TBank__TerminalPassword"));

        return McpJson.RunAsync(async () =>
        {
            var response = await client.CancelAsync(new RequestCancelPaymentContext
            {
                PaymentId = paymentId,
                Amount = amountMinorUnits
            }, cancellationToken).ConfigureAwait(false);
            return (object)response;
        });
    }

    [McpServerTool(Name = "tbank_create_qr")]
    [Description("Т‑Банк: создание QR (СБП и др.) для PaymentId.")]
    public Task<string> CreateQr(string paymentId, string dataType = "PAYLOAD", CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<ITBankAcquiringClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("TBank", "TBank__TerminalKey", "TBank__TerminalPassword"));

        return McpJson.RunAsync(async () =>
        {
            var response = await client.CreateQrAsync(new RequestGetQrContext
            {
                PaymentId = paymentId,
                DataType = dataType
            }, cancellationToken).ConfigureAwait(false);
            return (object)response;
        });
    }

    [McpServerTool(Name = "tbank_init_payout")]
    [Description("Т‑Банк: инициализация выплаты (E2C Init). Сумма в копейках.")]
    public Task<string> InitPayout(
        long amountMinorUnits,
        string orderId,
        string? cardId = null,
        long? dealId = null,
        CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<ITBankAcquiringClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("TBank", "TBank__TerminalKey", "TBank__TerminalPassword"));

        return McpJson.RunAsync(async () =>
        {
            var response = await client.InitPayoutAsync(new RequestInitPayoutContext
            {
                Amount = amountMinorUnits,
                OrderId = orderId,
                CardId = cardId,
                DealId = dealId
            }, cancellationToken).ConfigureAwait(false);
            return (object)response;
        });
    }
}
