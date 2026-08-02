namespace Awaken.Domain.Entities.Inventory;

/// <summary>
/// US-187: ponto de extensao para handlers de efeito/consumo de item (ADR-023).
/// US-230: interface expandida com metadados de limite de uso e método ApplyAsync.
/// </summary>
public interface IItemEffectHandler
{
    /// Chave estável (ver <see cref="ItemKeys"/>) do item que este handler
    /// sabe processar.
    string ItemKey { get; }

    /// Se verdadeiro, o item é consumido (quantity -1) após o uso bem-sucedido.
    bool ConsumesOnUse { get; }

    /// Máximo de usos por período. 0 = ilimitado.
    int UsageLimit { get; }

    /// Janela temporal para contagem de usos.
    ItemUsageLimitPeriod LimitPeriod { get; }

    /// Aplica o efeito do item e retorna o resultado.
    Task<ItemEffectResult> ApplyAsync(UseItemContext context, CancellationToken ct);
}

/// <summary>
/// US-230: contexto passado aos handlers de efeito de item.
/// </summary>
/// <param name="EffectiveQuestDateUtc">
/// Dia local do usuário (via IUserDateService.TodayLocal), resolvido uma única
/// vez pelo UseItemCommandHandler. Todo handler que grava um "dia-alvo" de
/// efeito (Tônico de Recuperação, Amuleto de Retorno) deve usar este campo,
/// nunca UtcNow.Date puro — UtcNow é hora de servidor, não o dia da quest do
/// usuário (podem divergir por fuso).
/// </param>
/// <param name="PayloadJson">
/// Parâmetros específicos do item (ex.: novo nickname, classe-alvo) — evita
/// criar endpoints dedicados por item. Nunca logar/auditar este campo cru
/// (pode conter dado pessoal, ADR-015).
/// </param>
public record UseItemContext(
    Guid UserId,
    string ItemKey,
    string? ContextType,
    string? ContextId,
    string UseRequestId,
    DateTime UtcNow,
    DateTime EffectiveQuestDateUtc,
    string? PayloadJson,
    IServiceProvider Services);
