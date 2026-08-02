using Awaken.Contracts.Admin.Security;
using MediatR;

namespace Awaken.Application.Admin.Security.Queries.GetSecuritySummary;

/// <summary>
/// US-219: resumo preventivo agregado de segurança (tendências, série temporal,
/// agrupamentos e indicadores mínimos da seção 7 da US).
/// Quando From/To não são informados, a janela padrão é "últimas 24 horas" (RN-003:
/// uma janela curta torna picos repentinos mais visíveis).
/// </summary>
public record GetSecuritySummaryQuery(DateTime? From, DateTime? To) : IRequest<SecuritySummaryResponse>;
