namespace Nexora.Contracts.Devices;

/// <summary>Porta de <c>deviceSchema</c> (<c>packages/contracts/src/devices.ts</c>).</summary>
public sealed record DeviceResponse(
    Guid Id,
    string Label,
    string Kind,
    bool Active,
    DateTimeOffset? LastSeenAt,
    bool NeedsReview);
