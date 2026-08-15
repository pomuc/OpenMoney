using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace OpenMoney.YooMoney;

/// <summary>
/// Настройки клиента ЮKassa (api.yookassa.ru).
/// </summary>
public sealed class YooMoneyOptions
{
    public const string SectionName = "YooMoney";

    /// <summary>Базовый URL API. По умолчанию https://api.yookassa.ru</summary>
    public string BaseUrl { get; set; } = "https://api.yookassa.ru";

    /// <summary>ShopId по умолчанию (логин Basic Auth).</summary>
    public string ShopId { get; set; } = "";

    /// <summary>Secret Key по умолчанию (пароль Basic Auth). Только из конфигурации.</summary>
    public string SecretKey { get; set; } = "";

    /// <summary>
    /// Дополнительные магазины: ShopId → SecretKey.
    /// Нужны, если у мерчанта несколько shop.
    /// </summary>
    public Dictionary<string, string> Shops { get; set; } = new(StringComparer.Ordinal);

    internal void Validate()
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
            throw new OptionsValidationException(nameof(YooMoneyOptions), typeof(YooMoneyOptions),
                ["BaseUrl must be absolute."]);
        if (string.IsNullOrWhiteSpace(ShopId) || string.IsNullOrWhiteSpace(SecretKey))
            throw new OptionsValidationException(nameof(YooMoneyOptions), typeof(YooMoneyOptions),
                ["ShopId and SecretKey are required."]);
    }

    internal (string ShopId, string SecretKey) ResolveShop(string? shopId)
    {
        var id = string.IsNullOrWhiteSpace(shopId) ? ShopId : shopId!;
        if (Shops.TryGetValue(id, out var key) && !string.IsNullOrWhiteSpace(key))
            return (id, key);
        return (ShopId, SecretKey);
    }
}

public sealed class YooMoneyApiException : HttpRequestException
{
    public HttpStatusCode StatusCodeValue { get; }
    public string? ResponseBody { get; }

    public YooMoneyApiException(HttpStatusCode status, string? body)
        : base($"YooKassa returned HTTP {(int)status} ({status}).", null, status)
    {
        StatusCodeValue = status;
        ResponseBody = body;
    }
}

public interface IYooMoneyClient
{
    /// <summary>Создать безопасную сделку (<c>POST /v3/deals</c>, type=safe_deal).</summary>
    Task<YooDealCreateResult> CreateSafeDealAsync(YooCreateDealRequest request, CancellationToken cancellationToken = default);

    /// <summary>Статус сделки (<c>GET /v3/deals/dl-{id}</c>).</summary>
    Task<YooDealStatus?> GetDealAsync(Guid dealId, string? shopId = null, CancellationToken cancellationToken = default);

    /// <summary>Есть ли положительный balance у сделки.</summary>
    Task<bool> HasDealBalanceAsync(Guid dealId, string? shopId = null, CancellationToken cancellationToken = default);

    /// <summary>Создать платёж с привязкой к сделке и settlement на выплату (<c>POST /v3/payments</c>).</summary>
    Task<YooPaymentCreateResult> CreatePaymentAsync(YooCreatePaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Статус платежа (<c>GET /v3/payments/{id}</c>).</summary>
    Task<YooPaymentStatus?> GetPaymentAsync(Guid paymentId, string? shopId = null, CancellationToken cancellationToken = default);

    /// <summary>Выплата физлицу по payout_token в рамках сделки (<c>POST /v3/payouts</c>).</summary>
    Task<YooPayoutCreateResult> CreatePayoutAsync(YooCreatePayoutRequest request, CancellationToken cancellationToken = default);
}

public sealed class YooMoneyClient : IYooMoneyClient
{
    private readonly HttpClient _http;
    private readonly YooMoneyOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public YooMoneyClient(HttpClient http, IOptions<YooMoneyOptions> options)
    {
        _http = http;
        _options = options.Value;
        _options.Validate();
    }

    public async Task<YooDealCreateResult> CreateSafeDealAsync(YooCreateDealRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var description = string.IsNullOrWhiteSpace(request.Description)
            ? $"Безопасная сделка {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"
            : request.Description!;

        var body = new
        {
            type = "safe_deal",
            fee_moment = string.IsNullOrWhiteSpace(request.FeeMoment) ? "payment_succeeded" : request.FeeMoment,
            description,
            metadata = request.Metadata
        };

        var response = await SendAsync<YooDealApiResponse>(HttpMethod.Post, "/v3/deals", body, request.ShopId, cancellationToken)
            .ConfigureAwait(false);

        var externalId = ParsePrefixedId(response?.Id, "dl-");
        return new YooDealCreateResult(
            Success: response != null && string.Equals(response.Status, "opened", StringComparison.OrdinalIgnoreCase),
            ExternalDealId: externalId,
            RawId: response?.Id,
            Status: response?.Status);
    }

