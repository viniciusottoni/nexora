using System.Security.Cryptography;
using Nexora.Application.Operation.Abstractions;

namespace Nexora.Infrastructure.Operation;

/// <summary>
/// <c>qr_token</c> opaco de <see cref="Nexora.Domain.Operation.DiningTable"/> — 256 bits de
/// entropia de <see cref="RandomNumberGenerator"/> (CSPRNG do SO, mesma família usada por
/// <c>PairingCodeGenerator</c>/<c>DeviceSecretGenerator</c>), codificados em Base64Url sem
/// padding. Não é sequencial nem derivado do id/rótulo da mesa — nenhuma relação previsível entre
/// o token da mesa 12 e o da mesa 13 (US-020, cenário Gherkin "Token não adivinhável").
/// </summary>
public sealed class QrTokenGenerator : IQrTokenGenerator
{
    private const int TokenSizeBytes = 32; // 256 bits — bem acima do mínimo recomendado (128 bits) para segredo de entrada não adivinhável por força bruta.

    public string Generate() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenSizeBytes))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
