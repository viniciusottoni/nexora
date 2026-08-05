using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Platform;

namespace Nexora.Application.Tenants.Commands.RecordSupportAccess;

/// <summary>
/// E-09/US-090 → estendido por US-145 "Acesso de suporte auditado" — a única exceção autorizada
/// ao isolamento da RN-015. Grava a concessão em <c>support_access</c> (token de escopo especial
/// com expiração curta, revogável pelo cliente), registra em <c>audit_log</c> e emite EVT-074
/// <c>support.access.granted</c> quando a Replay (papel <c>platform_admin</c>) solicita acesso aos
/// dados de um tenant. O registro é gravado no tenant ALVO (não existe "tenant do ator" — quem
/// chama é administração de plataforma, sem estabelecimento próprio) para que fique visível ao
/// cliente quando ele consultar a própria trilha (US-091) ou o histórico dedicado
/// (<c>GET /v1/tenant/support-access-history</c>, US-145 §10).
/// </summary>
/// <param name="TenantId">Tenant cujos dados serão acessados.</param>
/// <param name="SupportUserId">
/// Identificador do usuário de plataforma que solicitou o acesso, quando disponível (claim
/// <c>sub</c> do token com policy <c>PlatformAdmin</c>).
/// </param>
/// <param name="Reason">Motivo do acesso — sempre exigido, nunca acesso de suporte silencioso.</param>
/// <param name="DurationMinutes">Duração concedida — define <c>SupportAccess.ExpiresAt</c> e o payload do evento (EVT-074).</param>
public sealed record RecordSupportAccessCommand(
    Guid TenantId,
    Guid? SupportUserId,
    string Reason,
    int DurationMinutes) : ICommand<GrantSupportAccessResponse>;
