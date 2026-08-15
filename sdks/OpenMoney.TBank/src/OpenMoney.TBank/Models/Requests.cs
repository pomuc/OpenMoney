using System.Text.Json.Serialization;

namespace OpenMoney.TBank.Models;

public abstract class TokenRequest
{
    public string TerminalKey { get; set; } = string.Empty;
    [JsonIgnore]
    public string TerminalPassword { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public sealed class RequestInitPaymentContext : TokenRequest
{
    public long Amount { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SuccessURL { get; set; }
    public string? NotificationURL { get; set; }
    public string? FailURL { get; set; }
    public long? DealId { get; set; }
    public string PayType { get; set; } = "O";
    public SecureDealData? DATA { get; set; }
    public Receipt? Receipt { get; set; }
}

public sealed class RequestChargePaymentContext : TokenRequest
{
    public string PaymentId { get; set; } = string.Empty;
    public string RebillId { get; set; } = string.Empty;
}

public sealed class RequestConfirmPaymentContext : TokenRequest
{
    public string PaymentId { get; set; } = string.Empty;
    public string RebillId { get; set; } = string.Empty;
}

public sealed class RequestCancelPaymentContext : TokenRequest
{
    public string PaymentId { get; set; } = string.Empty;
    public long? Amount { get; set; }
    public Receipt? Receipt { get; set; }
}

public sealed class RequestGetStatePaymentContext : TokenRequest
{
    public string PaymentId { get; set; } = string.Empty;
    [JsonIgnore] public Guid? ExternalPaymentId { get; set; }
    [JsonIgnore] public string? CustomerKey { get; set; }
}

public sealed class RequestCheckOrderContext : TokenRequest
{
    public string OrderId { get; set; } = string.Empty;
}

public sealed class RequestInitPayoutContext : TokenRequest
{
    public long Amount { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string? CardId { get; set; }
    public long? DealId { get; set; }
    public bool? FinalPayout { get; set; }
    public SecureDealData? DATA { get; set; }
    public string? PaymentRecipientId { get; set; }
}

public sealed class RequestPayoutPaymentContext : TokenRequest
{
    public string PaymentId { get; set; } = string.Empty;
}

public sealed class RequestAddCardContext : TokenRequest
{
    public string CustomerKey { get; set; } = string.Empty;
    public string CheckType { get; set; } = "3DS";
}

public sealed class RequestRemoveCardContext : TokenRequest
{
    public string CustomerKey { get; set; } = string.Empty;
    public string CardId { get; set; } = string.Empty;
}

public sealed class RequestGetCardListContext : TokenRequest
{
    public string CustomerKey { get; set; } = string.Empty;
}

public sealed class RequestCreateSecureDealContext : TokenRequest
{
    public string SpDealType { get; set; } = SecureTransactionType.DealNN;
}

public sealed class RequestGetQrContext : TokenRequest
{
    public string PaymentId { get; set; } = string.Empty;
    public string DataType { get; set; } = "IMAGE";
}

public class RequestPaycheckContext
{
    public string CustomerKey { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public long Amount { get; set; }
}

public sealed class RequestAgentPaycheckContext : RequestPaycheckContext
{
    public TaxationSystem TaxationSystem { get; set; }
    public int? Vat { get; set; }
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerInn { get; set; } = string.Empty;
    public string? Label { get; set; }
}

public sealed class RequestPaymentNotificationContext
{
    public string? TerminalKey { get; set; }
    public string? OrderId { get; set; }
    public bool? Success { get; set; }
    public string? Status { get; set; }
    public long? PaymentId { get; set; }
    public string? ErrorCode { get; set; }
    public ulong? Amount { get; set; }
    public ulong? CardId { get; set; }
    public string? Pan { get; set; }
    public string? ExpDate { get; set; }
    public ulong? RebillId { get; set; }
    public string? Token { get; set; }
    public string? SpAccumulationId { get; set; }
    public SecureDealData? DATA { get; set; }
}

public sealed class RequestCardNotificationContext
{
    public string TerminalKey { get; set; } = string.Empty;
    public string CustomerKey { get; set; } = string.Empty;
    public string RequestKey { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public int? ErrorCode { get; set; }
    public ulong CardId { get; set; }
    public string? Pan { get; set; }
    public string? ExpDate { get; set; }
    public string? NotificationType { get; set; }
    public ulong RebillId { get; set; }
    public string? Token { get; set; }
}

public static class SecureTransactionType
{
    public const string Deal1N = "1N";
    public const string DealNN = "NN";
}
