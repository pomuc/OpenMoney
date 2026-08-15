using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Microsoft.Extensions.Options;
using OpenMoney.TBank.Models;
using OpenMoney.TBank.Signing;

namespace OpenMoney.TBank.Client;

public sealed class TBankAcquiringClient : ITBankAcquiringClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly TBankOptions _options;

    public TBankAcquiringClient(HttpClient httpClient, IOptions<TBankOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<HttpPayInResponseInit> InitPayInAsync(RequestInitPaymentContext request, CancellationToken cancellationToken = default) =>
        PostTokenAsync<RequestInitPaymentContext, HttpPayInResponseInit>(request, "/v2/Init", false, cancellationToken);

    public Task<HttpPayInResponseCharge> ChargeAsync(RequestChargePaymentContext request, CancellationToken cancellationToken = default) =>
        PostTokenAsync<RequestChargePaymentContext, HttpPayInResponseCharge>(request, "/v2/Charge", false, cancellationToken);

    public Task<HttpPayInResponseConfirm> ConfirmAsync(RequestConfirmPaymentContext request, CancellationToken cancellationToken = default) =>
        PostTokenAsync<RequestConfirmPaymentContext, HttpPayInResponseConfirm>(request, "/v2/Confirm", false, cancellationToken);

    public Task<HttpPayInResponseCancel> CancelAsync(RequestCancelPaymentContext request, CancellationToken cancellationToken = default) =>
        PostTokenAsync<RequestCancelPaymentContext, HttpPayInResponseCancel>(request, "/v2/Cancel", false, cancellationToken);

    public Task<HttpPayInResponseStatus> GetStatusAsync(RequestGetStatePaymentContext request, CancellationToken cancellationToken = default) =>
        PostTokenAsync<RequestGetStatePaymentContext, HttpPayInResponseStatus>(request, "/v2/GetState", false, cancellationToken);

    public Task<HttpPayInResponseCheckOrder> CheckOrderAsync(RequestCheckOrderContext request, CancellationToken cancellationToken = default) =>
        PostTokenAsync<RequestCheckOrderContext, HttpPayInResponseCheckOrder>(request, "/v2/CheckOrder", false, cancellationToken);

    public Task<HttpPayOutResponseInit> InitPayoutAsync(RequestInitPayoutContext request, CancellationToken cancellationToken = default) =>
        PostTokenAsync<RequestInitPayoutContext, HttpPayOutResponseInit>(request, "/e2c/v2/Init", true, cancellationToken);

    public Task<HttpPayOutResponsePayment> PaymentAsync(RequestPayoutPaymentContext request, CancellationToken cancellationToken = default) =>
        PostTokenAsync<RequestPayoutPaymentContext, HttpPayOutResponsePayment>(request, "/e2c/v2/Payment", true, cancellationToken);

    public Task<HttpPayOutResponseInit> InitMomentPayoutAsync(RequestInitMomentPayoutContext request, CancellationToken cancellationToken = default) =>
        PostSignedAsync<RequestInitMomentPayoutContext, HttpPayOutResponseInit>(request, "/e2c/v2/Init", cancellationToken);

    public Task<HttpPayOutResponsePayment> MomentPaymentAsync(RequestMomentPayoutPaymentContext request, CancellationToken cancellationToken = default) =>
        PostSignedAsync<RequestMomentPayoutPaymentContext, HttpPayOutResponsePayment>(request, "/e2c/v2/Payment", cancellationToken);

    public Task<HttpPayInResponseAddCard> AddCardAsync(RequestAddCardContext request, CancellationToken cancellationToken = default) =>
        PostTokenAsync<RequestAddCardContext, HttpPayInResponseAddCard>(request, "/v2/AddCard", false, cancellationToken);

    public Task<HttpPayInResponseRemoveCard> RemoveCardAsync(RequestRemoveCardContext request, CancellationToken cancellationToken = default) =>
        PostTokenAsync<RequestRemoveCardContext, HttpPayInResponseRemoveCard>(request, "/v2/RemoveCard", false, cancellationToken);

    public async Task<IReadOnlyList<HttpPayCardResponse>> GetCardListAsync(RequestGetCardListContext request, CancellationToken cancellationToken = default) =>
        await PostTokenAsync<RequestGetCardListContext, List<HttpPayCardResponse>>(request, "/v2/GetCardList", false, cancellationToken).ConfigureAwait(false);

    public Task<HttpPayOutResponseAddCustomer> AddPayoutCustomerAsync(RequestAddPayoutCustomerContext request, CancellationToken cancellationToken = default) =>
        PostSignedAsync<RequestAddPayoutCustomerContext, HttpPayOutResponseAddCustomer>(request, "/e2c/v2/AddCustomer", cancellationToken);

    public Task<HttpPayOutResponseGetCustomer> GetPayoutCustomerAsync(RequestGetPayoutCustomerContext request, CancellationToken cancellationToken = default) =>
        PostSignedAsync<RequestGetPayoutCustomerContext, HttpPayOutResponseGetCustomer>(request, "/e2c/v2/GetCustomer", cancellationToken);

    public Task<HttpPayOutResponseRemoveCustomer> RemovePayoutCustomerAsync(RequestRemovePayoutCustomerContext request, CancellationToken cancellationToken = default) =>
        PostSignedAsync<RequestRemovePayoutCustomerContext, HttpPayOutResponseRemoveCustomer>(request, "/e2c/v2/RemoveCustomer", cancellationToken);

    public Task<HttpPayOutResponseAddCard> AddPayoutCardAsync(RequestAddPayoutCardContext request, CancellationToken cancellationToken = default) =>
        PostSignedAsync<RequestAddPayoutCardContext, HttpPayOutResponseAddCard>(request, "/e2c/v2/AddCard", cancellationToken);

    public async Task<IReadOnlyList<HttpPayOutResponseCard>> GetPayoutCardsAsync(RequestGetPayoutCardsContext request, CancellationToken cancellationToken = default) =>
        await PostSignedAsync<RequestGetPayoutCardsContext, List<HttpPayOutResponseCard>>(request, "/e2c/v2/GetCardList", cancellationToken).ConfigureAwait(false);

    public Task<HttpPayOutResponseRemoveCard> RemovePayoutCardAsync(RequestRemovePayoutCardContext request, CancellationToken cancellationToken = default) =>
        PostSignedAsync<RequestRemovePayoutCardContext, HttpPayOutResponseRemoveCard>(request, "/e2c/v2/RemoveCard", cancellationToken);

    public Task<HttpPayInResponseCreateSecureTransaction> CreateSecureDealAsync(RequestCreateSecureDealContext request, CancellationToken cancellationToken = default) =>
        PostTokenAsync<RequestCreateSecureDealContext, HttpPayInResponseCreateSecureTransaction>(request, "/v2/createSpDeal", false, cancellationToken);

    public Task<HttpGetQr> CreateQrAsync(RequestGetQrContext request, CancellationToken cancellationToken = default) =>
        PostTokenAsync<RequestGetQrContext, HttpGetQr>(request, "/v2/GetQr", false, cancellationToken);

    public Task<HttpResponsePaycheck> MakePaycheckAsync(RequestPaycheckContext request, CancellationToken cancellationToken = default) =>
        PostPaycheckAsync(CreatePaycheck(request, PaycheckType.Income), cancellationToken);

    public Task<HttpResponsePaycheck> MakeReturnPaycheckAsync(RequestPaycheckContext request, CancellationToken cancellationToken = default) =>
        PostPaycheckAsync(CreatePaycheck(request, PaycheckType.IncomeReturn), cancellationToken);

    public Task<HttpResponsePaycheck> MakeAgentPaycheckAsync(RequestAgentPaycheckContext request, CancellationToken cancellationToken = default) =>
        PostPaycheckAsync(CreateAgentPaycheck(request, PaycheckType.Income), cancellationToken);

    public Task<HttpResponsePaycheck> MakeReturnAgentPaycheckAsync(RequestAgentPaycheckContext request, CancellationToken cancellationToken = default) =>
        PostPaycheckAsync(CreateAgentPaycheck(request, PaycheckType.IncomeReturn), cancellationToken);

    private async Task<TResponse> PostTokenAsync<TRequest, TResponse>(
        TRequest request, string path, bool payout, CancellationToken cancellationToken)
        where TRequest : TokenRequest
    {
        ArgumentNullException.ThrowIfNull(request);
        request.TerminalKey = payout ? _options.EffectivePayoutTerminalKey : _options.TerminalKey;
        request.TerminalPassword = payout ? _options.EffectivePayoutTerminalPassword : _options.TerminalPassword;
        ValidateCredentials(request.TerminalKey, request.TerminalPassword);
        request.Token = request.ToTinkoffHashToken(request.TerminalPassword);
        return await PostAsync<TRequest, TResponse>(_options.BaseUrl, path, request, null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TResponse> PostSignedAsync<TRequest, TResponse>(
        TRequest request, string path, CancellationToken cancellationToken)
        where TRequest : SignedRequest
    {
        ArgumentNullException.ThrowIfNull(request);
        request.TerminalKey = _options.MomentPayoutTerminalKey ?? _options.EffectivePayoutTerminalKey;
        if (string.IsNullOrWhiteSpace(request.TerminalKey))
            throw new InvalidOperationException("A payout terminal key is required.");
        if (string.IsNullOrWhiteSpace(_options.SigningCertificatePem) || string.IsNullOrWhiteSpace(_options.SigningPrivateKeyPem))
            throw new InvalidOperationException("SigningCertificatePem and SigningPrivateKeyPem are required for signed E2C operations.");

        SignatureHelper.SignRequest(request, _options.SigningCertificatePem, _options.SigningPrivateKeyPem, _options.SigningPrivateKeyPassword);
        return await PostAsync<TRequest, TResponse>(_options.BaseUrl, path, request, null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<HttpResponsePaycheck> PostPaycheckAsync(HttpRequestPaycheck request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.CloudPaymentsLogin) || string.IsNullOrWhiteSpace(_options.CloudPaymentsPassword))
            throw new InvalidOperationException("CloudPayments credentials are required for paycheck operations.");
        var credential = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.CloudPaymentsLogin}:{_options.CloudPaymentsPassword}"));
        return await PostAsync<HttpRequestPaycheck, HttpResponsePaycheck>(
            _options.CloudPaymentsBaseUrl, "/kkt/receipt", request,
            new AuthenticationHeaderValue("Basic", credential), cancellationToken).ConfigureAwait(false);
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        string baseUrl, string path, TRequest request, AuthenticationHeaderValue? authorization, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, BuildUri(baseUrl, path))
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        message.Headers.Authorization = authorization;
        if (authorization is not null)
            message.Headers.TryAddWithoutValidation("X-Request-ID", Guid.NewGuid().ToString());

        using var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new TBankApiException(response.StatusCode, body);

        return JsonSerializer.Deserialize<TResponse>(body, JsonOptions)
            ?? throw new JsonException("T-Bank returned an empty or invalid JSON response.");
    }

    private HttpRequestPaycheck CreatePaycheck(RequestPaycheckContext request, PaycheckType type)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new HttpRequestPaycheck
        {
            Inn = GetInn(),
            AccountId = request.CustomerKey,
            InvoiceId = request.OrderId,
            Type = type,
            TaxationSystem = TaxationSystem.SimplifiedIncome,
            CustomerReceipt = new CustomerReceipt
            {
                TaxationSystem = TaxationSystem.SimplifiedIncome,
                CalculationPlace = "Online",
                Items = [new CustomerReceiptItem(request.Amount / 100d, 1)]
            }
        };
    }

    private HttpRequestPaycheck CreateAgentPaycheck(RequestAgentPaycheckContext request, PaycheckType type)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = new CustomerReceiptItem(request.Amount / 100d, 1)
        {
            Label = request.Label ?? CustomerReceiptItem.DefaultLabel,
            AgentSign = 6,
            Vat = request.Vat,
            Method = CalculationMethod.FullPay,
            PurveyorData = new SupplierInfo
            {
                Phone = request.CustomerPhone.StartsWith('+') ? request.CustomerPhone : $"+{request.CustomerPhone}",
                Name = request.CustomerName,
                Inn = request.CustomerInn
            }
        };
        return new HttpRequestPaycheck
        {
            Inn = GetInn(),
            AccountId = request.CustomerKey,
            InvoiceId = request.OrderId,
            Type = type,
            TaxationSystem = request.TaxationSystem,
            CustomerReceipt = new CustomerReceipt
            {
                TaxationSystem = request.TaxationSystem,
                CalculationPlace = "Online",
                Items = [item]
            }
        };
    }

    private Inn GetInn() => new(_options.Inn ?? throw new InvalidOperationException("Inn is required for paycheck operations."));

    private static Uri BuildUri(string baseUrl, string path)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
            throw new InvalidOperationException("The configured base URL is invalid.");
        return new Uri(baseUri, path);
    }

    private static void ValidateCredentials(string terminalKey, string terminalPassword)
    {
        if (string.IsNullOrWhiteSpace(terminalKey) || string.IsNullOrWhiteSpace(terminalPassword))
            throw new InvalidOperationException("TerminalKey and TerminalPassword are required.");
    }
}
