using System.ComponentModel;
using ModelContextProtocol.Server;
using OpenMoney.Tochka;

namespace OpenMoney.Mcp.Tools;

[McpServerToolType]
public sealed class TochkaTools(IServiceProvider services)
{
    [McpServerTool(Name = "tochka_create_recipient")]
    [Description("Точка: создать получателя (recipient).")]
    public Task<string> CreateRecipient(Guid recipientId, string name, CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<TochkaClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured(
                "Tochka",
                "Tochka__BaseUrl", "Tochka__ClientId", "Tochka__KeyId",
                "Tochka__CertificatePemPath", "Tochka__PrivateKeyPemPath"));

        return McpJson.RunAsync(async () =>
            (object)await client.CreateRecipientAsync(recipientId, name, cancellationToken).ConfigureAwait(false));
    }

    [McpServerTool(Name = "tochka_get_order")]
    [Description("Точка: получить заказ по orderId.")]
    public Task<string> GetOrder(Guid orderId, CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<TochkaClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("Tochka", "Tochka__BaseUrl", "Tochka__ClientId"));

        return McpJson.RunAsync(async () =>
            (object)await client.GetOrderAsync(orderId, cancellationToken).ConfigureAwait(false));
    }

    [McpServerTool(Name = "tochka_create_order")]
    [Description("Точка: создать заказ на эквайринг. Суммы в копейках.")]
    public Task<string> CreateOrder(
        Guid orderId,
        Guid recipientId,
        Guid cardId,
        Guid serviceId,
        long amountMinorUnits,
        long commissionMinorUnits,
        string receiptEmail,
        string purpose,
        string? successRedirectUrl = null,
        string? failureRedirectUrl = null,
        int? paymentUrlTtlSeconds = null,
        CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<TochkaClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("Tochka", "Tochka__BaseUrl", "Tochka__ClientId"));

        return McpJson.RunAsync(async () =>
            (object)await client.CreateOrderAsync(new TochkaCreateOrderRequest(
                orderId, recipientId, cardId, amountMinorUnits, commissionMinorUnits,
                receiptEmail, purpose, serviceId, successRedirectUrl, failureRedirectUrl, paymentUrlTtlSeconds),
                cancellationToken).ConfigureAwait(false));
    }

    [McpServerTool(Name = "tochka_confirm_services")]
    [Description("Точка: подтвердить или отклонить все услуги заказа.")]
    public Task<string> ConfirmServices(Guid orderId, bool confirm = true, CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<TochkaClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("Tochka", "Tochka__BaseUrl", "Tochka__ClientId"));

        return McpJson.RunAsync(async () =>
            (object)await client.ConfirmAllServicesAsync(orderId, confirm, cancellationToken).ConfigureAwait(false));
    }
}
