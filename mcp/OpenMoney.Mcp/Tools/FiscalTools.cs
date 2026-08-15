using System.ComponentModel;
using ModelContextProtocol.Server;
using OpenMoney.Fiscal;

namespace OpenMoney.Mcp.Tools;

[McpServerToolType]
public sealed class FiscalTools(IServiceProvider services)
{
    [McpServerTool(Name = "fiscal_check_taxpayer_status")]
    [Description("ФНС / Мой налог: проверить статус налогоплательщика НПД по ИНН (12 цифр).")]
    public Task<string> CheckTaxpayerStatus(string inn, string? requestDate = null, CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<FnsClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("Fiscal"));

        DateOnly? date = null;
        if (!string.IsNullOrWhiteSpace(requestDate) && DateOnly.TryParse(requestDate, out var parsed))
            date = parsed;

        return McpJson.RunAsync(async () =>
            (object)await client.CheckTaxpayerStatusAsync(inn, date, cancellationToken).ConfigureAwait(false));
    }

    [McpServerTool(Name = "fiscal_start_sms")]
    [Description("ФНС / Мой налог: начать SMS‑challenge для авторизации.")]
    public Task<string> StartSms(string phone, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<FnsClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("Fiscal"));

        return McpJson.RunAsync(async () =>
            (object)await client.StartSmsChallengeAsync(
                    phone,
                    userAgent ?? "OpenMoney.Mcp/0.1",
                    cancellationToken)
                .ConfigureAwait(false));
    }

    [McpServerTool(Name = "fiscal_verify_sms")]
    [Description("ФНС / Мой налог: подтвердить SMS‑challenge и получить токены.")]
    public Task<string> VerifySms(
        string phone,
        string code,
        string challengeToken,
        string deviceId,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<FnsClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("Fiscal"));

        var ua = userAgent ?? "OpenMoney.Mcp/0.1";
        return McpJson.RunAsync(async () =>
            (object)await client.VerifySmsChallengeAsync(
                    phone, code, challengeToken, new FnsDevice(deviceId, ua), cancellationToken)
                .ConfigureAwait(false));
    }
}
