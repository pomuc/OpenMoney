namespace OpenMoney.SelfEmployed;

/// <summary>Configures access to the T-Bank Business self-employed API.</summary>
public sealed class TBankNpdOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "TBankNpd";

    /// <summary>Bearer token issued by T-Bank Business.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Selects the sandbox endpoints.</summary>
    public bool UseSandbox { get; set; }

    /// <summary>Production non-mTLS API base URL.</summary>
    public string OpenApiBaseUrl { get; set; } = "https://business.tbank.ru/openapi/api/v1";

    /// <summary>Sandbox non-mTLS API base URL.</summary>
    public string SandboxOpenApiBaseUrl { get; set; } = "https://business.tbank.ru/openapi/sandbox/api/v1";

    /// <summary>Production mTLS API base URL.</summary>
    public string SecuredOpenApiBaseUrl { get; set; } = "https://secured-openapi.tbank.ru/api/v1";

    /// <summary>Sandbox mTLS API base URL.</summary>
    public string SandboxSecuredOpenApiBaseUrl { get; set; } = "https://business.tbank.ru/openapi/sandbox/secured/api/v1";

    /// <summary>Path to the PEM encoded client certificate used by secured operations.</summary>
    public string? ClientCertificatePemPath { get; set; }

    /// <summary>Path to the PEM encoded RSA or ECDSA private key.</summary>
    public string? ClientPrivateKeyPemPath { get; set; }

    /// <summary>Optional password for an encrypted private key.</summary>
    public string? ClientPrivateKeyPassword { get; set; }

    /// <summary>Page size used by recipient synchronization. Valid values are 1 through 100.</summary>
    public int PageSize { get; set; } = 100;

    /// <summary>Delay between recipient pages to avoid request bursts.</summary>
    public TimeSpan PageDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Interval used by the optional recipient background synchronization service.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>How many past days to scan when listing payment registries for receipt sync.</summary>
    public int ReceiptsLookbackDays { get; set; } = 30;

    /// <summary>Attempts to poll receipts/result for each registry.</summary>
    public int ReceiptPollAttempts { get; set; } = 6;

    /// <summary>Delay between receipt result polls.</summary>
    public TimeSpan ReceiptPollDelay { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Interval for the optional receipt sync background service.</summary>
    public TimeSpan ReceiptSyncInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Optional PEM certificate content (alternative to <see cref="ClientCertificatePemPath"/>).
    /// Prefer paths or a secret store — do not commit real PEM material.
    /// </summary>
    public string? ClientCertificatePem { get; set; }

    /// <summary>
    /// Optional PEM private key content (alternative to <see cref="ClientPrivateKeyPemPath"/>).
    /// </summary>
    public string? ClientPrivateKeyPem { get; set; }

    internal string ApiBaseUrl => UseSandbox ? SandboxOpenApiBaseUrl : OpenApiBaseUrl;

    internal string SecuredApiBaseUrl => UseSandbox ? SandboxSecuredOpenApiBaseUrl : SecuredOpenApiBaseUrl;
}
