using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using Nexora.Application.Abstractions.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Nexora.Infrastructure.Auth;

/// <summary>
/// Emissão e validação de JWT HS256 — porta unificada de jwt-token-issuer.ts (edge) e
/// jwt-token.service.ts (cloud): mesmo formato de claims (<c>sub</c>, <c>tid</c>, <c>sid</c>,
/// <c>roles</c>, <c>perms</c>, <c>did</c>, <c>ses</c>, <c>mfa</c>) e mesmo segredo simétrico nas
/// duas apps. A claim custom <c>tokenUse</c> distingue access/refresh/authorization — o mesmo
/// mecanismo usado no TS para impedir que um token emitido para um uso sirva para outro.
/// </summary>
public sealed class JwtTokenIssuer : ITokenIssuer
{
    private readonly JwtOptions _options;
    private readonly SymmetricSecurityKey _key;

    public JwtTokenIssuer(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        if (string.IsNullOrEmpty(_options.Secret) || Encoding.UTF8.GetByteCount(_options.Secret) < 32)
        {
            throw new InvalidOperationException("Auth:Jwt:Secret deve ter ao menos 32 caracteres.");
        }

        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
    }

    public Task<string> IssueAccessTokenAsync(AccessClaims claims, int ttlSeconds, CancellationToken cancellationToken = default) =>
        Task.FromResult(Sign(BuildPayload(claims), ttlSeconds, "access", claims.Subject.ToString()));

    public Task<string> IssueRefreshTokenAsync(AccessClaims claims, int ttlSeconds, CancellationToken cancellationToken = default) =>
        Task.FromResult(Sign(BuildPayload(claims), ttlSeconds, "refresh", claims.Subject.ToString()));

    public Task<string> IssueAuthorizationTokenAsync(
        IReadOnlyDictionary<string, object> claims, int ttlSeconds, CancellationToken cancellationToken = default)
    {
        var payload = new JwtPayload();
        foreach (var (key, value) in claims)
        {
            payload[key] = value;
        }

        var subject = claims.TryGetValue("sub", out var sub) ? sub?.ToString() : "authorization";
        return Task.FromResult(Sign(payload, ttlSeconds, "authorization", subject ?? "authorization"));
    }

    public Task<string> IssueTableSessionTokenAsync(
        Guid sessionId, Guid tenantId, Guid tableId, int ttlSeconds, CancellationToken cancellationToken = default)
    {
        var payload = new JwtPayload
        {
            ["sub"] = sessionId.ToString(),
            ["tid"] = tenantId.ToString(),
            ["ses"] = sessionId.ToString(),
            ["tbl"] = tableId.ToString(),
        };

        return Task.FromResult(Sign(payload, ttlSeconds, "table_session", sessionId.ToString()));
    }

    public Task<TableSessionTokenClaims> ValidateTableSessionTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var jwtToken = ValidateSignature(token);

        if (!jwtToken.Payload.TryGetValue("tokenUse", out var tokenUse) || tokenUse as string != "table_session")
        {
            throw new SecurityTokenException("Token não é um token de sessão de mesa.");
        }

        var claims = new TableSessionTokenClaims(
            SessionId: RequireGuidClaim(jwtToken, "ses"),
            TenantId: RequireGuidClaim(jwtToken, "tid"),
            TableId: RequireGuidClaim(jwtToken, "tbl"));

