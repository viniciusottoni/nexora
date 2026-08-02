namespace Nexora.Application.Installations.Abstractions;

/// <summary>
/// Digest determinístico do token de instalação de uso único (SHA-256 do token cru, mesma
/// operação de <c>createHash('sha256').update(rawToken).digest('hex')</c> no
/// <c>register-installation.service.ts</c> original).
/// <para>
/// Interface própria, deliberadamente <b>não</b> reaproveitando <c>ISecretDigester</c>
/// (<c>Nexora.Application.Abstractions.Security</c>): esse contrato já tem um registro de
/// DI ambíguo documentado entre <c>DeviceSecretDigester</c> (Devices) e o segredo de convite de
/// dono (Tenants) — ver o comentário de "ATENÇÃO — risco de colisão de DI" em
/// <c>Nexora.Infrastructure.Devices.DeviceSecretDigester</c>. Adicionar um terceiro
/// consumidor à mesma interface sem chave pioraria essa colisão silenciosamente.
/// </para>
/// </summary>
public interface IInstallationTokenDigester
{
    string Digest(string rawToken);
}
