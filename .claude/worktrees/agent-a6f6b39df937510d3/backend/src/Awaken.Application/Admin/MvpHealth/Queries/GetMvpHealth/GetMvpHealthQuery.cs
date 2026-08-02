using Awaken.Contracts.Admin.MvpHealth;
using MediatR;

namespace Awaken.Application.Admin.MvpHealth.Queries.GetMvpHealth;

/// <summary>
/// US-216: consulta de saúde consolidada do MVP — agrega sinais de todos os domínios operacionais.
/// Sem parâmetros — sempre retorna todos os domínios monitorados.
/// </summary>
public record GetMvpHealthQuery : IRequest<MvpHealthStatusResponse>;
