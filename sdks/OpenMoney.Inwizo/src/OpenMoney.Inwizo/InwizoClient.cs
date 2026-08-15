using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace OpenMoney.Inwizo;

public sealed class InwizoOptions
{
    public const string SectionName = "Inwizo";
    /// <summary>
    /// Именной API host Inwizo (выдаётся клиенту). Задаётся только через конфигурацию / env — без дефолта в коде.
    /// </summary>
    public string BaseUrl { get; set; } = "";
    public string Account { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string? SbpAccount { get; set; }
    public string? SbpApiKey { get; set; }
    public string Operator { get; set; } = "";
    public string HostedPaymentUrl { get; set; } = "";
    public string HostedCardUrl { get; set; } = "";

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl) || !Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
            throw new OptionsValidationException(nameof(InwizoOptions), typeof(InwizoOptions),
                ["BaseUrl is required and must be an absolute URI (Inwizo выдаёт именной адрес клиенту)."]);
        if (string.IsNullOrWhiteSpace(Account) || string.IsNullOrWhiteSpace(ApiKey))
            throw new OptionsValidationException(nameof(InwizoOptions), typeof(InwizoOptions), ["Account and ApiKey are required."]);
    }
}

public sealed class InwizoApiException : HttpRequestException
{
    public HttpStatusCode StatusCodeValue { get; }
    public string? ResponseBody { get; }
    public InwizoApiException(HttpStatusCode status, string? body)
        : base($"Inwizo returned HTTP {(int)status} ({status}).", null, status)
    { StatusCodeValue = status; ResponseBody = body; }
}

public sealed class InwizoClient
{
    private readonly HttpClient _http;
    private readonly InwizoOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public InwizoClient(HttpClient http, IOptions<InwizoOptions> options)
    {
        _http = http;
        _options = options.Value;
        _options.Validate();
    }

    public InwizoPaymentInitialization InitializeHostedPayment(InwizoPaymentInitializationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.AmountMinorUnits <= 0) throw new ArgumentOutOfRangeException(nameof(request.AmountMinorUnits));
        if (!Uri.TryCreate(_options.HostedPaymentUrl, UriKind.Absolute, out var baseUri))
            throw new InvalidOperationException("HostedPaymentUrl must be configured.");
        var externalId = Guid.NewGuid();
        var builder = new UriBuilder(baseUri);
        var query = new Dictionary<string, string>
        {
            ["paymentId"] = externalId.ToString("D"),
            ["orderId"] = request.OrderId,
            ["amountRub"] = FormatAmount(request.AmountMinorUnits),
            ["email"] = request.Email,
            ["isSbp"] = request.Method == InwizoPaymentMethod.Sbp ? "true" : "false"
        };
        builder.Query = string.Join("&", query.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
        return new InwizoPaymentInitialization(request.OrderId, externalId, builder.Uri, InwizoTransactionState.New);
    }

    public Uri CreateCardRegistrationUrl(string customerId)
    {
        if (!Uri.TryCreate(_options.HostedCardUrl, UriKind.Absolute, out var baseUri))
            throw new InvalidOperationException("HostedCardUrl must be configured.");
        var builder = new UriBuilder(baseUri) { Query = $"customerId={Uri.EscapeDataString(customerId)}" };
        return builder.Uri;
    }

    public async Task<InwizoOperationResult> GetPaymentStatusAsync(InwizoPaymentStatusRequest request, CancellationToken ct = default)
    {
        var (account, key) = Credentials(request.Method);
        var signature = Md5($"check:{account}:{request.TransactionId}:{key}");
        var response = await PostAsync("/api/payment/operate", new Dictionary<string, string>
        {
            ["opertype"] = "check",
            ["transID"] = request.TransactionId,
            ["number"] = request.ExternalPaymentId.ToString("D"),
            ["account"] = account,
            ["appinfo"] = "1",
            ["signature"] = signature
        }, ct).ConfigureAwait(false);
        return ToResult(response);
    }

