using OpenMoney.TBank.Client;
using OpenMoney.TBank.Models;

namespace OpenMoney.SdkExamples.Samples;

/// <summary>Т‑Банк: Init pay-in → GetStatus.</summary>
public static class TBankSample
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var client = SampleRequire.Get<ITBankAcquiringClient>(
            services, "TBank", "TBank__TerminalKey", "TBank__TerminalPassword");

        var orderId = $"demo-{Guid.NewGuid():N}"[..20];
        Console.WriteLine($"Init pay-in, OrderId={orderId}, Amount=1000 коп.");

        var init = await client.InitPayInAsync(new RequestInitPaymentContext
        {
            Amount = 1_000,
            OrderId = orderId,
            Description = "OpenMoney SDK example"
        }, cancellationToken).ConfigureAwait(false);

        Console.WriteLine($"Success={init.Success}, PaymentId={init.PaymentId}, Status={init.Status}, Message={init.Message}");
        if (string.IsNullOrWhiteSpace(init.PaymentId))
            return;

        var status = await client.GetStatusAsync(new RequestGetStatePaymentContext
        {
            PaymentId = init.PaymentId
        }, cancellationToken).ConfigureAwait(false);

        Console.WriteLine($"GetStatus: Success={status.Success}, Status={status.Status}");
    }
}
