using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nexora.Infrastructure.Auth;
using Nexora.Infrastructure.Devices;
using Nexora.Infrastructure.Persistence;

const string commonPassword = "Nexora@Teste2026!";
const string pairingCode = "884422";
const string totpSecret = "JBSWY3DPEHPK3PXP";
const string pinLookupPepper = "__CHANGE_ME_DEV_ONLY__REPLACE_WITH_A_RANDOM_32_BYTE_PEPPER__";
const string devicePepper = "__CHANGE_ME_DEV_ONLY__REPLACE_WITH_A_RANDOM_32_BYTE_PEPPER__";
const string mfaEncryptionKey = "__CHANGE_ME_DEV_ONLY__REPLACE_WITH_A_RANDOM_32_BYTE_KEY__";

var parsed = Arguments.Parse(args);
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseNpgsql(parsed.ConnectionString)
    .UseSnakeCaseNamingConvention()
    .Options;

await using var db = new AppDbContext(options);
await db.Database.OpenConnectionAsync();
await using var transaction = await db.Database.BeginTransactionAsync();

var hasher = new Argon2CredentialHasher();
var pinDigester = new HmacPinLookupDigester(Options.Create(new AuthSecretsOptions
{
    PinLookupPepper = pinLookupPepper,
}));
var mfaCipher = new AesGcmMfaSecretCipher(Options.Create(new AuthSecretsOptions
{
    MfaEncryptionKey = mfaEncryptionKey,
}));
var deviceDigester = new DeviceSecretDigester(Options.Create(new DeviceSecurityOptions
{
    Pepper = devicePepper,
}));

var passwordHash = hasher.Hash(commonPassword);
var encryptedTotpSecret = mfaCipher.Encrypt(totpSecret);

await db.Database.ExecuteSqlInterpolatedAsync($"""
    INSERT INTO tenant
        (id, slug, name, status, plan, timezone, locale, currency, created_at, updated_at)
    VALUES
        ({SeedIds.TenantId}, 'dona-betinha-teste', 'Dona Betinha - Testes', 1, 'STANDARD',
         'America/Fortaleza', 'pt-BR', 'BRL', now(), now())
    ON CONFLICT (id) DO UPDATE SET
        slug = EXCLUDED.slug,
        name = EXCLUDED.name,
        status = EXCLUDED.status,
        updated_at = now();
    """);

await db.Database.ExecuteSqlInterpolatedAsync(
    $"SELECT set_config('app.tenant_id', {SeedIds.TenantId.ToString()}, false);");

const string emptyObjectJson = "{}";
const string emptyArrayJson = "[]";
const string operationJson = "{\"serviceFeePercent\":10,\"serviceFeeOptional\":true,\"halfAndHalfPricing\":\"HIGHEST\",\"maxFractions\":2,\"sessionInactivityMinutes\":30}";
const string thresholdsJson = "{\"orderWarnMinutes\":12,\"orderCriticalMinutes\":18,\"tableIdleMinutes\":10}";
const string modulesJson = "{\"dineIn\":true,\"kds\":true,\"cash\":true,\"delivery\":true,\"stock\":true,\"finance\":true}";

await db.Database.ExecuteSqlInterpolatedAsync($"""
    INSERT INTO tenant_config
        (tenant_id, branding, operation, thresholds, modules, fiscal, printers, payments,
         maintenance, catalog_version, config_version, branding_version, created_at, updated_at)
    VALUES
        ({SeedIds.TenantId}, {emptyObjectJson}::jsonb,
         {operationJson}::jsonb,
         {thresholdsJson}::jsonb,
         {modulesJson}::jsonb,
         {emptyObjectJson}::jsonb, {emptyArrayJson}::jsonb, {emptyObjectJson}::jsonb, {emptyObjectJson}::jsonb,
         1, 1, 1, now(), now())
    ON CONFLICT (tenant_id) DO UPDATE SET updated_at = now();
    """);

await db.Database.ExecuteSqlInterpolatedAsync($"""
    INSERT INTO store
        (id, tenant_id, name, timezone, is_default, is_active, created_at, updated_at)
    VALUES
        ({SeedIds.StoreId}, {SeedIds.TenantId}, 'Matriz - Testes', 'America/Fortaleza', true, true, now(), now())
    ON CONFLICT (id) DO UPDATE SET
        name = EXCLUDED.name,
        timezone = EXCLUDED.timezone,
        is_default = true,
        is_active = true,
        updated_at = now();
    """);

