using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Devices;

namespace Nexora.Application.Devices.Commands.CreatePairingCode;

/// <summary>
/// Gera um código de pareamento de uso único (10 min de validade) para um novo dispositivo se
/// registrar na loja do gestor autenticado. Porta de <c>DeviceRegistry.createPairingCode</c>
/// (<c>packages/domain/src/devices/device-registry.ts</c>).
///
/// Sem parâmetros de entrada: tenant, loja e gestor vêm de <c>ICurrentTenantContext</c> —
/// nunca do cliente (doc. 05, "tenant nunca vem do cliente"). Sem validator: não há campo de
/// entrada a validar — isenção documentada aqui, conforme "Como validar" do ADR-037.
/// </summary>
public sealed record CreatePairingCodeCommand : ICommand<CreatePairingCodeResponse>;
