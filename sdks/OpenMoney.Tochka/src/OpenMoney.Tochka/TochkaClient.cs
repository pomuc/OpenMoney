using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace OpenMoney.Tochka;

public enum TochkaSignatureEncoding { Hex, Base64 }

public sealed class TochkaOptions
{
    public const string SectionName = "Tochka";
    public string BaseUrl { get; set; } = "https://service.example.invalid/uapi/medusa/v1.0/";
    public string ClientId { get; set; } = "";
    public string KeyId { get; set; } = "";
    public string CertificatePemPath { get; set; } = "";
    public string PrivateKeyPemPath { get; set; } = "";
    public string? PrivateKeyPassword { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string SuccessRedirectUrl { get; set; } = "";
    public string FailureRedirectUrl { get; set; } = "";
    public int PaymentUrlTtlSeconds { get; set; } = 300;
    public TochkaSignatureEncoding SignatureEncoding { get; set; } = TochkaSignatureEncoding.Hex;
    public bool EnableSandboxOperations { get; set; }

    internal void Validate()
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _)) throw new OptionsValidationException(nameof(TochkaOptions), typeof(TochkaOptions), ["BaseUrl must be absolute."]);
        if (string.IsNullOrWhiteSpace(KeyId)) throw new OptionsValidationException(nameof(TochkaOptions), typeof(TochkaOptions), ["KeyId is required."]);
        if (string.IsNullOrWhiteSpace(PrivateKeyPemPath)) throw new OptionsValidationException(nameof(TochkaOptions), typeof(TochkaOptions), ["PrivateKeyPemPath is required."]);
    }
}

public sealed class TochkaRequestSigner
{
    private readonly TochkaOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public TochkaRequestSigner(IOptions<TochkaOptions> options) => _options = options.Value;

    public async Task<TochkaSignedBody> SignAsync(object? payload, CancellationToken cancellationToken = default)
    {
        var json = payload is null ? "" : FormatJsonStrict(JsonSerializer.Serialize(payload, JsonOptions));
        var pem = await File.ReadAllTextAsync(_options.PrivateKeyPemPath, cancellationToken).ConfigureAwait(false);
        using var rsa = RSA.Create();
        rsa.ImportFromEncryptedPemIfNeeded(pem, _options.PrivateKeyPassword);
        var signature = rsa.SignData(Encoding.UTF8.GetBytes(json), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return new TochkaSignedBody(
            _options.SignatureEncoding == TochkaSignatureEncoding.Hex ? Convert.ToHexString(signature) : Convert.ToBase64String(signature),
            json);
    }

    public static string FormatJsonStrict(string json)
    {
        var result = new StringBuilder(json.Length + 16);
        var inString = false;
        var escaped = false;
        for (var i = 0; i < json.Length; i++)
        {
            var c = json[i];
            if (inString)
            {
                result.Append(c);
                if (escaped) escaped = false;
                else if (c == '\\') escaped = true;
                else if (c == '"') inString = false;
                continue;
            }

            if (c == '"') { inString = true; result.Append(c); }
            else if (c == ':' || c == ',')
            {
                result.Append(c).Append(' ');
                while (i + 1 < json.Length && char.IsWhiteSpace(json[i + 1])) i++;
            }
            else if (!char.IsWhiteSpace(c)) result.Append(c);
        }
        return result.ToString();
    }
}

public sealed record TochkaSignedBody(string Signature, string Json);

internal static class RsaExtensions
{
    public static void ImportFromEncryptedPemIfNeeded(this RSA rsa, string pem, string? password)
    {
        if (pem.Contains("BEGIN ENCRYPTED PRIVATE KEY", StringComparison.Ordinal))
        {
            if (string.IsNullOrEmpty(password)) throw new CryptographicException("PrivateKeyPassword is required for an encrypted PEM.");
            rsa.ImportFromEncryptedPem(pem, password);
        }
        else rsa.ImportFromPem(pem);
    }
}

public sealed class TochkaApiException : HttpRequestException
{
    public HttpStatusCode StatusCodeValue { get; }
    public string? ResponseBody { get; }
    public TochkaApiException(HttpStatusCode statusCode, string? responseBody)
        : base($"Tochka returned HTTP {(int)statusCode} ({statusCode}).", null, statusCode)
    {
        StatusCodeValue = statusCode;
        ResponseBody = responseBody;
    }
}

public sealed class TochkaClient
{
    private readonly HttpClient _http;
    private readonly TochkaOptions _options;
    private readonly TochkaRequestSigner _signer;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public TochkaClient(HttpClient http, IOptions<TochkaOptions> options, TochkaRequestSigner signer)
    {
        _http = http;
        _options = options.Value;
        _options.Validate();
        _signer = signer;
    }

