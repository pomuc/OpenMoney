using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace OpenMoney.TBank.Signing;

/// <summary>Implements the canonical parameter ordering required by T-Bank APIs.</summary>
public static class TinkoffExtensions
{
    private static readonly HashSet<string> TokenExclusions =
    [
        "Shops", "Receipt", "DATA", "Token", "PaymentIdList", "EmailList",
        "DigestValue", "SignatureValue", "X509SerialNumber", "TerminalPassword"
    ];

    public static string ToTinkoffHashToken(this object request, string? terminalPassword = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        var canonical = GetCanonicalValues(request, terminalPassword);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    public static byte[] ToTinkoffHashTokenBytes(this object request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return SHA256.HashData(Encoding.UTF8.GetBytes(GetCanonicalValues(request, null)));
    }

    public static Dictionary<string, object?> ToDictionary(this object request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead && p.GetCustomAttribute<JsonIgnoreAttribute>() is null)
            .Select(p => new KeyValuePair<string, object?>(p.Name, p.GetValue(request)))
            .Where(p => p.Value is not null)
            .ToDictionary(p => p.Key, p => p.Value);
    }

    private static string GetCanonicalValues(object request, string? password)
    {
        var values = request.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead && !TokenExclusions.Contains(p.Name))
            .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() is null)
            .Select(p => new KeyValuePair<string, string?>(p.Name, FormatValue(p.GetValue(request))))
            .Where(p => p.Value is not null)
            .ToList();

        if (password is not null)
            values.Add(new("Password", password));

        return string.Concat(values.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Value));
    }

    private static string? FormatValue(object? value) => value switch
    {
        null => null,
        bool boolean => boolean ? "true" : "false",
        string text => text,
        IEnumerable sequence => string.Concat(sequence.Cast<object?>().Select(FormatValue)),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };
}
