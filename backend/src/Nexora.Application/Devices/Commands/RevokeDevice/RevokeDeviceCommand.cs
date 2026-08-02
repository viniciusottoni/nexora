using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Devices.Commands.RevokeDevice;

/// <summary>
/// Porta de <c>DeviceRegistry.revoke</c> (<c>device-registry.ts</c>) — desativa o dispositivo
/// e encerra todas as suas sessões de autenticação ativas na mesma transação.
/// </summary>
public sealed record RevokeDeviceCommand(Guid DeviceId) : ICommand;
