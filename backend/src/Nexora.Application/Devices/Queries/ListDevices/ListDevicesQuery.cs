using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Devices;

namespace Nexora.Application.Devices.Queries.ListDevices;

/// <summary>
/// Porta de <c>DeviceRegistry.list</c> (<c>device-registry.ts</c>) — lista todos os
/// dispositivos do tenant (todas as lojas), sinalizando <c>NeedsReview</c> para o dispositivo
/// sem heartbeat há mais de 30 dias.
/// </summary>
public sealed record ListDevicesQuery : IQuery<DeviceListResponse>;
