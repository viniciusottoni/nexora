using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Devices.Commands.DeleteDevice;

/// <summary>
/// Exclui (soft delete) um dispositivo já revogado — some da listagem sem deletar fisicamente
/// (CLAUDE.md, "Soft delete sempre"). Recusado enquanto o dispositivo ainda estiver ativo: o
/// gestor precisa revogar o acesso antes de removê-lo da lista.
/// </summary>
public sealed record DeleteDeviceCommand(Guid DeviceId) : ICommand;
