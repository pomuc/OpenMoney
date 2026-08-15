# Быстрый старт

## Требования

- .NET SDK **8.x**
- Доступ к песочнице провайдера (ключи терминала / shop / mTLS — по необходимости)

## Подключить пакет

Из исходников репозитория:

```bash
dotnet add MyApp.csproj reference path/to/sdks/OpenMoney.TBank/src/OpenMoney.TBank/OpenMoney.TBank.csproj
```

Или после `dotnet pack`:

```bash
dotnet add package OpenMoney.TBank --source ./artifacts/nupkg
```

## Минимальный pay-in (Т‑Банк)

`appsettings.json`:

```json
{
  "TBank": {
    "TerminalKey": "YOUR_TERMINAL_KEY",
    "TerminalPassword": "YOUR_TERMINAL_PASSWORD"
  }
}
```

Регистрация:

```csharp
services.AddTBank(o =>
{
    o.TerminalKey = configuration["TBank:TerminalKey"]!;
    o.TerminalPassword = configuration["TBank:TerminalPassword"]!;
});
```

Вызов:

```csharp
var response = await client.InitPayInAsync(new RequestInitPaymentContext
{
    Amount = 10_000,           // копейки
    OrderId = "order-123",
    Description = "Оплата заказа"
}, cancellationToken);

// Отправьте пользователя на response.PaymentURL
```

## Дальше

1. [Конфигурация всех секций](configuration.md)
2. [Процесс приёма оплаты](processes/pay-in.md)
3. [Runnable-примеры](examples.md): `dotnet run --project examples/OpenMoney.SdkExamples -- tbank`
4. [MCP для агентов](mcp/overview.md)

## Правила безопасности

- Не коммитьте ключи, PEM, payout_token, строки БД.
- В примерах — только `YOUR_*`.
- PAN/CVV на ваш сервер не принимайте: для карт используйте криптограмму виджета / hosted‑форму провайдера.
- Выплаты и возвраты — только с явным подтверждением человека в продуктовом UI/агенте.