    public async Task<YooDealStatus?> GetDealAsync(Guid dealId, string? shopId = null, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<YooDealStatusApiResponse>(
                HttpMethod.Get, $"/v3/deals/dl-{dealId:D}", body: null, shopId, cancellationToken)
            .ConfigureAwait(false);
        if (response is null) return null;
        return new YooDealStatus(
            BalanceMinorUnits: ParseAmountMinor(response.Balance?.Value),
            PayoutBalanceMinorUnits: ParseAmountMinor(response.PayoutBalance?.Value),
            BalanceValue: response.Balance?.Value,
            PayoutBalanceValue: response.PayoutBalance?.Value);
    }

    public async Task<bool> HasDealBalanceAsync(Guid dealId, string? shopId = null, CancellationToken cancellationToken = default)
    {
        var status = await GetDealAsync(dealId, shopId, cancellationToken).ConfigureAwait(false);
        return status is { BalanceMinorUnits: > 0 };
    }

    public async Task<YooPaymentCreateResult> CreatePaymentAsync(YooCreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.AmountMinorUnits <= 0) throw new ArgumentOutOfRangeException(nameof(request.AmountMinorUnits));
        if (request.PayoutAmountMinorUnits <= 0) throw new ArgumentOutOfRangeException(nameof(request.PayoutAmountMinorUnits));
        if (request.DealId == Guid.Empty) throw new ArgumentException("DealId is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.ReturnUrl)) throw new ArgumentException("ReturnUrl is required.", nameof(request));

        var body = new
        {
            amount = new { value = FormatAmount(request.AmountMinorUnits), currency = "RUB" },
            capture = request.Capture,
            confirmation = new { type = "redirect", return_url = request.ReturnUrl },
            description = request.Description,
            metadata = request.Metadata,
            deal = new
            {
                id = $"dl-{request.DealId:D}",
                settlements = new[]
                {
                    new
                    {
                        type = "payout",
                        amount = new { value = FormatAmount(request.PayoutAmountMinorUnits), currency = "RUB" }
                    }
                }
            }
        };

        var (resolvedShop, _) = _options.ResolveShop(request.ShopId);
        var response = await SendAsync<YooPaymentApiResponse>(HttpMethod.Post, "/v3/payments", body, request.ShopId, cancellationToken)
            .ConfigureAwait(false);

        var pending = response != null && string.Equals(response.Status, "pending", StringComparison.OrdinalIgnoreCase);
        return new YooPaymentCreateResult(
            Success: pending,
            PaymentId: response?.Id,
            ConfirmationUrl: pending ? response?.Confirmation?.ConfirmationUrl : null,
            Status: response?.Status,
            ShopId: resolvedShop);
    }

    public async Task<YooPaymentStatus?> GetPaymentAsync(Guid paymentId, string? shopId = null, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<YooPaymentApiResponse>(
                HttpMethod.Get, $"/v3/payments/{paymentId:D}", body: null, shopId, cancellationToken)
            .ConfigureAwait(false);
        if (response is null) return null;
        return new YooPaymentStatus(
            PaymentId: response.Id,
            Status: response.Status,
            Paid: response.Paid,
            PaymentMethodType: response.PaymentMethod?.Type);
    }

    public async Task<YooPayoutCreateResult> CreatePayoutAsync(YooCreatePayoutRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.AmountMinorUnits <= 0) throw new ArgumentOutOfRangeException(nameof(request.AmountMinorUnits));
        if (request.DealId == Guid.Empty) throw new ArgumentException("DealId is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.PayoutToken))
            throw new ArgumentException("PayoutToken is required (токен карты/кошелька физлица).", nameof(request));

        var description = string.IsNullOrWhiteSpace(request.Description)
            ? $"Выплата по договору {request.OrderId}"
            : request.Description!;

        var body = new
        {
            amount = new { value = FormatAmount(request.AmountMinorUnits), currency = "RUB" },
            payout_token = request.PayoutToken,
            description,
            metadata = request.Metadata,
            deal = new { id = $"dl-{request.DealId:D}" }
        };

        var response = await SendAsync<YooPayoutApiResponse>(HttpMethod.Post, "/v3/payouts", body, request.ShopId, cancellationToken)
            .ConfigureAwait(false);

        var ok = response != null &&
                 (string.Equals(response.Status, "succeeded", StringComparison.OrdinalIgnoreCase)
                  || string.Equals(response.Status, "pending", StringComparison.OrdinalIgnoreCase));

        return new YooPayoutCreateResult(
            Success: ok,
            ExternalPayoutId: ParsePrefixedId(response?.Id, "po-"),
            RawId: response?.Id,
            Status: response?.Status,
            AmountMinorUnits: request.AmountMinorUnits,
            OrderId: request.OrderId);
    }

