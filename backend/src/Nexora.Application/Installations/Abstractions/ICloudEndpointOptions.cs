namespace Nexora.Application.Installations.Abstractions;

/// <summary>
/// URL pública da nuvem, usada para montar <c>syncEndpoint</c> na resposta de registro de
/// instalação (mesmo papel de <c>CLOUD_PUBLIC_URL</c> em <c>prisma-installation-registration.repository.ts</c>).
/// Implementado em Infrastructure a partir de <c>IOptions&lt;T&gt;</c> — Application não referencia
/// o mecanismo de configuração do ASP.NET Core diretamente (ADR-039).
/// </summary>
public interface ICloudEndpointOptions
{
    /// <summary>Sem barra final (ex.: <c>https://cloud.donabetinha.com.br</c>).</summary>
    string PublicUrl { get; }
}
