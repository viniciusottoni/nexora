namespace Nexora.Shared.Errors;

/// <summary>
/// Códigos de erro da US-157 (Central operacional, auditoria e atalhos de suporte) — ADR-021.
/// Arquivo próprio (não editar <c>ApiErrorCodes.Tenants.cs</c>/<c>ApiErrorCodes.Ownership.cs</c>),
/// mesma disciplina de coordenação já usada por <c>ApiErrorCodes.Plans.cs</c>/<c>ApiErrorCodes.Ownership.cs</c>.
/// Estes códigos JÁ estão integrados no <c>switch</c> de <c>ResultExtensions.MapErrorCode</c>
/// (Api.Cloud e Api.Edge) — diferente do gap documentado nos arquivos de US anteriores, esta tarefa
/// roda sozinha (sem concorrência de outro agente), então a integração central foi feita junto.
/// </summary>
public static partial class ApiErrorCodes
{
    /// <summary>Item da fila de atenção referenciado por um <c>itemId</c> que não corresponde a nenhuma condição ativa reconhecível — 404.</summary>
    public const string AttentionItemNotFound = "ATTENTION_ITEM_NOT_FOUND";
}
