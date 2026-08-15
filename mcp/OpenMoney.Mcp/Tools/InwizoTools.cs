using System.ComponentModel;
using ModelContextProtocol.Server;
using OpenMoney.Inwizo;

namespace OpenMoney.Mcp.Tools;

[McpServerToolType]
public sealed class InwizoTools(IServiceProvider services)
{
    [McpServerTool(Name = "inwizo_init_hosted_payment")]
    [Description("Inwizo: сформировать URL hosted‑оплаты.")]
    public string InitHostedPayment(
        string orderId,
        long amountMinorUnits,
        string email,
        bool sbp = false)
    {
        var client = McpJson.Get<InwizoClient>(services);
        if (client is null)
            return McpJson.NotConfigured("Inwizo", "Inwizo__BaseUrl", "Inwizo__Account", "Inwizo__ApiKey", "Inwizo__Operator", "Inwizo__HostedPaymentUrl");

        try
        {
            var init = client.InitializeHostedPayment(new InwizoPaymentInitializationRequest(
                orderId,
                amountMinorUnits,
                email,
                sbp ? InwizoPaymentMethod.Sbp : InwizoPaymentMethod.Card));
            return McpJson.Ok(new { ok = true, result = init });
        }
        catch (Exception ex)
        {
            return McpJson.Error(ex.Message);
        }
    }

    [McpServerTool(Name = "inwizo_payment_status")]
    [Description("Inwizo: статус оплаты.")]
    public Task<string> PaymentStatus(
        string transactionId,
        Guid externalPaymentId,
        bool sbp = false,
        CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<InwizoClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("Inwizo", "Inwizo__BaseUrl", "Inwizo__Account", "Inwizo__ApiKey"));

        return McpJson.RunAsync(async () =>
            (object)await client.GetPaymentStatusAsync(new InwizoPaymentStatusRequest(
                transactionId,
                externalPaymentId,
                sbp ? InwizoPaymentMethod.Sbp : InwizoPaymentMethod.Card), cancellationToken).ConfigureAwait(false));
    }

    [McpServerTool(Name = "inwizo_payout")]
    [Description("Inwizo: выплата на карточный токен.")]
    public Task<string> Payout(
        string orderId,
        string cardToken,
        long amountMinorUnits,
        Guid? externalPaymentId = null,
        CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<InwizoClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("Inwizo", "Inwizo__BaseUrl", "Inwizo__Account", "Inwizo__ApiKey", "Inwizo__Operator"));

        return McpJson.RunAsync(async () =>
            (object)await client.InitializePayoutAsync(
                    new InwizoPayoutRequest(orderId, amountMinorUnits, cardToken, externalPaymentId), cancellationToken)
                .ConfigureAwait(false));
    }

    [McpServerTool(Name = "inwizo_payout_status")]
    [Description("Inwizo: статус выплаты.")]
    public Task<string> PayoutStatus(string transactionId, Guid externalPaymentId, CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<InwizoClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("Inwizo", "Inwizo__BaseUrl", "Inwizo__Account", "Inwizo__ApiKey"));

        return McpJson.RunAsync(async () =>
            (object)await client.GetPayoutStatusAsync(transactionId, externalPaymentId, cancellationToken).ConfigureAwait(false));
    }
}
