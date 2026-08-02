using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Abstractions.Security;

/// <summary>
/// Valida o header <c>X-Authorization-Token</c> (ADR-023, elevação pontual) contra o que
/// <c>AuthorizeSensitiveActionCommandHandler</c> emitiu — US-004, gap "autorização pontual é só
/// emitida, nunca validada". Nenhum endpoint de negócio consome isto ainda (cancelamento de item
/// iniciado, desconto acima do limite etc. são de USes futuras do E-03/E-05); este contrato existe
/// para que o mecanismo seja testável isoladamente hoje e reutilizável por quem precisar, sem
/// reinventar a checagem de assinatura/expiração/ação a cada módulo novo.
/// </summary>
/// <remarks>
/// Público (diferente de <c>PermissionEvaluator</c>, interno ao módulo Auth) porque precisa ser
/// resolvido via DI diretamente em <c>Program.cs</c> de <c>Api.Edge</c>/<c>Api.Cloud</c> — tanto
/// para um futuro filtro de ação/atributo (<c>[RequiresAuthorizationToken]</c>) quanto para
/// consumo direto por um handler que precise da elevação antes de agir.
/// </remarks>
public interface IAuthorizationTokenValidator
{
    /// <summary>
    /// <paramref name="token"/> é o valor cru do header <c>X-Authorization-Token</c> (pode ser nulo
    /// ou vazio — header ausente). <paramref name="requiredAction"/> é o código da ação sensível
    /// que o chamador está protegendo (ex.: <c>"CANCEL_STARTED_ITEM"</c> — mesmo vocabulário de
    /// <c>SensitiveActionCatalog</c>). Devolve <see cref="ApiErrorCodes.AuthorizationRequired"/> em
    /// toda falha (token ausente, assinatura/expiração inválida, ação divergente) — RNF-SEG-15:
    /// nunca distingue o motivo exato ao cliente.
    /// </summary>
    Task<Result<AuthorizationGrant>> ValidateAsync(
        string? token, string requiredAction, CancellationToken cancellationToken = default);
}

/// <summary>Quem autorizou uma ação sensível via elevação pontual, e em que contexto — devolvido por <see cref="IAuthorizationTokenValidator"/> em caso de sucesso.</summary>
public sealed record AuthorizationGrant(
    Guid AuthorizedBy,
    Guid ActorId,
    Guid TenantId,
    Guid StoreId,
    Guid? DeviceId,
    string Action,
    string ContextHash);
