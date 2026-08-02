using System.Reflection;
using Nexora.Application.Abstractions.Platform;
using Microsoft.Extensions.Options;

namespace Nexora.Infrastructure.Platform;

/// <summary>Configuração — porta de <c>APP_VERSION</c>/<c>npm_package_version</c>.</summary>
public sealed class AppVersionOptions
{
    public const string SectionName = "App";

    public string? Version { get; set; }
}

/// <summary>
/// Versão do binário em execução. Prioridade: <c>App:Version</c> configurado explicitamente
/// (equivalente a <c>APP_VERSION</c>) e, na ausência, a versão do assembly em execução.
/// </summary>
public sealed class AppVersionProvider : IAppVersionProvider
{
    public AppVersionProvider(IOptions<AppVersionOptions> options)
    {
        CurrentVersion = !string.IsNullOrWhiteSpace(options.Value.Version)
            ? options.Value.Version
            : Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.1.0";
    }

    public string CurrentVersion { get; }
}
