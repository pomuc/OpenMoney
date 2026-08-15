namespace OpenMoney.TBank.Models;

public interface IHasSignature
{
    string DigestValue { get; set; }
    string SignatureValue { get; set; }
    string X509SerialNumber { get; set; }
}

public abstract class SignedRequest : IHasSignature
{
    public string TerminalKey { get; set; } = string.Empty;
    public string DigestValue { get; set; } = string.Empty;
    public string SignatureValue { get; set; } = string.Empty;
    public string X509SerialNumber { get; set; } = string.Empty;
}

public sealed class RequestInitMomentPayoutContext : SignedRequest
{
    public long Amount { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string? CardId { get; set; }
    public SecureDealData? DATA { get; set; }
    public string? PaymentRecipientId { get; set; }
}

public sealed class RequestMomentPayoutPaymentContext : SignedRequest
{
    public string PaymentId { get; set; } = string.Empty;
}

public sealed class RequestAddPayoutCustomerContext : SignedRequest
{
    public string CustomerKey { get; set; } = string.Empty;
}

public sealed class RequestGetPayoutCustomerContext : SignedRequest
{
    public string CustomerKey { get; set; } = string.Empty;
}

public sealed class RequestRemovePayoutCustomerContext : SignedRequest
{
    public string CustomerKey { get; set; } = string.Empty;
}

public sealed class RequestAddPayoutCardContext : SignedRequest
{
    public string CustomerKey { get; set; } = string.Empty;
    public string CheckType { get; set; } = "3DS";
}

public sealed class RequestGetPayoutCardsContext : SignedRequest
{
    public string CustomerKey { get; set; } = string.Empty;
}

public sealed class RequestRemovePayoutCardContext : SignedRequest
{
    public string CardId { get; set; } = string.Empty;
    public string CustomerKey { get; set; } = string.Empty;
}
