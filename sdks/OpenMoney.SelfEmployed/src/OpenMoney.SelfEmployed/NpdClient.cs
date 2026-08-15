using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using OpenMoney.SelfEmployed.Models;

namespace OpenMoney.SelfEmployed;

/// <summary>Client for T-Bank Business self-employed recipient and payment registry APIs.</summary>
public sealed class NpdClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly TBankNpdOptions _options;
    private readonly INpdRecipientStore _store;
    private readonly Lazy<HttpClient> _securedHttpClient;
    private bool _disposed;

    /// <summary>Creates an NPD client.</summary>
    public NpdClient(
        HttpClient httpClient,
        IOptions<TBankNpdOptions> options,
        INpdRecipientStore store)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _store = store;
        _securedHttpClient = new Lazy<HttpClient>(CreateSecuredHttpClient);
    }

    /// <summary>Lists one page of self-employed recipients.</summary>
    public Task<RecipientsResponse> ListRecipientsAsync(
        RecipientsListRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<RecipientsListRequest, RecipientsResponse>(
            _httpClient,
            HttpMethod.Post,
            BuildUrl(_options.ApiBaseUrl, "self-employed/recipients/list"),
            request,
            cancellationToken);

    /// <summary>Lists every recipient page and persists each page through the host store.</summary>
    /// <returns>The total number of recipient records processed.</returns>
    public async Task<int> CheckNpdAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured(requireCertificate: false);
        if (_options.PageSize is < 1 or > 100)
        {
            throw new InvalidOperationException("TBankNpd:PageSize must be between 1 and 100.");
        }

        var processed = 0;
        for (var offset = 0; ; offset += _options.PageSize)
        {
            var page = await ListRecipientsAsync(
                new RecipientsListRequest { Offset = offset, Limit = _options.PageSize },
                cancellationToken).ConfigureAwait(false);

            if (page.Recipients.Count == 0)
            {
                break;
            }

            await _store.UpsertRecipientsAsync(page.Recipients, cancellationToken).ConfigureAwait(false);
            processed += page.Recipients.Count;

            if (page.Recipients.Count < _options.PageSize)
            {
                break;
            }

            if (_options.PageDelay > TimeSpan.Zero)
            {
                await Task.Delay(_options.PageDelay, cancellationToken).ConfigureAwait(false);
            }
        }

        return processed;
    }

    /// <summary>Starts asynchronous creation of recipients from requisites.</summary>
    public Task<CorrelationResponse> AddRecipientsByRequisitesAsync(
        AddRecipientsByRequisitesRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<AddRecipientsByRequisitesRequest, CorrelationResponse>(
            _httpClient,
            HttpMethod.Post,
            BuildUrl(_options.ApiBaseUrl, "self-employed/recipients/add/by-requisites"),
            request,
            cancellationToken);

    /// <summary>Gets the result of an add-by-requisites operation.</summary>
    public Task<AddRecipientsResult> GetAddResultAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default) =>
        SendAsync<object, AddRecipientsResult>(
            _httpClient,
            HttpMethod.Get,
            BuildUrl(
                _options.ApiBaseUrl,
                $"self-employed/recipients/add/by-requisites/result?correlationId={Uri.EscapeDataString(correlationId.ToString())}"),
            body: null,
            cancellationToken);

    /// <summary>Starts asynchronous creation of a payment registry draft.</summary>
    public Task<CorrelationResponse> CreatePaymentRegistryAsync(
        CreatePaymentRegistryRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<CreatePaymentRegistryRequest, CorrelationResponse>(
            _httpClient,
            HttpMethod.Post,
            BuildUrl(_options.ApiBaseUrl, "self-employed/payment-registry/create"),
            request,
            cancellationToken);

    /// <summary>Gets the result of payment registry draft creation.</summary>
    public Task<PaymentRegistryCreateResult> GetPaymentRegistryCreateResultAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default) =>
        SendAsync<object, PaymentRegistryCreateResult>(
            _httpClient,
            HttpMethod.Get,
            BuildUrl(
                _options.ApiBaseUrl,
                $"salary/payment-registry/create/result?correlationId={Uri.EscapeDataString(correlationId.ToString())}"),
            body: null,
            cancellationToken);

    /// <summary>Submits a payment registry over mTLS.</summary>
    public Task<CorrelationResponse> SubmitPaymentRegistryAsync(
        PaymentRegistryRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<PaymentRegistryRequest, CorrelationResponse>(
            SecuredClient,
            HttpMethod.Post,
            BuildUrl(_options.SecuredApiBaseUrl, "self-employed/payment-registry/submit"),
            request,
            cancellationToken);

    /// <summary>Gets the result of payment registry submission over mTLS.</summary>
    public Task<PaymentRegistrySubmitResult> GetSubmitResultAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default) =>
        SendAsync<object, PaymentRegistrySubmitResult>(
            SecuredClient,
            HttpMethod.Get,
            BuildUrl(
                _options.SecuredApiBaseUrl,
                $"self-employed/payment-registry/submit/result?correlationId={Uri.EscapeDataString(correlationId.ToString())}"),
            body: null,
            cancellationToken);

    /// <summary>Starts payment of a submitted registry over mTLS.</summary>
    public Task<CorrelationResponse> PayPaymentRegistryAsync(
        PaymentRegistryRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<PaymentRegistryRequest, CorrelationResponse>(
            SecuredClient,
            HttpMethod.Post,
            BuildUrl(_options.SecuredApiBaseUrl, "self-employed/payment-registry/pay"),
            request,
            cancellationToken);

    /// <summary>Gets payment execution results over mTLS.</summary>
    public Task<PaymentRegistryPayResult> GetPayResultAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default) =>
        SendAsync<CorrelationRequest, PaymentRegistryPayResult>(
            SecuredClient,
            HttpMethod.Post,
            BuildUrl(_options.SecuredApiBaseUrl, "self-employed/payment-registry/pay/result"),
            new CorrelationRequest { CorrelationId = correlationId },
            cancellationToken);

    /// <summary>
    /// Lists payment registry ids for an organization over a date range.
    /// Tries <c>salary/payment-registry/list</c> then <c>self-employed/payment-registry/list</c>
    /// (same fallback order as production receipt sync).
    /// </summary>
    public async Task<IReadOnlyList<long>> ListPaymentRegistryIdsAsync(
        DateOnly fromDate,
        DateOnly toDate,
        int page = 0,
        int pageSize = 200,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured(requireCertificate: false);
        var body = new PaymentRegistryListRequest
        {
            From = fromDate.ToString("yyyy-MM-dd"),
            To = toDate.ToString("yyyy-MM-dd"),
            Page = page,
            PageSize = pageSize
        };

        foreach (var relative in new[]
                 {
                     "salary/payment-registry/list",
                     "self-employed/payment-registry/list"
                 })
        {
            try
            {
                var json = await SendRawAsync(
                    _httpClient,
                    HttpMethod.Post,
                    BuildUrl(_options.ApiBaseUrl, relative),
                    body,
                    cancellationToken).ConfigureAwait(false);
                var node = JsonNode.Parse(json);
                var ids = NpdJsonTraversal.ExtractRegistryIds(node);
                if (ids.Count > 0)
                {
                    return ids;
                }
            }
            catch (NpdApiException)
            {
                // try next endpoint
            }
        }

        return Array.Empty<long>();
    }

    /// <summary>Starts asynchronous receipt collection for a payment registry (mTLS).</summary>
    public async Task<string> RequestRegistryReceiptsAsync(
        long paymentRegistryId,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var request = new PaymentRegistryReceiptsRequest
        {
            CorrelationId = correlationId ?? Guid.NewGuid().ToString(),
            PaymentRegistryId = paymentRegistryId
        };
        var json = await SendRawAsync(
            SecuredClient,
            HttpMethod.Post,
            BuildUrl(_options.SecuredApiBaseUrl, "self-employed/payment-registry/receipts"),
            request,
            cancellationToken).ConfigureAwait(false);
        try
        {
            var node = JsonNode.Parse(json);
            return node?["correlationId"]?.GetValue<string>() ?? request.CorrelationId;
        }
        catch (JsonException)
        {
            return request.CorrelationId;
        }
    }

    /// <summary>Polls receipts/result once and returns parsed receipt candidates (mTLS).</summary>
    public async Task<IReadOnlyList<SelfEmployedReceiptCandidate>> GetRegistryReceiptsResultAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var json = await SendRawAsync<object>(
            SecuredClient,
            HttpMethod.Get,
            BuildUrl(
                _options.SecuredApiBaseUrl,
                $"self-employed/payment-registry/receipts/result?correlationId={Uri.EscapeDataString(correlationId)}"),
            body: null,
            cancellationToken).ConfigureAwait(false);
        var node = JsonNode.Parse(json);
        return NpdJsonTraversal.CollectReceiptCandidates(node);
    }

    /// <summary>
    /// End-to-end sync used by production <c>TinkoffReceiptSyncHostedService</c>:
    /// list registries → request receipts → poll → download → persist via <see cref="INpdReceiptStore"/>.
    /// </summary>
    public async Task<int> SyncRegistryReceiptsAsync(
        INpdReceiptStore receiptStore,
        HttpClient? downloadClient = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(receiptStore);
        EnsureConfigured(requireCertificate: true);

        var lookback = _options.ReceiptsLookbackDays > 0 ? _options.ReceiptsLookbackDays : 30;
        var fromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-lookback));
        var toDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var registryIds = await ListPaymentRegistryIdsAsync(fromDate, toDate, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (registryIds.Count == 0)
        {
            return 0;
        }

        var candidates = new List<SelfEmployedReceiptCandidate>();
        var attempts = Math.Max(1, _options.ReceiptPollAttempts);
        var delay = _options.ReceiptPollDelay < TimeSpan.Zero ? TimeSpan.Zero : _options.ReceiptPollDelay;

        foreach (var registryId in registryIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string correlationId;
            try
            {
                correlationId = await RequestRegistryReceiptsAsync(registryId, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (NpdApiException)
            {
                continue;
            }

            for (var attempt = 0; attempt < attempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var page = await GetRegistryReceiptsResultAsync(correlationId, cancellationToken)
                        .ConfigureAwait(false);
                    candidates.AddRange(page);
                }
                catch (NpdApiException)
                {
                    // keep polling
                }

                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        if (candidates.Count == 0)
        {
            return 0;
        }

        using var http = downloadClient ?? new HttpClient();
        var ownsHttp = downloadClient is null;
        var saved = 0;
        try
        {
            foreach (var item in candidates)
            {
                if (string.IsNullOrWhiteSpace(item.OperationId))
                {
                    continue;
                }

                if (await receiptStore.ExistsAsync(item.OperationId, cancellationToken).ConfigureAwait(false))
                {
                    continue;
                }

                byte[]? bytes = null;
                string? contentType = null;
                if (!string.IsNullOrWhiteSpace(item.ReceiptUrl))
                {
                    try
                    {
                        using var fileResp = await http.GetAsync(item.ReceiptUrl, cancellationToken)
                            .ConfigureAwait(false);
                        if (fileResp.IsSuccessStatusCode)
                        {
                            bytes = await fileResp.Content.ReadAsByteArrayAsync(cancellationToken)
                                .ConfigureAwait(false);
                            contentType = fileResp.Content.Headers.ContentType?.MediaType;
                        }
                    }
                    catch (HttpRequestException)
                    {
                        // host can retry later
                    }
                }

                await receiptStore.SaveAsync(
                    new SelfEmployedReceiptRecord
                    {
                        ExternalOperationId = item.OperationId,
                        PaymentRegistryId = item.PaymentRegistryId,
                        ReceiptUrl = item.ReceiptUrl,
                        Content = bytes,
                        ContentType = contentType,
                        SuggestedExtension = NpdJsonTraversal.ResolveExtension(contentType, item.ReceiptUrl),
                        Raw = item.Raw
                    },
                    cancellationToken).ConfigureAwait(false);
                saved++;
            }
        }
        finally
        {
            if (ownsHttp)
            {
                http.Dispose();
            }
        }

        return saved;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_securedHttpClient.IsValueCreated)
        {
            _securedHttpClient.Value.Dispose();
        }

        _disposed = true;
    }

    private HttpClient SecuredClient
    {
        get
        {
            EnsureConfigured(requireCertificate: true);
            return _securedHttpClient.Value;
        }
    }

    private async Task<TResponse> SendAsync<TRequest, TResponse>(
        HttpClient client,
        HttpMethod method,
        string url,
        TRequest? body,
        CancellationToken cancellationToken)
    {
        var content = await SendRawAsync(client, method, url, body, cancellationToken).ConfigureAwait(false);
        try
        {
            return JsonSerializer.Deserialize<TResponse>(content, JsonDefaults.Options)
                ?? throw new NpdApiException(HttpStatusCode.OK, "T-Bank returned an empty JSON response.");
        }
        catch (JsonException exception)
        {
            throw new NpdApiException(HttpStatusCode.OK, "T-Bank returned invalid JSON.", exception);
        }
    }

    private async Task<string> SendRawAsync<TRequest>(
        HttpClient client,
        HttpMethod method,
        string url,
        TRequest? body,
        CancellationToken cancellationToken)
    {
        EnsureConfigured(requireCertificate: false);
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonDefaults.Options);
        }

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new NpdApiException(response.StatusCode, content);
        }

        return content;
    }

    private HttpClient CreateSecuredHttpClient()
    {
        var certPem = ResolvePem(
            _options.ClientCertificatePem,
            _options.ClientCertificatePemPath,
            "client certificate");
        var keyPem = ResolvePem(
            _options.ClientPrivateKeyPem,
            _options.ClientPrivateKeyPemPath,
            "client private key");

        using var certificateWithoutKey = X509Certificate2.CreateFromPem(certPem);
        using var privateKey = ImportPrivateKey(keyPem, _options.ClientPrivateKeyPassword);
        using var certificateWithKey = privateKey switch
        {
            RSA rsa => certificateWithoutKey.CopyWithPrivateKey(rsa),
            ECDsa ecdsa => certificateWithoutKey.CopyWithPrivateKey(ecdsa),
            _ => throw new NotSupportedException("Only RSA and ECDSA client keys are supported.")
        };

        var pfx = certificateWithKey.Export(X509ContentType.Pkcs12);
        var clientCertificate = new X509Certificate2(
            pfx,
            (string?)null,
            X509KeyStorageFlags.EphemeralKeySet);
        var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        };
        handler.ClientCertificates.Add(clientCertificate);
        return new HttpClient(handler, disposeHandler: true);
    }

    private static string ResolvePem(string? inlinePem, string? path, string description)
    {
        if (!string.IsNullOrWhiteSpace(inlinePem)
            && !inlinePem.StartsWith("YOUR_", StringComparison.Ordinal))
        {
            return inlinePem;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException(
                $"Secured NPD operations require {description} via PEM content or file path.");
        }

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"T-Bank {description} file was not found.", fullPath);
        }

        return File.ReadAllText(fullPath);
    }

    private static AsymmetricAlgorithm ImportPrivateKey(string pem, string? password)
    {
        try
        {
            var rsa = RSA.Create();
            ImportPem(rsa, pem, password);
            return rsa;
        }
        catch (CryptographicException)
        {
            var ecdsa = ECDsa.Create();
            try
            {
                ImportPem(ecdsa, pem, password);
                return ecdsa;
            }
            catch
            {
                ecdsa.Dispose();
                throw;
            }
        }
    }

    private static void ImportPem(AsymmetricAlgorithm algorithm, string pem, string? password)
    {
        var encrypted = pem.Contains("BEGIN ENCRYPTED PRIVATE KEY", StringComparison.OrdinalIgnoreCase)
            || pem.Contains("Proc-Type: 4,ENCRYPTED", StringComparison.OrdinalIgnoreCase);
        if (encrypted && string.IsNullOrEmpty(password))
        {
            throw new CryptographicException("The client private key is encrypted but no password was configured.");
        }

        if (algorithm is RSA rsa)
        {
            if (encrypted)
            {
                rsa.ImportFromEncryptedPem(pem, password);
            }
            else
            {
                rsa.ImportFromPem(pem);
            }
        }
        else if (algorithm is ECDsa ecdsa)
        {
            if (encrypted)
            {
                ecdsa.ImportFromEncryptedPem(pem, password);
            }
            else
            {
                ecdsa.ImportFromPem(pem);
            }
        }
    }

    private void EnsureConfigured(bool requireCertificate)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(_options.Token)
            || _options.Token.StartsWith("YOUR_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Configure TBankNpd:Token using a secret provider.");
        }

        if (!Uri.TryCreate(_options.ApiBaseUrl, UriKind.Absolute, out _)
            || !Uri.TryCreate(_options.SecuredApiBaseUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("T-Bank NPD base URLs must be absolute URLs.");
        }

        if (requireCertificate)
        {
            var hasInline = !string.IsNullOrWhiteSpace(_options.ClientCertificatePem)
                && !string.IsNullOrWhiteSpace(_options.ClientPrivateKeyPem)
                && !_options.ClientCertificatePem.StartsWith("YOUR_", StringComparison.Ordinal);
            var hasPaths = !string.IsNullOrWhiteSpace(_options.ClientCertificatePemPath)
                && !string.IsNullOrWhiteSpace(_options.ClientPrivateKeyPemPath);
            if (!hasInline && !hasPaths)
            {
                throw new InvalidOperationException(
                    "Secured NPD operations require client certificate and private key (paths or PEM content).");
            }
        }
    }

    private static string BuildUrl(string baseUrl, string relativePath) =>
        $"{baseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
}

/// <summary>Represents a non-success response from a T-Bank NPD endpoint.</summary>
public sealed class NpdApiException : HttpRequestException
{
    /// <summary>Creates an API exception.</summary>
    public NpdApiException(HttpStatusCode statusCode, string responseBody, Exception? innerException = null)
        : base($"T-Bank NPD request failed with HTTP {(int)statusCode}.", innerException, statusCode)
    {
        ResponseBody = responseBody;
    }

    /// <summary>Raw response body returned by the API.</summary>
    public string ResponseBody { get; }
}
