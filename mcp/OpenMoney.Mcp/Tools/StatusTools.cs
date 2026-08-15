using System.ComponentModel;
using ModelContextProtocol.Server;
using Microsoft.Extensions.DependencyInjection;

namespace OpenMoney.Mcp.Tools;

[McpServerToolType]
public sealed class StatusTools(IServiceProvider services)
{
    [McpServerTool(Name = "openmoney_status")]
    [Description("Показать, какие SDK реально подключены к этому MCP по текущей конфигурации.")]
    public string Status()
    {
        var enabled = services.GetService<IReadOnlyDictionary<string, bool>>()
                      ?? new Dictionary<string, bool>();
        return McpJson.Ok(new
        {
            ok = true,
            configured = enabled,
            note = "Инструменты несконфигурированных провайдеров вернут ошибку с подсказкой по env/appsettings."
        });
    }

    [McpServerTool(Name = "openmoney_list_tools_help")]
    [Description("Краткий список боевых MCP‑инструментов OpenMoney по провайдерам.")]
    public string Help() => McpJson.Ok(new
    {
        TBank = new[] { "tbank_init_payin", "tbank_get_status", "tbank_cancel", "tbank_create_qr", "tbank_init_payout" },
        YooMoney = new[] { "yoomoney_create_safe_deal", "yoomoney_create_payment", "yoomoney_get_payment", "yoomoney_get_deal", "yoomoney_create_payout" },
        VTB = new[] { "vtb_start_payment" },
        CloudPayments = new[] { "cloudpayments_refund", "cloudpayments_void", "cloudpayments_confirm" },
        Inwizo = new[] { "inwizo_init_hosted_payment", "inwizo_payment_status", "inwizo_payout", "inwizo_payout_status" },
        Tochka = new[] { "tochka_create_recipient", "tochka_get_order", "tochka_create_order", "tochka_confirm_services" },
        Fiscal = new[] { "fiscal_check_taxpayer_status", "fiscal_start_sms", "fiscal_verify_sms" },
        SelfEmployed = new[] { "npd_list_recipients", "npd_sync_recipients" },
        Kyc = new[]
        {
            "kyc_moynalog_check_status",
            "kyc_didit_create_session", "kyc_didit_get_decision",
            "kyc_mts_start_si", "kyc_mts_submit_otp",
            "kyc_mts_rim_create_applicant", "kyc_mts_rim_start_identification", "kyc_mts_rim_get_identification"
        }
    });
}
