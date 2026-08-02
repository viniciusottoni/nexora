namespace Nexora.Contracts.Stations;

/// <summary>Corpo de <c>POST /v1/catalog/stations</c> (US-017 §7).</summary>
public sealed record CreateStationRequest(
    string Code,
    string Name,
    string? Color,
    short? CapacitySlots,
    bool IsBottleneck,
    short Position);
