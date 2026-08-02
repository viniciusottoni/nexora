namespace Nexora.Infrastructure.Auth;

/// <summary>Configuração de emissão/validação de JWT — porta de JWT_SECRET/TOKEN_ISSUER/TOKEN_AUDIENCE (jwt-token-issuer.ts / jwt-token.service.ts).</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Auth:Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "food-operations-platform";
    public string Audience { get; set; } = "food-operations-apps";
}
