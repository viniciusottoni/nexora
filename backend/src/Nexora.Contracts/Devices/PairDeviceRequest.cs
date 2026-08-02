namespace Nexora.Contracts.Devices;

/// <summary>
/// Porta de <c>pairDeviceRequestSchema</c> (<c>packages/contracts/src/devices.ts</c>).
/// Endpoint público (sem JWT) — o dispositivo novo ainda não tem credencial; ADR-020 ainda
/// exige <c>Idempotency-Key</c> por ser POST.
/// </summary>
public sealed record PairDeviceRequest(string Code, string Label, string Kind, string Fingerprint);
