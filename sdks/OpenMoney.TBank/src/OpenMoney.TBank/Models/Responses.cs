using System.Text.Json.Serialization;

namespace OpenMoney.TBank.Models;

public abstract class TBankResponse
{
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }
    public string? Details { get; set; }
    public string? TerminalKey { get; set; }
}

public sealed class HttpPayInResponseInit : TBankResponse
{
    public string? Status { get; set; }
    public string? PaymentId { get; set; }
    public Guid? ExternalPaymentId { get; set; }
    public string? OrderId { get; set; }
    [JsonPropertyName("Amount")] public long Amount { get; set; }
    public string? PaymentURL { get; set; }
}

public sealed class HttpPayInResponseCharge : TBankResponse
{
    public string? OrderId { get; set; }
    public string? Status { get; set; }
    public string? PaymentId { get; set; }
    public long Amount { get; set; }
}

public sealed class HttpPayInResponseConfirm : TBankResponse
{
    public string? OrderId { get; set; }
    public string? Status { get; set; }
    public string? PaymentId { get; set; }
    public long Amount { get; set; }
}

public sealed class HttpPayInResponseCancel : TBankResponse
{
    public string? OrderId { get; set; }
    public string? Status { get; set; }
    public string? PaymentId { get; set; }
    public long? OriginalAmount { get; set; }
    public long? NewAmount { get; set; }
}

public sealed class HttpPayInResponseStatus : TBankResponse
{
    public string? Status { get; set; }
    public string? PaymentId { get; set; }
    public string? OrderId { get; set; }
    public List<TBankParameter>? Params { get; set; }
    public long Amount { get; set; }

    [JsonIgnore]
    public PaymentType PaymentType => Params?.FirstOrDefault(x => x.Key == "Source")?.Value switch
    {
        "cards" => PaymentType.Card,
        "qrsbp" => PaymentType.Sbp,
        "TinkoffPay" => PaymentType.TPay,
        _ => PaymentType.Other
    };
}

public sealed class TBankParameter
{
    public string? Key { get; set; }
    public string? Value { get; set; }
}

public sealed class HttpPayInResponseCheckOrder : TBankResponse
{
    public string? OrderId { get; set; }
}

public sealed class HttpPayOutResponseInit : TBankResponse
{
    public long Amount { get; set; }
    public string? PaymentId { get; set; }
    public string? OrderId { get; set; }
    public string? Status { get; set; }
    public Guid? ExternalPaymentId { get; set; }
}

public sealed class HttpPayOutResponsePayment : TBankResponse
{
    public string? OrderId { get; set; }
    public string? PaymentId { get; set; }
    public string? Status { get; set; }
}

public sealed class HttpPayInResponseAddCard : TBankResponse
{
    public string? PaymentId { get; set; }
    public string? CustomerKey { get; set; }
    public string? RequestKey { get; set; }
    public string? PaymentURL { get; set; }
}

public sealed class HttpPayInResponseRemoveCard : TBankResponse
{
    public string? CardId { get; set; }
    public string? CustomerKey { get; set; }
    public string? Status { get; set; }
}

public sealed class HttpPayCardResponse
{
    public string? CardId { get; set; }
    public string? Pan { get; set; }
    public char Status { get; set; }
    public string? RebillId { get; set; }
    public int CardType { get; set; }
    public string? ExpDate { get; set; }
}

public sealed class HttpPayInResponseCreateSecureTransaction : TBankResponse
{
    public string? SpAccumulationId { get; set; }
    public Guid? ExternalDealId { get; set; }
}

public sealed class HttpGetQr : TBankResponse
{
    public string? Data { get; set; }
}

public sealed class HttpResponsePaycheck
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public PaycheckData? Model { get; set; }
}

public sealed class PaycheckData
{
    public string? Id { get; set; }
    public int ErrorCode { get; set; }
    public string? ReceiptLocalUrl { get; set; }
}

public sealed class HttpPayOutResponseAddCustomer : TBankResponse { public string? CustomerKey { get; set; } }
public sealed class HttpPayOutResponseGetCustomer : TBankResponse
{
    public string? CustomerKey { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}
public sealed class HttpPayOutResponseRemoveCustomer : TBankResponse { public string? CustomerKey { get; set; } }
public sealed class HttpPayOutResponseAddCard : TBankResponse
{
    public string? PaymentId { get; set; }
    public string? CustomerKey { get; set; }
    public string? PaymentURL { get; set; }
    public Guid RequestKey { get; set; }
}
public sealed class HttpPayOutResponseCard
{
    public string? Pan { get; set; }
    public string? CardId { get; set; }
    public string? Status { get; set; }
    public string? RebillId { get; set; }
    public string? ExpDate { get; set; }
}
public sealed class HttpPayOutResponseRemoveCard : TBankResponse
{
    public string? CardId { get; set; }
    public string? CustomerKey { get; set; }
    public string? Status { get; set; }
}
