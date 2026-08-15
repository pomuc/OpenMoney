using OpenMoney.Inwizo;

namespace OpenMoney.SdkExamples.Samples;

/// <summary>Inwizo: сформировать hosted URL и (опционально) спросить статус.</summary>
public static class InwizoSample
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var client = SampleRequire.Get<InwizoClient>(
            services, "Inwizo", "Inwizo__BaseUrl", "Inwizo__Account", "Inwizo__ApiKey", "Inwizo__Operator");

        var orderId = $"om-{Guid.NewGuid():N}"[..16];
        var init = client.InitializeHostedPayment(new InwizoPaymentInitializationRequest(
            orderId,
            AmountMinorUnits: 1_500,
            Email: "buyer@example.com",
            InwizoPaymentMethod.Card));

        Console.WriteLine($"OrderId={init.OrderId}");
        Console.WriteLine($"ExternalPaymentId={init.ExternalPaymentId}");
        Console.WriteLine($"PaymentUrl={init.PaymentUrl}");
        Console.WriteLine($"State={init.State}");

        // Статус имеет смысл после редиректа пользователя.
        if (string.Equals(Environment.GetEnvironmentVariable("INWIZO_POLL"), "1", StringComparison.Ordinal))
        {
            var status = await client.GetPaymentStatusAsync(
                new InwizoPaymentStatusRequest(orderId, init.ExternalPaymentId, InwizoPaymentMethod.Card),
                cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"Status={status}");
        }
    }
}
