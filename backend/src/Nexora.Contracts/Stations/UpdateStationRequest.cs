namespace Nexora.Contracts.Stations;

/// <summary>
/// Corpo de <c>PATCH /v1/catalog/stations/:id</c> — todos os campos opcionais, só o que for
/// enviado é alterado (mesmo padrão de <c>UpdateRoleRequest</c>). <see cref="IsBottleneck"/> igual
/// a <c>true</c> desmarca qualquer outra praça marcada como gargalo no mesmo tenant/loja (US-017
/// §10: "o gargalo é, por definição, um só").
/// </summary>
public sealed record UpdateStationRequest(
    string? Name,
    string? Color,
    short? CapacitySlots,
    bool? IsBottleneck,
    short? Position);
