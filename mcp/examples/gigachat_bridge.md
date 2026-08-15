# GigaChat ↔ OpenMoney

У публичного GigaChat (чат / API) **нет встроенного MCP stdio**, как у Claude Desktop / Cursor.

## Рабочие варианты

### 1. Ваш бэкенд + function calling

Опишите функции‑зеркала MCP tools (`openmoney_pick_provider`, `openmoney_scenario_guide`, …) в схеме tools GigaChat API и реализуйте их вызовом тех же хелперов / SDK OpenMoney.

```text
Пользователь → GigaChat (tool call) → ваш API / OpenMoney.Mcp → OpenMoney.* SDK (боевые вызовы)
```

### 2. OpenWebUI / LiteLLM / MCP‑bridge

Поднимите MCP‑совместимый шлюз, который запускает `OpenMoney.Mcp` по stdio, а к GigaChat ходит как к LLM с tools.

### 3. Только код‑ассистент

Для написания интеграций удобнее Cursor/Claude/Codex с этим MCP; GigaChat используйте для пользовательских диалогов поверх уже собранного API.

## Пример tool‑схемы (идея)

```json
{
  "name": "openmoney_pick_provider",
  "description": "Какой пакет OpenMoney взять под задачу",
  "parameters": {
    "type": "object",
    "properties": {
      "task": { "type": "string" }
    },
    "required": ["task"]
  }
}
```

Секреты банков — только на бэкенде, не в промпте GigaChat.
