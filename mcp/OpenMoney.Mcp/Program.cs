using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenMoney.Mcp;
using OpenMoney.Mcp.Tools;

var builder = Host.CreateApplicationBuilder(args);

// MCP stdio: все логи — только в stderr, иначе ломается JSON-RPC на stdout.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Logging.SetMinimumLevel(LogLevel.Information);

var enabled = SdkBootstrap.RegisterConfiguredSdks(builder.Services, builder.Configuration);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<StatusTools>()
    .WithTools<TBankTools>()
    .WithTools<YooMoneyTools>()
    .WithTools<VtbTools>()
    .WithTools<CloudPaymentsTools>()
    .WithTools<InwizoTools>()
    .WithTools<TochkaTools>()
    .WithTools<FiscalTools>()
    .WithTools<SelfEmployedTools>()
    .WithTools<KycTools>();

var host = builder.Build();
var log = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("OpenMoney.Mcp");
log.LogInformation(
    "OpenMoney MCP ready. Live SDKs: {Sdks}",
    string.Join(", ", enabled.Where(kv => kv.Value).Select(kv => kv.Key)));

await host.RunAsync();
