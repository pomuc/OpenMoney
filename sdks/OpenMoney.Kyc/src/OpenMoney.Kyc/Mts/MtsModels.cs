namespace OpenMoney.Kyc.Mts;

public sealed class MtsSiAuthorizeResult
{
    public string AuthReqId { get; init; } = "";
    public int? ExpiresIn { get; init; }
}

public sealed class MtsNotifyPayload
{
    public string? AuthReqId { get; init; }
    public string? AccessToken { get; init; }
    public string? JwksUri { get; init; }
}

public sealed class MtsSmsNotifyPayload
{
    public string? AuthReqId { get; init; }
    public string? SmsOtpEndpoint { get; init; }
}

public sealed class MtsKycMatchResult
{
    public string? BirthdateMatch { get; init; }
    public string? Sub { get; init; }
    public bool IsMatch => string.Equals(BirthdateMatch, "Y", StringComparison.OrdinalIgnoreCase);
}

public sealed class MtsPremiumInfo
{
    public string? Address { get; init; }
    public string? Birthplace { get; init; }
    public string? DocumentType { get; init; }
    public string? FamilyName { get; init; }
    public string? GivenName { get; init; }
    public string? MiddleName { get; init; }
    public string? NationalIdentifier { get; init; }
    public string? NationalIdentifierAuthority { get; init; }
    public string? NationalIdentifierAuthorityCode { get; init; }
    public string? NationalIdentifierDate { get; init; }
    public string? Sex { get; init; }
}

public sealed class MtsOAuthTokenResponse
{
    public string? AccessToken { get; init; }
    public string? TokenType { get; init; }
    public string? IdToken { get; init; }
    public int? ExpiresIn { get; init; }
}

public sealed class MtsUserInfo
{
    public string? Sub { get; init; }
    public string? PhoneNumber { get; init; }
}

public sealed class MtsRimApplicant
{
    public Guid? ExternalId { get; init; }
}

public sealed class MtsRimIdentification
{
    public Guid? Id { get; init; }
    public string? IdentificationUrl { get; init; }
    public string? Status { get; init; }
}

public sealed class MtsRimPassport
{
    public string? FirstName { get; init; }
    public string? Surname { get; init; }
    public string? MiddleName { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Number { get; init; }
    public string? Series { get; init; }
    public DateOnly? DateOfIssue { get; init; }
    public string? IssuedBy { get; init; }
    public string? PlaceOfBirth { get; init; }
}
