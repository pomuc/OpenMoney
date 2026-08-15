using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace OpenMoney.TBank.Models;

public sealed class SecureDealData
{
    public Guid? UserId { get; set; }
    public long? DealId { get; set; }
    public long? Amount { get; set; }
    public string? OrderId { get; set; }
    public Guid? GoodId { get; set; }
    public string? Passport { get; set; }
    public long? PaycheckAmount { get; set; }
    public string? Phone { get; set; }
    public object? Extra { get; set; }
    public long? TelegramId { get; set; }
    public string? Referer { get; set; }
    public string? IP { get; set; }
    public string? PaycheckUrl { get; set; }
    public string? NpdPaycheckUrl { get; set; }
    public string? ReturnPaycheckUrl { get; set; }
    public List<Guid>? Cart { get; set; }
}

public sealed class Receipt
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Taxation { get; set; }
    public ReceiptItem[] Items { get; set; } = [];
}

public sealed class ReceiptItem
{
    public string Name { get; set; } = string.Empty;
    public long Price { get; set; }
    public decimal Quantity { get; set; }
    public long Amount { get; set; }
    public string? Tax { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentObject { get; set; }
}

public sealed class HttpRequestPaycheck
{
    public required TaxationSystem TaxationSystem { get; init; }
    public required Inn Inn { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required PaycheckType Type { get; init; }
    public required CustomerReceipt CustomerReceipt { get; init; }
    public string InvoiceId { get; init; } = string.Empty;
    public string AccountId { get; init; } = string.Empty;
}

[JsonConverter(typeof(InnJsonConverter))]
public sealed partial class Inn
{
    public Inn(string value)
    {
        if (!InnPattern().IsMatch(value))
            throw new ArgumentException("INN must contain 10 or 12 digits.", nameof(value));
        Value = value;
    }

    public string Value { get; }
    public override string ToString() => Value;

    [GeneratedRegex(@"^(\d{10}|\d{12})$", RegexOptions.CultureInvariant)]
    private static partial Regex InnPattern();
}

public sealed class InnJsonConverter : JsonConverter<Inn>
{
    public override Inn Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetString() ?? throw new JsonException("INN cannot be null."));
    public override void Write(Utf8JsonWriter writer, Inn value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}

public sealed class CustomerReceipt
{
    public required TaxationSystem TaxationSystem { get; init; }
    public string CalculationPlace { get; set; } = string.Empty;
    public CustomerReceiptItem[] Items { get; set; } = [];
}

public sealed class CustomerReceiptItem
{
    public const string DefaultLabel = "Service";

    public CustomerReceiptItem(double price, ushort quantity)
    {
        Price = price.ToString(CultureInfo.InvariantCulture);
        Quantity = quantity.ToString(CultureInfo.InvariantCulture);
        Amount = (price * quantity).ToString(CultureInfo.InvariantCulture);
    }

    public CalculationMethod Method { get; set; } = CalculationMethod.FullPay;
    public string Label { get; set; } = DefaultLabel;
    public string Object { get; set; } = "4";
    public string Price { get; }
    public string Quantity { get; }
    public string Amount { get; }
    public int? Vat { get; set; }
    public int? AgentSign { get; set; }
    public SupplierInfo? PurveyorData { get; set; }
}

public sealed class SupplierInfo
{
    public string Phone { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Inn { get; set; } = string.Empty;
}

public enum CalculationMethod { Unknown, FullPrepayment, PartialPrepayment, AdvancePay, FullPay, PartialPayAndCredit, Credit, CreditPayment }
public enum TaxationSystem { Common, SimplifiedIncome, SimplifiedIncomeMinusExpenses, SingleIncomeTax, SingleAgriculturalTax, PatentTaxationSystem }
public enum PaycheckType { Income, IncomeReturn, Expense, ExpenseReturn }
public enum PaymentType { Card, Sbp, TPay, Other }
