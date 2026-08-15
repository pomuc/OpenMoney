using Microsoft.Extensions.DependencyInjection;
using OpenMoney.Kyc.Didit;
using OpenMoney.Kyc.MoyNalog;
using OpenMoney.Kyc.Mts;

namespace OpenMoney.SdkExamples.Samples;

/// <summary>KYC: MoyNalog status; Didit / MTS — если сконфигурированы.</summary>
public static class KycSample
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var moy = SampleRequire.Get<MoyNalogKycClient>(services, "Kyc.MoyNalog");
        var inn = Environment.GetEnvironmentVariable("NPD_INN");
        if (!string.IsNullOrWhiteSpace(inn) && inn.Length == 12)
        {
            var status = await moy.CheckTaxpayerStatusAsync(inn, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            Console.WriteLine($"MoyNalog: Status={status.Status}, Message={status.Message}");
        }
        else
        {
            Console.WriteLine("MoyNalog: задайте NPD_INN=############ чтобы проверить статус.");
        }

        var didit = services.GetService<DiditClient>();
        if (didit is not null)
        {
            var session = await didit.CreateSessionAsync(
                callbackUrl: "https://merchant.example/kyc/didit/callback",
                vendorData: $"om-{Guid.NewGuid():N}",
                cancellationToken: cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"Didit: SessionId={session.SessionId}, Url={session.Url}");
        }
        else
        {
            Console.WriteLine("Didit: не сконфигурирован (Kyc__Didit__ClientId / ClientSecret).");
        }

        var mtsId = services.GetService<MtsIdClient>();
        Console.WriteLine(mtsId is null
            ? "MTS ID: не сконфигурирован."
            : "MTS ID: готов. Пример: await mtsId.StartSiAuthorizeAsync(79xxxxxxxxx);");

        var mtsRim = services.GetService<MtsRimClient>();
        Console.WriteLine(mtsRim is null
            ? "MTS RIM: не сконфигурирован."
            : "MTS RIM: готов. Пример: CreateApplicantAsync → StartIdentificationAsync.");
    }
}
