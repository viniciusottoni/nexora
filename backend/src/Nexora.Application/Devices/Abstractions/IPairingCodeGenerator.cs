namespace Nexora.Application.Devices.Abstractions;

/// <summary>
/// Gera o código de pareamento de 6 dígitos mostrado ao gestor para registrar um novo
/// dispositivo — porta de <c>pairingCode: () => randomInt(0, 1_000_000)...</c> em
/// <c>apps/api-edge/src/modules/devices/devices.module.ts</c>. Não é um identificador
/// (ADR-016 não se aplica) — é um código de apresentação, curto e de uso único.
/// </summary>
public interface IPairingCodeGenerator
{
    /// <summary>Sempre uma string de exatamente 6 dígitos numéricos (ex.: "042317").</summary>
    string Generate();
}
