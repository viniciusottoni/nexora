using MediatR;

namespace Awaken.Application.Admin.Security.Commands.ClassifyAlert;

/// <summary>
/// US-219 RN-005: classifica um alerta de segurança (ex.: "false_positive", "confirmed").
/// IMPORTANTE: esta ação NUNCA apaga/remove o alerta — apenas altera sua classificação.
/// Toda execução gera auditoria (RN-004).
/// </summary>
public record ClassifyAlertCommand(Guid AlertId, string Classification, string? Note) : IRequest<Unit>;
