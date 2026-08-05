using System.Text.Json;

namespace Nexora.Contracts.Devices;

/// <summary>Preferências completas do dispositivo após a mescla, já como objeto (não string) para o cliente consumir direto.</summary>
public sealed record DevicePreferencesResponse(Guid DeviceId, JsonElement Preferences);
