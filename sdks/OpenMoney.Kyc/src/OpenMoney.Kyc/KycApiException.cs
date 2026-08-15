using System.Net;

namespace OpenMoney.Kyc;

public sealed class KycApiException : HttpRequestException
{
    public string Provider { get; }
    public HttpStatusCode StatusCodeValue { get; }
    public string? ResponseBody { get; }

    public KycApiException(string provider, HttpStatusCode status, string? body)
        : base($"{provider} returned HTTP {(int)status} ({status}).", null, status)
    {
        Provider = provider;
        StatusCodeValue = status;
        ResponseBody = body;
    }
}
