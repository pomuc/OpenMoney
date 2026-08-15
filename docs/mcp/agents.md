# MCP: подключение агентов

Готовые файлы: [`mcp/examples/`](../../mcp/examples/).

| Агент | Файл |
|---|---|
| Claude Desktop | `claude_desktop_config.json` |
| Cursor | `cursor_mcp.json` |
| Codex | `codex_config.toml` |
| VS Code / Copilot | `vscode_mcp.json` |
| GigaChat | `gigachat_bridge.md` |
| Алиса | `alice_skill.md` |

## Cursor (кратко)

В MCP config:

```json
{
  "mcpServers": {
    "openmoney": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "${workspaceFolder}/mcp/OpenMoney.Mcp/OpenMoney.Mcp.csproj",
        "-c",
        "Release"
      ],
      "env": {
        "TBank__TerminalKey": "",
        "TBank__TerminalPassword": "",
        "YooMoney__ShopId": "",
        "YooMoney__SecretKey": ""
      }
    }
  }
}
```

Замените пустые env на значения из secret store (или заполните `appsettings.json` MCP).

## Важно

- Логи MCP идут в **stderr**, stdout — только JSON‑RPC.
- Путь `YOUR_OPENMONEY_ROOT` замените на абсолютный путь клоны, если нет `${workspaceFolder}`.
