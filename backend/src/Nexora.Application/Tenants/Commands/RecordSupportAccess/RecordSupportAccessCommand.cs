using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Tenants.Commands.RecordSupportAccess;

/// <summary>
/// E-09/US-090, cenário Gherkin "Acesso de suporte da plataforma" — registra em <c>audit_log</c> e
/// emite EVT-074 <c>support.access.granted</c> quando a Replay (papel <c>platform_admin</c>)
/// acessa dados de um tenant que não é o seu. O registro é gravado no tenant ALVO (não existe
/// "tenant do ator" — quem chama é administração de plataforma, sem estabelecimento próprio) para
/// que fique visível ao cliente quando ele consultar a própria trilha (US-091) — é isso que torna
/// o acesso de suporte auditado, não apenas logado internamente.
/// </summary>
/// <param name="TenantId">Tenant cujos dados foram acessados.</param>
/// <param name="SupportUserId">
/// Identificador do usuário de plataforma que acessou, quando disponível (claim <c>sub</c> do
/// token com policy <c>PlatformAdmin</c>).
/// </param>
/// <param name="Reason">Motivo do acesso — sempre exigido, nunca acesso de suporte silencioso.</param>
/// <param name="DurationMinutes">Duração planejada do acesso, para o payload do evento (EVT-074).</param>
public sealed record RecordSupportAccessCommand(
    Guid TenantId,
    Guid? SupportUserId,
    string Reason,
    int DurationMinutes) : ICommand;
