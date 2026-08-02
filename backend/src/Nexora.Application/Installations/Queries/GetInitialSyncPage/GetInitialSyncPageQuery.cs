using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Installations;

namespace Nexora.Application.Installations.Queries.GetInitialSyncPage;

/// <summary>
/// GET /v1/sync/pull (cloud, atrás de <c>AuthenticateInstallationRequestCommand</c>) — porta de
/// <c>PrismaInitialSyncReader.pull</c>. <paramref name="TenantId"/>/<paramref name="StoreId"/>
/// vêm do <c>InstallationAuthContext</c> já resolvido pelo controller (ver nota de design no
/// handler sobre por que a query filtra por tenant explicitamente em vez de confiar apenas na
/// RLS por <c>SET LOCAL</c> para este fluxo específico, autenticado por assinatura e não por JWT).
/// </summary>
public sealed record GetInitialSyncPageQuery(
    Guid TenantId,
    Guid StoreId,
    Guid InstallationId,
    int Cursor,
    int Limit) : IQuery<InitialSyncPageResponse>;
