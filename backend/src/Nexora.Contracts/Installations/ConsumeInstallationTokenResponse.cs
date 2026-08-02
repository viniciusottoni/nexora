namespace Nexora.Contracts.Installations;

/// <summary>
/// Resposta do consumo do token de instalação emitido no provisionamento do tenant (elo entre
/// o módulo de Tenants e o de Installations — ver <c>consume-installation-token.service.ts</c>
/// original). Só identifica a quem o token pertence; a chave pública e o restante do registro
/// completo só chegam em <see cref="RegisterInstallationResponse"/>, quando o dispositivo físico
/// efetivamente sobe.
/// </summary>
public sealed record ConsumeInstallationTokenResponse(
    Guid TenantId,
    Guid StoreId,
    Guid EdgeInstallationId,
    DateTimeOffset ConsumedAt);
