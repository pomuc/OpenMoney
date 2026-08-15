using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace OpenMoney.Kyc.Didit;

public sealed class DiditOptions
{
    public const string SectionName = "Kyc:Didit";
    public string AuthBaseUrl { get; set; } = "https://apx.didit.me/";
    public string VerificationBaseUrl { get; set; } = "https://verification.didit.me/";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string DefaultFeatures { get; set; } = "OCR + FACE";
}

/// <summary>
/// Didit.me hosted KYC: client-credentials token, session create, decision poll.
/// </summary>
public sealed class DiditClient
{
    private readonly HttpClient _http;
    private readonly DiditOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public DiditClient(HttpClient http, IOptions<DiditOptions> options)
    {
        _http = http;
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
            throw new OptionsValidationException(nameof(DiditOptions), typeof(DiditOptions), ["ClientId and ClientSecret are required."]);
    }

    public async Task<DiditTokenResponse> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Combine(_options.AuthBaseUrl, "auth/v2/token/"))
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials"
            })
        };
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new KycApiException("Didit", response.StatusCode, text);

        var dto = JsonSerializer.Deserialize<TokenDto>(text, JsonOptions)
            ?? throw new KycApiException("Didit", response.StatusCode, "Empty token body.");
        if (string.IsNullOrWhiteSpace(dto.AccessToken))
            throw new KycApiException("Didit", response.StatusCode, "access_token missing.");

        return new DiditTokenResponse { AccessToken = dto.AccessToken };
    }

    public async Task<DiditSession> CreateSessionAsync(
        string callbackUrl,
        string vendorData,
        string? features = null,
        string? accessToken = null,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(callbackUrl, UriKind.Absolute, out _))
            throw new ArgumentException("callbackUrl must be absolute.", nameof(callbackUrl));

        var token = accessToken ?? (await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false)).AccessToken;
        using var request = new HttpRequestMessage(HttpMethod.Post, Combine(_options.VerificationBaseUrl, "v1/session/"))
        {
            Content = JsonContent.Create(new
            {
                callback = callbackUrl,
                features = features ?? _options.DefaultFeatures,
                vendor_data = vendorData
            }, options: JsonOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new KycApiException("Didit", response.StatusCode, text);

        var dto = JsonSerializer.Deserialize<SessionDto>(text, JsonOptions)
            ?? throw new KycApiException("Didit", response.StatusCode, "Empty session body.");
        if (string.IsNullOrWhiteSpace(dto.SessionId) || string.IsNullOrWhiteSpace(dto.Url))
            throw new KycApiException("Didit", response.StatusCode, "session_id/url missing.");

        return new DiditSession
        {
            SessionId = dto.SessionId,
            Url = dto.Url,
            SessionToken = dto.SessionToken
        };
    }

    public async Task<DiditDecision> GetDecisionAsync(
        string sessionId, string? accessToken = null, CancellationToken cancellationToken = default)
    {
        var token = accessToken ?? (await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false)).AccessToken;
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            Combine(_options.VerificationBaseUrl, $"v1/session/{sessionId}/decision/"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new KycApiException("Didit", response.StatusCode, text);

        var dto = JsonSerializer.Deserialize<DecisionDto>(text, JsonOptions)
            ?? throw new KycApiException("Didit", response.StatusCode, "Empty decision body.");

        return new DiditDecision
        {
            Status = dto.Status,
            VendorData = dto.VendorData?.ToString(),
            SessionId = dto.SessionId?.ToString(),
            CreatedAt = dto.CreatedAt,
            Kyc = dto.Kyc is null ? null : new DiditKycData
            {
                Address = dto.Kyc.Address,
                DateOfBirth = dto.Kyc.DateOfBirth,
                DateOfIssue = dto.Kyc.DateOfIssue,
                DocumentNumber = dto.Kyc.DocumentNumber,
                DocumentType = dto.Kyc.DocumentType,
                ExpirationDate = dto.Kyc.ExpirationDate,
                FirstName = dto.Kyc.FirstName,
                LastName = dto.Kyc.LastName,
                IssuingState = dto.Kyc.IssuingState,
                PlaceOfBirth = dto.Kyc.PlaceOfBirth,
                Status = dto.Kyc.Status
            },
            Face = dto.Face is null ? null : new DiditFaceData
            {
                LivenessStatus = dto.Face.LivenessStatus,
                Status = dto.Face.Status
            }
        };
    }

    private static string Combine(string baseUrl, string relative) =>
        new Uri(new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/"), relative.TrimStart('/')).ToString();

    private sealed class TokenDto
    {
        public string? AccessToken { get; init; }
    }

    private sealed class SessionDto
    {
        public string? SessionId { get; init; }
        public string? Url { get; init; }
        public string? SessionToken { get; init; }
    }

    private sealed class DecisionDto
    {
        public DateTimeOffset? CreatedAt { get; init; }
        public KycDto? Kyc { get; init; }
        public string? Status { get; init; }
        public object? VendorData { get; init; }
        public object? SessionId { get; init; }
        public FaceDto? Face { get; init; }
    }

    private sealed class KycDto
    {
        public string? Address { get; init; }
        public DateOnly? DateOfBirth { get; init; }
        public DateOnly? DateOfIssue { get; init; }
        public string? DocumentNumber { get; init; }
        public string? DocumentType { get; init; }
        public string? ExpirationDate { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? IssuingState { get; init; }
        public string? PlaceOfBirth { get; init; }
        public string? Status { get; init; }
    }

    private sealed class FaceDto
    {
        public string? LivenessStatus { get; init; }
        public string? Status { get; init; }
    }
}
