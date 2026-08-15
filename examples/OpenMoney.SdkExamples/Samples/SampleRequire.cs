using Microsoft.Extensions.DependencyInjection;

namespace OpenMoney.SdkExamples.Samples;

internal static class SampleRequire
{
    public static T Get<T>(IServiceProvider sp, string provider, params string[] envHints) where T : class
    {
        var service = sp.GetService<T>();
        if (service is not null)
            return service;

        var hint = envHints.Length == 0
            ? ""
            : " Задайте: " + string.Join(", ", envHints);
        throw new InvalidOperationException(
            $"Провайдер {provider} не сконфигурирован.{hint}");
    }
}
