using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace OpenMoney.Kyc.MoyNalog;

public sealed class MoyNalogOptions
{
    public const string SectionName = "Kyc:MoyNalog";
    public string FnsBaseUrl { get; set; } = "https://lknpd.nalog.ru/";
    public string FnsStatusBaseUrl { get; set; } = "https://statusnpd.nalog.ru/";
    public string AppVersion { get; set; } = "1.0.0";
}

/// <summary>
/// KYC against FNS «Мой налог»: SMS login, NPD status by INN, taxpayer passport identity.
/// Income/receipt APIs live in <c>OpenMoney.Fiscal</c>.
/// </summary>
public sealed class MoyNalogKycClient
{
    private readonly HttpClient _http;
    private readonly MoyNalogOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public MoyNalogKycClient(HttpClient http, IOptions<MoyNalogOptions> options)
    {
        _http = http;
        _options = options.Value;
        if (!Uri.TryCreate(_options.FnsBaseUrl, UriKind.Absolute, out _))
            throw new OptionsValidationException(nameof(MoyNalogOptions), typeof(MoyNalogOptions), ["FnsBaseUrl must be absolute."]);
    }

    public async Task<MoyNalogChallenge> StartSmsChallengeAsync(string phone, string userAgent, CancellationToken cancellationToken = default)
    {
        using var request = JsonRequest(HttpMethod.Post, "api/v2/auth/challenge/sms/start", new { phone, requireTpToBeActive = true });
        request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
        return await SendAsync<MoyNalogChallenge>(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<MoyNalogTokens> VerifySmsChallengeAsync(
        string phone, string code, string challengeToken, MoyNalogDevice device, CancellationToken cancellationToken = default)
    {
        using var request = JsonRequest(HttpMethod.Post, "api/v1/auth/challenge/sms/verify", new
        {
            phone,
            code,
            challengeToken,
            deviceInfo = DeviceInfo(device)
        });
        request.Headers.TryAddWithoutValidation("User-Agent", device.UserAgent);
        request.Headers.Referrer = new Uri(_options.FnsBaseUrl.TrimEnd('/') + "/auth/login");
        return await SendAsync<MoyNalogTokens>(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<MoyNalogTokens> RefreshTokenAsync(
        MoyNalogTokens tokens, MoyNalogDevice device, CancellationToken cancellationToken = default)
    {
        using var request = JsonRequest(HttpMethod.Post, "api/v1/auth/token",
            new { refreshToken = tokens.RefreshToken, deviceInfo = DeviceInfo(device) });
        Authorize(request, tokens.Token);
        request.Headers.TryAddWithoutValidation("User-Agent", device.UserAgent);
        return await SendAsync<MoyNalogTokens>(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<MoyNalogTaxpayerStatus> CheckTaxpayerStatusAsync(
        string inn, DateOnly? requestDate = null, CancellationToken cancellationToken = default)
    {
        if (!Regex.IsMatch(inn ?? "", @"^\d{12}$"))
            throw new ArgumentException("A 12-digit taxpayer INN is required.", nameof(inn));

        using var request = JsonRequest(HttpMethod.Post,
            new Uri(new Uri(_options.FnsStatusBaseUrl), "api/v1/tracker/taxpayer_status").ToString(),
            new { inn, requestDate = (requestDate ?? DateOnly.FromDateTime(DateTime.UtcNow)).ToString("yyyy-MM-dd") });
        return await SendAsync<MoyNalogTaxpayerStatus>(request, cancellationToken).ConfigureAwait(false);
    }

    public Task<MoyNalogAuthenticatedResult<MoyNalogTaxpayerIdentity>> GetTaxpayerIdentityAsync(
        MoyNalogTokens tokens, MoyNalogDevice device, CancellationToken cancellationToken = default) =>
        SendAuthenticatedAsync<MoyNalogTaxpayerIdentity>(HttpMethod.Get, "api/v1/taxpayer", null, tokens, device, cancellationToken);

    public async Task<MoyNalogAuthenticatedResult<bool>> IsTaxpayerActiveAsync(
        MoyNalogTokens tokens, MoyNalogDevice device, CancellationToken cancellationToken = default)
    {
        var result = await SendAuthenticatedAsync<UserStatusDto>(HttpMethod.Get, "api/v1/user", null, tokens, device, cancellationToken)
            .ConfigureAwait(false);
        return new MoyNalogAuthenticatedResult<bool>(
            string.Equals(result.Value.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase),
            result.Tokens);
    }

    private async Task<MoyNalogAuthenticatedResult<T>> SendAuthenticatedAsync<T>(
        HttpMethod method, string path, object? body, MoyNalogTokens tokens, MoyNalogDevice device, CancellationToken ct)
    {
        async Task<MoyNalogAuthenticatedResult<T>> Once(MoyNalogTokens current)
        {
            using var request = body is null ? new HttpRequestMessage(method, path) : JsonRequest(method, path, body);
            Authorize(request, current.Token);
            request.Headers.TryAddWithoutValidation("User-Agent", device.UserAgent);
            var value = await SendAsync<T>(request, ct).ConfigureAwait(false);
            return new MoyNalogAuthenticatedResult<T>(value, current);
        }

        try
        {
            return await Once(tokens).ConfigureAwait(false);
        }
        catch (KycApiException ex) when (ex.StatusCodeValue == HttpStatusCode.Unauthorized)
        {
            var refreshed = await RefreshTokenAsync(tokens, device, ct).ConfigureAwait(false);
            return await Once(refreshed).ConfigureAwait(false);
        }
    }

    private object DeviceInfo(MoyNalogDevice device) => new
    {
        sourceDeviceId = device.SourceDeviceId ?? device.DeviceId,
        sourceType = "WEB",
        appVersion = _options.AppVersion,
        metaDetails = new { userAgent = device.UserAgent }
    };

    private static void Authorize(HttpRequestMessage request, string token) =>
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private HttpRequestMessage JsonRequest(HttpMethod method, string url, object body)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        return request;
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken ct)
    {
        using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new KycApiException("MoyNalog", response.StatusCode, text);
        var value = JsonSerializer.Deserialize<T>(text, JsonOptions);
        return value ?? throw new KycApiException("MoyNalog", response.StatusCode, "Empty JSON body.");
    }

    private sealed class UserStatusDto
    {
        public string? Status { get; init; }
    }
}
