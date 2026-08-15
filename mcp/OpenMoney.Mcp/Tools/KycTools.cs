using System.ComponentModel;
using ModelContextProtocol.Server;
using OpenMoney.Kyc.Didit;
using OpenMoney.Kyc.MoyNalog;
using OpenMoney.Kyc.Mts;

namespace OpenMoney.Mcp.Tools;

[McpServerToolType]
public sealed class KycTools(IServiceProvider services)
{
    [McpServerTool(Name = "kyc_moynalog_check_status")]
    [Description("KYC Мой налог: статус налогоплательщика НПД по ИНН (12 цифр).")]
    public Task<string> MoyNalogCheckStatus(string inn, string? requestDate = null, CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<MoyNalogKycClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("Kyc.MoyNalog"));

        DateOnly? date = null;
        if (!string.IsNullOrWhiteSpace(requestDate) && DateOnly.TryParse(requestDate, out var parsed))
            date = parsed;

        return McpJson.RunAsync(async () =>
            (object)await client.CheckTaxpayerStatusAsync(inn, date, cancellationToken).ConfigureAwait(false));
    }

    [McpServerTool(Name = "kyc_didit_create_session")]
    [Description("Didit.me: создать KYC‑сессию (hosted).")]
    public Task<string> DiditCreateSession(
        string callbackUrl,
        string vendorData,
        string? features = null,
        CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<DiditClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("Kyc.Didit", "Kyc__Didit__ClientId", "Kyc__Didit__ClientSecret"));

        return McpJson.RunAsync(async () =>
            (object)await client.CreateSessionAsync(callbackUrl, vendorData, features, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
    }

    [McpServerTool(Name = "kyc_didit_get_decision")]
    [Description("Didit.me: получить решение по sessionId.")]
    public Task<string> DiditGetDecision(string sessionId, CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<DiditClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("Kyc.Didit", "Kyc__Didit__ClientId", "Kyc__Didit__ClientSecret"));

        return McpJson.RunAsync(async () =>
            (object)await client.GetDecisionAsync(sessionId, cancellationToken: cancellationToken).ConfigureAwait(false));
    }

    [McpServerTool(Name = "kyc_mts_start_si")]
    [Description("MTS ID: старт SI‑authorize по MSISDN (номер без +).")]
    public Task<string> MtsStartSi(long phoneMsisdn, CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<MtsIdClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured(
                "Kyc.MtsId",
                "Kyc__MtsId__ClientId",
                "Kyc__MtsId__SigningPrivateKeyPem",
                "Kyc__MtsId__SigningKeyKid",
                "Kyc__MtsId__NotificationUri",
                "Kyc__MtsId__ClientNotificationToken"));

        return McpJson.RunAsync(async () =>
            (object)await client.StartSiAuthorizeAsync(phoneMsisdn, cancellationToken).ConfigureAwait(false));
    }

    [McpServerTool(Name = "kyc_mts_submit_otp")]
    [Description("MTS ID: отправить OTP на smsOtpEndpoint из StartSi.")]
    public Task<string> MtsSubmitOtp(string smsOtpEndpoint, string code, CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<MtsIdClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("Kyc.MtsId", "Kyc__MtsId__ClientId"));

        return McpJson.RunAsync(async () =>
            (object)new { ok = await client.SubmitOtpAsync(smsOtpEndpoint, code, cancellationToken).ConfigureAwait(false) });
    }

    [McpServerTool(Name = "kyc_mts_rim_create_applicant")]
    [Description("MTS RIM: создать applicant.")]
    public Task<string> MtsRimCreateApplicant(Guid externalId, CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<MtsRimClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("Kyc.MtsRim", "Kyc__MtsRim__AccessToken"));

        return McpJson.RunAsync(async () =>
        {
            await client.CreateApplicantAsync(externalId, cancellationToken).ConfigureAwait(false);
            return (object)new { externalId, created = true };
        });
    }

    [McpServerTool(Name = "kyc_mts_rim_start_identification")]
    [Description("MTS RIM: старт идентификации, вернёт IdentificationUrl.")]
    public Task<string> MtsRimStartIdentification(
        Guid externalId,
        string? redirectUrl = null,
        CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<MtsRimClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("Kyc.MtsRim", "Kyc__MtsRim__AccessToken", "Kyc__MtsRim__DefaultRedirectUrl"));

        return McpJson.RunAsync(async () =>
            (object)await client.StartIdentificationAsync(externalId, redirectUrl, cancellationToken).ConfigureAwait(false));
    }

    [McpServerTool(Name = "kyc_mts_rim_get_identification")]
    [Description("MTS RIM: статус идентификации.")]
    public Task<string> MtsRimGetIdentification(
        Guid externalId,
        Guid identificationId,
        CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<MtsRimClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("Kyc.MtsRim", "Kyc__MtsRim__AccessToken"));

        return McpJson.RunAsync(async () =>
            (object)await client.GetIdentificationAsync(externalId, identificationId, cancellationToken).ConfigureAwait(false));
    }
}
