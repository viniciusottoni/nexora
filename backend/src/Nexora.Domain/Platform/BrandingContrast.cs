using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>Um problema de contraste encontrado entre duas cores da marca.</summary>
public sealed record BrandingContrastIssue(string Pair, double Ratio, string Suggested);

/// <summary>Resultado da validação de contraste WCAG AA de uma paleta de marca.</summary>
public sealed record BrandingContrastResult(bool Valid, double MinimumRatio, IReadOnlyList<BrandingContrastIssue> Issues);

/// <summary>
/// Cálculo de contraste WCAG 2.x (relative luminance / contrast ratio) — regra de negócio pura,
/// sem I/O, porta exata de <c>packages/domain/src/branding/contrast.ts</c>. Garante que a marca
/// de qualquer tenant seja legível (texto sobre fundo), nunca condicional por cliente (ADR-013):
/// a regra vale para todos, o que muda é a paleta que cada tenant escolhe.
/// </summary>
public static class BrandingContrast
{
    public const double MinimumAaRatio = 4.5;

    public static double ContrastRatio(string foreground, string background)
    {
        var bright = RelativeLuminance(ParseHex(foreground));
        var dark = RelativeLuminance(ParseHex(background));
        var lighter = Math.Max(bright, dark);
        var darker = Math.Min(bright, dark);
        return Round((lighter + 0.05) / (darker + 0.05), 4);
    }

    public static string SuggestAccessibleColor(string foreground, string background, double minimumRatio = MinimumAaRatio)
    {
        var source = ParseHex(foreground);
        ParseHex(background); // valida o formato, mesmo comportamento do TS original

        if (ContrastRatio(foreground, background) >= minimumRatio)
            return ToHex(source);

        var candidates = new List<int[]>();
        foreach (var target in new[] { new[] { 0, 0, 0 }, new[] { 255, 255, 255 } })
        {
            var candidate = ClosestAccessibleBlend(source, target, background, minimumRatio);
            if (candidate is not null)
                candidates.Add(candidate);
        }

        candidates.Sort((left, right) => Distance(source, left).CompareTo(Distance(source, right)));

        if (candidates.Count == 0)
            throw new DomainException("Não existe variação com o contraste solicitado.");

        return ToHex(candidates[0]);
    }

    public static BrandingContrastResult Validate(string primary, string surface, string onPrimary)
    {
        var pairs = new (string Pair, string Foreground, string Background)[]
        {
            ("primary/surface", primary, surface),
            ("onPrimary/primary", onPrimary, primary)
        };

        var issues = new List<BrandingContrastIssue>();
        foreach (var (pair, foreground, background) in pairs)
        {
            var ratio = ContrastRatio(foreground, background);
            if (ratio < MinimumAaRatio)
                issues.Add(new BrandingContrastIssue(pair, ratio, SuggestAccessibleColor(foreground, background)));
        }

        return new BrandingContrastResult(issues.Count == 0, MinimumAaRatio, issues);
    }

    private static int[]? ClosestAccessibleBlend(int[] source, int[] target, string background, double minimumRatio)
    {
        for (var step = 1; step <= 1_000; step++)
        {
            var weight = step / 1_000.0;
            var candidate = new int[3];
            for (var i = 0; i < 3; i++)
                candidate[i] = (int)Math.Round(source[i] + (target[i] - source[i]) * weight);

            if (ContrastRatio(ToHex(candidate), background) >= minimumRatio)
                return candidate;
        }

        return null;
    }

    private static int[] ParseHex(string value)
    {
        if (value.Length != 7 || value[0] != '#')
            throw new DomainException("Cor hexadecimal inválida.");

        try
        {
            return new[]
            {
                Convert.ToInt32(value.Substring(1, 2), 16),
                Convert.ToInt32(value.Substring(3, 2), 16),
                Convert.ToInt32(value.Substring(5, 2), 16)
            };
        }
        catch (FormatException)
        {
            throw new DomainException("Cor hexadecimal inválida.");
        }
    }

    private static string ToHex(int[] rgb) =>
        $"#{rgb[0]:X2}{rgb[1]:X2}{rgb[2]:X2}";

    private static double RelativeLuminance(int[] rgb)
    {
        double Channel(int value)
        {
            var normalized = value / 255.0;
            return normalized <= 0.03928 ? normalized / 12.92 : Math.Pow((normalized + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(rgb[0]) + 0.7152 * Channel(rgb[1]) + 0.0722 * Channel(rgb[2]);
    }

    private static double Distance(int[] left, int[] right)
    {
        double sum = 0;
        for (var i = 0; i < left.Length; i++)
            sum += Math.Pow(left[i] - right[i], 2);

        return Math.Sqrt(sum);
    }

    private static double Round(double value, int places)
    {
        var scale = Math.Pow(10, places);
        return Math.Round(value * scale, MidpointRounding.AwayFromZero) / scale;
    }
}