    public async Task<InwizoPayoutInitialization> InitializePayoutAsync(InwizoPayoutRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.AmountMinorUnits <= 0) throw new ArgumentOutOfRangeException(nameof(request.AmountMinorUnits));
        if (request.CardToken.Length < 10) throw new ArgumentException("Card token is too short.", nameof(request.CardToken));
        var externalId = request.ExternalPaymentId ?? Guid.NewGuid();
        var nonce = Guid.NewGuid().ToString("N");
        var amount = FormatAmount(request.AmountMinorUnits);
        var maskedPan = request.CardToken[..6] + "******" + request.CardToken[^4..];
        var signature = Md5($"{nonce}:{_options.Account}:{_options.Operator}:{maskedPan}:{amount}:RUB:{externalId:D}:{_options.ApiKey}");
        var response = await PostAsync("/api/payout/execute", new Dictionary<string, string>
        {
            ["account"] = _options.Account,
            ["operator"] = _options.Operator,
            ["params"] = request.CardToken,
            ["amount"] = amount,
            ["amountcurr"] = "RUB",
            ["number"] = externalId.ToString("D"),
            ["nonce"] = nonce,
            ["signature"] = signature
        }, ct).ConfigureAwait(false);
        return new InwizoPayoutInitialization(response.TransId ?? "0", externalId, ToState(response.Status), response.ErrorCode, response.ErrorText);
    }

    public async Task<InwizoOperationResult> GetPayoutStatusAsync(string transactionId, Guid externalPaymentId, CancellationToken ct = default)
    {
        var nonce = Guid.NewGuid().ToString("N");
        var signature = Md5($"{nonce}:{_options.Account}:{externalPaymentId:D}:{transactionId}:{_options.ApiKey}");
        var response = await PostAsync("/api/payout/status", new Dictionary<string, string>
        {
            ["transID"] = transactionId,
            ["number"] = externalPaymentId.ToString("D"),
            ["account"] = _options.Account,
            ["nonce"] = nonce,
            ["signature"] = signature
        }, ct).ConfigureAwait(false);
        return ToResult(response);
    }

    private async Task<InwizoWireResponse> PostAsync(string path, IReadOnlyDictionary<string, string> values, CancellationToken ct)
    {
        using var content = new FormUrlEncodedContent(values);
        using var response = await _http.PostAsync(path, content, ct).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) throw new InwizoApiException(response.StatusCode, body);
        return JsonSerializer.Deserialize<InwizoWireResponse>(body, JsonOptions) ?? throw new JsonException("Inwizo response was empty or invalid.");
    }

    private (string Account, string Key) Credentials(InwizoPaymentMethod method) =>
        method == InwizoPaymentMethod.Sbp
            ? (_options.SbpAccount ?? _options.Account, _options.SbpApiKey ?? _options.ApiKey)
            : (_options.Account, _options.ApiKey);

    private static InwizoOperationResult ToResult(InwizoWireResponse value) =>
        new(value.TransId, value.Number, ToState(value.Status), value.ErrorCode, value.ErrorText, value.Amount, value.AmountCurrency);

    private static InwizoTransactionState ToState(string? status) => status?.ToUpperInvariant() switch
    {
        "OK" => InwizoTransactionState.Confirmed,
        "WAIT" => InwizoTransactionState.New,
        _ => InwizoTransactionState.Rejected
    };
    private static string Md5(string value) => Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(value)));
    public static string FormatAmount(long minorUnits) => (minorUnits / 100m).ToString("0.##", CultureInfo.InvariantCulture);
}

public enum InwizoPaymentMethod { Card, Sbp }
public enum InwizoTransactionState { New, Confirmed, Rejected }
public sealed record InwizoPaymentInitializationRequest(string OrderId, long AmountMinorUnits, string Email, InwizoPaymentMethod Method);
public sealed record InwizoPaymentInitialization(string OrderId, Guid ExternalPaymentId, Uri PaymentUrl, InwizoTransactionState State);
public sealed record InwizoPaymentStatusRequest(string TransactionId, Guid ExternalPaymentId, InwizoPaymentMethod Method);
public sealed record InwizoPayoutRequest(string OrderId, long AmountMinorUnits, string CardToken, Guid? ExternalPaymentId = null);
public sealed record InwizoPayoutInitialization(string TransactionId, Guid ExternalPaymentId, InwizoTransactionState State, string? ErrorCode, string? ErrorMessage);
public sealed record InwizoOperationResult(
    string? TransactionId, string? ExternalNumber, InwizoTransactionState State,
    string? ErrorCode, string? ErrorMessage, string? Amount, string? Currency);

internal sealed class InwizoWireResponse
{
    [JsonPropertyName("status")] public string? Status { get; init; }
    [JsonPropertyName("transID")] public string? TransId { get; init; }
    [JsonPropertyName("number")] public string? Number { get; init; }
    [JsonPropertyName("amount")] public string? Amount { get; init; }
    [JsonPropertyName("amountcurr")] public string? AmountCurrency { get; init; }
    [JsonPropertyName("errorcode")] public string? ErrorCode { get; init; }
    [JsonPropertyName("errortext")] public string? ErrorText { get; init; }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenMoneyInwizo(this IServiceCollection services, Action<InwizoOptions> configure)
    {
        services.AddOptions<InwizoOptions>().Configure(configure);
        services.AddHttpClient<InwizoClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<InwizoOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        });
        return services;
    }
}
