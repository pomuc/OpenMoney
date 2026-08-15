using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace OpenMoney.Kyc.Mts;

public sealed class MtsRimOptions
{
    public const string SectionName = "Kyc:MtsRim";
    public string BaseUrl { get; set; } = "https://api.mts.ru/rim/2.0/";
    public string AccessToken { get; set; } = "";
    public string? DefaultRedirectUrl { get; set; }
    public int LinkLifetimeInMinutes { get; set; } = 5;
}

/// <summary>
/// MTS RIM document OCR + selfie identification sessions.
/// </summary>
public sealed class MtsRimClient
{
    private readonly HttpClient _http;
    private readonly MtsRimOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public MtsRimClient(HttpClient http, IOptions<MtsRimOptions> options)
    {
        _http = http;
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.AccessToken))
            throw new OptionsValidationException(nameof(MtsRimOptions), typeof(MtsRimOptions), ["AccessToken is required."]);
    }

    public async Task CreateApplicantAsync(Guid externalId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v2/applicants")
        {
            Content = JsonContent.Create(new { externalId }, options: JsonOptions)
        };
        Authorize(request);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new KycApiException("MtsRim", response.StatusCode, text);
    }

    public async Task<MtsRimIdentification> StartIdentificationAsync(
        Guid externalId, string? redirectUrl = null, CancellationToken cancellationToken = default)
    {
        var redirect = redirectUrl ?? _options.DefaultRedirectUrl
            ?? throw new InvalidOperationException("DefaultRedirectUrl or redirectUrl is required.");

        var body = new
        {
            linkLifetimeInMinutes = _options.LinkLifetimeInMinutes,
            redirectUrl = redirect,
            workflowPreferences = new
            {
                bio = new
                {
                    isActive = true,
                    allowedDocuments = new[] { "_any_.passport", "_any_.drvlic" },
                    steps = new[] { "selfie", "document", "documentForm", "successPage" },
                    lastSelfieMatching = true
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v2/applicants/{externalId}/identifications")
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        Authorize(request);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new KycApiException("MtsRim", response.StatusCode, text);

        return JsonSerializer.Deserialize<RimIdentificationDto>(text, JsonOptions) is { } dto
            ? new MtsRimIdentification
            {
                Id = dto.Id,
                IdentificationUrl = dto.IdentificationUrl,
                Status = dto.Status
            }
            : throw new KycApiException("MtsRim", response.StatusCode, "Empty identification body.");
    }

    public async Task<MtsRimIdentification> GetIdentificationAsync(
        Guid externalId, Guid identificationId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v2/applicants/{externalId}/identifications/{identificationId}");
        Authorize(request);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new KycApiException("MtsRim", response.StatusCode, text);

        return JsonSerializer.Deserialize<RimIdentificationDto>(text, JsonOptions) is { } dto
            ? new MtsRimIdentification
            {
                Id = dto.Id,
                IdentificationUrl = dto.IdentificationUrl,
                Status = dto.Status
            }
            : throw new KycApiException("MtsRim", response.StatusCode, "Empty identification body.");
    }

    private void Authorize(HttpRequestMessage request) =>
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);

    private sealed class RimIdentificationDto
    {
        public Guid? Id { get; init; }
        public string? IdentificationUrl { get; init; }
        public string? Status { get; init; }
    }
}
