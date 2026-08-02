using Awaken.Contracts.Admin.Media;
using MediatR;

namespace Awaken.Application.Admin.Media.Queries.GetMediaDiagnosticsList;

/// <summary>
/// US-222: lista paginada de exercícios do catálogo com diagnóstico de mídia, com filtros por
/// ambiente, dificuldade, equipamento, região muscular e status de mídia.
/// Todos os filtros são opcionais.
/// </summary>
public record GetMediaDiagnosticsListQuery(
    string? Environment,
    string? DifficultyLevel,
    string? EquipmentCategory,
    string? PrimaryRegion,
    string? MediaStatus,
    int Page,
    int PageSize) : IRequest<MediaDiagnosticsListResponse>;
