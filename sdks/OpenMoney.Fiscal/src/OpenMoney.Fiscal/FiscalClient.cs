using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace OpenMoney.Fiscal;

public sealed class FiscalOptions
{
    public const string SectionName = "Fiscal";
    public string FnsBaseUrl { get; set; } = "https://lknpd.nalog.ru/";
    public string FnsStatusBaseUrl { get; set; } = "https://statusnpd.nalog.ru/";
    public string? LegalEntityInn { get; set; }
    public string? LegalEntityName { get; set; }
    public string AppVersion { get; set; } = "1.0.0";
}

public sealed class FiscalApiException : HttpRequestException
{
    public HttpStatusCode StatusCodeValue { get; }
    public string? ResponseBody { get; }
    public FiscalApiException(HttpStatusCode status, string? body)
        : base($"FNS returned HTTP {(int)status} ({status}).", null, status)
    { StatusCodeValue = status; ResponseBody = body; }
}

public sealed class FnsClient
{
    private readonly HttpClient _http;
    private readonly FiscalOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public FnsClient(HttpClient http, IOptions<FiscalOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<FnsChallenge> StartSmsChallengeAsync(string phone, string userAgent, CancellationToken ct = default)
    {
        using var request = JsonRequest(HttpMethod.Post, "api/v2/auth/challenge/sms/start", new { phone, requireTpToBeActive = true });
        request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
        return await SendAsync<FnsChallenge>(request, ct).ConfigureAwait(false);
    }

    public async Task<FnsTokens> VerifySmsChallengeAsync(string phone, string code, string challengeToken, FnsDevice device, CancellationToken ct = default)
    {
        using var request = JsonRequest(HttpMethod.Post, "api/v1/auth/challenge/sms/verify", new
        {
            phone, code, challengeToken, deviceInfo = DeviceInfo(device)
        });
        request.Headers.TryAddWithoutValidation("User-Agent", device.UserAgent);
        request.Headers.Referrer = new Uri(_options.FnsBaseUrl.TrimEnd('/') + "/auth/login");
        return await SendAsync<FnsTokens>(request, ct).ConfigureAwait(false);
    }

    public async Task<FnsTokens> RefreshTokenAsync(FnsTokens tokens, FnsDevice device, CancellationToken ct = default)
    {
        using var request = JsonRequest(HttpMethod.Post, "api/v1/auth/token",
            new { refreshToken = tokens.RefreshToken, deviceInfo = DeviceInfo(device) });
        Authorize(request, tokens.Token);
        request.Headers.TryAddWithoutValidation("User-Agent", device.UserAgent);
        request.Headers.Referrer = new Uri(_options.FnsBaseUrl.TrimEnd('/') + "/auth/login");
        return await SendAsync<FnsTokens>(request, ct).ConfigureAwait(false);
    }

    public async Task<FnsTaxpayerStatus> CheckTaxpayerStatusAsync(string inn, DateOnly? requestDate = null, CancellationToken ct = default)
    {
        if (!Regex.IsMatch(inn ?? "", @"^\d{12}$")) throw new ArgumentException("A 12-digit taxpayer INN is required.", nameof(inn));
        using var request = JsonRequest(HttpMethod.Post,
            new Uri(new Uri(_options.FnsStatusBaseUrl), "api/v1/tracker/taxpayer_status").ToString(),
            new { inn, requestDate = (requestDate ?? DateOnly.FromDateTime(DateTime.UtcNow)).ToString("yyyy-MM-dd") });
        return await SendAsync<FnsTaxpayerStatus>(request, ct).ConfigureAwait(false);
    }

    public Task<FnsAuthenticatedResult<FnsProfile>> GetProfileAsync(FnsTokens tokens, FnsDevice device, CancellationToken ct = default) =>
        SendAuthenticatedAsync<FnsProfile>(HttpMethod.Get, "api/v1/taxpayer", null, tokens, device, ct);

    public async Task<FnsAuthenticatedResult<bool>> GetActiveStatusAsync(FnsTokens tokens, FnsDevice device, CancellationToken ct = default)
    {
        var result = await SendAuthenticatedAsync<FnsUserStatus>(HttpMethod.Get, "api/v1/user", null, tokens, device, ct).ConfigureAwait(false);
        return new FnsAuthenticatedResult<bool>(string.Equals(result.Value.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase), result.Tokens);
    }

    public Task<FnsAuthenticatedResult<FnsReceiptResult>> IssueIncomeAsync(
        FnsIncomeReceipt request, FnsTokens tokens, FnsDevice device, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.AmountMinorUnits <= 0) throw new ArgumentOutOfRangeException(nameof(request.AmountMinorUnits));
        var timestamp = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3)).ToString("yyyy-MM-dd'T'HH:mm:sszzz");
        var payload = new
        {
            operationTime = timestamp,
            requestTime = timestamp,
            services = new[] { new { name = request.Description, amount = request.AmountMinorUnits / 100m, quantity = 1 } },
            totalAmount = request.AmountMinorUnits / 100m,
            client = BuildClient(request.Customer),
            paymentType = request.PaymentType.ToString().ToUpperInvariant(),
            ignoreMaxTotalIncomeRestriction = false
        };
        return SendAuthenticatedAsync<FnsReceiptResult>(HttpMethod.Post, "api/v1/income", payload, tokens, device, ct);
    }

    public Task<FnsAuthenticatedResult<FnsReceiptResult>> CancelIncomeAsync(
        FnsReturnReceipt request, FnsTokens tokens, FnsDevice device, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.AmountMinorUnits <= 0) throw new ArgumentOutOfRangeException(nameof(request.AmountMinorUnits));
        var timestamp = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3)).ToString("yyyy-MM-dd'T'HH:mm:sszzz");
        var payload = new
        {
            approvedReceiptUuid = request.OriginalReceiptUuid,
            operationTime = timestamp,
            requestTime = timestamp,
            totalAmount = request.AmountMinorUnits / 100m,
            partnerCode = (string?)null,
            name = request.Description,
            cancellationInfo = new
            {
                comment = request.Comment,
                operationTime = timestamp,
                registerTime = timestamp,
                taxPeriodId = request.TaxPeriodId
            },
            paymentType = request.PaymentType.ToString().ToUpperInvariant(),
            ignoreMaxTotalIncomeRestriction = false,
            sourceDeviceId = device.DeviceId
        };
        return SendAuthenticatedAsync<FnsReceiptResult>(HttpMethod.Post, "api/v1/income", payload, tokens, device, ct);
    }

    public static string GenerateDeviceId(int length = 21)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return string.Create(length, alphabet, static (span, chars) =>
        {
            Span<byte> bytes = stackalloc byte[span.Length];
            RandomNumberGenerator.Fill(bytes);
            for (var i = 0; i < span.Length; i++) span[i] = chars[bytes[i] % chars.Length];
        });
    }

    private async Task<FnsAuthenticatedResult<T>> SendAuthenticatedAsync<T>(
        HttpMethod method, string path, object? payload, FnsTokens tokens, FnsDevice device, CancellationToken ct)
    {
        using var initial = JsonRequest(method, path, payload);
        Authorize(initial, tokens.Token);
        using var response = await _http.SendAsync(initial, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return new FnsAuthenticatedResult<T>(await ReadAsync<T>(response, ct).ConfigureAwait(false), tokens);

        var refreshed = await RefreshTokenAsync(tokens, device, ct).ConfigureAwait(false);
        using var retry = JsonRequest(method, path, payload);
        Authorize(retry, refreshed.Token);
        using var retryResponse = await _http.SendAsync(retry, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        return new FnsAuthenticatedResult<T>(await ReadAsync<T>(retryResponse, ct).ConfigureAwait(false), refreshed);
    }

    private object BuildClient(FnsReceiptCustomer customer)
    {
        if (customer.Kind == FnsCustomerKind.Individual)
            return new { contactPhone = customer.Phone, displayName = (string?)null, inn = (string?)null, incomeType = "FROM_INDIVIDUAL" };
        var inn = customer.Inn ?? _options.LegalEntityInn;
        var name = customer.Name ?? _options.LegalEntityName;
        if (string.IsNullOrWhiteSpace(inn) || string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Legal entity name and INN must be supplied by the request or FiscalOptions.");
        return new { contactPhone = customer.Phone, displayName = name, inn, incomeType = "FROM_LEGAL_ENTITY" };
    }

    private object DeviceInfo(FnsDevice device) => new
    {
        sourceDeviceId = device.DeviceId, sourceType = "WEB", appVersion = _options.AppVersion,
        metaDetails = new { userAgent = device.UserAgent }
    };
    private static void Authorize(HttpRequestMessage request, string token) => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    private static HttpRequestMessage JsonRequest(HttpMethod method, string path, object? value) =>
        new(method, path) { Content = value is null ? null : JsonContent.Create(value, options: JsonOptions) };
    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken ct)
    {
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        return await ReadAsync<T>(response, ct).ConfigureAwait(false);
    }
    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) throw new FiscalApiException(response.StatusCode, body);
        return JsonSerializer.Deserialize<T>(body, JsonOptions) ?? throw new JsonException("FNS response was empty or invalid.");
    }
}

