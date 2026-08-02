namespace Nexora.Application.Installation.Abstractions;

/// <summary>
/// Mesma costura de <see cref="IBootstrapCatalogImporter"/>, para a seção
/// <c>bootstrap.authorization</c> (papéis, usuários operacionais com PIN e vínculo
/// usuário↔papel — necessários para login por PIN funcionar offline no primeiro boot do edge).
/// Delegado ao módulo de Autenticação/Autorização, fora do escopo desta tarefa.
/// </summary>
public interface IBootstrapAuthorizationImporter
{
    Task ImportAsync(Guid tenantId, Guid storeId, string authorizationJson, CancellationToken cancellationToken);
}
