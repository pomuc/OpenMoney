namespace OpenMoney.TBank;

/// <summary>Configuration for T-Bank acquiring and optional receipt operations.</summary>
public sealed class TBankOptions
{
    public const string SectionName = "TBank";

    public string BaseUrl { get; set; } = "https://securepay.tinkoff.ru";
    public string TerminalKey { get; set; } = string.Empty;
    public string TerminalPassword { get; set; } = string.Empty;
    public string? PayoutTerminalKey { get; set; }
    public string? PayoutTerminalPassword { get; set; }
    public string? MomentPayoutTerminalKey { get; set; }

    public string CloudPaymentsBaseUrl { get; set; } = "https://api.cloudpayments.ru";
    public string? CloudPaymentsLogin { get; set; }
    public string? CloudPaymentsPassword { get; set; }
    public string? Inn { get; set; }

    /// <summary>PEM certificate used only by certificate-signed E2C operations.</summary>
    public string? SigningCertificatePem { get; set; }
    /// <summary>PEM private key used only by certificate-signed E2C operations.</summary>
    public string? SigningPrivateKeyPem { get; set; }
    public string? SigningPrivateKeyPassword { get; set; }

    internal string EffectivePayoutTerminalKey => PayoutTerminalKey ?? TerminalKey;
    internal string EffectivePayoutTerminalPassword => PayoutTerminalPassword ?? TerminalPassword;
}
