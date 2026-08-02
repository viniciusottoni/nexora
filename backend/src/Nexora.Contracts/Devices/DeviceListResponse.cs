namespace Nexora.Contracts.Devices;

/// <summary>Porta de <c>deviceListResponseSchema</c> (<c>packages/contracts/src/devices.ts</c>).</summary>
public sealed record DeviceListResponse(IReadOnlyList<DeviceResponse> Items);
