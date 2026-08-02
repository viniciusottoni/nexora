namespace Nexora.Application.Auth.Shared;

/// <summary>Resultado de <see cref="PinPolicy.Validate"/> — porta de PinValidation (pin-policy.ts).</summary>
public sealed record PinValidationResult(bool Valid, string? Reason)
{
    public static PinValidationResult Ok() => new(true, null);

    public static PinValidationResult Invalid(string reason) => new(false, reason);
}

/// <summary>
/// Regras de PIN "não óbvio" — porta de packages/domain/src/auth/pin-policy.ts. Usada na
/// definição/rotação de PIN (fora do escopo deste porte, que cobriu apenas login), não na
/// verificação de login em si: um PIN existente pode ter sido criado antes desta política e o
/// login não deve rejeitá-lo por causa disso — só a criação/rotação aplica <see cref="Validate"/>.
/// </summary>
internal static class PinPolicy
{
    private static readonly HashSet<string> BlockedPins = new(StringComparer.Ordinal)
    {
        "1234", "4321", "0123", "3210", "2580", "0852",
    };

    public static PinValidationResult Validate(string pin)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(pin, @"^\d{4,6}$"))
        {
            return PinValidationResult.Invalid("O PIN deve ter de 4 a 6 dígitos.");
        }

        if (pin.Distinct().Count() == 1 || BlockedPins.Contains(pin) || IsSequential(pin))
        {
            return PinValidationResult.Invalid("Escolha um PIN menos previsível.");
        }

        return PinValidationResult.Ok();
    }

    private static bool IsSequential(string pin)
    {
        var digits = pin.Select(c => c - '0').ToArray();
        var changes = new int[digits.Length - 1];
        for (var i = 1; i < digits.Length; i++)
        {
            changes[i - 1] = digits[i] - digits[i - 1];
        }

        return changes.All(change => change == 1) || changes.All(change => change == -1);
    }
}
