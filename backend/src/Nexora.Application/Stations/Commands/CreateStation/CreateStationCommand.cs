using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Stations;

namespace Nexora.Application.Stations.Commands.CreateStation;

/// <summary>Cria uma praça de produção no tenant/loja autenticados. Porta de <c>POST /v1/catalog/stations</c>.</summary>
public sealed record CreateStationCommand(
    string Code,
    string Name,
    string? Color,
    short? CapacitySlots,
    bool IsBottleneck,
    short Position) : ICommand<StationResponse>;
