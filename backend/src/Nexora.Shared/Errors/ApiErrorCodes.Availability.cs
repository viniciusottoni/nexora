namespace Nexora.Shared.Errors;

/// <summary>
/// Códigos de erro do módulo de indisponibilidade de produto (US-015, ADR-021). Arquivo reservado
/// pela convenção de "um arquivo <c>ApiErrorCodes.&lt;Modulo&gt;.cs</c> por módulo" (ver docstring de
/// <c>ApiErrorCodes.cs</c>) — nenhum código novo foi necessário para o escopo desta história:
/// <list type="bullet">
/// <item><description>Produto inexistente reaproveita o código estável <c>"PRODUCT_NOT_FOUND"</c> (US-010,
/// <c>ApiErrorCodes.Catalog.cs</c>) — usado como literal, não como o símbolo
/// <c>ApiErrorCodes.ProductNotFound</c>, porque este worktree isolado não tem
/// <c>ApiErrorCodes.Catalog.cs</c> nem o restante da camada Application/Api de US-010/US-011 (só
/// Domain+Infrastructure+schema de catálogo foram commitados na criação do worktree — ver
/// relatório da tarefa). Depois do merge, com <c>ApiErrorCodes.Catalog.cs</c> presente, o literal
/// pode virar o símbolo <c>ApiErrorCodes.ProductNotFound</c> — já mapeado em
/// <c>ResultExtensions.MapErrorCode</c> nessa branch.</description></item>
/// <item><description>Motivo de indisponibilidade vazio é recusado pelo <c>ValidationBehavior</c> (FluentValidation),
/// que já produz <see cref="ApiErrorCodes.ValidationError"/> genérico — mesmo padrão de
/// <c>CreateProductCommandValidator</c>.</description></item>
/// <item><description>Tenant não identificado reaproveita <see cref="ApiErrorCodes.TenantContextMissing"/> — mesmo
/// padrão de todo handler de catálogo (US-010/US-011).</description></item>
/// </list>
/// Mantido como classe <c>partial</c> vazia (em vez de omitir o arquivo) para reservar o local —
/// um código específico de disponibilidade que venha a ser necessário em história futura (ex.: a
/// convergência por <c>occurredAt</c> entre marcação simultânea no KDS e no painel, RN-019, hoje
/// não implementada — ver <c>MarkProductUnavailableCommandHandler</c>) deve ser adicionado aqui.
/// </summary>
public static partial class ApiErrorCodes
{
}