public sealed record FnsDevice(string DeviceId, string UserAgent);
public sealed class FnsChallenge
{
    public string? Code { get; init; }
    public string? Message { get; init; }
    public JsonElement? AdditionalInfo { get; init; }
    public string? ChallengeToken { get; init; }
}
public sealed class FnsTokens
{
    public required string Token { get; init; }
    public DateTimeOffset? TokenExpireIn { get; init; }
    public required string RefreshToken { get; init; }
    public FnsProfile? Profile { get; init; }
}
public sealed class FnsProfile
{
    public long? Id { get; init; }
    public string? LastName { get; init; }
    public string? MiddleName { get; init; }
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
    public string? Inn { get; init; }
    public string? Snils { get; init; }
    public string? Status { get; init; }
    public DateTimeOffset? RegistrationDate { get; init; }
}
public sealed class FnsUserStatus { public string? Status { get; init; } }
public sealed class FnsTaxpayerStatus { public bool Status { get; init; } public string? Message { get; init; } }
public sealed class FnsReceiptResult { public string? ApprovedReceiptUuid { get; init; } }
public sealed record FnsAuthenticatedResult<T>(T Value, FnsTokens Tokens);
public enum FnsCustomerKind { Individual, LegalEntity }
public enum FnsPaymentType { Cash, Card }
public sealed record FnsReceiptCustomer(FnsCustomerKind Kind, string? Inn = null, string? Name = null, string? Phone = null);
public sealed record FnsIncomeReceipt(long AmountMinorUnits, string Description, FnsReceiptCustomer Customer, FnsPaymentType PaymentType = FnsPaymentType.Cash);
public sealed record FnsReturnReceipt(string OriginalReceiptUuid, long TaxPeriodId, long AmountMinorUnits, string Description, string Comment = "Возврат средств", FnsPaymentType PaymentType = FnsPaymentType.Cash);

