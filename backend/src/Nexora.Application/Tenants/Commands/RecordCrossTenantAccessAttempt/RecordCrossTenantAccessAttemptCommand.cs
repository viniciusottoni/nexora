using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Tenants.Commands.RecordCrossTenantAccessAttempt;

/// <summary>
/// Registra em <c>audit_log</c> a tentativa de um usuário autenticado de um tenant acessar, por
/// ID exato, um recurso de plataforma de outro tenant (US-001, cenário Gherkin "Tentativa de
/// acesso cruzado por ID"). A resposta HTTP para o chamador continua 404 — nunca 403 — para não
/// confirmar a existência do recurso alvo (ADR-021); este comando só existe para o efeito
/// colateral de auditoria, por isso não tem retorno de dado.
/// </summary>
/// <param name="ActorTenantId">
/// Tenant do usuário autenticado que fez a tentativa — dono do registro de auditoria (RLS exige
/// que <c>tenant_id</c> gravado bata com <c>app.tenant_id</c> da conexão corrente, ADR-004).
/// </param>
/// <param name="ActorUserId">Usuário autenticado que fez a tentativa, quando disponível.</param>
/// <param name="TargetTenantId">Id do tenant alheio que foi informado na rota.</param>
/// <param name="Ip">IP de origem da requisição, para o campo <c>audit_log.ip</c>.</param>
public sealed record RecordCrossTenantAccessAttemptCommand(
    Guid ActorTenantId,
    Guid? ActorUserId,
    Guid TargetTenantId,
    string? Ip) : ICommand;
