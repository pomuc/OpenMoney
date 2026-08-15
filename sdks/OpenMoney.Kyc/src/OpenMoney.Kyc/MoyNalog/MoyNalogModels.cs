namespace OpenMoney.Kyc.MoyNalog;

public sealed record MoyNalogDevice(string DeviceId, string UserAgent, string? SourceDeviceId = null);

public sealed class MoyNalogTokens
{
    public string Token { get; init; } = "";
    public string RefreshToken { get; init; } = "";
}

public sealed class MoyNalogChallenge
{
    public string ChallengeToken { get; init; } = "";
    public int? ExpireIn { get; init; }
}

public sealed class MoyNalogTaxpayerStatus
{
    public bool? Status { get; init; }
    public string? Message { get; init; }
}

public sealed class MoyNalogTaxpayerIdentity
{
    public string? PassportSeries { get; init; }
    public string? PassportNumber { get; init; }
    public DateOnly? PassportIssuedDate { get; init; }
    public string? PassportIssuer { get; init; }
    public DateOnly? Birthday { get; init; }
    public string? Address { get; init; }
    public string? Sex { get; init; }
    public string? BirthdayAddress { get; init; }
}

public sealed record MoyNalogAuthenticatedResult<T>(T Value, MoyNalogTokens Tokens);
