using System.Text.Json.Serialization;

namespace OpenMoney.SelfEmployed.Models;

/// <summary>Requests asynchronous creation of self-employed recipients.</summary>
public sealed class AddRecipientsByRequisitesRequest
{
    /// <summary>Client-generated idempotency and result lookup identifier.</summary>
    [JsonPropertyName("correlationId")]
    public Guid CorrelationId { get; set; } = Guid.NewGuid();

    /// <summary>Recipients to create.</summary>
    [JsonPropertyName("recipients")]
    public List<RecipientRequisites> Recipients { get; set; } = new();
}

/// <summary>Bank and identity details used to create a recipient.</summary>
public sealed class RecipientRequisites
{
    /// <summary>Caller-defined row number echoed in asynchronous results.</summary>
    [JsonPropertyName("number")]
    public long Number { get; set; }

    /// <summary>First name.</summary>
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Last name.</summary>
    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Optional middle name.</summary>
    [JsonPropertyName("middleName")]
    public string? MiddleName { get; set; }

    /// <summary>Mobile number in the format accepted by T-Bank.</summary>
    [JsonPropertyName("mobileNumber")]
    public string MobileNumber { get; set; } = string.Empty;

    /// <summary>Taxpayer identification number.</summary>
    [JsonPropertyName("inn")]
    public string Inn { get; set; } = string.Empty;

    /// <summary>Recipient bank details.</summary>
    [JsonPropertyName("bankInfo")]
    public RecipientBankInfo BankInfo { get; set; } = new();
}

/// <summary>Correlation response returned by asynchronous operations.</summary>
public sealed class CorrelationResponse
{
    /// <summary>Identifier used to poll the operation result.</summary>
    [JsonPropertyName("correlationId")]
    public Guid CorrelationId { get; set; }
}

/// <summary>Result of an add-by-requisites operation.</summary>
public sealed class AddRecipientsResult
{
    /// <summary>Per-recipient results.</summary>
    [JsonPropertyName("recipientResults")]
    public List<RecipientAddResult> RecipientResults { get; set; } = new();
}

/// <summary>Creation result for one recipient.</summary>
public sealed class RecipientAddResult
{
    /// <summary>Caller-defined row number.</summary>
    [JsonPropertyName("number")]
    public long Number { get; set; }

    /// <summary>Created recipient identifier, when creation succeeded.</summary>
    [JsonPropertyName("recipientId")]
    public long? RecipientId { get; set; }

    /// <summary>First name echoed by the API.</summary>
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    /// <summary>Last name echoed by the API.</summary>
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    /// <summary>Middle name echoed by the API.</summary>
    [JsonPropertyName("middleName")]
    public string? MiddleName { get; set; }

    /// <summary>Operation status, for example CREATED or ERROR.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Validation errors for this recipient.</summary>
    [JsonPropertyName("errors")]
    public List<NpdApiError> Errors { get; set; } = new();
}

/// <summary>Requests creation of a self-employed payment registry draft.</summary>
public sealed class CreatePaymentRegistryRequest
{
    /// <summary>Client-generated idempotency and result lookup identifier.</summary>
    [JsonPropertyName("correlationId")]
    public Guid CorrelationId { get; set; } = Guid.NewGuid();

    /// <summary>Organization debit account.</summary>
    [JsonPropertyName("companyAccountNumber")]
    public string CompanyAccountNumber { get; set; } = string.Empty;

    /// <summary>Registry validation mode, for example FAIL_ERRORS.</summary>
    [JsonPropertyName("registryCreateType")]
    public string RegistryCreateType { get; set; } = "FAIL_ERRORS";

    /// <summary>Payments included in the registry.</summary>
    [JsonPropertyName("payments")]
    public List<SelfEmployedPayment> Payments { get; set; } = new();

    /// <summary>Whether T-Bank should reserve tax from each payment.</summary>
    [JsonPropertyName("taxHolding")]
    public bool TaxHolding { get; set; }

    /// <summary>Income source classification.</summary>
    [JsonPropertyName("incomeType")]
    public string IncomeType { get; set; } = "FROM_LEGAL_ENTITY";
}

/// <summary>One payment in a self-employed registry.</summary>
public sealed class SelfEmployedPayment
{
    /// <summary>Unique payment number within the registry.</summary>
    [JsonPropertyName("number")]
    public long Number { get; set; }

    /// <summary>Recipient account number.</summary>
    [JsonPropertyName("accountNumber")]
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>Payment purpose shown in bank documents.</summary>
    [JsonPropertyName("paymentPurpose")]
    public string PaymentPurpose { get; set; } = string.Empty;

