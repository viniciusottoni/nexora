using Awaken.Domain.Entities.Inventory;

namespace Awaken.Domain.Entities.Progression;

/// US-230: registro estático de classes de Hunter disponíveis. Cada classe
/// além da padrão é desbloqueada pela posse do Pack correspondente (mesmo
/// padrão de gate por posse do AvatarCatalog) e concedida imediatamente ao
/// abrir o Pack, ou trocável depois via Pergaminho da Classe entre classes
/// já desbloqueadas.
public static class HunterClassCatalog
{
    public static readonly IReadOnlyList<HunterClassCatalogEntry> Classes =
    [
        new HunterClassCatalogEntry(HunterProgression.DefaultHunterClass),
        new HunterClassCatalogEntry("striker", ItemKeys.PackStriker),
        new HunterClassCatalogEntry("runner", ItemKeys.PackRunner),
        new HunterClassCatalogEntry("guardian", ItemKeys.PackGuardian),
        new HunterClassCatalogEntry("shadow", ItemKeys.PackShadow),
        new HunterClassCatalogEntry("reawakened", ItemKeys.PackReawakened),
    ];

    public static HunterClassCatalogEntry? Find(string classKey) =>
        Classes.FirstOrDefault(c => c.ClassKey == classKey);
}
