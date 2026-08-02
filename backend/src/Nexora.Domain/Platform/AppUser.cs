using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// Usuário da plataforma. Renomeado de "user" para "app_user" porque "user" é palavra
/// reservada no PostgreSQL. Precisa ter senha (gestor/administrativo) ou PIN (operação,
/// ADR-014) — ver ck_app_user_credential.
/// </summary>
public sealed class AppUser
{
    private readonly List<UserRole> _userRoles = new();
    private readonly List<AuthSession> _sessions = new();
    private readonly List<OwnerInvite> _invites = new();

    private AppUser() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? PasswordHash { get; private set; }
    public string? PinHash { get; private set; }
    public string? MfaSecret { get; private set; }
    public string? PinLookup { get; private set; }
    public DateTimeOffset? PinRotatedAt { get; private set; }
    public UserStatus Status { get; private set; } = UserStatus.Active;
    public short FailedAttempts { get; private set; }
    public DateTimeOffset? BlockedUntil { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public Tenant Tenant { get; private set; } = null!;
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
    public IReadOnlyCollection<AuthSession> Sessions => _sessions.AsReadOnly();
    public IReadOnlyCollection<OwnerInvite> Invites => _invites.AsReadOnly();

    /// <summary>
    /// <paramref name="pinLookup"/> é opcional na assinatura (aditivo, para não quebrar chamadas
    /// posicionais existentes nos testes) mas é o digest HMAC determinístico (<c>IPinLookupDigester</c>)
    /// do MESMO PIN que gerou <paramref name="pinHash"/> — sem ele, <c>uq_app_user_pin</c>
    /// (ADR-014, US-004 gap "índice de unicidade de PIN aponta pro campo errado") nunca detecta
    /// colisão nenhuma (todo usuário fica com <c>pin_lookup</c> nulo, e valores nulos nunca colidem
    /// num índice único do Postgres) e o login por PIN nunca encontra o usuário de volta
    /// (a busca é por <c>PinLookup</c>, nunca por <c>PinHash</c> — ver LoginWithPinCommandHandler).
    /// Quem cria um usuário com PIN precisa digerir o mesmo valor em cima e passar os dois.
    /// </summary>
    public static AppUser Create(
        Guid tenantId, string name, string? email, string? passwordHash, string? pinHash, string? pinLookup = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do usuário é obrigatório.");

        if (string.IsNullOrWhiteSpace(passwordHash) && string.IsNullOrWhiteSpace(pinHash))
            throw new DomainException("O usuário precisa ter senha ou PIN definido.");

        var now = DateTimeOffset.UtcNow;

        return new AppUser
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            PinHash = pinHash,
            PinLookup = pinLookup,
            Status = UserStatus.Active,
            FailedAttempts = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Cria um usuário convidado (dono de tenant recém-provisionado) sem credencial ainda —
    /// o convite (<see cref="OwnerInvite"/>) é o único jeito de ativá-lo. Diferente de
    /// <see cref="Create"/>, que exige senha ou PIN desde a criação; este fica <see cref="UserStatus.Invited"/>
    /// (Docs/Domain/12 §8) até <see cref="SetPassword"/> ser chamado no aceite do convite, que o
    /// transiciona para <see cref="UserStatus.Active"/>.
    /// </summary>
    public static AppUser Invite(Guid tenantId, string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do usuário é obrigatório.");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("O e-mail do usuário é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        return new AppUser
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            Name = name,
            Email = email,
            Status = UserStatus.Invited,
            FailedAttempts = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>Define a senha do usuário e o ativa — usado no aceite de convite (<see cref="OwnerInvite.Consume"/>).</summary>
    public void SetPassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("O hash de senha é obrigatório.");

        PasswordHash = passwordHash;
        Status = UserStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordFailedLogin()
    {
        FailedAttempts++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordSuccessfulLogin()
    {
        FailedAttempts = 0;
        BlockedUntil = null;
        LastLoginAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Block(DateTimeOffset until)
    {
        BlockedUntil = until;
        Status = UserStatus.Blocked;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Unblock()
    {
        BlockedUntil = null;
        FailedAttempts = 0;
        Status = UserStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// <paramref name="pinLookup"/> obrigatório (diferente do parâmetro opcional equivalente em
    /// <see cref="Create"/>, que precisa continuar aceitando chamadas posicionais antigas sem PIN
    /// nenhum): todo caminho que ROTACIONA um PIN já sabe qual PIN está sendo definido, então não há
    /// razão para permitir null aqui e reintroduzir o mesmo bug do índice único (ADR-014, US-004
    /// gap "RotatePin é código morto" — nenhum command chama isto ainda; ver gap documentado no
    /// relatório da correção).
    /// </summary>
    public void RotatePin(string pinHash, string pinLookup)
    {
        if (string.IsNullOrWhiteSpace(pinHash))
            throw new DomainException("O novo PIN é obrigatório.");

        if (string.IsNullOrWhiteSpace(pinLookup))
            throw new DomainException("O digest de busca do novo PIN é obrigatório.");

        PinHash = pinHash;
        PinLookup = pinLookup;
        PinRotatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        Status = UserStatus.Inactive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SoftDelete()
    {
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
