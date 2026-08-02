using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Stations;

namespace Nexora.Application.Stations.Commands.UpdateStation;

/// <summary>Atualiza uma praça de produção do tenant autenticado. Porta de <c>PATCH /v1/catalog/stations/:id</c>.</summary>
public sealed record UpdateStationCommand(
    Guid StationId,
    string? Name,
    string? Color,
    short? CapacitySlots,
    bool? IsBottleneck,
    short? Position) : ICommand<StationResponse>;
