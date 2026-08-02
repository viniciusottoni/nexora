namespace Nexora.Contracts.Devices;

/// <summary>Porta de <c>pairDeviceResponseSchema</c> (<c>packages/contracts/src/devices.ts</c>).</summary>
public sealed record PairedDeviceInfo(Guid Id, string Label);

/// <summary>
/// <see cref="DeviceSecret"/> é mostrado uma única vez — o servidor nunca guarda o valor em
/// claro (só o hash, ver <c>ISecretDigester</c>).
/// </summary>
public sealed record PairDeviceResponse(PairedDeviceInfo Device, string DeviceSecret);