var roles = new[]
{
    new RoleSeed("PLATFORM_ADMIN", "Admin da plataforma", new[] { "*" }),
    new RoleSeed("OWNER", "Proprietário", new[] { "*" }),
    new RoleSeed("MANAGER", "Gerente", new[]
    {
        "order:*", "table:*", "kds:*", "cash:*", "stock:*", "report:read",
        "order:cancel_started", "cash:discount_any", "cash:close_divergent", "stock:adjust",
        "payment:refund", "order:close_with_pending", "user:read", "user:write",
        "catalog:read", "catalog:write", "device:manage", "config:read", "config:write"
    }),
    new RoleSeed("CASHIER", "Caixa", new[]
    {
        "order:read", "order:create", "table:read", "table:close_request", "cash:open",
        "cash:close", "cash:movement", "cash:discount_limited", "payment:register", "report:read_own"
    }),
    new RoleSeed("WAITER", "Garçom", new[]
    {
        "table:open", "table:read", "table:transfer", "table:close_request", "order:create",
        "order:read", "order:add_item", "order:cancel_queued", "kds:read", "report:read_own"
    }),
    new RoleSeed("KITCHEN", "Cozinha", new[]
    {
        "kds:read", "kds:advance", "kds:refire", "catalog:set_unavailable", "order:read"
    }),
    new RoleSeed("STOCK", "Estoque", new[]
    {
        "stock:read", "stock:purchase", "stock:waste", "stock:count", "recipe:read",
        "recipe:write", "supplier:*"
    }),
    new RoleSeed("COURIER", "Entregador", new[] { "delivery:read_own", "delivery:advance" }),
};

for (var index = 0; index < roles.Length; index++)
{
    var role = roles[index];
    var roleId = SeedIds.Role(index);
    var permissions = System.Text.Json.JsonSerializer.Serialize(role.Permissions);
    await db.Database.ExecuteSqlInterpolatedAsync($"""
        INSERT INTO role
            (id, tenant_id, code, name, permissions, is_system, created_at, updated_at)
        VALUES
            ({roleId}, {SeedIds.TenantId}, {role.Code}, {role.Name}, {permissions}::jsonb, true, now(), now())
        ON CONFLICT (id) DO UPDATE SET
            code = EXCLUDED.code,
            name = EXCLUDED.name,
            permissions = EXCLUDED.permissions,
            is_system = true,
            deleted_at = NULL,
            updated_at = now();
        """);
}

var users = new[]
{
    new UserSeed("Admin Plataforma", "plataforma@nexora.local", "PLATFORM_ADMIN", null, true),
    new UserSeed("Dona Betinha", "proprietario@nexora.local", "OWNER", "2101"),
    new UserSeed("Gerente Teste", "gerente@nexora.local", "MANAGER", "2102"),
    new UserSeed("Caixa Teste", "caixa@nexora.local", "CASHIER", "2103"),
    new UserSeed("Garçom Teste", "garcom@nexora.local", "WAITER", "2104"),
    new UserSeed("Cozinha Teste", "cozinha@nexora.local", "KITCHEN", "2105"),
    new UserSeed("Estoque Teste", "estoque@nexora.local", "STOCK", "2106"),
    new UserSeed("Entregador Teste", "entregador@nexora.local", "COURIER", "2107"),
};

for (var index = 0; index < users.Length; index++)
{
    var user = users[index];
    var userId = SeedIds.User(index);
    var roleId = SeedIds.Role(Array.FindIndex(roles, role => role.Code == user.Role));
    string? pinHash = null;
    string? pinLookup = null;
    if (user.Pin is not null)
    {
        pinHash = hasher.Hash(user.Pin);
        pinLookup = pinDigester.Digest(user.Pin);
    }

    var mfaSecret = user.HasMfa ? encryptedTotpSecret : null;
    DateTimeOffset? pinRotatedAt = user.Pin is null ? null : DateTimeOffset.UtcNow;
    await db.Database.ExecuteSqlInterpolatedAsync($"""
        INSERT INTO app_user
            (id, tenant_id, name, email, password_hash, pin_hash, mfa_secret_encrypted,
             pin_lookup, pin_rotated_at, status, failed_attempts, created_at, updated_at)
        VALUES
            ({userId}, {SeedIds.TenantId}, {user.Name}, {user.Email}, {passwordHash}, {pinHash},
             {mfaSecret}, {pinLookup}, {pinRotatedAt}, 0, 0, now(), now())
        ON CONFLICT (id) DO UPDATE SET
            name = EXCLUDED.name,
            email = EXCLUDED.email,
            password_hash = EXCLUDED.password_hash,
            pin_hash = EXCLUDED.pin_hash,
            mfa_secret_encrypted = EXCLUDED.mfa_secret_encrypted,
            pin_lookup = EXCLUDED.pin_lookup,
            pin_rotated_at = EXCLUDED.pin_rotated_at,
            status = 0,
            failed_attempts = 0,
            blocked_until = NULL,
            deleted_at = NULL,
            updated_at = now();
        """);

    await db.Database.ExecuteSqlInterpolatedAsync($"""
        INSERT INTO user_role (id, tenant_id, user_id, role_id, store_id)
        VALUES ({SeedIds.UserRole(index)}, {SeedIds.TenantId}, {userId}, {roleId}, {SeedIds.StoreId})
        ON CONFLICT (id) DO UPDATE SET
            user_id = EXCLUDED.user_id,
            role_id = EXCLUDED.role_id,
            store_id = EXCLUDED.store_id;
        """);
}

