using MediatR;

namespace Awaken.Application.Admin.Security.Commands.AddAlertNote;

/// <summary>US-219: adiciona/atualiza a nota de triagem de um alerta de segurança. Gera auditoria (RN-004).</summary>
public record AddAlertNoteCommand(Guid AlertId, string Note) : IRequest<Unit>;
