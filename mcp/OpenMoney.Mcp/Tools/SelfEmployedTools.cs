using System.ComponentModel;
using ModelContextProtocol.Server;
using OpenMoney.SelfEmployed;
using OpenMoney.SelfEmployed.Models;

namespace OpenMoney.Mcp.Tools;

[McpServerToolType]
public sealed class SelfEmployedTools(IServiceProvider services)
{
    [McpServerTool(Name = "npd_list_recipients")]
    [Description("Т‑Банк НПД: страница реестра самозанятых получателей.")]
    public Task<string> ListRecipients(int offset = 0, int limit = 50, CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<NpdClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("SelfEmployed", "TBankNpd__Token"));

        return McpJson.RunAsync(async () =>
            (object)await client.ListRecipientsAsync(
                    new RecipientsListRequest { Offset = offset, Limit = limit },
                    cancellationToken)
                .ConfigureAwait(false));
    }

    [McpServerTool(Name = "npd_sync_recipients")]
    [Description("Т‑Банк НПД: выгрузить все страницы получателей во внутренний store MCP.")]
    public Task<string> SyncRecipients(CancellationToken cancellationToken = default)
    {
        var client = McpJson.Get<NpdClient>(services);
        if (client is null)
            return Task.FromResult(McpJson.NotConfigured("SelfEmployed", "TBankNpd__Token"));

        return McpJson.RunAsync(async () =>
        {
            var processed = await client.CheckNpdAsync(cancellationToken).ConfigureAwait(false);
            return (object)new { processed };
        });
    }
}
