using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Devices;

namespace Nexora.Application.Devices.Commands.PairDevice;

/// <summary>
/// Consome um código de pareamento e registra um novo dispositivo na loja do edge corrente.
/// Endpoint público (sem JWT) — o servidor edge resolve tenant/loja pela própria identidade
/// fixa (ADR-004, "uma loja = um tenant" no edge), não pelo chamador, que ainda não tem
/// credencial nenhuma. Porta de <c>DeviceRegistry.pair</c>
/// (<c>packages/domain/src/devices/device-registry.ts</c>).
/// </summary>
public sealed record PairDeviceCommand(
    string Code,
    string Label,
    string Kind,
    string Fingerprint) : ICommand<PairDeviceResponse>, IPersistsStateOnFailureCommand;
