using OpenMoney.SelfEmployed;
using OpenMoney.SelfEmployed.Models;

namespace OpenMoney.SdkExamples.Samples;

/// <summary>Т‑Банк НПД: первая страница получателей.</summary>
public static class SelfEmployedSample
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var client = SampleRequire.Get<NpdClient>(services, "SelfEmployed", "TBankNpd__Token");

        var page = await client.ListRecipientsAsync(
            new RecipientsListRequest { Offset = 0, Limit = 10 },
            cancellationToken).ConfigureAwait(false);

        Console.WriteLine($"Recipients on page: {page.Recipients.Count}");
        foreach (var r in page.Recipients.Take(10))
            Console.WriteLine($"  Id={r.Id}, Inn={r.Inn}");

        if (string.Equals(Environment.GetEnvironmentVariable("NPD_SYNC_ALL"), "1", StringComparison.Ordinal))
        {
            var processed = await client.CheckNpdAsync(cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"Synced recipients: {processed}");
        }
    }
}
