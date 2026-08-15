# Алиса ↔ OpenMoney

Навыки Алисы (Яндекс Диалоги) **не подключают MCP stdio** напрямую.

## Рекомендуемая схема

```text
Пользователь → Алиса (voice/text)
            → webhook вашего Skill backend (HTTPS)
            → OpenMoney.* SDK (песочница / прод с vault)
```

## Что вынести в skill backend

| Интент пользователя | SDK |
|---|---|
| «создай ссылку на оплату» | TBank / YooMoney / VTB / Inwizo |
| «безопасная сделка и выплата» | **OpenMoney.YooMoney** |
| «статус самозанятого / чек» | Fiscal / SelfEmployed |
| «проверь паспорт / KYC» | OpenMoney.Kyc |

## Для разработки навыка

1. Соберите сценарий через MCP в Cursor/Claude (`openmoney_scenario_guide`).
2. Перенесите DI‑сниппет в ASP.NET webhook навыка.
3. В Алисе держите только UX‑фразы; деньги и PII — на бэкенде.

Не кладите ShopId/SecretKey в код навыка или в ответы Алисы.
