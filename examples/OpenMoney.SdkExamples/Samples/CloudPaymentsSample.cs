using OpenMoney.CloudPayments;

namespace OpenMoney.SdkExamples.Samples;

/// <summary>
/// CloudPayments: операции над уже созданной транзакцией.
/// Charge/Auth требуют CardCryptogramPacket из виджета — в пример не включены (PAN/CVV не должны попадать на сервер).
/// </summary>
public static class CloudPaymentsSample
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var client = SampleRequire.Get<CloudPaymentsClient>(
            services, "CloudPayments", "CloudPayments__PublicId", "CloudPayments__ApiSecret");

        // Для демо нужны реальные TransactionId из песочницы.
        // Запустите так: TRANSACTION_ID=12345 AMOUNT=10.00 dotnet run -- cloudpayments
        if (!long.TryParse(Environment.GetEnvironmentVariable("TRANSACTION_ID"), out var txId))
        {
            Console.WriteLine("""
                Задайте TRANSACTION_ID (и опционально AMOUNT) в env, например:
                  $env:TRANSACTION_ID = "123456789"
                  $env:AMOUNT = "10.00"
                  $env:CP_ACTION = "confirm"   # confirm | refund | void

                Пример charge (криптограмма с клиента):
                  await client.ChargeCryptogramAsync(new CardPaymentRequest(
                      Amount: 10m, Currency: "RUB", InvoiceId: "inv-1", AccountId: "user-1",
                      CardCryptogramPacket: cryptogramFromWidget));
                """);
            return;
        }

        var amount = decimal.TryParse(
            Environment.GetEnvironmentVariable("AMOUNT"),
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture,
            out var a)
            ? a
            : 10m;

        var action = (Environment.GetEnvironmentVariable("CP_ACTION") ?? "confirm").ToLowerInvariant();
        var response = action switch
        {
            "refund" => await client.RefundAsync(txId, amount, cancellationToken).ConfigureAwait(false),
            "void" => await client.VoidAsync(txId, cancellationToken).ConfigureAwait(false),
            _ => await client.ConfirmAsync(txId, amount, cancellationToken).ConfigureAwait(false)
        };

        Console.WriteLine($"Action={action}, Success={response.Success}, Message={response.Message}, Tx={response.Model?.TransactionId}");
    }
}