    private async Task<T?> SendAsync<T>(HttpMethod method, string path, object? body, string? shopId, CancellationToken ct)
        where T : class
    {
        var (login, password) = _options.ResolveShop(shopId);
        using var message = new HttpRequestMessage(method, path.TrimStart('/'));
        message.Headers.Add("Idempotence-Key", Guid.NewGuid().ToString("D"));
        var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{login}:{password}"));
        message.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);

        if (body is not null)
            message.Content = JsonContent.Create(body, options: JsonOptions);

        using var response = await _http.SendAsync(message, ct).ConfigureAwait(false);
        var raw = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new YooMoneyApiException(response.StatusCode, raw);

        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return JsonSerializer.Deserialize<T>(raw, JsonOptions);
    }

    private static string FormatAmount(long amountMinorUnits) =>
        (amountMinorUnits / 100.0m).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

    private static long? ParseAmountMinor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!decimal.TryParse(value, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var rub))
            return null;
        return (long)Math.Round(rub * 100m, MidpointRounding.AwayFromZero);
    }

    private static Guid ParsePrefixedId(string? raw, string prefix)
    {
        if (string.IsNullOrWhiteSpace(raw)) return Guid.Empty;
        var s = raw.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? raw[prefix.Length..]
            : raw;
        return Guid.TryParse(s, out var id) ? id : Guid.Empty;
    }

    private sealed class YooDealApiResponse
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
    }

    private sealed class YooDealStatusApiResponse
    {
        public YooAmountDto? Balance { get; set; }
        public YooAmountDto? PayoutBalance { get; set; }
    }

    private sealed class YooAmountDto
    {
        public string? Value { get; set; }
        public string? Currency { get; set; }
    }

    private sealed class YooPaymentApiResponse
    {
        public Guid? Id { get; set; }
        public string? Status { get; set; }
        public bool Paid { get; set; }
        public YooConfirmationDto? Confirmation { get; set; }
        public YooPaymentMethodDto? PaymentMethod { get; set; }
    }

    private sealed class YooConfirmationDto
    {
        public string? ConfirmationUrl { get; set; }
    }

    private sealed class YooPaymentMethodDto
    {
        public string? Type { get; set; }
    }

    private sealed class YooPayoutApiResponse
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
    }
}

public sealed record YooCreateDealRequest(
    string? Description = null,
    string? FeeMoment = null,
    Dictionary<string, object?>? Metadata = null,
    string? ShopId = null);

public sealed record YooCreatePaymentRequest(
    long AmountMinorUnits,
    long PayoutAmountMinorUnits,
    Guid DealId,
    string ReturnUrl,
    string? Description = null,
    Dictionary<string, object?>? Metadata = null,
    bool Capture = true,
    string? ShopId = null);

public sealed record YooCreatePayoutRequest(
    long AmountMinorUnits,
    Guid DealId,
    string PayoutToken,
    string OrderId,
    string? Description = null,
    Dictionary<string, object?>? Metadata = null,
    string? ShopId = null);

public sealed record YooDealCreateResult(bool Success, Guid ExternalDealId, string? RawId, string? Status);
public sealed record YooDealStatus(long? BalanceMinorUnits, long? PayoutBalanceMinorUnits, string? BalanceValue, string? PayoutBalanceValue);
public sealed record YooPaymentCreateResult(bool Success, Guid? PaymentId, string? ConfirmationUrl, string? Status, string ShopId);
public sealed record YooPaymentStatus(Guid? PaymentId, string? Status, bool Paid, string? PaymentMethodType);
public sealed record YooPayoutCreateResult(bool Success, Guid ExternalPayoutId, string? RawId, string? Status, long AmountMinorUnits, string OrderId);

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenMoneyYooMoney(this IServiceCollection services, Action<YooMoneyOptions> configure)
    {
        services.AddOptions<YooMoneyOptions>().Configure(configure);
        services.AddHttpClient<IYooMoneyClient, YooMoneyClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<YooMoneyOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        });
        return services;
    }

    public static IServiceCollection AddOpenMoneyYooMoney(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);
        services.Configure<YooMoneyOptions>(section);
        services.AddHttpClient<IYooMoneyClient, YooMoneyClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<YooMoneyOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        });
        return services;
    }
}