    /// <summary>Recipient name.</summary>
    [JsonPropertyName("selfEmployedInfo")]
    public SelfEmployedPerson SelfEmployedInfo { get; set; } = new();

    /// <summary>Amount in rubles.</summary>
    [JsonPropertyName("sum")]
    public decimal Sum { get; set; }

    /// <summary>Revenue type code required by payment regulations.</summary>
    [JsonPropertyName("revenueTypeCode")]
    public string? RevenueTypeCode { get; set; }
}

/// <summary>Name of a self-employed payment recipient.</summary>
public sealed class SelfEmployedPerson
{
    /// <summary>First name.</summary>
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Last name.</summary>
    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Optional middle name.</summary>
    [JsonPropertyName("middleName")]
    public string? MiddleName { get; set; }
}

/// <summary>Result of payment registry draft creation.</summary>
public sealed class PaymentRegistryCreateResult
{
    /// <summary>Created registry identifier.</summary>
    [JsonPropertyName("paymentRegistryId")]
    public long? PaymentRegistryId { get; set; }

    /// <summary>Operation status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Registry-level error.</summary>
    [JsonPropertyName("error")]
    public NpdApiError? Error { get; set; }

    /// <summary>Per-payment validation errors.</summary>
    [JsonPropertyName("paymentErrors")]
    public List<PaymentError> PaymentErrors { get; set; } = new();
}

/// <summary>Request operating on an existing payment registry.</summary>
public sealed class PaymentRegistryRequest
{
    /// <summary>Client-generated idempotency and result lookup identifier.</summary>
    [JsonPropertyName("correlationId")]
    public Guid CorrelationId { get; set; } = Guid.NewGuid();

    /// <summary>Payment registry identifier.</summary>
    [JsonPropertyName("paymentRegistryId")]
    public long PaymentRegistryId { get; set; }
}

/// <summary>Request for the result of a registry operation.</summary>
public sealed class CorrelationRequest
{
    /// <summary>Correlation identifier returned by the operation.</summary>
    [JsonPropertyName("correlationId")]
    public Guid CorrelationId { get; set; }
}

/// <summary>Result of registry submission.</summary>
public sealed class PaymentRegistrySubmitResult
{
    /// <summary>Payment registry identifier.</summary>
    [JsonPropertyName("paymentRegistryId")]
    public long? PaymentRegistryId { get; set; }

    /// <summary>Submission status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Per-payment errors.</summary>
    [JsonPropertyName("paymentErrors")]
    public List<PaymentError> PaymentErrors { get; set; } = new();
}

/// <summary>Result of payment registry execution.</summary>
public sealed class PaymentRegistryPayResult
{
    /// <summary>Payment registry identifier.</summary>
    [JsonPropertyName("paymentRegistryId")]
    public long? PaymentRegistryId { get; set; }

    /// <summary>Execution status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Number of payment results.</summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>Per-payment execution results.</summary>
    [JsonPropertyName("paymentResults")]
    public List<PaymentResult> PaymentResults { get; set; } = new();
}

/// <summary>Validation errors for one registry payment.</summary>
public sealed class PaymentError
{
    /// <summary>Payment number.</summary>
    [JsonPropertyName("number")]
    public long Number { get; set; }

    /// <summary>Recipient account number.</summary>
    [JsonPropertyName("accountNumber")]
    public string? AccountNumber { get; set; }

    /// <summary>Errors reported for this payment.</summary>
    [JsonPropertyName("errors")]
    public List<NpdApiError> Errors { get; set; } = new();
}

/// <summary>Execution result for one payment.</summary>
public sealed class PaymentResult
{
    /// <summary>Payment number.</summary>
    [JsonPropertyName("number")]
    public long Number { get; set; }

    /// <summary>Payment status.</summary>
    [JsonPropertyName("paymentStatus")]
    public string? PaymentStatus { get; set; }

    /// <summary>Errors reported for this payment.</summary>
    [JsonPropertyName("errors")]
    public List<NpdApiError> Errors { get; set; } = new();
}

/// <summary>Error returned by a T-Bank NPD endpoint.</summary>
public sealed class NpdApiError
{
    /// <summary>Field associated with the error.</summary>
    [JsonPropertyName("fieldName")]
    public string? FieldName { get; set; }

    /// <summary>Human-readable error description.</summary>
    [JsonPropertyName("errorDescription")]
    public string? ErrorDescription { get; set; }
}
