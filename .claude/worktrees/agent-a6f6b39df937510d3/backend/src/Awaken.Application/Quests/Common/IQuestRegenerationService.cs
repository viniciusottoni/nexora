using Awaken.Domain.Entities.Quests;

namespace Awaken.Application.Quests.Common;

/// <summary>
/// US-230: mecânica de regeneração da quest diária, extraída de
/// RegenerateDailyQuestCommandHandler para ser reutilizada pelo
/// ReforgeScrollEffectHandler (uso do item via endpoint genérico de
/// inventário) sem duplicar a lógica de geração/auditoria.
/// </summary>
public interface IQuestRegenerationService
{
    /// Busca a quest diária de hoje (dia local do usuário) e a regenera.
    /// Não persiste (SaveChangesAsync é responsabilidade do chamador).
    Task<Quest> RegenerateAsync(Guid userId, bool viaReforgeScroll, CancellationToken cancellationToken);
}
