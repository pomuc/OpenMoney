using System.ComponentModel;
using ModelContextProtocol.Server;
using OpenMoney.CloudPayments;

namespace OpenMoney.Mcp.Tools;

[McpServerToolType]
public sealed class CloudPaymentsTools(IServiceProvider services)
{
    [McpServerTool(Name = "cloudpayments_confirm")]
    [Description("CloudPayments: confirm двухстадийной оплаты.")]
    public Task<string> Confirm(long transactionId, decimal amount, CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<CloudPaymentsClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("CloudPayments", "CloudPayments__PublicId", "CloudPayments__ApiSecret"));

        return McpJson.RunAsync(async () =>
            (object)await client.ConfirmAsync(transactionId, amount, cancellationToken).ConfigureAwait(false));
    }

    [McpServerTool(Name = "cloudpayments_refund")]
    [Description("CloudPayments: refund.")]
    public Task<string> Refund(long transactionId, decimal amount, CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<CloudPaymentsClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("CloudPayments", "CloudPayments__PublicId", "CloudPayments__ApiSecret"));

        return McpJson.RunAsync(async () =>
            (object)await client.RefundAsync(transactionId, amount, cancellationToken).ConfigureAwait(false));
    }

    [McpServerTool(Name = "cloudpayments_void")]
    [Description("CloudPayments: void (отмена холда).")]
    public Task<string> Void(long transactionId, CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<CloudPaymentsClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("CloudPayments", "CloudPayments__PublicId", "CloudPayments__ApiSecret"));

        return McpJson.RunAsync(async () =>
            (object)await client.VoidAsync(transactionId, cancellationToken).ConfigureAwait(false));
    }
}
