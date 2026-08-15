using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jose;
using Microsoft.Extensions.Options;

namespace OpenMoney.Kyc.Mts;

public sealed class MtsIdOptions
{
    public const string SectionName = "Kyc:MtsId";

    public string IdGatewayBaseUrl { get; set; } = "https://idgw.mobileid.mts.ru/";
    public string LoginBaseUrl { get; set; } = "https://login.mts.ru/";
    public string ClientId { get; set; } = "";
    public string? ClientSecret { get; set; }
    public string Scope { get; set; } = "openid mc_kyc_plain mc_identity_full";
    public string NotificationUri { get; set; } = "";
    public string ClientNotificationToken { get; set; } = "";
    public string SigningKeyKid { get; set; } = "";
    /// <summary>PEM-encoded RSA private key used to sign SI requests and decrypt JWE responses.</summary>
    public string SigningPrivateKeyPem { get; set; } = "";
    public string? RedirectUri { get; set; }
    public string OAuthAuthorizePath { get; set; } = "amserver/oauth2/authorize";
    public string OAuthTokenPath { get; set; } = "amserver/oauth2/access_token";
    public string OAuthUserInfoPath { get; set; } = "amserver/oauth2/userinfo";
}

/// <summary>
/// MTS Mobile Connect SI async KYC (passport via birthdate match) and optional OAuth helpers.
/// </summary>
public sealed class MtsIdClient
{
    private readonly HttpClient _http;
    private readonly MtsIdOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public MtsIdClient(HttpClient http, IOptions<MtsIdOptions> options)
    {
        _http = http;
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.ClientId))
            throw new OptionsValidationException(nameof(MtsIdOptions), typeof(MtsIdOptions), ["ClientId is required."]);
        if (string.IsNullOrWhiteSpace(_options.SigningPrivateKeyPem))
            throw new OptionsValidationException(nameof(MtsIdOptions), typeof(MtsIdOptions), ["SigningPrivateKeyPem is required."]);
    }

    public async Task<MtsSiAuthorizeResult> StartSiAuthorizeAsync(long phoneMsisdn, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = new Dictionary<string, object>
        {
            ["client_id"] = _options.ClientId,
            ["scope"] = _options.Scope,
            ["nonce"] = Guid.NewGuid().ToString("N"),
            ["login_hint"] = $"MSISDN:{phoneMsisdn}",
            ["acr_values"] = "2",
            ["notification_uri"] = _options.NotificationUri,
            ["client_notification_token"] = _options.ClientNotificationToken,
            ["version"] = "mc_si_r2_v1.0",
            ["response_type"] = "mc_si_async_code",
            ["iss"] = _options.ClientId,
            ["aud"] = _options.IdGatewayBaseUrl.TrimEnd('/'),
            ["exp"] = now + 300,
            ["iat"] = now,
            ["nbf"] = now
        };

        using var rsa = LoadPrivateKey();
        var extra = new Dictionary<string, object> { ["alg"] = "RS256", ["typ"] = "JWT" };
        if (!string.IsNullOrWhiteSpace(_options.SigningKeyKid))
            extra["kid"] = _options.SigningKeyKid;

        var requestJwt = JWT.Encode(payload, rsa, JwsAlgorithm.RS256, extraHeaders: extra);

        using var response = await _http.PostAsJsonAsync(
            Combine(_options.IdGatewayBaseUrl, "oidc/si-authorize"),
            new
            {
                client_id = _options.ClientId,
                response_type = "mc_si_async_code",
                scope = _options.Scope,
                request = requestJwt
            },
            cancellationToken).ConfigureAwait(false);

        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new KycApiException("MtsId", response.StatusCode, text);

        var dto = JsonSerializer.Deserialize<SiAuthDto>(text, JsonOptions)
            ?? throw new KycApiException("MtsId", response.StatusCode, "Empty SI authorize body.");
        if (string.IsNullOrWhiteSpace(dto.AuthReqId))
            throw new KycApiException("MtsId", response.StatusCode, "auth_req_id missing.");

        return new MtsSiAuthorizeResult { AuthReqId = dto.AuthReqId, ExpiresIn = dto.ExpiresIn };
    }

    public async Task<bool> SubmitOtpAsync(string smsOtpEndpoint, string code, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(smsOtpEndpoint, UriKind.Absolute, out _))
            throw new ArgumentException("smsOtpEndpoint must be an absolute URL.", nameof(smsOtpEndpoint));

        using var response = await _http.PostAsJsonAsync(smsOtpEndpoint, new { verify_code = code }, cancellationToken)
            .ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public async Task<MtsPremiumInfo?> MatchBirthdateAndGetPremiumInfoAsync(
        string accessToken, string jwksUri, DateOnly birthdate, CancellationToken cancellationToken = default)
    {
        var jwksText = await _http.GetStringAsync(jwksUri, cancellationToken).ConfigureAwait(false);
        var jwks = JsonSerializer.Deserialize<JwksDto>(jwksText, JsonOptions)
            ?? throw new KycApiException("MtsId", System.Net.HttpStatusCode.BadGateway, "Invalid JWKS.");
        var enc = jwks.Keys?.FirstOrDefault(k => string.Equals(k.Use, "enc", StringComparison.OrdinalIgnoreCase))
            ?? throw new KycApiException("MtsId", System.Net.HttpStatusCode.BadGateway, "No enc key in JWKS.");

        using var encRsa = RSA.Create();
        encRsa.ImportParameters(new RSAParameters
        {
            Modulus = Base64UrlDecode(enc.N!),
            Exponent = Base64UrlDecode(enc.E!)
        });

        var birthJwe = JWT.Encode(
            new { birthdate = birthdate.ToString("yyyy-MM-dd") },
            encRsa,
            JweAlgorithm.RSA_OAEP_256,
            JweEncryption.A256GCM);

        using var matchRequest = new HttpRequestMessage(HttpMethod.Post, Combine(_options.IdGatewayBaseUrl, "oidc/kyc-match-split"))
        {
            Content = new StringContent(birthJwe, Encoding.UTF8, "application/jwt")
        };
        matchRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var matchResponse = await _http.SendAsync(matchRequest, cancellationToken).ConfigureAwait(false);
        var matchBody = await matchResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!matchResponse.IsSuccessStatusCode)
            throw new KycApiException("MtsId", matchResponse.StatusCode, matchBody);

        using var priv = LoadPrivateKey();
        var matchJson = JWT.Decode(matchBody, priv);
        var match = JsonSerializer.Deserialize<MtsKycMatchResult>(matchJson, JsonOptions);
        if (match is null || !match.IsMatch)
            return null;

        using var premiumRequest = new HttpRequestMessage(HttpMethod.Get, Combine(_options.IdGatewayBaseUrl, "oidc/premiuminfo"));
        premiumRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var premiumResponse = await _http.SendAsync(premiumRequest, cancellationToken).ConfigureAwait(false);
        var premiumBody = await premiumResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!premiumResponse.IsSuccessStatusCode)
            throw new KycApiException("MtsId", premiumResponse.StatusCode, premiumBody);

        var premiumJson = JWT.Decode(premiumBody, priv);
        return JsonSerializer.Deserialize<MtsPremiumInfo>(premiumJson, JsonOptions);
    }

    public string BuildAuthorizeUrl(string state, string? redirectUri = null, string scope = "phone")
    {
        var redirect = redirectUri ?? _options.RedirectUri
            ?? throw new InvalidOperationException("RedirectUri must be configured.");
        var query = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["scope"] = scope,
            ["state"] = state,
            ["redirect_uri"] = redirect,
            ["response_type"] = "code"
        };
        var builder = new UriBuilder(Combine(_options.LoginBaseUrl, _options.OAuthAuthorizePath))
        {
            Query = string.Join("&", query.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"))
        };
        return builder.Uri.ToString();
    }

    public async Task<MtsOAuthTokenResponse> ExchangeAuthorizationCodeAsync(
        string code, string? redirectUri = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientSecret))
            throw new InvalidOperationException("ClientSecret is required for OAuth token exchange.");

        var redirect = redirectUri ?? _options.RedirectUri
            ?? throw new InvalidOperationException("RedirectUri must be configured.");

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["redirect_uri"] = redirect
        });
        using var response = await _http.PostAsync(Combine(_options.LoginBaseUrl, _options.OAuthTokenPath), content, cancellationToken)
            .ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new KycApiException("MtsId", response.StatusCode, text);
        return JsonSerializer.Deserialize<MtsOAuthTokenResponse>(text, JsonOptions)
            ?? throw new KycApiException("MtsId", response.StatusCode, "Empty token body.");
    }

    public async Task<MtsUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, Combine(_options.LoginBaseUrl, _options.OAuthUserInfoPath));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new KycApiException("MtsId", response.StatusCode, text);
        return JsonSerializer.Deserialize<MtsUserInfo>(text, JsonOptions)
            ?? throw new KycApiException("MtsId", response.StatusCode, "Empty userinfo body.");
    }

    private RSA LoadPrivateKey()
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(_options.SigningPrivateKeyPem);
        return rsa;
    }

    private static string Combine(string baseUrl, string relative) =>
        new Uri(new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/"), relative.TrimStart('/')).ToString();

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            _ => ""
        };
        return Convert.FromBase64String(padded);
    }

    private sealed class SiAuthDto
    {
        public string? AuthReqId { get; init; }
        public int? ExpiresIn { get; init; }
    }

    private sealed class JwksDto
    {
        public List<JwksKeyDto>? Keys { get; init; }
    }

    private sealed class JwksKeyDto
    {
        public string? N { get; init; }
        public string? E { get; init; }
        public string? Use { get; init; }
    }
}
