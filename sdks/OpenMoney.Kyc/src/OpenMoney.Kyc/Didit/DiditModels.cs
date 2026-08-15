namespace OpenMoney.Kyc.Didit;

public sealed class DiditTokenResponse
{
    public string AccessToken { get; init; } = "";
}

public sealed class DiditSession
{
    public string SessionId { get; init; } = "";
    public string Url { get; init; } = "";
    public string? SessionToken { get; init; }
}

public sealed class DiditDecision
{
    public string? Status { get; init; }
    public string? VendorData { get; init; }
    public string? SessionId { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public DiditKycData? Kyc { get; init; }
    public DiditFaceData? Face { get; init; }

    public bool IsApproved =>
        string.Equals(Status, "Approved", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Status, "In Review", StringComparison.OrdinalIgnoreCase);
}

public sealed class DiditKycData
{
    public string? Address { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public DateOnly? DateOfIssue { get; init; }
    public string? DocumentNumber { get; init; }
    public string? DocumentType { get; init; }
    public string? ExpirationDate { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? IssuingState { get; init; }
    public string? PlaceOfBirth { get; init; }
    public string? Status { get; init; }
}

public sealed class DiditFaceData
{
    public string? LivenessStatus { get; init; }
    public string? Status { get; init; }
}
