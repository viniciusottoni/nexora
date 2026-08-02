using Awaken.Contracts.Admin.Media;
using MediatR;

namespace Awaken.Application.Admin.Media.Queries.GetMediaDiagnostics;

/// <summary>
/// US-222: cards de cobertura de mídia/CDN do catálogo de exercícios.
/// Sem parâmetros — agrega sobre uma amostra do catálogo (ver handler para o critério).
/// </summary>
public record GetMediaDiagnosticsQuery : IRequest<MediaCoverageResponse>;
