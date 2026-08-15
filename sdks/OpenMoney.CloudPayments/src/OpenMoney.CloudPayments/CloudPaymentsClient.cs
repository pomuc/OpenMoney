using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace OpenMoney.CloudPayments;

public sealed class CloudPaymentsOptions
{
    public const string SectionName = "CloudPayments";
    public string BaseUrl { get; set; } = "https://api.cloudpayments.ru/";
    public string PublicId { get; set; } = "";
    public string ApiSecret { get; set; } = "";
    public string? Inn { get; set; }
    public string CalculationPlace { get; set; } = "";

    internal void Validate()
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _)) throw new OptionsValidationException(nameof(CloudPaymentsOptions), typeof(CloudPaymentsOptions), ["BaseUrl must be absolute."]);
        if (string.IsNullOrWhiteSpace(PublicId) || string.IsNullOrWhiteSpace(ApiSecret))
            throw new OptionsValidationException(nameof(CloudPaymentsOptions), typeof(CloudPaymentsOptions), ["PublicId and ApiSecret are required."]);
    }
}

public sealed class CloudPaymentsApiException : HttpRequestException
{
    public HttpStatusCode StatusCodeValue { get; }
    public string? ResponseBody { get; }
    public CloudPaymentsApiException(HttpStatusCode status, string? body)
        : base($"CloudPayments returned HTTP {(int)status} ({status}).", null, status)
    { StatusCodeValue = status; ResponseBody = body; }
}

public sealed class CloudPaymentsClient
{
    private readonly HttpClient _http;
    private readonly CloudPaymentsOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public CloudPaymentsClient(HttpClient http, IOptions<CloudPaymentsOptions> options)
    {
        _http = http;
        _options = options.Value;
        _options.Validate();
    }

    public Task<CloudPaymentsResponse<PaymentTransaction>> ChargeCryptogramAsync(CardPaymentRequest request, CancellationToken ct = default) =>
        PostFormAsync<PaymentTransaction>("/payments/cards/charge", PaymentForm(request), ct);

    public Task<CloudPaymentsResponse<PaymentTransaction>> AuthorizeCryptogramAsync(CardPaymentRequest request, CancellationToken ct = default) =>
        PostFormAsync<PaymentTransaction>("/payments/cards/auth", PaymentForm(request), ct);

    public Task<CloudPaymentsResponse<PaymentTransaction>> ConfirmAsync(long transactionId, decimal amount, CancellationToken ct = default) =>
        PostFormAsync<PaymentTransaction>("/payments/confirm",
            new Dictionary<string, string> { ["TransactionId"] = transactionId.ToString(CultureInfo.InvariantCulture), ["Amount"] = Money(amount) }, ct);

    public Task<CloudPaymentsResponse<PaymentTransaction>> RefundAsync(long transactionId, decimal amount, CancellationToken ct = default) =>
        PostFormAsync<PaymentTransaction>("/payments/refund",
            new Dictionary<string, string> { ["TransactionId"] = transactionId.ToString(CultureInfo.InvariantCulture), ["Amount"] = Money(amount) }, ct);

    public Task<CloudPaymentsResponse<PaymentTransaction>> VoidAsync(long transactionId, CancellationToken ct = default) =>
        PostFormAsync<PaymentTransaction>("/payments/void",
            new Dictionary<string, string> { ["TransactionId"] = transactionId.ToString(CultureInfo.InvariantCulture) }, ct);

    public async Task<CloudPaymentsResponse<ReceiptResult>> IssueReceiptAsync(ReceiptRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateReceipt(request);
        using var response = await _http.PostAsJsonAsync("/kkt/receipt", request, JsonOptions, ct).ConfigureAwait(false);
        return await ReadResponseAsync<ReceiptResult>(response, ct).ConfigureAwait(false);
    }

    public Task<CloudPaymentsResponse<ReceiptResult>> IssueCommissionReceiptAsync(
        string invoiceId, string accountId, long amountMinorUnits, string label,
        ReceiptType type = ReceiptType.Income, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Inn)) throw new InvalidOperationException("CloudPaymentsOptions.Inn is required for receipts.");
        var receipt = new ReceiptRequest
        {
            Inn = new Inn(_options.Inn),
            TaxationSystem = TaxationSystem.SimplifiedIncome,
            Type = type,
            CustomerReceipt = new CustomerReceipt
            {
                TaxationSystem = TaxationSystem.SimplifiedIncome,
                CalculationPlace = _options.CalculationPlace,
                Items = [new ReceiptItem(label, amountMinorUnits / 100m, 1)]
            },
            InvoiceId = invoiceId,
            AccountId = accountId
        };
        return IssueReceiptAsync(receipt, ct);
    }

    private static Dictionary<string, string> PaymentForm(CardPaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Amount <= 0) throw new ArgumentOutOfRangeException(nameof(request.Amount));
        if (string.IsNullOrWhiteSpace(request.CardCryptogramPacket)) throw new ArgumentException("CardCryptogramPacket is required.");
        var form = new Dictionary<string, string>
        {
            ["Amount"] = Money(request.Amount),
            ["Currency"] = request.Currency,
            ["InvoiceId"] = request.InvoiceId,
            ["AccountId"] = request.AccountId,
            ["CardCryptogramPacket"] = request.CardCryptogramPacket
        };
        Add(form, "Email", request.Email);
        Add(form, "Description", request.Description);
        Add(form, "IpAddress", request.IpAddress);
        Add(form, "Name", request.Name);
        if (request.JsonData is not null) form["JsonData"] = JsonSerializer.Serialize(request.JsonData, JsonOptions);
        return form;
    }

    private async Task<CloudPaymentsResponse<T>> PostFormAsync<T>(string path, IReadOnlyDictionary<string, string> values, CancellationToken ct)
    {
        using var content = new FormUrlEncodedContent(values);
        using var response = await _http.PostAsync(path, content, ct).ConfigureAwait(false);
        return await ReadResponseAsync<T>(response, ct).ConfigureAwait(false);
    }

    private static async Task<CloudPaymentsResponse<T>> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) throw new CloudPaymentsApiException(response.StatusCode, body);
            return JsonSerializer.Deserialize<CloudPaymentsResponse<T>>(body, JsonOptions)
                ?? throw new JsonException("CloudPayments response was empty or invalid.");
        }
    }

    private static void ValidateReceipt(ReceiptRequest request)
    {
        if (request.CustomerReceipt.Items.Count == 0) throw new ArgumentException("At least one receipt item is required.");
        if (request.CustomerReceipt.Items.Any(x => x.Price <= 0 || x.Quantity <= 0)) throw new ArgumentException("Receipt item price and quantity must be positive.");
    }
    private static void Add(IDictionary<string, string> values, string key, string? value) { if (!string.IsNullOrWhiteSpace(value)) values[key] = value; }
    private static string Money(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);
}

