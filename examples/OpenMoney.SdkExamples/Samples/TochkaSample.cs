using OpenMoney.Tochka;

namespace OpenMoney.SdkExamples.Samples;

/// <summary>Точка: создать recipient (заказ требует card/service ids из вашей интеграции).</summary>
public static class TochkaSample
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var client = SampleRequire.Get<TochkaClient>(
            services, "Tochka",
            "Tochka__BaseUrl", "Tochka__ClientId", "Tochka__KeyId",
            "Tochka__CertificatePemPath", "Tochka__PrivateKeyPemPath");

        var recipientId = Guid.NewGuid();
        var created = await client.CreateRecipientAsync(recipientId, "OpenMoney Demo Recipient", cancellationToken)
            .ConfigureAwait(false);

        Console.WriteLine($"Recipient ExtId={created.Data?.ExtId}, Name={created.Data?.Name}");

        // Полный CreateOrderAsync:
        // await client.CreateOrderAsync(new TochkaCreateOrderRequest(
        //     OrderId: Guid.NewGuid(),
        //     RecipientId: recipientId,
        //     CardId: cardExtId,
        //     AmountMinorUnits: 10_000,
        //     CommissionMinorUnits: 100,
        //     ReceiptEmail: "buyer@example.com",
        //     Purpose: "Оплата услуги",
        //     ServiceId: Guid.NewGuid()));
        Console.WriteLine("Дальше: CreateCardAsync → CreateOrderAsync → ConfirmAllServicesAsync.");
    }
}
