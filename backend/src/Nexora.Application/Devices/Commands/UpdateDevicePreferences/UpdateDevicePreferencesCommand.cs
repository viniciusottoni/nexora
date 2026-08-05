using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Devices;

namespace Nexora.Application.Devices.Commands.UpdateDevicePreferences;

/// <summary>
/// Porta de <c>PATCH /v1/devices/{id}/preferences</c> — usada por US-042 (filtro de praça do KDS),
/// US-045 (som) e US-047 (modo pico), cada uma gravando sua própria sub-chave dentro de
/// <c>kds</c> (ver <see cref="Nexora.Application.Devices.Support.DevicePreferencesJsonMerger"/>
/// para por que é mescla, não substituição). <see cref="PreferencesPatchJson"/> é o corpo cru da
/// requisição já serializado — o Application nunca precisa conhecer o formato interno de cada
/// feature, só mesclar objetos JSON.
/// </summary>
public sealed record UpdateDevicePreferencesCommand(Guid DeviceId, string PreferencesPatchJson) : ICommand<DevicePreferencesResponse>;
