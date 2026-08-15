using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace OpenMoney.Mcp.Tools;

internal static class McpJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static string Ok(object value) => JsonSerializer.Serialize(value, Options);

    public static string Error(string message, object? details = null) =>
        JsonSerializer.Serialize(new { ok = false, error = message, details }, Options);

    public static string NotConfigured(string provider, params string[] envHints) =>
        JsonSerializer.Serialize(new
        {
            ok = false,
            error = $"Провайдер {provider} не сконфигурирован.",
            hint = "Задайте значения в appsettings.json рядом с OpenMoney.Mcp или через переменные окружения (секции через __).",
            examples = envHints
        }, Options);

    public static async Task<string> RunAsync(Func<Task<object>> action)
    {
        try
        {
            var result = await action().ConfigureAwait(false);
            return Ok(new { ok = true, result });
        }
        catch (Exception ex)
        {
            return Error(ex.Message, new { type = ex.GetType().Name });
        }
    }

    public static T? Get<T>(IServiceProvider sp) where T : class => sp.GetService<T>();
}
