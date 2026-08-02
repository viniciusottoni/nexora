namespace Nexora.Contracts.Devices;

/// <summary>Porta de <c>createPairingCodeResponseSchema</c> (<c>packages/contracts/src/devices.ts</c>).</summary>
public sealed record CreatePairingCodeResponse(string Code, DateTimeOffset ExpiresAt, int ExpiresInSeconds);