    public Task<TochkaEnvelope<TochkaRecipient>> CreateRecipientAsync(Guid recipientId, string name, CancellationToken ct = default) =>
        SendAsync<TochkaRecipient>(HttpMethod.Post, "recipients", new { extId = recipientId, name }, ct);

    public Task<TochkaEnvelope<TochkaRecipient>> GetRecipientAsync(Guid recipientId, CancellationToken ct = default) =>
        SendAsync<TochkaRecipient>(HttpMethod.Get, $"recipients/{recipientId:D}", null, ct);

    public async Task<IReadOnlyList<TochkaPayoutMethod>> GetRecipientCardsAsync(Guid recipientId, CancellationToken ct = default) =>
        (await GetRecipientAsync(recipientId, ct).ConfigureAwait(false)).Data?.PayoutMethods ?? [];

    public Task<TochkaEnvelope<TochkaCardForm>> CreateCardAsync(Guid recipientId, Guid payoutMethodId, string redirectUrl, CancellationToken ct = default) =>
        SendAsync<TochkaCardForm>(HttpMethod.Post, $"recipients/{recipientId:D}/payout_methods/cards",
            new { CardPayoutMethod = new { redirectUrl, payoutMethodExtId = payoutMethodId } }, ct);

    public Task<TochkaEnvelope<TochkaOrder>> CreateOrderAsync(TochkaCreateOrderRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.AmountMinorUnits <= 0) throw new ArgumentOutOfRangeException(nameof(request.AmountMinorUnits));
        if (request.CommissionMinorUnits < 0) throw new ArgumentOutOfRangeException(nameof(request.CommissionMinorUnits));
        var successUrl = request.SuccessRedirectUrl ?? _options.SuccessRedirectUrl;
        var failureUrl = request.FailureRedirectUrl ?? _options.FailureRedirectUrl;
        if (!Uri.TryCreate(successUrl, UriKind.Absolute, out _) || !Uri.TryCreate(failureUrl, UriKind.Absolute, out _))
            throw new ArgumentException("Absolute success and failure redirect URLs are required.");
        var payload = new
        {
            orderExtId = request.OrderId,
            orderCommission = Money(request.CommissionMinorUnits),
            receiptEmail = request.ReceiptEmail,
            IncomingPayment = new
            {
                type = "acquiring",
                redirectUrl = successUrl,
                redirectFailUrl = failureUrl,
                paymentUrlTtl = request.PaymentUrlTtlSeconds ?? _options.PaymentUrlTtlSeconds,
                purpose = request.Purpose
            },
            Services = new[]
            {
                new
                {
                    extId = request.ServiceId,
                    price = Money(request.AmountMinorUnits),
                    Recipient = new { extId = request.RecipientId, method = "CARD", cardExtId = request.CardId },
                    startDecision = "not_decided"
                }
            }
        };
        return SendAsync<TochkaOrder>(HttpMethod.Post, "orders", payload, ct);
    }

    public Task<TochkaEnvelope<TochkaOrder>> GetOrderAsync(Guid orderId, CancellationToken ct = default) =>
        SendAsync<TochkaOrder>(HttpMethod.Get, $"orders/{orderId:D}", null, ct);

    public Task<TochkaEnvelope<TochkaOrder>> SetOrderDecisionAsync(Guid orderId, IEnumerable<Guid> serviceIds, bool confirm, CancellationToken ct = default) =>
        SendAsync<TochkaOrder>(HttpMethod.Post, $"orders/{orderId:D}/decisions",
            new { Decisions = serviceIds.Select(id => new { serviceExtId = id, decision = confirm ? "confirmed" : "rejected" }).ToArray() }, ct);

    public async Task<TochkaEnvelope<TochkaOrder>> ConfirmAllServicesAsync(Guid orderId, bool confirm, CancellationToken ct = default)
    {
        var order = await GetOrderAsync(orderId, ct).ConfigureAwait(false);
        var ids = order.Data?.Services.Select(x => x.ExtId).ToArray() ?? [];
        return await SetOrderDecisionAsync(orderId, ids, confirm, ct).ConfigureAwait(false);
    }

