using System.ComponentModel;
using ModelContextProtocol.Server;
using OpenMoney.VTB;

namespace OpenMoney.Mcp.Tools;

[McpServerToolType]
public sealed class VtbTools(IServiceProvider services)
{
    [McpServerTool(Name = "vtb_start_payment")]
    [Description("ВТБ RBS: старт оплаты. byCard=true — карта, false — СБП QR. Сумма в копейках.")]
    public Task<string> StartPayment(
        long amountMinorUnits,
        bool byCard = false,
        Guid? orderNumber = null,
        CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<VtbAcquiringClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("VTB", "VtbAcquiring__Token", "VtbAcquiring__BaseUrl", "VtbAcquiring__ReturnUrl"));

        return McpJson.RunAsync(async () =>
        {
            var (redirectOrPayload, bankOrderId) = await client.StartPaymentAsync(
                orderNumber ?? Guid.NewGuid(),
                amountMinorUnits,
                byCard,
                cancellationToken).ConfigureAwait(false);
            return (object)new { redirectOrPayload, bankOrderId, byCard, amountMinorUnits };
        });
    }
}
