using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using OpenMoney.VTB.Models;

namespace OpenMoney.VTB;

/// <summary>
/// VTB acquiring client: register order (card form) and create SBP C2B dynamic QR.
/// Extracted from production entertainment payment stack; entertainment/UI code is not included.
/// </summary>
/// <remarks>
/// Unofficial community code. Not affiliated with VTB Bank.
/// </remarks>
public sealed class VtbAcquiringClient
{
    private readonly HttpClient _http;
    private readonly VtbAcquiringOptions _options;

    /// <summary>Creates a typed VTB acquiring client.</summary>
    public VtbAcquiringClient(HttpClient http, IOptions<VtbAcquiringOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    /// <summary>
    /// Registers a payment and returns the hosted card payment form URL.
    /// GET {base}/rest/register.do
    /// </summary>
    /// <param name="orderNumber">Merchant order id.</param>
    /// <param name="amountMinorUnits">Amount in kopecks.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<VtbRegisterResult> RegisterCardPaymentAsync(
        Guid orderNumber,
        long amountMinorUnits,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        ValidatePayment(orderNumber, amountMinorUnits);
        var url =
            $"{TrimSlash(_options.BaseUrl)}/rest/register.do" +
            $"?token={Uri.EscapeDataString(_options.Token)}" +
            $"&orderNumber={orderNumber}" +
            $"&amount={amountMinorUnits}" +
            $"&returnUrl={Uri.EscapeDataString(_options.ReturnUrl)}";

        var result = await _http.GetFromJsonAsync<VtbRegisterResult>(url, cancellationToken).ConfigureAwait(false);
        if (result is null)
        {
            throw new VtbAcquiringException("VTB returned an empty registration response.");
        }

        if (!string.IsNullOrWhiteSpace(result.ErrorCode) || result.OrderId == Guid.Empty)
        {
            throw new VtbAcquiringException(
                $"VTB registration failed ({result.ErrorCode ?? "unknown"}): {result.ErrorMessage ?? "No order id returned."}");
        }

        return result;
    }

    /// <summary>
    /// Registers a payment, then requests an SBP C2B dynamic QR payload (nspk URL).
    /// </summary>
    public async Task<VtbSbpQrResult> CreateSbpQrAsync(
        Guid orderNumber,
        long amountMinorUnits,
        CancellationToken cancellationToken = default)
    {
        var register = await RegisterCardPaymentAsync(orderNumber, amountMinorUnits, cancellationToken)
            .ConfigureAwait(false);
        var qrUrl = $"{TrimSlash(_options.BaseUrl)}/rest/sbp/c2b/internal/qr/dynamic/get.do";
        using var response = await _http.PostAsJsonAsync(
            qrUrl,
            new
            {
                mdOrder = register.OrderId,
                qrHeight = _options.QrHeight,
                qrWidth = _options.QrWidth,
                qrFormat = "matrix",
                createSubscription = false,
                additionalParameters = new { }
            },
            cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        var qr = await response.Content.ReadFromJsonAsync<VtbQrResponse>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (qr is null)
        {
            throw new VtbAcquiringException("VTB returned an empty SBP QR response.");
        }

        if (!string.IsNullOrWhiteSpace(qr.ErrorCode) || string.IsNullOrWhiteSpace(qr.Payload))
        {
            throw new VtbAcquiringException(
                $"VTB SBP QR creation failed ({qr.ErrorCode ?? "unknown"}): {qr.ErrorMessage ?? "No QR payload returned."}");
        }

        return new VtbSbpQrResult
        {
            MdOrderId = register.OrderId,
            FormUrl = register.FormUrl,
            QrId = qr?.QrId,
            Payload = qr?.Payload,
            Status = qr?.Status,
            RenderedQr = qr?.RenderedQr
        };
    }

    /// <summary>
    /// Convenience: card → form URL; SBP → QR payload URL (same shape as original production helper).
    /// </summary>
    public async Task<(string? RedirectOrPayload, Guid? MdOrderId)> StartPaymentAsync(
        Guid orderNumber,
        long amountMinorUnits,
        bool byCard,
        CancellationToken cancellationToken = default)
    {
        if (byCard)
        {
            var reg = await RegisterCardPaymentAsync(orderNumber, amountMinorUnits, cancellationToken)
                .ConfigureAwait(false);
            return (reg.FormUrl, reg.OrderId);
        }

        var qr = await CreateSbpQrAsync(orderNumber, amountMinorUnits, cancellationToken).ConfigureAwait(false);
        return (qr.Payload, qr.MdOrderId);
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.Token) ||
            _options.Token.StartsWith("YOUR_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Configure VtbAcquiring:Token (do not commit real tokens).");
        }

        if (!Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out _)
            || !Uri.TryCreate(_options.ReturnUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("VTB BaseUrl and ReturnUrl must be absolute URLs.");
        }
    }

    private static void ValidatePayment(Guid orderNumber, long amountMinorUnits)
    {
        if (orderNumber == Guid.Empty)
        {
            throw new ArgumentException("Order number must not be empty.", nameof(orderNumber));
        }

        if (amountMinorUnits <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amountMinorUnits),
                "Payment amount must be greater than zero.");
        }
    }

    private static string TrimSlash(string url) => url.TrimEnd('/');
}

/// <summary>Represents a semantic error returned by the VTB acquiring gateway.</summary>
public sealed class VtbAcquiringException : HttpRequestException
{
    /// <summary>Creates a VTB gateway exception.</summary>
    public VtbAcquiringException(string message) : base(message)
    {
    }
}
