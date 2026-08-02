namespace Nexora.Application.Installation.Abstractions;

/// <summary>
/// Costura de integração deliberada: a importação do bootstrap do edge (ADR-019, primeira
/// subida do servidor local) recebe também o cardápio inteiro (estações, categorias, produtos,
/// variantes, preços, grupos de modificador e modificadores — ver <c>import-bootstrap.ts</c>
/// original, seção <c>bootstrap.catalog</c>). Persistir essas entidades é responsabilidade do
/// módulo de Catálogo (fora do escopo desta tarefa de portar "Installation"), então
/// <see cref="ImportBootstrapCommandHandler"/> delega aqui em vez de duplicar/reimplementar
/// os agregados de <c>Nexora.Domain.Catalog</c>.
/// <para>
/// Contrato: recebe o payload de catálogo já desserializado (mesmo formato JSON do evento
/// <c>tenant.config_updated</c>) e faz upsert por id explícito — os agregados de Catálogo
/// hoje só expõem <c>Create</c> com id gerado, então a implementação real precisa de uma
/// variante com id explícito análoga à que foi adicionada em <c>Tenant</c>/<c>Store</c> aqui.
/// Até essa implementação existir, um adaptador "no-op com log" em Infrastructure mantém o
/// bootstrap de identidade/config funcionando (ver <c>NullBootstrapCatalogImporter</c>).
/// </para>
/// </summary>
public interface IBootstrapCatalogImporter
{
    Task ImportAsync(Guid tenantId, Guid storeId, string catalogJson, CancellationToken cancellationToken);
}