public sealed record CardPaymentRequest(
    decimal Amount, string Currency, string InvoiceId, string AccountId, string CardCryptogramPacket,
    string? Email = null, string? Description = null, string? IpAddress = null, string? Name = null,
    IReadOnlyDictionary<string, object?>? JsonData = null);

public sealed class CloudPaymentsResponse<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Model { get; init; }
}
public sealed class PaymentTransaction
{
    public long TransactionId { get; init; }
    public decimal Amount { get; init; }
    public string? Currency { get; init; }
    public string? CurrencyCode { get; init; }
    public string? InvoiceId { get; init; }
    public string? AccountId { get; init; }
    public string? Status { get; init; }
    public string? CardHolderMessage { get; init; }
    public string? PaReq { get; init; }
    public string? AcsUrl { get; init; }
}

[JsonConverter(typeof(InnJsonConverter))]
public sealed record Inn
{
    public string Value { get; }
    public Inn(string? value)
    {
        if (!Regex.IsMatch(value ?? "", @"^(?:\d{10}|\d{12})$", RegexOptions.CultureInvariant))
            throw new ArgumentException("INN must contain 10 or 12 digits.", nameof(value));
        Value = value!;
    }
    public override string ToString() => Value;
}
public sealed class InnJsonConverter : JsonConverter<Inn>
{
    public override Inn Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => new(reader.GetString() ?? "");
    public override void Write(Utf8JsonWriter writer, Inn value, JsonSerializerOptions options) => writer.WriteStringValue(value.Value);
}
public sealed class ReceiptRequest
{
    public required TaxationSystem TaxationSystem { get; init; }
    public required Inn Inn { get; init; }
    public required ReceiptType Type { get; init; }
    public required CustomerReceipt CustomerReceipt { get; init; }
    public required string InvoiceId { get; init; }
    public required string AccountId { get; init; }
}
public sealed class CustomerReceipt
{
    public required TaxationSystem TaxationSystem { get; init; }
    public string CalculationPlace { get; init; } = "";
    public IReadOnlyList<ReceiptItem> Items { get; init; } = [];
    public ReceiptAmounts? Amounts { get; init; }
}
public sealed class ReceiptItem
{
    public ReceiptItem(string label, decimal price, decimal quantity)
    { Label = label; Price = price; Quantity = quantity; Amount = price * quantity; }
    public string Label { get; init; }
    public string Object { get; init; } = "4";
    public CalculationMethod Method { get; init; } = CalculationMethod.FullPay;
    public decimal Price { get; init; }
    public decimal Quantity { get; init; }
    public decimal Amount { get; init; }
    public int? Vat { get; init; }
    public int? AgentSign { get; init; }
    public SupplierInfo? PurveyorData { get; init; }
}
public sealed record SupplierInfo(string Phone, string Name, string Inn);
public sealed record ReceiptAmounts(decimal Electronic);
public sealed class ReceiptResult
{
    public string? Id { get; init; }
    public int ErrorCode { get; init; }
    public string? ReceiptLocalUrl { get; init; }
}
public enum ReceiptType { Income, IncomeReturn, Expense, ExpenseReturn }
public enum TaxationSystem { Common, SimplifiedIncome, SimplifiedIncomeMinusExpenses, SingleIncomeTax, SingleAgriculturalTax, PatentTaxationSystem }
public enum CalculationMethod { Unknown, FullPrepayment, PartialPrepayment, AdvancePay, FullPay, PartialPayAndCredit, Credit, CreditPayment }

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenMoneyCloudPayments(this IServiceCollection services, Action<CloudPaymentsOptions> configure)
    {
        services.AddOptions<CloudPaymentsOptions>().Configure(configure);
        services.AddHttpClient<CloudPaymentsClient>((sp, client) =>
        {
            var o = sp.GetRequiredService<IOptions<CloudPaymentsOptions>>().Value;
            client.BaseAddress = new Uri(o.BaseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{o.PublicId}:{o.ApiSecret}")));
        });
        return services;
    }
}
