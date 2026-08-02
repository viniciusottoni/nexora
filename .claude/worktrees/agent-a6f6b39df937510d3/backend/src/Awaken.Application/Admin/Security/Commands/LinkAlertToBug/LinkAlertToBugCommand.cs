using MediatR;

namespace Awaken.Application.Admin.Security.Commands.LinkAlertToBug;

/// <summary>US-219: vincula um alerta de segurança a um bug/incidente operacional já existente. Gera auditoria (RN-004).</summary>
public record LinkAlertToBugCommand(Guid AlertId, Guid BugId) : IRequest<Unit>;
