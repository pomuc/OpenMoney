# Документация OpenMoney

Русскоязычная документация для разработчиков: подключение SDK, конфигурация и **бизнес‑процессы** (оплата, выплаты, безопасная сделка, СБП, НПД, фискализация, KYC).

> Пакеты **неофициальные** и не аффилированы с банками и провайдерами. Перед боем — песочница и официальная документация провайдера.

## С чего начать

1. [Быстрый старт](getting-started.md)
2. [Справочник конфигурации](configuration.md)
3. Выберите процесс из таблицы ниже
4. При необходимости — [пакет](packages/) и [примеры кода](examples.md)

## Карта: процесс → пакет

| Процесс | Пакеты | Документ |
|---|---|---|
| Приём оплаты (pay-in) | TBank, VTB, Inwizo, CloudPayments, Tochka | [pay-in](processes/pay-in.md) |
| Выплата (payout) | TBank E2C, Inwizo, YooMoney | [payout](processes/payout.md) |
| Безопасная сделка | **TBank**, **Tochka**, YooMoney (+ Inwizo) | [safe-deal](processes/safe-deal.md) |
| СБП / QR | TBank, VTB, Inwizo | [sbp-qr](processes/sbp-qr.md) |
| Реестр выплат самозанятым | SelfEmployed | [npd-registry](processes/npd-registry.md) |
| Доход «Мой налог» / CloudKassir | Fiscal, CloudPayments, TBank paycheck | [fiscal-income](processes/fiscal-income.md) |
| Идентификация (KYC) | Kyc | [kyc-session](processes/kyc-session.md) |

## Разделы

### Основы

- [Быстрый старт](getting-started.md)
- [Конфигурация](configuration.md)
- [Суммы и идентификаторы](concepts/amounts-and-ids.md)
- [Песочница и дисклеймер](concepts/unofficial-and-sandbox.md)
- [Ошибки](concepts/errors.md)

### Процессы

- [Приём оплаты](processes/pay-in.md)
- [Выплаты](processes/payout.md)
- [Безопасная сделка](processes/safe-deal.md)
- [СБП и QR](processes/sbp-qr.md)
- [Реестр НПД](processes/npd-registry.md)
- [Фискализация дохода](processes/fiscal-income.md)
- [KYC](processes/kyc-session.md)

### Пакеты SDK

- [TBank](packages/tbank.md)
- [Tochka](packages/tochka.md)
- [VTB](packages/vtb.md)
- [CloudPayments](packages/cloudpayments.md)
- [Inwizo](packages/inwizo.md)
- [YooMoney](packages/yoomoney.md)
- [Fiscal](packages/fiscal.md)
- [SelfEmployed](packages/selfemployed.md)
- [Kyc](packages/kyc.md)

### MCP и примеры

- [MCP: обзор](mcp/overview.md)
- [MCP: справочник tools](mcp/tools-reference.md)
- [MCP: подключение агентов](mcp/agents.md)
- [Примеры SdkExamples](examples.md)

### Разработка

- [Сборка и локальная разработка](development.md)
- [CI](CI.md)
- [Участие в проекте](../CONTRIBUTING.md)
