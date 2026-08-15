using System.Collections.Concurrent;
using OpenMoney.SelfEmployed;
using OpenMoney.SelfEmployed.Models;

namespace OpenMoney.SdkExamples;

internal sealed class MemoryNpdRecipientStore : INpdRecipientStore
{
    private readonly ConcurrentDictionary<long, SelfEmployedRecipient> _items = new();

    public Task UpsertRecipientsAsync(IReadOnlyList<SelfEmployedRecipient> recipients, CancellationToken cancellationToken = default)
    {
        foreach (var r in recipients)
            _items[r.Id] = r;
        return Task.CompletedTask;
    }
}

internal sealed class MemoryNpdReceiptStore : INpdReceiptStore
{
    private readonly ConcurrentDictionary<string, SelfEmployedReceiptRecord> _items = new(StringComparer.Ordinal);

    public Task<bool> ExistsAsync(string externalOperationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.ContainsKey(externalOperationId));

    public Task SaveAsync(SelfEmployedReceiptRecord record, CancellationToken cancellationToken = default)
    {
        _items[record.ExternalOperationId] = record;
        return Task.CompletedTask;
    }
}