public enum FiscalReceiptType { Income, IncomeReturn, Expense, ExpenseReturn }
public enum FiscalTaxationSystem { Common, SimplifiedIncome, SimplifiedIncomeMinusExpenses, SingleIncomeTax, SingleAgriculturalTax, Patent }
public enum FiscalCalculationMethod { Unknown, FullPrepayment, PartialPrepayment, AdvancePay, FullPay, PartialPayAndCredit, Credit, CreditPayment }
public sealed record FiscalSupplier(string Phone, string Name, string Inn);
public sealed record FiscalReceiptItem(
    string Label, decimal Price, decimal Quantity = 1, string Object = "4",
    FiscalCalculationMethod Method = FiscalCalculationMethod.FullPay, int? Vat = null, FiscalSupplier? Supplier = null);
public sealed record FiscalReceipt(
    FiscalReceiptType Type, FiscalTaxationSystem TaxationSystem, string Inn, string InvoiceId,
    string AccountId, string CalculationPlace, IReadOnlyList<FiscalReceiptItem> Items);

public static class CloudKassirReceiptFactory
{
    public static object CreatePayload(FiscalReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        if (!Regex.IsMatch(receipt.Inn ?? "", @"^(?:\d{10}|\d{12})$")) throw new ArgumentException("INN must contain 10 or 12 digits.");
        if (receipt.Items.Count == 0) throw new ArgumentException("At least one item is required.");
        return new
        {
            taxationSystem = (int)receipt.TaxationSystem,
            inn = receipt.Inn,
            type = (int)receipt.Type,
            customerReceipt = new
            {
                taxationSystem = (int)receipt.TaxationSystem,
                calculationPlace = receipt.CalculationPlace,
                items = receipt.Items.Select(x => new
                {
                    label = x.Label, @object = x.Object, method = (int)x.Method,
                    price = x.Price, quantity = x.Quantity, amount = x.Price * x.Quantity,
                    vat = x.Vat, agentSign = x.Supplier is null ? (int?)null : 6,
                    purveyorData = x.Supplier is null ? null : new { phone = x.Supplier.Phone, name = x.Supplier.Name, inn = x.Supplier.Inn }
                })
            },
            invoiceId = receipt.InvoiceId,
            accountId = receipt.AccountId
        };
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenMoneyFiscal(this IServiceCollection services, Action<FiscalOptions> configure)
    {
        services.AddOptions<FiscalOptions>().Configure(configure);
        services.AddHttpClient<FnsClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<FiscalOptions>>().Value;
            client.BaseAddress = new Uri(options.FnsBaseUrl.TrimEnd('/') + "/");
        });
        return services;
    }
}
