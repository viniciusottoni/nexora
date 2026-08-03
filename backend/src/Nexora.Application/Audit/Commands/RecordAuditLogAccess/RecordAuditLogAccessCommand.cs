using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Audit.Commands.RecordAuditLogAccess;

/// <summary>
/// US-091, cenário Gherkin "Acesso restrito e registrado" — "quando um autorizado acessar, o
/// acesso deve ser registrado". Comando separado da <c>GetAuditLogQuery</c> porque
/// <c>TransactionBehavior</c> nunca chama <c>SaveChangesAsync</c> para uma <em>query</em>
/// (ADR-006, "queries nunca escrevem") — o controller dispara os dois na mesma requisição, mesmo
/// padrão de <c>TenantsController.Get</c> (query + <c>RecordCrossTenantAccessAttemptCommand</c>).
/// </summary>
public sealed record RecordAuditLogAccessCommand(IReadOnlyDictionary<string, string?> Filters) : ICommand;
