# MCP: справочник tools

Ответ tools — JSON. Успех: `{ "ok": true, "result": … }`. Ошибка конфигурации: `{ "ok": false, "error", "hint", "examples": […] }`.

## Статус

| Tool | Назначение |
|---|---|
| `openmoney_status` | Карта сконфигурированных SDK |
| `openmoney_list_tools_help` | Краткий список имён tools |

## TBank

| Tool | Суть |
|---|---|
| `tbank_init_payin` | Init оплаты (копейки, orderId, URL) |
| `tbank_get_status` | Статус по PaymentId |
| `tbank_cancel` | Отмена/возврат |
| `tbank_create_qr` | QR для PaymentId |
| `tbank_init_payout` | Init E2C выплаты |

Env: `TBank__TerminalKey`, `TBank__TerminalPassword`.

## YooMoney

| Tool | Суть |
|---|---|
| `yoomoney_create_safe_deal` | Создать safe_deal |
| `yoomoney_create_payment` | Платёж к сделке |
| `yoomoney_get_payment` / `yoomoney_get_deal` | Статусы |
| `yoomoney_create_payout` | Выплата (нужен payout token) |

Env: `YooMoney__ShopId`, `YooMoney__SecretKey`.

## VTB / CloudPayments / Inwizo / Tochka

| Tool | Провайдер |
|---|---|
| `vtb_start_payment` | ВТБ RBS start (карта/СБП) |
| `cloudpayments_confirm` / `_refund` / `_void` | CloudPayments |
| `inwizo_init_hosted_payment` / `_payment_status` / `_payout` / `_payout_status` | Inwizo |
| `tochka_create_recipient` / `_create_order` / `_get_order` / `_confirm_services` | Точка |

## Fiscal / NPD / KYC

| Tool | Суть |
|---|---|
| `fiscal_check_taxpayer_status` | Статус НПД по ИНН |
| `fiscal_start_sms` / `fiscal_verify_sms` | SMS‑login ФНС |
| `npd_list_recipients` / `npd_sync_recipients` | Реестр самозанятых |
| `kyc_moynalog_check_status` | KYC статус НПД |
| `kyc_didit_create_session` / `kyc_didit_get_decision` | Didit |
| `kyc_mts_start_si` / `kyc_mts_submit_otp` | MTS ID |
| `kyc_mts_rim_*` | MTS RIM applicant / identification |

Связанные процессы: [docs/processes](../processes/pay-in.md).
