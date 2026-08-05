using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Platform;

namespace Nexora.Application.Platform.Queries.ListSupportAccessReport;

/// <summary>
/// US-145 §11 "Acessos de suporte por tenant e por período" — porta de
/// <c>GET /v1/platform/support-access</c> (só <c>PlatformAdmin</c>). Sem <see cref="TenantId"/>,
/// varre todos os estabelecimentos (mesmo custo O(tenants) documentado em
/// <c>EmailOutboxDeliveryWorker</c> — <c>support_access</c> tem RLS com <c>USING</c>, então não há
/// como consultar todos os tenants em uma única query sem o papel de banco <c>platform_admin</c>
/// (BYPASSRLS), hoje criado pela migration mas ainda sem nenhuma conexão da aplicação usando-o —
/// mesma limitação, mesmo caminho de evolução futura já registrado naquele worker). Simplificação
/// de MVP aceita para esta tarefa: sem paginação por cursor, resultado limitado a
/// <see cref="MaxRows"/> linhas mais recentes.
/// </summary>
public sealed record ListSupportAccessReportQuery(
    Guid? TenantId,
    DateTimeOffset? From,
    DateTimeOffset? To) : IQuery<SupportAccessListResponse>
{
    public const int MaxRows = 500;
}