        return Task.FromResult(claims);
    }

    public Task<RefreshTokenClaims> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var jwtToken = ValidateSignature(refreshToken);

        if (!jwtToken.Payload.TryGetValue("tokenUse", out var tokenUse) || tokenUse as string != "refresh")
        {
            throw new SecurityTokenException("Token não é um refresh token.");
        }

        var claims = new RefreshTokenClaims(
            Subject: RequireGuidClaim(jwtToken, "sub"),
            TenantId: RequireGuidClaim(jwtToken, "tid"),
            StoreId: RequireGuidClaim(jwtToken, "sid"),
            SessionId: RequireGuidClaim(jwtToken, "ses"),
            Roles: ReadStringArray(jwtToken, "roles"),
            Permissions: ReadStringArray(jwtToken, "perms"),
            Mfa: ReadBoolClaim(jwtToken, "mfa"));

        return Task.FromResult(claims);
    }

    public Task<AuthorizationTokenClaims> ValidateAuthorizationTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var jwtToken = ValidateSignature(token);

        if (!jwtToken.Payload.TryGetValue("tokenUse", out var tokenUse) || tokenUse as string != "authorization")
        {
            throw new SecurityTokenException("Token não é um token de autorização.");
        }

        var claims = new AuthorizationTokenClaims(
            ActorId: RequireGuidClaim(jwtToken, "sub"),
            TenantId: RequireGuidClaim(jwtToken, "tid"),
            StoreId: RequireGuidClaim(jwtToken, "sid"),
            DeviceId: TryReadGuidClaim(jwtToken, "did"),
            Action: RequireStringClaim(jwtToken, "action"),
            ContextHash: RequireStringClaim(jwtToken, "contextHash"),
            AuthorizedBy: RequireGuidClaim(jwtToken, "authorizedBy"));

        return Task.FromResult(claims);
    }

    private static JwtPayload BuildPayload(AccessClaims claims)
    {
        var payload = new JwtPayload
        {
            ["sub"] = claims.Subject.ToString(),
            ["tid"] = claims.TenantId.ToString(),
            ["sid"] = claims.StoreId.ToString(),
            ["roles"] = claims.Roles,
            ["perms"] = claims.Permissions,
        };

        if (claims.DeviceId is { } deviceId)
        {
            payload["did"] = deviceId.ToString();
        }

        if (claims.SessionId is { } sessionId)
        {
            payload["ses"] = sessionId.ToString();
        }

        if (claims.Mfa)
        {
            payload["mfa"] = true;
        }

        return payload;
    }

    private string Sign(JwtPayload payload, int ttlSeconds, string tokenUse, string subject)
    {
        var now = DateTime.UtcNow;
        payload["tokenUse"] = tokenUse;
        payload["iss"] = _options.Issuer;
        payload["aud"] = _options.Audience;
        payload["iat"] = EpochTime.GetIntDate(now);
        payload["exp"] = EpochTime.GetIntDate(now.AddSeconds(ttlSeconds));
        if (!payload.ContainsKey("sub"))
        {
            payload["sub"] = subject;
        }

        var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        var header = new JwtHeader(credentials);
        var token = new JwtSecurityToken(header, payload);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private JwtSecurityToken ValidateSignature(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        handler.ValidateToken(token, parameters, out var validatedToken);
        return (JwtSecurityToken)validatedToken;
    }

    private static Guid RequireGuidClaim(JwtSecurityToken token, string claimType)
    {
        var value = token.Payload.TryGetValue(claimType, out var raw) ? raw?.ToString() : null;
        if (string.IsNullOrEmpty(value) || !Guid.TryParse(value, out var guid))
        {
            throw new SecurityTokenException($"Claim '{claimType}' ausente ou inválida no token.");
        }

        return guid;
    }

    /// <summary>Igual a <see cref="RequireGuidClaim"/>, mas devolve <c>null</c> em vez de lançar quando a claim está ausente — usada por claims opcionais (ex.: <c>did</c>, ausente no cloud).</summary>
    private static Guid? TryReadGuidClaim(JwtSecurityToken token, string claimType)
    {
        var value = token.Payload.TryGetValue(claimType, out var raw) ? raw?.ToString() : null;
        return !string.IsNullOrEmpty(value) && Guid.TryParse(value, out var guid) ? guid : null;
    }

    private static string RequireStringClaim(JwtSecurityToken token, string claimType)
    {
        var value = token.Payload.TryGetValue(claimType, out var raw) ? raw?.ToString() : null;
        if (string.IsNullOrEmpty(value))
        {
            throw new SecurityTokenException($"Claim '{claimType}' ausente ou inválida no token.");
        }

        return value;
    }

    private static bool ReadBoolClaim(JwtSecurityToken token, string claimType)
    {
        if (!token.Payload.TryGetValue(claimType, out var raw) || raw is null)
        {
            return false;
        }

        return raw switch
        {
            bool b => b,
            JsonElement element when element.ValueKind == JsonValueKind.True => true,
            JsonElement element when element.ValueKind == JsonValueKind.False => false,
            _ => false,
        };
    }

    private static IReadOnlyList<string> ReadStringArray(JwtSecurityToken token, string claimType)
    {
        if (!token.Payload.TryGetValue(claimType, out var raw) || raw is null)
        {
            return Array.Empty<string>();
        }

        return raw switch
        {
            IEnumerable<object> items => items.Select(i => i?.ToString() ?? string.Empty).ToArray(),
            JsonElement element when element.ValueKind == JsonValueKind.Array =>
                element.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToArray(),
            string single => new[] { single },
            _ => Array.Empty<string>(),
        };
    }
}
