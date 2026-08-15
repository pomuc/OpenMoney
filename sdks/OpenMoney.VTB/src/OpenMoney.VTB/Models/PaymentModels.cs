using System.Text.Json.Serialization;

namespace OpenMoney.VTB.Models;

/// <summary>Response of register.do</summary>
public sealed class VtbRegisterResult
{
    /// <summary>Bank-side order identifier.</summary>
    [JsonPropertyName("orderId")]
    public Guid OrderId { get; set; }

    /// <summary>Hosted card payment form URL.</summary>
    [JsonPropertyName("formUrl")]
    public string? FormUrl { get; set; }

    /// <summary>Gateway error code when registration fails.</summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    /// <summary>Gateway error message when registration fails.</summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}

/// <summary>Response of sbp/c2b/internal/qr/dynamic/get.do</summary>
public sealed class VtbQrResponse
{
    /// <summary>VTB QR identifier.</summary>
    [JsonPropertyName("qrId")]
    public string? QrId { get; set; }

    /// <summary>Optional matrix representation returned by the gateway.</summary>
    [JsonPropertyName("renderedQr")]
    public string? RenderedQr { get; set; }

    /// <summary>QR lifecycle status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>NSPK payload URL encoded by the QR.</summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    /// <summary>Gateway error code when QR creation fails.</summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    /// <summary>Gateway error message when QR creation fails.</summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}

/// <summary>Combined registration and SBP QR result.</summary>
public sealed class VtbSbpQrResult
{
    /// <summary>Bank-side order identifier.</summary>
    public Guid MdOrderId { get; set; }
    /// <summary>Hosted form URL returned during registration.</summary>
    public string? FormUrl { get; set; }
    /// <summary>VTB QR identifier.</summary>
    public string? QrId { get; set; }
    /// <summary>NSPK payload URL.</summary>
    public string? Payload { get; set; }
    /// <summary>QR lifecycle status.</summary>
    public string? Status { get; set; }
    /// <summary>Optional matrix representation.</summary>
    public string? RenderedQr { get; set; }
}

/// <summary>
/// Bank callback payload (form-urlencoded fields mapped to DTO).
/// Verify checksum per VTB docs before trusting status.
/// </summary>
public sealed class VtbAcquiringCallback
{
    /// <summary>Bank-side order identifier.</summary>
    public Guid MdOrder { get; set; }
    /// <summary>Unique callback processing identifier used for deduplication.</summary>
    public long ProcessingId { get; set; }
    /// <summary>Bank-supplied callback checksum.</summary>
    public string? Checksum { get; set; }

    /// <summary>deposited, approved, reversed, refunded, declinedByTimeout, …</summary>
    public string? Operation { get; set; }

    /// <summary>payment_deposited, payment_declined, …</summary>
    public string? PaymentState { get; set; }

    /// <summary>Amount in kopecks.</summary>
    public long Amount { get; set; }

    /// <summary>
    /// All decoded callback fields, including fields required by a host checksum verifier.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyDictionary<string, string> Fields { get; internal set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Domain-neutral request for starting a payment.</summary>
public sealed class StartPaymentRequest
{
    /// <summary>true = card form; false = SBP QR payload.</summary>
    public bool ByCard { get; set; }

    /// <summary>Amount in kopecks.</summary>
    public long Amount { get; set; }
}

/// <summary>Domain-neutral payment start response.</summary>
public sealed class StartPaymentResponse
{
    /// <summary>Merchant-side order identifier.</summary>
    public Guid OrderId { get; set; }
    /// <summary>Card form URL or SBP payload URL.</summary>
    public string? RedirectUrl { get; set; }
}

/// <summary>Domain-neutral stored payment status response.</summary>
public sealed class PaymentStatusResponse
{
    /// <summary>Current payment status.</summary>
    public string? Status { get; set; }
    /// <summary>Payment amount in minor units.</summary>
    public long Amount { get; set; }
    /// <summary>Payment type such as card or QR.</summary>
    public string? Type { get; set; }
}
