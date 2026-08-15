using System.Globalization;
using OpenMoney.VTB.Models;

namespace OpenMoney.VTB;

/// <summary>Parses VTB acquiring callbacks sent as form-urlencoded request bodies.</summary>
public static class VtbCallbackParser
{
    /// <summary>Parses and validates the callback fields required by the SDK.</summary>
    /// <exception cref="FormatException">The body is malformed or a required field is invalid.</exception>
    public static VtbAcquiringCallback Parse(string formUrlEncodedBody)
    {
        if (!TryParse(formUrlEncodedBody, out var callback, out var error))
        {
            throw new FormatException(error);
        }

        return callback!;
    }

    /// <summary>Attempts to parse a form-urlencoded callback without throwing.</summary>
    public static bool TryParse(
        string? formUrlEncodedBody,
        out VtbAcquiringCallback? callback,
        out string? error)
    {
        callback = null;
        error = null;
        if (string.IsNullOrWhiteSpace(formUrlEncodedBody))
        {
            error = "The VTB callback body is empty.";
            return false;
        }

        Dictionary<string, string> fields;
        try
        {
            fields = ParseFields(formUrlEncodedBody);
        }
        catch (FormatException exception)
        {
            error = exception.Message;
            return false;
        }

        if (!TryGetGuid(fields, "mdOrder", out var mdOrder)
            || !TryGetInt64(fields, "processingId", out var processingId)
            || !TryGetInt64(fields, "amount", out var amount))
        {
            error = "The VTB callback contains an invalid mdOrder, processingId, or amount.";
            return false;
        }

        if (!fields.TryGetValue("checksum", out var checksum)
            || string.IsNullOrWhiteSpace(checksum)
            || !fields.TryGetValue("operation", out var operation)
            || string.IsNullOrWhiteSpace(operation)
            || !fields.TryGetValue("paymentState", out var paymentState)
            || string.IsNullOrWhiteSpace(paymentState))
        {
            error = "The VTB callback is missing checksum, operation, or paymentState.";
            return false;
        }

        callback = new VtbAcquiringCallback
        {
            MdOrder = mdOrder,
            ProcessingId = processingId,
            Checksum = checksum,
            Operation = operation,
            PaymentState = paymentState,
            Amount = amount,
            Fields = fields
        };
        return true;
    }

    private static Dictionary<string, string> ParseFields(string body)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var segment in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = segment.IndexOf('=');
            var encodedName = separator >= 0 ? segment[..separator] : segment;
            var encodedValue = separator >= 0 ? segment[(separator + 1)..] : string.Empty;
            var name = Decode(encodedName);
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new FormatException("The VTB callback contains an empty field name.");
            }

            if (!fields.TryAdd(name, Decode(encodedValue)))
            {
                throw new FormatException($"The VTB callback contains duplicate field '{name}'.");
            }
        }

        return fields;
    }

    private static string Decode(string value)
    {
        try
        {
            return Uri.UnescapeDataString(value.Replace('+', ' '));
        }
        catch (UriFormatException exception)
        {
            throw new FormatException("The VTB callback contains invalid URL encoding.", exception);
        }
    }

    private static bool TryGetGuid(
        IReadOnlyDictionary<string, string> fields,
        string name,
        out Guid value)
    {
        value = Guid.Empty;
        return fields.TryGetValue(name, out var text)
            && Guid.TryParse(text, out value)
            && value != Guid.Empty;
    }

    private static bool TryGetInt64(
        IReadOnlyDictionary<string, string> fields,
        string name,
        out long value)
    {
        value = 0;
        return fields.TryGetValue(name, out var text)
            && long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value)
            && value >= 0;
    }
}

/// <summary>
/// Verifies the bank-supplied checksum before a callback can change payment state.
/// Implement this interface using the algorithm and key material issued for your VTB
/// integration. See https://sandbox.vtb.ru/sandbox/ru/integration/api/rest/rest.html#callback-code-examples.
/// </summary>
public interface IVtbCallbackVerifier
{
    /// <summary>Returns true only when the callback checksum is authentic.</summary>
    bool Verify(VtbAcquiringCallback callback);
}
