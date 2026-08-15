using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OpenMoney.SelfEmployed.Models;

/// <summary>Request for listing organization payment registries in a date range.</summary>
public sealed class PaymentRegistryListRequest
{
    /// <summary>Inclusive start date (UTC calendar day).</summary>
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    /// <summary>Inclusive end date (UTC calendar day).</summary>
    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    /// <summary>Zero-based page index.</summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>Page size (production code used up to 200).</summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 200;
}

/// <summary>Starts asynchronous retrieval of self-employed receipts for a registry.</summary>
public sealed class PaymentRegistryReceiptsRequest
{
    /// <summary>Client correlation identifier.</summary>
    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Payment registry identifier.</summary>
    [JsonPropertyName("paymentRegistryId")]
    public long PaymentRegistryId { get; set; }
}

/// <summary>One receipt discovered in a registry receipts result payload.</summary>
public sealed class SelfEmployedReceiptCandidate
{
    /// <summary>Bank operation / payment identifier used for deduplication.</summary>
    public string OperationId { get; set; } = string.Empty;

    /// <summary>Optional payment registry id from the payload.</summary>
    public string? PaymentRegistryId { get; set; }

    /// <summary>Remote receipt URL (PDF/image).</summary>
    public string ReceiptUrl { get; set; } = string.Empty;

    /// <summary>Raw JSON object that produced this candidate (for host persistence).</summary>
    public JsonElement Raw { get; set; }
}

/// <summary>Host persistence for synchronized self-employed receipts.</summary>
public interface INpdReceiptStore
{
    /// <summary>Returns true when a receipt for <paramref name="externalOperationId"/> already exists.</summary>
    Task<bool> ExistsAsync(string externalOperationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a downloaded receipt. Host may upload bytes to object storage and link payout/user ids.
    /// </summary>
    Task SaveAsync(SelfEmployedReceiptRecord record, CancellationToken cancellationToken = default);
}

/// <summary>Sanitized receipt record ready for host storage.</summary>
public sealed class SelfEmployedReceiptRecord
{
    /// <summary>External operation id from T-Bank.</summary>
    public string ExternalOperationId { get; set; } = string.Empty;

    /// <summary>Optional registry id.</summary>
    public string? PaymentRegistryId { get; set; }

    /// <summary>Original receipt URL.</summary>
    public string? ReceiptUrl { get; set; }

    /// <summary>Downloaded content, when available.</summary>
    public byte[]? Content { get; set; }

    /// <summary>Content-Type of downloaded content.</summary>
    public string? ContentType { get; set; }

    /// <summary>Suggested file extension including the leading dot.</summary>
    public string SuggestedExtension { get; set; } = ".bin";

    /// <summary>Raw API node for auditing (may contain PII — do not log publicly).</summary>
    public JsonElement Raw { get; set; }
}

/// <summary>Helpers for walking flexible T-Bank JSON payloads.</summary>
public static class NpdJsonTraversal
{
    /// <summary>Extracts registry ids from list responses (paymentRegistryId / registryId / id).</summary>
    public static IReadOnlyList<long> ExtractRegistryIds(JsonNode? node)
    {
        var ids = new HashSet<long>();
        Traverse(node, obj =>
        {
            var idText = ReadString(obj, "paymentRegistryId")
                         ?? ReadString(obj, "registryId")
                         ?? ReadString(obj, "id");
            if (long.TryParse(idText, out var id))
            {
                ids.Add(id);
            }
        });
        return ids.ToList();
    }

    /// <summary>Collects receipt candidates from a receipts/result payload.</summary>
    public static IReadOnlyList<SelfEmployedReceiptCandidate> CollectReceiptCandidates(JsonNode? node)
    {
        var result = new List<SelfEmployedReceiptCandidate>();
        Traverse(node, obj =>
        {
            var receiptUrl = ReadString(obj, "receiptUrl")
                             ?? ReadString(obj, "receiptLink")
                             ?? ReadString(obj, "receiptLocalUrl")
                             ?? ReadString(obj, "photoUrl")
                             ?? ReadString(obj, "imageUrl");
            if (string.IsNullOrWhiteSpace(receiptUrl))
            {
                return;
            }

            var operationId = ReadString(obj, "operationId")
                              ?? ReadString(obj, "paymentOperationId")
                              ?? ReadString(obj, "paymentId")
                              ?? ReadString(obj, "number")
                              ?? ReadString(obj, "orderId");
            if (string.IsNullOrWhiteSpace(operationId))
            {
                return;
            }

            result.Add(new SelfEmployedReceiptCandidate
            {
                OperationId = operationId,
                PaymentRegistryId = ReadString(obj, "paymentRegistryId"),
                ReceiptUrl = receiptUrl,
                Raw = JsonSerializer.SerializeToElement(obj)
            });
        });
        return result;
    }

    /// <summary>Suggests a file extension from content type or URL.</summary>
    public static string ResolveExtension(string? contentType, string? url)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            if (contentType.Contains("jpeg", StringComparison.OrdinalIgnoreCase)) return ".jpg";
            if (contentType.Contains("png", StringComparison.OrdinalIgnoreCase)) return ".png";
            if (contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase)) return ".pdf";
            if (contentType.Contains("webp", StringComparison.OrdinalIgnoreCase)) return ".webp";
        }

        if (!string.IsNullOrWhiteSpace(url)
            && Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var ext = Path.GetExtension(uri.AbsolutePath);
            if (!string.IsNullOrWhiteSpace(ext))
            {
                return ext;
            }
        }

        return ".bin";
    }

    private static void Traverse(JsonNode? node, Action<JsonObject> onObject)
    {
        if (node is null)
        {
            return;
        }

        if (node is JsonObject obj)
        {
            onObject(obj);
            foreach (var kv in obj)
            {
                Traverse(kv.Value, onObject);
            }

            return;
        }

        if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                Traverse(item, onObject);
            }
        }
    }

    private static string? ReadString(JsonObject obj, string propertyName)
    {
        var val = obj[propertyName];
        if (val is null)
        {
            return null;
        }

        if (val is JsonValue v)
        {
            if (v.TryGetValue<string>(out var s))
            {
                return s;
            }

            return v.ToJsonString().Trim('"');
        }

        return val.ToJsonString().Trim('"');
    }
}
