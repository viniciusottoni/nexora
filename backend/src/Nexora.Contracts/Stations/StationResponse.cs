namespace Nexora.Contracts.Stations;

/// <summary>Praça de produção, com a contagem de produtos vinculados para a UI (US-017 §10).</summary>
public sealed record StationResponse(
    Guid Id,
    string Code,
    string Name,
    string? Color,
    short? CapacitySlots,
    bool IsBottleneck,
    short Position,
    bool IsActive,
    int LinkedProductCount);

public sealed record StationListResponse(IReadOnlyList<StationResponse> Items);
