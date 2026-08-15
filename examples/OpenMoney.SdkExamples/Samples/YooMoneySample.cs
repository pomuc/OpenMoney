using OpenMoney.YooMoney;

namespace OpenMoney.SdkExamples.Samples;

/// <summary>ЮKassa: safe_deal → payment (без автоматической выплаты).</summary>
public static class YooMoneySample
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var yoo = SampleRequire.Get<IYooMoneyClient>(
            services, "YooMoney", "YooMoney__ShopId", "YooMoney__SecretKey");

        var deal = await yoo.CreateSafeDealAsync(new YooCreateDealRequest("OpenMoney example deal"), cancellationToken)
            .ConfigureAwait(false);
        Console.WriteLine($"Deal: Success={deal.Success}, Id={deal.ExternalDealId}, Status={deal.Status}");
        if (!deal.Success)
            return;

        var payment = await yoo.CreatePaymentAsync(new YooCreatePaymentRequest(
            AmountMinorUnits: 10_000,
            PayoutAmountMinorUnits: 8_000,
            DealId: deal.ExternalDealId,
            ReturnUrl: "https://merchant.example/pay/return",
            Description: "OpenMoney SDK example"), cancellationToken).ConfigureAwait(false);

        Console.WriteLine(
            $"Payment: Success={payment.Success}, Id={payment.PaymentId}, Status={payment.Status}");
        Console.WriteLine($"ConfirmationUrl: {payment.ConfirmationUrl}");
        Console.WriteLine("Выплату (CreatePayoutAsync) вызывайте только после оплаты и human‑approval.");
    }
}
