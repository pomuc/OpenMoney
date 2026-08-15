using OpenMoney.Fiscal;

namespace OpenMoney.SdkExamples.Samples;

/// <summary>ФНС: проверка статуса налогоплательщика НПД.</summary>
public static class FiscalSample
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var client = SampleRequire.Get<FnsClient>(services, "Fiscal");

        var inn = Environment.GetEnvironmentVariable("NPD_INN");
        if (string.IsNullOrWhiteSpace(inn) || inn.Length != 12)
        {
            Console.WriteLine("Задайте NPD_INN=############ (12 цифр) и перезапустите: -- fiscal");
            return;
        }

        var status = await client.CheckTaxpayerStatusAsync(inn, ct: cancellationToken)
            .ConfigureAwait(false);
        Console.WriteLine($"Status={status.Status}, Message={status.Message}");
    }
}
