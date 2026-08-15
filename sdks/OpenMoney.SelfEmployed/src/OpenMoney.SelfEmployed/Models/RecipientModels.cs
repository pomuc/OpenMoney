using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenMoney.SelfEmployed.Models;

/// <summary>Filters and pages a recipient list request.</summary>
public sealed class RecipientsListRequest
{
    /// <summary>Optional recipient identifiers to include.</summary>
    [JsonPropertyName("recipientIds")]
    public List<long> RecipientIds { get; set; } = new();

    /// <summary>Optional taxpayer identifiers to include.</summary>
    [JsonPropertyName("inn")]
    public string[] Inn { get; set; } = Array.Empty<string>();

    /// <summary>Zero-based result offset.</summary>
    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    /// <summary>Maximum number of results.</summary>
    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 100;
}

/// <summary>One page of self-employed recipients.</summary>
public sealed class RecipientsResponse
{
    /// <summary>Recipients returned by the API.</summary>
    [JsonPropertyName("recipients")]
    public List<SelfEmployedRecipient> Recipients { get; set; } = new();
}

/// <summary>A recipient registered in T-Bank Business.</summary>
public sealed class SelfEmployedRecipient
{
    /// <summary>T-Bank recipient identifier.</summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>Recipient lifecycle status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>ACTIVE | NOT_ACTIVE | …</summary>
    [JsonPropertyName("selfEmployedStatus")]
    public string? SelfEmployedStatus { get; set; }

    /// <summary>First name.</summary>
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    /// <summary>Last name.</summary>
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    /// <summary>Optional middle name.</summary>
    [JsonPropertyName("middleName")]
    public string? MiddleName { get; set; }

    /// <summary>Registered phone numbers.</summary>
    [JsonPropertyName("phones")]
    public List<RecipientPhone> Phones { get; set; } = new();

    /// <summary>Unstructured identity documents returned by the API.</summary>
    [JsonPropertyName("documents")]
    public List<JsonElement> Documents { get; set; } = new();

    /// <summary>Recipient bank details.</summary>
    [JsonPropertyName("bankInfo")]
    public RecipientBankInfo? BankInfo { get; set; }

    /// <summary>Taxpayer identification number.</summary>
    [JsonPropertyName("inn")]
    public string? Inn { get; set; }

    /// <summary>Recipient creation timestamp.</summary>
    [JsonPropertyName("creationDate")]
    public DateTimeOffset CreationDate { get; set; }

    /// <summary>Optional date of birth.</summary>
    [JsonPropertyName("birthDate")]
    public DateOnly? BirthDate { get; set; }
}

/// <summary>A recipient phone number.</summary>
public sealed class RecipientPhone
{
    /// <summary>Phone type reported by the bank.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>Phone number.</summary>
    [JsonPropertyName("number")]
    public string? Number { get; set; }
}

/// <summary>Recipient bank account details.</summary>
public sealed class RecipientBankInfo
{
    /// <summary>Recipient account number.</summary>
    [JsonPropertyName("accountNumber")]
    public string? AccountNumber { get; set; }

    /// <summary>Bank identification code.</summary>
    [JsonPropertyName("bankBic")]
    public string? BankBic { get; set; }
}

/// <summary>Host-side mapping helper for NPD status labels.</summary>
public static class SelfEmployedStatusMap
{
    /// <summary>Active NPD status.</summary>
    public const string Active = "ACTIVE";
    /// <summary>Inactive NPD status.</summary>
    public const string NotActive = "NOT_ACTIVE";

    /// <summary>Maps T-Bank NPD status to a conventional host status label.</summary>
    public static string ToHostStatus(string? selfEmployedStatus) =>
        selfEmployedStatus switch
        {
            Active => "Selfemployed",
            NotActive => "Inactive",
            _ => "Temp"
        };
}
