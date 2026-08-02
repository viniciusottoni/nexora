using Awaken.Domain.Entities.Inventory;

namespace Awaken.Domain.Entities.Avatars;

/// US-234: registro estatico de avatares internos disponiveis para selecao
/// manual (RN-003). Espelha os assets ja existentes em
/// apps/mobile/assets/images/avatar-*.jpg. Novos avatares entram aqui apenas
/// como dado, sem exigir migracao de schema - mesmo padrao de ItemCatalog
/// (US-187).
///
/// RN-005: avatares de pack (chave "avatar_{sexo}_pack_{tema}") exigem o
/// pack correspondente (ver ItemKeys) no inventario do usuario. Cada pack
/// libera a mesma tematica para os dois sexos (ex.: pack_striker libera
/// avatar_male_pack_striker e avatar_female_pack_striker).
public static class AvatarCatalog
{
    public const string DefaultAvatarKey = "avatar_male_default";
    public const string DefaultFemaleAvatarKey = "avatar_female_default";

    public static readonly IReadOnlyList<AvatarCatalogEntry> Avatars =
    [
        new AvatarCatalogEntry(DefaultAvatarKey),
        new AvatarCatalogEntry(DefaultFemaleAvatarKey),
        new AvatarCatalogEntry("avatar_male_1"),
        new AvatarCatalogEntry("avatar_male_2"),
        new AvatarCatalogEntry("avatar_male_3"),
        new AvatarCatalogEntry("avatar_male_4"),
        new AvatarCatalogEntry("avatar_male_5"),
        new AvatarCatalogEntry("avatar_male_6"),
        new AvatarCatalogEntry("avatar_male_7"),
        new AvatarCatalogEntry("avatar_male_8"),
        new AvatarCatalogEntry("avatar_male_9"),
        new AvatarCatalogEntry("avatar_female_1"),
        new AvatarCatalogEntry("avatar_female_2"),
        new AvatarCatalogEntry("avatar_female_3"),
        new AvatarCatalogEntry("avatar_female_4"),
        new AvatarCatalogEntry("avatar_female_5"),
        new AvatarCatalogEntry("avatar_female_6"),
        new AvatarCatalogEntry("avatar_female_7"),
        new AvatarCatalogEntry("avatar_female_8"),
        new AvatarCatalogEntry("avatar_female_9"),
        new AvatarCatalogEntry("avatar_male_pack_striker", ItemKeys.PackStriker),
        new AvatarCatalogEntry("avatar_male_pack_runner", ItemKeys.PackRunner),
        new AvatarCatalogEntry("avatar_male_pack_guardian", ItemKeys.PackGuardian),
        new AvatarCatalogEntry("avatar_male_pack_shadow", ItemKeys.PackShadow),
        new AvatarCatalogEntry("avatar_male_pack_reawakened", ItemKeys.PackReawakened),
        new AvatarCatalogEntry("avatar_female_pack_striker", ItemKeys.PackStriker),
        new AvatarCatalogEntry("avatar_female_pack_runner", ItemKeys.PackRunner),
        new AvatarCatalogEntry("avatar_female_pack_guardian", ItemKeys.PackGuardian),
        new AvatarCatalogEntry("avatar_female_pack_shadow", ItemKeys.PackShadow),
        new AvatarCatalogEntry("avatar_female_pack_reawakened", ItemKeys.PackReawakened),
    ];

    public static AvatarCatalogEntry? Find(string avatarKey) =>
        Avatars.FirstOrDefault(a => a.AvatarKey == avatarKey);

    /// RN-002: avatar padrao do sistema quando nao ha imagem do Google nem
    /// selecao manual. Depende do sexo biologico informado no onboarding
    /// (UserProfile.BiologicalSex - "masculino"/"feminino", ver
    /// onboarding_page.dart); sem essa informacao (pre-onboarding), cai no
    /// padrao masculino como ultimo fallback.
    public static string GenderDefaultAvatarKey(string? biologicalSex) => biologicalSex switch
    {
        "feminino" => DefaultFemaleAvatarKey,
        _ => DefaultAvatarKey,
    };
}
