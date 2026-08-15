# Песочница и дисклеймер

## Дисклеймер

OpenMoney — community‑экосистема. Пакеты **не являются** официальными SDK банков и провайдеров и **не аффилированы** с ними. API провайдеров могут меняться без уведомления.

Перед продакшеном:

1. Сверьте контракты с актуальной официальной документацией провайдера.
2. Пройдите бой на UAT / sandbox.
3. Включите мониторинг ошибок и алерты по payout / refund.

## Песочницы

| Пакет | Как включить sandbox / UAT |
|---|---|
| TBank | Тестовые TerminalKey/Password из кабинета |
| VTB | `BaseUrl = https://vtb.rbsuat.com/payment` |
| Tochka | `EnableSandboxOperations = true` только на UAT |
| YooMoney | Тестовый магазин ЮKassa |
| SelfEmployed | `TBankNpd:UseSandbox = true` |
| CloudPayments / Inwizo | Тестовые credentials кабинета |

В проде:

- Tochka: `EnableSandboxOperations = false`
- VTB: боевой `https://platezh.vtb24.ru/payment`
- SelfEmployed: `UseSandbox = false` + боевые mTLS‑сертификаты
