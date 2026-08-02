namespace Awaken.Domain.Services.Progression;

public static class RankCalculator
{
    public const int OnboardingRankScoreCap = 48;

    private static readonly string[] RankOrder = ["E", "D", "C", "B", "A", "S", "SS", "SSS"];

    public static string CalculateRank(int rankScore) => rankScore switch
    {
        <= 17 => "E",
        <= 29 => "D",
        <= 47 => "C",
        <= 83 => "B",
        <= 155 => "A",
        <= 299 => "S",
        <= 587 => "SS",
        _ => "SSS",
    };

    /// US-231 RN-002/RN-004: compara ranks em ordem crescente (E &lt; D &lt; C &lt; B &lt; A &lt; S &lt; SS &lt; SSS).
    /// Retorna false quando qualquer um dos ranks informados nao e reconhecido.
    public static bool MeetsMinimumRank(string userRank, string minimumRank)
    {
        static string? Normalize(string? rank) => rank?.Trim().TrimEnd('+');

        var normalizedUserRank = Normalize(userRank);
        var normalizedMinimumRank = Normalize(minimumRank);
        var userIndex = normalizedUserRank is null ? -1 : Array.IndexOf(RankOrder, normalizedUserRank);
        var minIndex = normalizedMinimumRank is null ? -1 : Array.IndexOf(RankOrder, normalizedMinimumRank);
        return userIndex >= 0 && minIndex >= 0 && userIndex >= minIndex;
    }

    /// <summary>
    /// Converte o rank minimo para o formato exibido no catalogo, sempre com o sufixo '+'.
    /// </summary>
    public static string FormatMinimumRank(string minimumRank)
    {
        static string? Normalize(string? rank) => rank?.Trim().TrimEnd('+');

        var normalized = Normalize(minimumRank);
        return string.IsNullOrWhiteSpace(normalized) ? minimumRank.Trim() : $"{normalized}+";
    }
}