    public Task<JsonElement> RunSandboxOperationAsync(TochkaSandboxOperation operation, Guid orderId, Guid? serviceId = null, CancellationToken ct = default)
    {
        if (!_options.EnableSandboxOperations) throw new InvalidOperationException("Sandbox operations are disabled.");
        var path = operation switch
        {
            TochkaSandboxOperation.ProceedAcquiringCommission => "sandbox/proceed_acquiring_commission",
            TochkaSandboxOperation.MovePlatformCommission => "sandbox/move_platform_commission_to_commission_account",
            TochkaSandboxOperation.ProceedPlatformCommission => "sandbox/proceed_platform_commission",
            TochkaSandboxOperation.ProceedRefund => "sandbox/proceed_refund",
            TochkaSandboxOperation.ProceedRecipientPayout => "sandbox/proceed_service_payout_to_recipient",
            TochkaSandboxOperation.ProceedServiceCommission => "sandbox/proceed_service_payout_commission",
            TochkaSandboxOperation.MarkOrderPaid => "sandbox/mark_order_paid_by_acquirer",
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
        object payload = serviceId.HasValue ? new { serviceExtId = serviceId, orderExtId = orderId } : new { orderExtId = orderId };
        return SendRawAsync(HttpMethod.Post, path, payload, ct);
    }

    public async Task<TochkaEnvelope<T>> SendAsync<T>(HttpMethod method, string path, object? payload, CancellationToken ct = default)
    {
        var json = await SendRawAsync(method, path, payload, ct).ConfigureAwait(false);
        return json.Deserialize<TochkaEnvelope<T>>(JsonOptions) ?? throw new JsonException("Tochka response was empty or invalid.");
    }

    private async Task<JsonElement> SendRawAsync(HttpMethod method, string path, object? payload, CancellationToken ct)
    {
        var signed = await _signer.SignAsync(payload, ct).ConfigureAwait(false);
        using var request = new HttpRequestMessage(method, path);
        if (payload is not null) request.Content = new StringContent(signed.Json, Encoding.UTF8, "application/json");
        request.Headers.TryAddWithoutValidation("Sign-Key-Id", _options.KeyId);
        request.Headers.TryAddWithoutValidation("Sign-Body", signed.Signature);
        if (!string.IsNullOrWhiteSpace(_options.ClientId)) request.Headers.TryAddWithoutValidation("Client-Id", _options.ClientId);
        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.Username}:{_options.Password ?? ""}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
        }
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) throw new TochkaApiException(response.StatusCode, body);
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
        return document.RootElement.Clone();
    }

    private static string Money(long minorUnits) => (minorUnits / 100m).ToString("0.00", CultureInfo.InvariantCulture);
}

public enum TochkaSandboxOperation
{
    ProceedAcquiringCommission, MovePlatformCommission, ProceedPlatformCommission, ProceedRefund,
    ProceedRecipientPayout, ProceedServiceCommission, MarkOrderPaid
}

public sealed record TochkaCreateOrderRequest(
    Guid OrderId, Guid RecipientId, Guid CardId, long AmountMinorUnits, long CommissionMinorUnits,
    string ReceiptEmail, string Purpose, Guid ServiceId, string? SuccessRedirectUrl = null,
    string? FailureRedirectUrl = null, int? PaymentUrlTtlSeconds = null);

public sealed class TochkaEnvelope<T>
{
    public T? Data { get; init; }
    public TochkaLinks? Links { get; init; }
    public TochkaMeta? Meta { get; init; }
}
public sealed class TochkaLinks { public string? Self { get; init; } }
public sealed class TochkaMeta { public int TotalPages { get; init; } }
public sealed class TochkaCardForm { public string? FormUrl { get; init; } }
public sealed class TochkaRecipient
{
    public Guid ExtId { get; init; }
    public string? Name { get; init; }
    public IReadOnlyList<TochkaPayoutMethod> PayoutMethods { get; init; } = [];
    public DateTimeOffset? CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
public sealed class TochkaPayoutMethod
{
    public Guid ExtId { get; init; }
    public string? MethodType { get; init; }
    public string? MaskedCardNumber { get; init; }
}
public sealed class TochkaOrder
{
    public Guid OrderExtId { get; init; }
    public string? State { get; init; }
    public string? PaymentUrl { get; init; }
    public string? ReceiptEmail { get; init; }
    public string? TotalPrice { get; init; }
    public string? TotalAmount { get; init; }
    public string? Purpose { get; init; }
    public IReadOnlyList<TochkaOrderService> Services { get; init; } = [];
}
public sealed class TochkaOrderService
{
    public Guid ExtId { get; init; }
    public string? State { get; init; }
    public string? Decision { get; init; }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenMoneyTochka(this IServiceCollection services, Action<TochkaOptions> configure)
    {
        services.AddOptions<TochkaOptions>().Configure(configure);
        services.AddSingleton<TochkaRequestSigner>();
        services.AddHttpClient<TochkaClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<TochkaOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        });
        return services;
    }
}