if (parsed.Mode == SeedMode.Edge)
{
    var pairingHash = deviceDigester.Digest(pairingCode);
    await db.Database.ExecuteSqlInterpolatedAsync($"""
        INSERT INTO pairing_code
            (id, tenant_id, store_id, code_hash, created_by, expires_at, consumed_at, attempts, created_at)
        VALUES
            ({SeedIds.PairingCodeId}, {SeedIds.TenantId}, {SeedIds.StoreId}, {pairingHash},
             {SeedIds.User(1)}, now() + interval '10 minutes', NULL, 0, now())
        ON CONFLICT (id) DO UPDATE SET
            code_hash = EXCLUDED.code_hash,
            created_by = EXCLUDED.created_by,
            expires_at = now() + interval '10 minutes',
            consumed_at = NULL,
            attempts = 0,
            created_at = now();
        """);
}

await transaction.CommitAsync();

Console.WriteLine($"Massa de teste aplicada em {parsed.Mode.ToString().ToUpperInvariant()}.");
Console.WriteLine($"TenantId: {SeedIds.TenantId}");
Console.WriteLine($"StoreId:  {SeedIds.StoreId}");
Console.WriteLine($"Senha comum: {commonPassword}");
Console.WriteLine($"OTP atual da plataforma: {Totp.Current(totpSecret)}");
if (parsed.Mode == SeedMode.Edge)
{
    Console.WriteLine($"Código inicial da gestão local (10 min / uso único): {pairingCode}");
}

internal sealed record RoleSeed(string Code, string Name, IReadOnlyList<string> Permissions);
internal sealed record UserSeed(string Name, string Email, string Role, string? Pin, bool HasMfa = false);

internal enum SeedMode { Cloud, Edge }

internal sealed record Arguments(string ConnectionString, SeedMode Mode)
{
    public static Arguments Parse(string[] values)
    {
        string? connection = null;
        SeedMode? mode = null;
        for (var index = 0; index < values.Length; index++)
        {
            if (values[index] == "--connection" && index + 1 < values.Length)
            {
                connection = values[++index];
            }
            else if (values[index] == "--mode" && index + 1 < values.Length &&
                     Enum.TryParse<SeedMode>(values[++index], true, out var parsedMode))
            {
                mode = parsedMode;
            }
        }

        if (string.IsNullOrWhiteSpace(connection) || mode is null)
        {
            throw new ArgumentException("Uso: --connection <connection-string> --mode <cloud|edge>");
        }

        return new Arguments(connection, mode.Value);
    }
}

internal static class SeedIds
{
    public static readonly Guid TenantId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    public static readonly Guid StoreId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    public static readonly Guid PairingCodeId = Guid.Parse("55555555-5555-4555-8555-555555555555");

    public static Guid Role(int index) => Guid.Parse($"33333333-3333-4333-8333-{index + 1:D12}");
    public static Guid User(int index) => Guid.Parse($"44444444-4444-4444-8444-{index + 1:D12}");
    public static Guid UserRole(int index) => Guid.Parse($"66666666-6666-4666-8666-{index + 1:D12}");
}

internal static class Totp
{
    public static string Current(string secret)
    {
        var key = Base32Decode(secret);
        var step = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var counter = BitConverter.GetBytes(step);
        if (BitConverter.IsLittleEndian) Array.Reverse(counter);
        var hash = HMACSHA1.HashData(key, counter);
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24) |
                     ((hash[offset + 1] & 0xFF) << 16) |
                     ((hash[offset + 2] & 0xFF) << 8) |
                     (hash[offset + 3] & 0xFF);
        return (binary % 1_000_000).ToString("D6");
    }

    private static byte[] Base32Decode(string value)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var result = new List<byte>();
        var buffer = 0;
        var bitsLeft = 0;
        foreach (var character in value.Trim().TrimEnd('=').ToUpperInvariant())
        {
            var index = alphabet.IndexOf(character);
            if (index < 0) continue;
            buffer = (buffer << 5) | index;
            bitsLeft += 5;
            if (bitsLeft < 8) continue;
            bitsLeft -= 8;
            result.Add((byte)((buffer >> bitsLeft) & 0xFF));
        }
        return result.ToArray();
    }
}
