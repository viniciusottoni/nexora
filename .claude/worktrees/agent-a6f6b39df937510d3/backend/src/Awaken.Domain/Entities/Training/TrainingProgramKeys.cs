namespace Awaken.Domain.Entities.Training;

/// Chaves estaveis do catalogo de programas de treino (US-231).
public static class TrainingProgramKeys
{
    public const string FullBody = "full_body";
    public const string Ab = "ab";
    public const string Abc = "abc";
    public const string Abcd = "abcd";
    public const string Abcde = "abcde";
    public const string Perfect2 = "perfect_2";
    public const string System = "system";

    public static readonly string[] CatalogOrder =
    [
        FullBody,
        Ab,
        Abc,
        Abcd,
        Abcde,
        Perfect2,
        System,
    ];
}
