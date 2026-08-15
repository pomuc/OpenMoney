# Ошибки

## Два слоя

1. **Транспорт / HTTP** — SDK бросает `*ApiException` / `HttpRequestException` (сеть, 4xx/5xx, пустое тело).
2. **Бизнес‑ответ при HTTP 200** — у Т‑Банка и части других API смотрите поля `Success`, `ErrorCode`, `Message`, `Details`.

Пример (TBank):

```csharp
try
{
    var r = await client.InitPayInAsync(ctx, ct);
    if (!r.Success)
        // бизнес-отказ терминала, не обязательно exception
        throw new InvalidOperationException($"{r.ErrorCode}: {r.Message}");
}
catch (TBankApiException ex)
{
    // HTTP / разбор ответа
}
```

## По пакетам

| Пакет | Тип исключения / проверка |
|---|---|
| TBank | `TBankApiException` + `Success` в ответе |
| Tochka | `TochkaApiException` (status + body) |
| VTB | HTTP / пустой redirect |
| CloudPayments | `Success` / `Message` в `CloudPaymentsResponse<T>` |
| Inwizo | `ErrorCode` / `ErrorMessage` в результате операции |
| YooMoney | `Success` в create‑результатах; HTTP‑ошибки — exception |
| Fiscal / Kyc | `FiscalApiException` / `KycApiException` |
| SelfEmployed | `NpdApiException` |

## Практика

- Не логируйте полные тела ответов с PII и токенами.
- Для агентов (MCP) ошибки сериализуются в JSON `{ ok: false, error, … }` без stack‑trace секретов.
- Повторы (retry) — только для идемпотентных GET/status и явно безопасных POST (с тем же ключом идемпотентности).
