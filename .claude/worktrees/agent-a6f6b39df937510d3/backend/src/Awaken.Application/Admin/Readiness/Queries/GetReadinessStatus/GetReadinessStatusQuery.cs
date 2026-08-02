using Awaken.Contracts.Admin.Readiness;
using MediatR;

namespace Awaken.Application.Admin.Readiness.Queries.GetReadinessStatus;

/// <summary>
/// US-218: consulta de readiness de configuração obrigatória, build mobile e CI por ambiente.
/// Sem parâmetros — sempre retorna todos os ambientes monitorados (dev, staging, produção).
/// </summary>
public record GetReadinessStatusQuery : IRequest<ReadinessStatusResponse>;
