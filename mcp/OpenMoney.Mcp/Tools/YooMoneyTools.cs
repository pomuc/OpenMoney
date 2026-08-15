using System.ComponentModel;
using ModelContextProtocol.Server;
using OpenMoney.YooMoney;

namespace OpenMoney.Mcp.Tools;

[McpServerToolType]
public sealed class YooMoneyTools(IServiceProvider services)
{
    [McpServerTool(Name = "yoomoney_create_safe_deal")]
    [Description("ЮKassa: создать безопасную сделку (safe_deal).")]
    public Task<string> CreateSafeDeal(string? description = null, string? shopId = null, CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<IYooMoneyClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("YooMoney", "YooMoney__ShopId", "YooMoney__SecretKey"));

        return McpJson.RunAsync(async () =>
            (object)await client.CreateSafeDealAsync(new YooCreateDealRequest(description, ShopId: shopId), cancellationToken)
                .ConfigureAwait(false));
    }

    [McpServerTool(Name = "yoomoney_create_payment")]
    [Description("ЮKassa: создать платёж с привязкой к сделке. Суммы в копейках.")]
    public Task<string> CreatePayment(
        long amountMinorUnits,
        long payoutAmountMinorUnits,
        Guid dealId,
        string returnUrl,
        string? description = null,
        string? shopId = null,
        CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<IYooMoneyClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("YooMoney", "YooMoney__ShopId", "YooMoney__SecretKey"));

        return McpJson.RunAsync(async () =>
            (object)await client.CreatePaymentAsync(new YooCreatePaymentRequest(
                amountMinorUnits, payoutAmountMinorUnits, dealId, returnUrl, description, ShopId: shopId), cancellationToken)
                .ConfigureAwait(false));
    }

    [McpServerTool(Name = "yoomoney_get_payment")]
    [Description("ЮKassa: статус платежа.")]
    public Task<string> GetPayment(Guid paymentId, string? shopId = null, CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<IYooMoneyClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("YooMoney", "YooMoney__ShopId", "YooMoney__SecretKey"));

        return McpJson.RunAsync(async () =>
            (object?)await client.GetPaymentAsync(paymentId, shopId, cancellationToken).ConfigureAwait(false)
            ?? new { found = false });
    }

    [McpServerTool(Name = "yoomoney_get_deal")]
    [Description("ЮKassa: статус/баланс сделки.")]
    public Task<string> GetDeal(Guid dealId, string? shopId = null, CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<IYooMoneyClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("YooMoney", "YooMoney__ShopId", "YooMoney__SecretKey"));

        return McpJson.RunAsync(async () =>
            (object?)await client.GetDealAsync(dealId, shopId, cancellationToken).ConfigureAwait(false)
            ?? new { found = false });
    }

    [McpServerTool(Name = "yoomoney_create_payout")]
    [Description("ЮKassa: выплата физлицу по payout_token в рамках сделки. Сумма в копейках.")]
    public Task<string> CreatePayout(
        long amountMinorUnits,
        Guid dealId,
        string payoutToken,
        string orderId,
        string? description = null,
        string? shopId = null,
        CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<IYooMoneyClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("YooMoney", "YooMoney__ShopId", "YooMoney__SecretKey"));

        return McpJson.RunAsync(async () =>
            (object)await client.CreatePayoutAsync(new YooCreatePayoutRequest(
                amountMinorUnits, dealId, payoutToken, orderId, description, ShopId: shopId), cancellationToken)
                .ConfigureAwait(false));
    }
}
