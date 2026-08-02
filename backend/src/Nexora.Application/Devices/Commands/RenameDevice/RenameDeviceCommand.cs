using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Devices.Commands.RenameDevice;

/// <summary>
/// Porta de <c>DeviceRegistry.rename</c> (<c>device-registry.ts</c>). Sem retorno de dado —
/// 204 no sucesso, como no TS original (<c>@HttpCode(204)</c>).
/// </summary>
public sealed record RenameDeviceCommand(Guid DeviceId, string Label) : ICommand;
