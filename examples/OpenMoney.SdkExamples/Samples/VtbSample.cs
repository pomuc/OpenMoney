using OpenMoney.VTB;

namespace OpenMoney.SdkExamples.Samples;

/// <summary>ВТБ RBS: старт оплаты (СБП QR по умолчанию).</summary>
public static class VtbSample
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var client = SampleRequire.Get<VtbAcquiringClient>(
            services, "VTB", "VtbAcquiring__Token", "VtbAcquiring__ReturnUrl");

        var orderId = Guid.NewGuid();
        var (redirectOrPayload, bankOrderId) = await client.StartPaymentAsync(
            orderId,
            amountMinorUnits: 50_000,
            byCard: false,
            cancellationToken).ConfigureAwait(false);

        Console.WriteLine($"MerchantOrderId={orderId}");
        Console.WriteLine($"BankOrderId={bankOrderId}");
        Console.WriteLine($"RedirectOrQrPayload={redirectOrPayload}");
        Console.WriteLine("Сохраните оба id до callback; проверяйте checksum через IVtbCallbackVerifier.");
    }
}
