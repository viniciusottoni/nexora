using System.Text.Json;

namespace Nexora.Contracts.Devices;

/// <summary>
/// Corpo de <c>PATCH /v1/devices/{id}/preferences</c> — objeto livre (ex.:
/// <c>{"kds":{"stationIds":["..."]}}</c>), mesclado (não substituído) sobre o que já existe. Ver
/// <c>Nexora.Application.Devices.Commands.UpdateDevicePreferences.UpdateDevicePreferencesCommand</c>.
/// </summary>
public sealed record UpdateDevicePreferencesRequest(JsonElement Preferences);
