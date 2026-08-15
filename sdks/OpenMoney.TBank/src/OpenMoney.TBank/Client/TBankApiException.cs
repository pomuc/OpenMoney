using System.Net;

namespace OpenMoney.TBank.Client;

public sealed class TBankApiException : HttpRequestException
{
    public TBankApiException(HttpStatusCode statusCode, string responseBody)
        : base($"T-Bank returned HTTP {(int)statusCode} ({statusCode}).", null, statusCode)
    {
        ResponseBody = responseBody;
    }

    public string ResponseBody { get; }
}
