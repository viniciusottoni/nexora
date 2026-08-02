using MediatR;

namespace Awaken.Application.Admin.Security.Commands.MarkAlertAnalyzed;

/// <summary>
/// US-165 RN-004: marca um alerta de segurança como analisado.
/// Toda alteração de status de um alerta CRITICAL deve gerar auditoria.
/// </summary>
public record MarkAlertAnalyzedCommand(Guid AlertId, string? Note) : IRequest<Unit>;
