# US-010 — Manter Sessão Ativa com Segurança — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement secure session persistence — validate session against backend at app launch, rotate refresh tokens, fire analytics events, and redirect by access status.

**Architecture:** Backend gets a `refresh_tokens` table + `GET /api/auth/session` endpoint + `POST /api/auth/refresh-token` handler. Flutter gets a Dio auth interceptor that attaches JWTs and refreshes on 401, plus `SecureSessionRepository` is updated to call backend validation at splash.

**Tech Stack:** ASP.NET Core 10 (MediatR, EF Core, Npgsql), Flutter (Riverpod, Dio, flutter_secure_storage), xUnit + FluentAssertions + Testcontainers, flutter_test + mocktail

---

## Context for the engineer

- `sessionRepositoryProvider` is **already overridden** in `main.dart` with `SecureSessionRepository` — token persistence after login works. But session is never validated against the backend on app launch.
- `dioClientProvider` in `apps/mobile/lib/core/network/dio_client.dart` is a plain Dio with no auth interceptor. Auth endpoints use it. We will **rename** it `unauthenticatedDioProvider` and add a new `authenticatedDioProvider` (with interceptor) for protected routes.
- `RefreshTokenCommand` already exists but has **no handler** — it will throw at runtime. We implement the handler here.
- Auth handlers (Register, Login, Google) generate a refresh token but **never store it** in the DB. We fix that here.
- `UserDto.AccessStatus` is always `null` in current responses. We add `User.TrialEndsAt` and `ComputeAccessStatus()` to fix this.
- Existing integration test `AuthLoginEndpointTests` asserts `body.User.AccessStatus.Should().BeNull()` — this must be updated.

---

## File Map

### Backend — create

| File | Purpose |
|------|---------|
| `backend/src/Awaken.Domain/Entities/Auth/RefreshToken.cs` | Entity with UserId, TokenHash (SHA-256), ExpiresAt, IsRevoked |
| `backend/src/Awaken.Domain/Repositories/IRefreshTokenRepository.cs` | GetByTokenHashAsync + RevokeAllByUserIdAsync |
| `backend/src/Awaken.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs` | EF table mapping |
| `backend/src/Awaken.Infrastructure/Persistence/Repositories/RefreshTokenRepository.cs` | EF implementation |
| `backend/src/Awaken.Application/Auth/Commands/RefreshToken/RefreshTokenCommandHandler.cs` | Token rotation handler |
| `backend/src/Awaken.Contracts/Auth/SessionResponse.cs` | DTO for GET /api/auth/session |
| `backend/src/Awaken.Application/Auth/Queries/GetSession/GetSessionQuery.cs` | Query marker |
| `backend/src/Awaken.Application/Auth/Queries/GetSession/GetSessionQueryHandler.cs` | Loads user, computes status |
| `backend/tests/Awaken.UnitTests/Auth/RefreshTokenCommandHandlerTests.cs` | Handler unit tests |
| `backend/tests/Awaken.UnitTests/Auth/GetSessionQueryHandlerTests.cs` | Handler unit tests |
| `backend/tests/Awaken.IntegrationTests/AuthRefreshTokenEndpointTests.cs` | Endpoint tests |
| `backend/tests/Awaken.IntegrationTests/AuthSessionEndpointTests.cs` | Endpoint tests |

### Backend — modify

| File | Change |
|------|--------|
| `backend/src/Awaken.Domain/Entities/Auth/User.cs` | Add `TrialEndsAt`, `ComputeAccessStatus()` |
| `backend/src/Awaken.Infrastructure/Persistence/Configurations/UserConfiguration.cs` | Map `TrialEndsAt` |
| `backend/src/Awaken.Infrastructure/Persistence/AwakenDbContext.cs` | Add `DbSet<RefreshToken>` |
| `backend/src/Awaken.Infrastructure/DependencyInjection.cs` | Register `IRefreshTokenRepository` |
| `backend/src/Awaken.Application/Common/Interfaces/IJwtService.cs` | Add `HashRefreshToken(string)` |
| `backend/src/Awaken.Infrastructure/Services/JwtService.cs` | Implement `HashRefreshToken` |
| `backend/src/Awaken.Application/Auth/Commands/Register/RegisterUserCommandHandler.cs` | Store refresh token + return AccessStatus |
| `backend/src/Awaken.Application/Auth/Commands/Login/LoginUserCommandHandler.cs` | Store refresh token + return AccessStatus |
| `backend/src/Awaken.Application/Auth/Commands/GoogleSignIn/GoogleSignInCommandHandler.cs` | Store refresh token + return AccessStatus |
| `backend/src/Awaken.Api/Controllers/V1/AuthController.cs` | Add GET /api/auth/session |
| `backend/tests/Awaken.UnitTests/Auth/LoginUserCommandHandlerTests.cs` | Add IRefreshTokenRepository mock |
| `backend/tests/Awaken.UnitTests/Auth/RegisterUserCommandHandlerTests.cs` | Add IRefreshTokenRepository mock |
| `backend/tests/Awaken.UnitTests/Auth/GoogleSignInCommandHandlerTests.cs` | Add IRefreshTokenRepository mock |
| `backend/tests/Awaken.IntegrationTests/AuthLoginEndpointTests.cs` | Update AccessStatus assertion |

### Flutter — create

| File | Purpose |
|------|---------|
| `apps/mobile/lib/features/auth/data/dtos/session_response_dto.dart` | DTO for GET /api/auth/session response |
| `apps/mobile/lib/core/network/auth_interceptor.dart` | Dio interceptor: attach token + 401 refresh |
| `apps/mobile/test/core/network/auth_interceptor_test.dart` | Unit tests |
| `apps/mobile/test/features/auth/data/dtos/session_response_dto_test.dart` | DTO parse tests |
| `apps/mobile/test/core/auth/secure_session_repository_test.dart` | Repository unit tests |

### Flutter — modify

| File | Change |
|------|--------|
| `apps/mobile/lib/core/errors/app_error.dart` | Add `SessionInvalidError` |
| `apps/mobile/lib/features/auth/data/datasources/auth_remote_data_source.dart` | Add `validateSession()` + `refreshToken()` |
| `apps/mobile/lib/core/network/dio_client.dart` | Rename to `unauthenticatedDioProvider`; add `authenticatedDioProvider` |
| `apps/mobile/lib/features/auth/presentation/providers/auth_providers.dart` | Update to use `unauthenticatedDioProvider` |
| `apps/mobile/lib/core/auth/session_repository.dart` | Add `hasLocalSession()` |
| `apps/mobile/lib/core/auth/fake_session_repository.dart` | Implement `hasLocalSession()` |
| `apps/mobile/lib/core/auth/secure_session_repository.dart` | Add backend validation flow |
| `apps/mobile/lib/features/splash/presentation/controllers/splash_controller.dart` | Fire session analytics events |
| `apps/mobile/test/features/splash/presentation/controllers/splash_controller_test.dart` | Test new analytics events |

---

## Task 1: Domain — User.TrialEndsAt + RefreshToken entity + IRefreshTokenRepository

**Files:**
- Modify: `backend/src/Awaken.Domain/Entities/Auth/User.cs`
- Create: `backend/src/Awaken.Domain/Entities/Auth/RefreshToken.cs`
- Create: `backend/src/Awaken.Domain/Repositories/IRefreshTokenRepository.cs`

- [ ] **Step 1.1: Add TrialEndsAt to User entity**

Open `backend/src/Awaken.Domain/Entities/Auth/User.cs` and add the property and method:

```csharp
public DateTime TrialEndsAt { get; private set; }
```

In `Create()`, inside the object initializer, add:
```csharp
TrialEndsAt = DateTime.UtcNow.AddDays(14),
```

In `CreateFromGoogle()`, inside the object initializer, add:
```csharp
TrialEndsAt = DateTime.UtcNow.AddDays(14),
```

After `UpdateProfile()`, add the new method:
```csharp
public string ComputeAccessStatus(DateTime utcNow) =>
    TrialEndsAt > utcNow ? "trial_active" : "trial_expired";
```

- [ ] **Step 1.2: Create RefreshToken entity**

Create `backend/src/Awaken.Domain/Entities/Auth/RefreshToken.cs`:

```csharp
using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Auth;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public bool IsRevoked { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAtUtc) =>
        new()
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc,
        };

    public void Revoke()
    {
        IsRevoked = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
```

- [ ] **Step 1.3: Create IRefreshTokenRepository**

Create `backend/src/Awaken.Domain/Repositories/IRefreshTokenRepository.cs`:

```csharp
using Awaken.Domain.Common;
using Awaken.Domain.Entities.Auth;

namespace Awaken.Domain.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task RevokeAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 1.4: Build to verify no compile errors**

```bash
cd backend
dotnet build src/Awaken.Domain/Awaken.Domain.csproj
```

Expected: Build succeeded, 0 error(s).

---

## Task 2: Infrastructure — EF config + Repository + DbContext + DI

**Files:**
- Create: `backend/src/Awaken.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`
- Create: `backend/src/Awaken.Infrastructure/Persistence/Repositories/RefreshTokenRepository.cs`
- Modify: `backend/src/Awaken.Infrastructure/Persistence/AwakenDbContext.cs`
- Modify: `backend/src/Awaken.Infrastructure/DependencyInjection.cs`

- [ ] **Step 2.1: Create RefreshTokenConfiguration**

Create `backend/src/Awaken.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`:

```csharp
using Awaken.Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.UserId).IsRequired();

        builder.Property(rt => rt.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(rt => rt.TokenHash).IsUnique();

        builder.Property(rt => rt.ExpiresAtUtc).IsRequired();

        builder.Property(rt => rt.IsRevoked).IsRequired();
    }
}
```

- [ ] **Step 2.2: Create RefreshTokenRepository**

Create `backend/src/Awaken.Infrastructure/Persistence/Repositories/RefreshTokenRepository.cs`:

```csharp
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository(AwakenDbContext context) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Id == id, cancellationToken);

    public async Task<IEnumerable<RefreshToken>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.RefreshTokens.ToListAsync(cancellationToken);

    public async Task AddAsync(RefreshToken entity, CancellationToken cancellationToken = default) =>
        await context.RefreshTokens.AddAsync(entity, cancellationToken);

    public void Update(RefreshToken entity) => context.RefreshTokens.Update(entity);

    public void Remove(RefreshToken entity) => context.RefreshTokens.Remove(entity);

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

    public async Task RevokeAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);
        foreach (var token in tokens)
            token.Revoke();
    }
}
```

- [ ] **Step 2.3: Add DbSet<RefreshToken> to AwakenDbContext**

In `backend/src/Awaken.Infrastructure/Persistence/AwakenDbContext.cs`, add:

```csharp
public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
```

(After the existing DbSet<User> line.)

- [ ] **Step 2.4: Register IRefreshTokenRepository in DI**

In `backend/src/Awaken.Infrastructure/DependencyInjection.cs`, add after the `IUserRepository` registration:

```csharp
services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
```

- [ ] **Step 2.5: Update UserConfiguration to map TrialEndsAt**

In `backend/src/Awaken.Infrastructure/Persistence/Configurations/UserConfiguration.cs`, add before the closing brace of `Configure()`:

```csharp
builder.Property(u => u.TrialEndsAt)
    .IsRequired();
```

- [ ] **Step 2.6: Build Infrastructure to verify**

```bash
dotnet build src/Awaken.Infrastructure/Awaken.Infrastructure.csproj
```

Expected: Build succeeded, 0 error(s).

---

## Task 3: EF Migration

- [ ] **Step 3.1: Add migration**

```bash
cd backend
dotnet ef migrations add AddRefreshTokensAndUserTrialEndsAt -p src/Awaken.Infrastructure -s src/Awaken.Api
```

Expected: A new migration file appears in `src/Awaken.Infrastructure/Persistence/Migrations/`.

- [ ] **Step 3.2: Verify migration content**

Open the generated migration file. It must:
- Create table `refresh_tokens` with columns: `Id`, `UserId`, `TokenHash` (varchar 128, unique index), `ExpiresAtUtc`, `IsRevoked`, `CreatedAtUtc`, `UpdatedAtUtc`, `DeletedAtUtc`, `CreatedByUserId`, `UpdatedByUserId`, `IsDeleted`
- Add column `TrialEndsAt` to `users` table

If the migration is empty or incorrect, something is missing from the entity configuration. Do not proceed until the migration looks correct.

---

## Task 4: IJwtService.HashRefreshToken + update auth handlers + existing tests

**Files:**
- Modify: `backend/src/Awaken.Application/Common/Interfaces/IJwtService.cs`
- Modify: `backend/src/Awaken.Infrastructure/Services/JwtService.cs`
- Modify: `backend/src/Awaken.Application/Auth/Commands/Register/RegisterUserCommandHandler.cs`
- Modify: `backend/src/Awaken.Application/Auth/Commands/Login/LoginUserCommandHandler.cs`
- Modify: `backend/src/Awaken.Application/Auth/Commands/GoogleSignIn/GoogleSignInCommandHandler.cs`
- Modify: `backend/tests/Awaken.UnitTests/Auth/RegisterUserCommandHandlerTests.cs`
- Modify: `backend/tests/Awaken.UnitTests/Auth/LoginUserCommandHandlerTests.cs`
- Modify: `backend/tests/Awaken.UnitTests/Auth/GoogleSignInCommandHandlerTests.cs`
- Modify: `backend/tests/Awaken.IntegrationTests/AuthLoginEndpointTests.cs`

- [ ] **Step 4.1: Add HashRefreshToken to IJwtService**

In `backend/src/Awaken.Application/Common/Interfaces/IJwtService.cs`, add:

```csharp
string HashRefreshToken(string token);
```

- [ ] **Step 4.2: Implement HashRefreshToken in JwtService**

In `backend/src/Awaken.Infrastructure/Services/JwtService.cs`, add using at top:

```csharp
using System.Security.Cryptography;
```

Add the method (SHA-256 hex, uppercase):

```csharp
public string HashRefreshToken(string token) =>
    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
```

(Note: `using System.Text` is already present in this file.)

- [ ] **Step 4.3: Write failing unit tests for updated handlers**

Open `backend/tests/Awaken.UnitTests/Auth/LoginUserCommandHandlerTests.cs`.

Add the `IRefreshTokenRepository` mock field at the top of the class (after `_unitOfWork`):

```csharp
private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
```

Update `CreateHandler()`:

```csharp
private LoginUserCommandHandler CreateHandler() => new(
    _userRepository.Object,
    _passwordHasher.Object,
    _jwtService.Object,
    _dateTimeService.Object,
    _unitOfWork.Object,
    _refreshTokenRepository.Object,
    _configuration);
```

Add setup for `HashRefreshToken` in the existing test `HandleReturnsAuthResponseWhenCredentialsAreValid`:

```csharp
_jwtService.Setup(j => j.HashRefreshToken("refresh-token")).Returns("hashed-token");
```

Add a new assertion in that same test:

```csharp
_refreshTokenRepository.Verify(
    r => r.AddAsync(It.Is<RefreshToken>(rt =>
        rt.UserId == user.Id &&
        rt.TokenHash == "hashed-token"),
        It.IsAny<CancellationToken>()),
    Times.Once);
```

Also update the `AccessStatus` assertion in that test:

```csharp
result.User.AccessStatus.Should().Be("trial_active");
```

- [ ] **Step 4.4: Run tests — expect failures**

```bash
cd backend
dotnet test tests/Awaken.UnitTests --filter "FullyQualifiedName~LoginUserCommandHandlerTests" --no-build 2>&1 | tail -20
```

Expected: Compile error (constructor mismatch) or test failures. Proceed to implement.

- [ ] **Step 4.5: Update LoginUserCommandHandler**

Full updated file `backend/src/Awaken.Application/Auth/Commands/Login/LoginUserCommandHandler.cs`:

```csharp
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Auth;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Awaken.Application.Auth.Commands.Login;

public class LoginUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtService jwtService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IRefreshTokenRepository refreshTokenRepository,
    IConfiguration configuration) : IRequestHandler<LoginUserCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user?.PasswordHash is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("INVALID_CREDENTIALS", "E-mail ou senha inválidos.");

        var utcNow = dateTimeService.UtcNow;
        user.RecordLogin(utcNow);

        var accessToken = jwtService.GenerateAccessToken(user.Id, user.Email, []);
        var rawRefreshToken = jwtService.GenerateRefreshToken();
        var tokenHash = jwtService.HashRefreshToken(rawRefreshToken);
        var expiryMinutes = int.Parse(configuration["Jwt:AccessTokenExpiryMinutes"] ?? "15");
        var refreshExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryDays"] ?? "30");

        await refreshTokenRepository.AddAsync(
            RefreshToken.Create(user.Id, tokenHash, utcNow.AddDays(refreshExpiryDays)),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken,
            rawRefreshToken,
            utcNow.AddMinutes(expiryMinutes),
            new UserDto(
                user.Id,
                user.Email,
                user.DisplayName,
                user.AvatarUrl,
                user.PreferredLanguage,
                user.IsOnboardingComplete,
                user.ComputeAccessStatus(utcNow)));
    }
}
```

- [ ] **Step 4.6: Update RegisterUserCommandHandler**

Full updated file `backend/src/Awaken.Application/Auth/Commands/Register/RegisterUserCommandHandler.cs`:

```csharp
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Auth;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Awaken.Application.Auth.Commands.Register;

public class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtService jwtService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IRefreshTokenRepository refreshTokenRepository,
    IConfiguration configuration) : IRequestHandler<RegisterUserCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
            throw new ConflictException("EMAIL_ALREADY_EXISTS", "Este e-mail já possui uma conta.");

        var utcNow = dateTimeService.UtcNow;
        var passwordHash = passwordHasher.Hash(request.Password);
        var user = User.Create(request.Email, passwordHash, request.DisplayName, request.Language);

        await userRepository.AddAsync(user, cancellationToken);

        var accessToken = jwtService.GenerateAccessToken(user.Id, user.Email, []);
        var rawRefreshToken = jwtService.GenerateRefreshToken();
        var tokenHash = jwtService.HashRefreshToken(rawRefreshToken);
        var expiryMinutes = int.Parse(configuration["Jwt:AccessTokenExpiryMinutes"] ?? "15");
        var refreshExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryDays"] ?? "30");

        await refreshTokenRepository.AddAsync(
            RefreshToken.Create(user.Id, tokenHash, utcNow.AddDays(refreshExpiryDays)),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken,
            rawRefreshToken,
            utcNow.AddMinutes(expiryMinutes),
            new UserDto(
                user.Id,
                user.Email,
                user.DisplayName,
                user.AvatarUrl,
                user.PreferredLanguage,
                user.IsOnboardingComplete,
                user.ComputeAccessStatus(utcNow)));
    }
}
```

- [ ] **Step 4.7: Update GoogleSignInCommandHandler**

Full updated file `backend/src/Awaken.Application/Auth/Commands/GoogleSignIn/GoogleSignInCommandHandler.cs`:

```csharp
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Auth;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Awaken.Application.Auth.Commands.GoogleSignIn;

public class GoogleSignInCommandHandler(
    IUserRepository userRepository,
    IGoogleTokenValidator googleTokenValidator,
    IJwtService jwtService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IRefreshTokenRepository refreshTokenRepository,
    IConfiguration configuration) : IRequestHandler<GoogleSignInCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(GoogleSignInCommand request, CancellationToken cancellationToken)
    {
        var payload = await googleTokenValidator.ValidateAsync(request.ProviderCredential, cancellationToken);
        if (payload is null)
            throw new UnauthorizedException("GOOGLE_AUTH_FAILED", "Não foi possível entrar com Google.");

        var utcNow = dateTimeService.UtcNow;
        var user = await userRepository.GetByProviderAsync(AuthProvider.Google, payload.ProviderUserId, cancellationToken);

        if (user is null)
        {
            user = await userRepository.GetByEmailAsync(payload.Email, cancellationToken);
            if (user is not null)
                user.LinkGoogleProvider(payload.ProviderUserId, utcNow);
            else
            {
                user = User.CreateFromGoogle(payload.Email, payload.ProviderUserId, payload.Name, payload.Picture);
                await userRepository.AddAsync(user, cancellationToken);
            }
        }

        user.RecordLogin(utcNow);

        var accessToken = jwtService.GenerateAccessToken(user.Id, user.Email, []);
        var rawRefreshToken = jwtService.GenerateRefreshToken();
        var tokenHash = jwtService.HashRefreshToken(rawRefreshToken);
        var expiryMinutes = int.Parse(configuration["Jwt:AccessTokenExpiryMinutes"] ?? "15");
        var refreshExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryDays"] ?? "30");

        await refreshTokenRepository.AddAsync(
            RefreshToken.Create(user.Id, tokenHash, utcNow.AddDays(refreshExpiryDays)),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken,
            rawRefreshToken,
            utcNow.AddMinutes(expiryMinutes),
            new UserDto(
                user.Id,
                user.Email,
                user.DisplayName,
                user.AvatarUrl,
                user.PreferredLanguage,
                user.IsOnboardingComplete,
                user.ComputeAccessStatus(utcNow)));
    }
}
```

- [ ] **Step 4.8: Update existing unit tests to add IRefreshTokenRepository mock**

In `backend/tests/Awaken.UnitTests/Auth/RegisterUserCommandHandlerTests.cs`:

Add at top of class:
```csharp
private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
```

Add `using Awaken.Domain.Repositories;` if not present.

Update `CreateHandler()` to include `_refreshTokenRepository.Object` in the constructor. (Match the order in the updated handler: `userRepository, passwordHasher, jwtService, dateTimeService, unitOfWork, refreshTokenRepository, configuration`)

In all tests that call the handler, add setup:
```csharp
_jwtService.Setup(j => j.HashRefreshToken(It.IsAny<string>())).Returns("hashed-token");
```

Update any assertion on `result.User.AccessStatus` from `.BeNull()` to `.Be("trial_active")`.

In `backend/tests/Awaken.UnitTests/Auth/GoogleSignInCommandHandlerTests.cs`, make the same additions (IRefreshTokenRepository mock + HashRefreshToken setup).

- [ ] **Step 4.9: Fix AuthLoginEndpointTests AccessStatus assertion**

In `backend/tests/Awaken.IntegrationTests/AuthLoginEndpointTests.cs`, line:
```csharp
body.User.AccessStatus.Should().BeNull();
```
Change to:
```csharp
body.User.AccessStatus.Should().Be("trial_active");
```

- [ ] **Step 4.10: Run unit tests**

```bash
cd backend
dotnet test tests/Awaken.UnitTests -v q
```

Expected: All pass.

- [ ] **Step 4.11: Commit**

```bash
git add backend/src backend/tests
git commit -m "feat(auth): add TrialEndsAt + RefreshToken storage + return AccessStatus in auth handlers"
```

---

## Task 5: RefreshTokenCommandHandler

**Files:**
- Create: `backend/src/Awaken.Application/Auth/Commands/RefreshToken/RefreshTokenCommandHandler.cs`
- Create: `backend/tests/Awaken.UnitTests/Auth/RefreshTokenCommandHandlerTests.cs`

- [ ] **Step 5.1: Write failing unit tests**

Create `backend/tests/Awaken.UnitTests/Auth/RefreshTokenCommandHandlerTests.cs`:

```csharp
using Awaken.Application.Auth.Commands.RefreshToken;
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Awaken.UnitTests.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly IConfiguration _configuration;

    private readonly DateTime _utcNow = new(2026, 6, 17, 12, 0, 0, DateTimeKind.Utc);

    public RefreshTokenCommandHandlerTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:AccessTokenExpiryMinutes"] = "15",
                ["Jwt:RefreshTokenExpiryDays"] = "30",
            })
            .Build();

        _dateTimeService.Setup(d => d.UtcNow).Returns(_utcNow);
    }

    private RefreshTokenCommandHandler CreateHandler() => new(
        _refreshTokenRepository.Object,
        _userRepository.Object,
        _jwtService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _configuration);

    [Fact]
    public async Task HandleReturnsNewAuthResponseOnValidRefreshToken()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter", "pt-BR");
        var storedToken = RefreshToken.Create(user.Id, "hashed-old-token", _utcNow.AddDays(30));

        _jwtService.Setup(j => j.HashRefreshToken("raw-old-token")).Returns("hashed-old-token");
        _refreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync("hashed-old-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _jwtService.Setup(j => j.GenerateAccessToken(user.Id, user.Email, It.IsAny<string[]>()))
            .Returns("new-access-token");
        _jwtService.Setup(j => j.GenerateRefreshToken()).Returns("raw-new-token");
        _jwtService.Setup(j => j.HashRefreshToken("raw-new-token")).Returns("hashed-new-token");

        var command = new RefreshTokenCommand("raw-old-token");
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().Be("raw-new-token");
        storedToken.IsRevoked.Should().BeTrue();
        _refreshTokenRepository.Verify(
            r => r.AddAsync(It.Is<RefreshToken>(rt =>
                rt.UserId == user.Id && rt.TokenHash == "hashed-new-token"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleThrowsWhenTokenNotFound()
    {
        _jwtService.Setup(j => j.HashRefreshToken("unknown-token")).Returns("hashed-unknown");
        _refreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync("hashed-unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var act = () => CreateHandler().Handle(new RefreshTokenCommand("unknown-token"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<UnauthorizedException>();
        ex.Which.Code.Should().Be("SESSION_INVALID");
    }

    [Fact]
    public async Task HandleThrowsWhenTokenIsRevoked()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter", "pt-BR");
        var revokedToken = RefreshToken.Create(user.Id, "hashed-token", _utcNow.AddDays(30));
        revokedToken.Revoke();

        _jwtService.Setup(j => j.HashRefreshToken("raw-token")).Returns("hashed-token");
        _refreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync("hashed-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedToken);

        var act = () => CreateHandler().Handle(new RefreshTokenCommand("raw-token"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<UnauthorizedException>();
        ex.Which.Code.Should().Be("SESSION_INVALID");
    }

    [Fact]
    public async Task HandleThrowsWhenTokenIsExpired()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter", "pt-BR");
        var expiredToken = RefreshToken.Create(user.Id, "hashed-token", _utcNow.AddDays(-1));

        _jwtService.Setup(j => j.HashRefreshToken("raw-token")).Returns("hashed-token");
        _refreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync("hashed-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);

        var act = () => CreateHandler().Handle(new RefreshTokenCommand("raw-token"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<UnauthorizedException>();
        ex.Which.Code.Should().Be("SESSION_INVALID");
    }
}
```

- [ ] **Step 5.2: Run tests — expect compile failure**

```bash
cd backend
dotnet test tests/Awaken.UnitTests --filter "FullyQualifiedName~RefreshTokenCommandHandlerTests" 2>&1 | tail -10
```

Expected: Compile error — `RefreshTokenCommandHandler` doesn't exist yet.

- [ ] **Step 5.3: Implement RefreshTokenCommandHandler**

Create `backend/src/Awaken.Application/Auth/Commands/RefreshToken/RefreshTokenCommandHandler.cs`:

```csharp
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Auth;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Awaken.Application.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IJwtService jwtService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IConfiguration configuration) : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = jwtService.HashRefreshToken(request.RefreshToken);
        var storedToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        var utcNow = dateTimeService.UtcNow;

        if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAtUtc <= utcNow)
            throw new UnauthorizedException("SESSION_INVALID", "Sua sessão expirou. Entre novamente.");

        var user = await userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);
        if (user is null)
            throw new UnauthorizedException("SESSION_INVALID", "Sua sessão expirou. Entre novamente.");

        storedToken.Revoke();

        var newAccessToken = jwtService.GenerateAccessToken(user.Id, user.Email, []);
        var newRawRefreshToken = jwtService.GenerateRefreshToken();
        var newTokenHash = jwtService.HashRefreshToken(newRawRefreshToken);
        var refreshExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryDays"] ?? "30");
        var accessExpiryMinutes = int.Parse(configuration["Jwt:AccessTokenExpiryMinutes"] ?? "15");

        await refreshTokenRepository.AddAsync(
            RefreshToken.Create(user.Id, newTokenHash, utcNow.AddDays(refreshExpiryDays)),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            newAccessToken,
            newRawRefreshToken,
            utcNow.AddMinutes(accessExpiryMinutes),
            new UserDto(
                user.Id,
                user.Email,
                user.DisplayName,
                user.AvatarUrl,
                user.PreferredLanguage,
                user.IsOnboardingComplete,
                user.ComputeAccessStatus(utcNow)));
    }
}
```

- [ ] **Step 5.4: Run unit tests**

```bash
cd backend
dotnet test tests/Awaken.UnitTests --filter "FullyQualifiedName~RefreshTokenCommandHandlerTests" -v q
```

Expected: 4 tests, all pass.

- [ ] **Step 5.5: Commit**

```bash
git add backend/src/Awaken.Application/Auth/Commands/RefreshToken/ backend/tests/Awaken.UnitTests/Auth/RefreshTokenCommandHandlerTests.cs
git commit -m "feat(auth): implement RefreshTokenCommandHandler with token rotation"
```

---

## Task 6: GetSession query + handler + controller endpoint + unit tests

**Files:**
- Create: `backend/src/Awaken.Contracts/Auth/SessionResponse.cs`
- Create: `backend/src/Awaken.Application/Auth/Queries/GetSession/GetSessionQuery.cs`
- Create: `backend/src/Awaken.Application/Auth/Queries/GetSession/GetSessionQueryHandler.cs`
- Modify: `backend/src/Awaken.Api/Controllers/V1/AuthController.cs`
- Create: `backend/tests/Awaken.UnitTests/Auth/GetSessionQueryHandlerTests.cs`

- [ ] **Step 6.1: Create SessionResponse DTO**

Create `backend/src/Awaken.Contracts/Auth/SessionResponse.cs`:

```csharp
namespace Awaken.Contracts.Auth;

public record SessionResponse(
    Guid UserId,
    bool IsAuthenticated,
    string AccessStatus,
    bool OnboardingCompleted);
```

- [ ] **Step 6.2: Create GetSessionQuery**

Create `backend/src/Awaken.Application/Auth/Queries/GetSession/GetSessionQuery.cs`:

```csharp
using Awaken.Contracts.Auth;
using MediatR;

namespace Awaken.Application.Auth.Queries.GetSession;

public record GetSessionQuery : IRequest<SessionResponse>;
```

- [ ] **Step 6.3: Write failing unit tests for GetSessionQueryHandler**

Create `backend/tests/Awaken.UnitTests/Auth/GetSessionQueryHandlerTests.cs`:

```csharp
using Awaken.Application.Auth.Queries.GetSession;
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Auth;

public class GetSessionQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();

    private readonly DateTime _utcNow = new(2026, 6, 17, 12, 0, 0, DateTimeKind.Utc);

    public GetSessionQueryHandlerTests()
    {
        _dateTimeService.Setup(d => d.UtcNow).Returns(_utcNow);
    }

    private GetSessionQueryHandler CreateHandler() => new(
        _userRepository.Object,
        _currentUserService.Object,
        _dateTimeService.Object);

    [Fact]
    public async Task HandleReturnsSessionResponseWithTrialActiveForNewUser()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hash", "Hunter", "pt-BR");
        // TrialEndsAt is set to DateTime.UtcNow.AddDays(14) in Create()

        _currentUserService.Setup(s => s.UserId).Returns(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await CreateHandler().Handle(new GetSessionQuery(), CancellationToken.None);

        result.IsAuthenticated.Should().BeTrue();
        result.AccessStatus.Should().Be("trial_active");
        result.OnboardingCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task HandleThrowsWhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        _currentUserService.Setup(s => s.UserId).Returns(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => CreateHandler().Handle(new GetSessionQuery(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<UnauthorizedException>();
        ex.Which.Code.Should().Be("SESSION_INVALID");
    }
}
```

- [ ] **Step 6.4: Run tests — expect compile failure**

```bash
dotnet test tests/Awaken.UnitTests --filter "FullyQualifiedName~GetSessionQueryHandlerTests" 2>&1 | tail -5
```

Expected: `GetSessionQueryHandler` not found.

- [ ] **Step 6.5: Implement GetSessionQueryHandler**

Create `backend/src/Awaken.Application/Auth/Queries/GetSession/GetSessionQueryHandler.cs`:

```csharp
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Auth;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Auth.Queries.GetSession;

public class GetSessionQueryHandler(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService) : IRequestHandler<GetSessionQuery, SessionResponse>
{
    public async Task<SessionResponse> Handle(GetSessionQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUserService.UserId, cancellationToken);
        if (user is null)
            throw new UnauthorizedException("SESSION_INVALID", "Sessão inválida.");

        return new SessionResponse(
            user.Id,
            true,
            user.ComputeAccessStatus(dateTimeService.UtcNow),
            user.IsOnboardingComplete);
    }
}
```

- [ ] **Step 6.6: Add GET /api/auth/session to AuthController**

In `backend/src/Awaken.Api/Controllers/V1/AuthController.cs`, add usings:

```csharp
using Awaken.Application.Auth.Queries.GetSession;
using Microsoft.AspNetCore.Authorization;
```

Add the new action after the `RefreshToken` action:

```csharp
[HttpGet("session")]
[Authorize]
public async Task<IActionResult> GetSession(CancellationToken ct)
{
    var result = await mediator.Send(new GetSessionQuery(), ct);
    return Ok(result);
}
```

- [ ] **Step 6.7: Run GetSession unit tests**

```bash
dotnet test tests/Awaken.UnitTests --filter "FullyQualifiedName~GetSessionQueryHandlerTests" -v q
```

Expected: 2 tests pass.

- [ ] **Step 6.8: Commit**

```bash
git add backend/src/Awaken.Contracts/Auth/SessionResponse.cs \
         backend/src/Awaken.Application/Auth/Queries/ \
         backend/src/Awaken.Api/Controllers/V1/AuthController.cs \
         backend/tests/Awaken.UnitTests/Auth/GetSessionQueryHandlerTests.cs
git commit -m "feat(auth): add GET /api/auth/session endpoint"
```

---

## Task 7: Backend integration tests (refresh-token + session)

**Files:**
- Create: `backend/tests/Awaken.IntegrationTests/AuthRefreshTokenEndpointTests.cs`
- Create: `backend/tests/Awaken.IntegrationTests/AuthSessionEndpointTests.cs`

- [ ] **Step 7.1: Create AuthRefreshTokenEndpointTests**

Create `backend/tests/Awaken.IntegrationTests/AuthRefreshTokenEndpointTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Common;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Awaken.IntegrationTests;

public class AuthRefreshTokenEndpointTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("awaken")
        .WithUsername("awaken")
        .WithPassword("awaken_test_password")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:PostgreSQL", _postgres.GetConnectionString());
        });
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        await dbContext.Database.MigrateAsync();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task<AuthResponse> RegisterAndLoginAsync()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "hunter@awaken.app",
            password = "Str0ngPass!",
            name = "Hunter",
            language = "pt-BR"
        });
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "hunter@awaken.app",
            password = "Str0ngPass!"
        });
        return (await loginResp.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    [Fact]
    public async Task RefreshTokenReturnsNewTokenPairOnValidToken()
    {
        var auth = await RegisterAndLoginAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/refresh-token", new
        {
            refreshToken = auth.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBe(auth.RefreshToken);
        body.User.AccessStatus.Should().Be("trial_active");
    }

    [Fact]
    public async Task RefreshTokenRejectsReuseOfRotatedToken()
    {
        var auth = await RegisterAndLoginAsync();

        // Use the refresh token once
        await _client.PostAsJsonAsync("/api/auth/refresh-token", new { refreshToken = auth.RefreshToken });

        // Try to use it again — must fail
        var response = await _client.PostAsJsonAsync("/api/auth/refresh-token", new { refreshToken = auth.RefreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.Code.Should().Be("SESSION_INVALID");
    }

    [Fact]
    public async Task RefreshTokenReturnsUnauthorizedForInvalidToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/refresh-token", new
        {
            refreshToken = "invalid-token-that-does-not-exist"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.Code.Should().Be("SESSION_INVALID");
    }
}
```

- [ ] **Step 7.2: Create AuthSessionEndpointTests**

Create `backend/tests/Awaken.IntegrationTests/AuthSessionEndpointTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Common;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Awaken.IntegrationTests;

public class AuthSessionEndpointTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("awaken")
        .WithUsername("awaken")
        .WithPassword("awaken_test_password")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:PostgreSQL", _postgres.GetConnectionString());
        });
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        await dbContext.Database.MigrateAsync();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task<string> RegisterAndGetTokenAsync()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "hunter@awaken.app",
            password = "Str0ngPass!",
            name = "Hunter",
            language = "pt-BR"
        });
        var auth = await resp.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.AccessToken;
    }

    [Fact]
    public async Task GetSessionReturnsSessionInfoForAuthenticatedUser()
    {
        var token = await RegisterAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/auth/session");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SessionResponse>();
        body!.IsAuthenticated.Should().BeTrue();
        body.AccessStatus.Should().Be("trial_active");
        body.OnboardingCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetSessionReturnsUnauthorizedWithNoToken()
    {
        var response = await _client.GetAsync("/api/auth/session");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSessionReturnsUnauthorizedWithInvalidToken()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.jwt.token");

        var response = await _client.GetAsync("/api/auth/session");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

- [ ] **Step 7.3: Run all integration tests**

```bash
cd backend
dotnet test tests/Awaken.IntegrationTests -v q
```

Expected: All pass (including updated `AuthLoginEndpointTests` with `trial_active`).

- [ ] **Step 7.4: Run all backend tests**

```bash
dotnet test -v q
```

Expected: All tests pass.

- [ ] **Step 7.5: Commit**

```bash
git add backend/tests/Awaken.IntegrationTests/AuthRefreshTokenEndpointTests.cs \
         backend/tests/Awaken.IntegrationTests/AuthSessionEndpointTests.cs \
         backend/tests/Awaken.IntegrationTests/AuthLoginEndpointTests.cs
git commit -m "test(auth): add integration tests for refresh-token and session endpoints"
```

---

## Task 8: Flutter — SessionInvalidError + SessionResponseDto + AuthRemoteDataSource

**Files:**
- Modify: `apps/mobile/lib/core/errors/app_error.dart`
- Create: `apps/mobile/lib/features/auth/data/dtos/session_response_dto.dart`
- Modify: `apps/mobile/lib/features/auth/data/datasources/auth_remote_data_source.dart`
- Create: `apps/mobile/test/features/auth/data/dtos/session_response_dto_test.dart`

- [ ] **Step 8.1: Add SessionInvalidError**

In `apps/mobile/lib/core/errors/app_error.dart`, add at the end:

```dart
final class SessionInvalidError extends AppError {
  const SessionInvalidError();
}
```

- [ ] **Step 8.2: Write failing DTO test**

Create `apps/mobile/test/features/auth/data/dtos/session_response_dto_test.dart`:

```dart
import 'package:awaken/features/auth/data/dtos/session_response_dto.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('SessionResponseDto', () {
    test('parses valid session response', () {
      final dto = SessionResponseDto.fromJson({
        'userId': 'abc-123',
        'isAuthenticated': true,
        'accessStatus': 'trial_active',
        'onboardingCompleted': false,
      });

      expect(dto.userId, 'abc-123');
      expect(dto.isAuthenticated, true);
      expect(dto.accessStatus, 'trial_active');
      expect(dto.onboardingCompleted, false);
    });

    test('accessStatus can be null', () {
      final dto = SessionResponseDto.fromJson({
        'userId': 'abc-123',
        'isAuthenticated': true,
        'accessStatus': null,
        'onboardingCompleted': true,
      });

      expect(dto.accessStatus, isNull);
    });
  });
}
```

- [ ] **Step 8.3: Run DTO test — expect failure**

```bash
cd apps/mobile
flutter test test/features/auth/data/dtos/session_response_dto_test.dart
```

Expected: Error — `session_response_dto.dart` not found.

- [ ] **Step 8.4: Create SessionResponseDto**

Create `apps/mobile/lib/features/auth/data/dtos/session_response_dto.dart`:

```dart
class SessionResponseDto {
  const SessionResponseDto({
    required this.userId,
    required this.isAuthenticated,
    required this.accessStatus,
    required this.onboardingCompleted,
  });

  final String userId;
  final bool isAuthenticated;
  final String? accessStatus;
  final bool onboardingCompleted;

  factory SessionResponseDto.fromJson(Map<String, dynamic> json) =>
      SessionResponseDto(
        userId: json['userId'] as String,
        isAuthenticated: json['isAuthenticated'] as bool,
        accessStatus: json['accessStatus'] as String?,
        onboardingCompleted: json['onboardingCompleted'] as bool,
      );
}
```

- [ ] **Step 8.5: Add validateSession + refreshToken to AuthRemoteDataSource**

In `apps/mobile/lib/features/auth/data/datasources/auth_remote_data_source.dart`, add the import at the top:

```dart
import '../dtos/session_response_dto.dart';
import 'package:dio/dio.dart';
```

Add two new methods before `_mapError`:

```dart
Future<SessionResponseDto> validateSession(String accessToken) async {
  try {
    final response = await _dio.get(
      '/api/auth/session',
      options: Options(headers: {'Authorization': 'Bearer $accessToken'}),
    );
    return SessionResponseDto.fromJson(response.data as Map<String, dynamic>);
  } on DioException catch (e) {
    throw _mapSessionError(e);
  }
}

Future<AuthResponseDto> refreshToken(String refreshToken) async {
  try {
    final response = await _dio.post(
      '/api/auth/refresh-token',
      data: {'refreshToken': refreshToken},
    );
    return AuthResponseDto.fromJson(response.data as Map<String, dynamic>);
  } on DioException catch (e) {
    throw _mapSessionError(e);
  }
}
```

Add a new private error mapper after `_mapError`:

```dart
AppError _mapSessionError(DioException e) {
  switch (e.type) {
    case DioExceptionType.connectionTimeout:
    case DioExceptionType.sendTimeout:
    case DioExceptionType.receiveTimeout:
    case DioExceptionType.connectionError:
      return const NetworkError();
    default:
      break;
  }

  final statusCode = e.response?.statusCode;
  final data = e.response?.data;
  final code = data is Map<String, dynamic> ? data['code'] as String? : null;

  if (statusCode == 401 && code == 'SESSION_INVALID') {
    return const SessionInvalidError();
  }

  return const UnexpectedError();
}
```

- [ ] **Step 8.6: Run DTO test**

```bash
flutter test test/features/auth/data/dtos/session_response_dto_test.dart
```

Expected: 2 tests pass.

- [ ] **Step 8.7: Run flutter analyze**

```bash
flutter analyze lib/
```

Expected: No issues.

- [ ] **Step 8.8: Commit**

```bash
git add apps/mobile/lib/core/errors/app_error.dart \
         apps/mobile/lib/features/auth/data/dtos/session_response_dto.dart \
         apps/mobile/lib/features/auth/data/datasources/auth_remote_data_source.dart \
         apps/mobile/test/features/auth/data/dtos/session_response_dto_test.dart
git commit -m "feat(auth): add SessionResponseDto + validateSession + refreshToken to data source"
```

---

## Task 9: Flutter — AuthInterceptor + Dio providers

**Files:**
- Create: `apps/mobile/lib/core/network/auth_interceptor.dart`
- Modify: `apps/mobile/lib/core/network/dio_client.dart`
- Modify: `apps/mobile/lib/features/auth/presentation/providers/auth_providers.dart`
- Create: `apps/mobile/test/core/network/auth_interceptor_test.dart`

- [ ] **Step 9.1: Write failing AuthInterceptor tests**

Create `apps/mobile/test/core/network/auth_interceptor_test.dart`:

```dart
import 'package:awaken/core/errors/app_error.dart';
import 'package:awaken/core/network/auth_interceptor.dart';
import 'package:awaken/core/storage/secure_token_storage.dart';
import 'package:awaken/features/auth/data/dtos/auth_response_dto.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class MockSecureTokenStorage extends Mock implements SecureTokenStorage {}

void main() {
  late MockSecureTokenStorage mockStorage;
  late List<String> sessionExpiredCalls;

  setUp(() {
    mockStorage = MockSecureTokenStorage();
    sessionExpiredCalls = [];
  });

  AuthInterceptor buildInterceptor({
    Future<AuthResponseDto> Function(String)? refreshFn,
  }) =>
      AuthInterceptor(
        storage: mockStorage,
        refreshFn: refreshFn ?? (_) async => throw const SessionInvalidError(),
        onSessionExpired: () => sessionExpiredCalls.add('expired'),
      );

  group('AuthInterceptor.onRequest', () {
    test('attaches Bearer token when access token exists', () async {
      when(() => mockStorage.readAccessToken())
          .thenAnswer((_) async => 'stored-access-token');

      final interceptor = buildInterceptor();
      final options = RequestOptions(path: '/api/some/endpoint');
      final handler = RequestInterceptorHandler();
      await interceptor.onRequest(options, handler);

      expect(
        options.headers['Authorization'],
        'Bearer stored-access-token',
      );
    });

    test('does not attach token when no access token in storage', () async {
      when(() => mockStorage.readAccessToken()).thenAnswer((_) async => null);

      final interceptor = buildInterceptor();
      final options = RequestOptions(path: '/api/some/endpoint');
      final handler = RequestInterceptorHandler();
      await interceptor.onRequest(options, handler);

      expect(options.headers.containsKey('Authorization'), isFalse);
    });
  });

  group('AuthInterceptor.onError — 401 handling', () {
    test('non-401 errors pass through unchanged', () async {
      final interceptor = buildInterceptor();
      final error = DioException(
        requestOptions: RequestOptions(path: '/api/foo'),
        response: Response(
          requestOptions: RequestOptions(path: '/api/foo'),
          statusCode: 500,
        ),
        type: DioExceptionType.badResponse,
      );
      final handler = ErrorInterceptorHandler();
      await interceptor.onError(error, handler);
      // handler.next() is called — if it throws, the test fails
    });

    test('skips retry on refresh-token endpoint to avoid infinite loop', () async {
      bool sessionExpiredFired = false;
      when(() => mockStorage.readRefreshToken())
          .thenAnswer((_) async => 'some-refresh-token');

      final interceptor = AuthInterceptor(
        storage: mockStorage,
        refreshFn: (_) async => throw const SessionInvalidError(),
        onSessionExpired: () => sessionExpiredFired = true,
      );

      final error = DioException(
        requestOptions: RequestOptions(path: '/api/auth/refresh-token'),
        response: Response(
          requestOptions: RequestOptions(path: '/api/auth/refresh-token'),
          statusCode: 401,
          data: {'code': 'SESSION_INVALID'},
        ),
        type: DioExceptionType.badResponse,
      );

      final handler = ErrorInterceptorHandler();
      await interceptor.onError(error, handler);

      expect(sessionExpiredFired, isTrue);
    });

    test('fires onSessionExpired when refresh fails', () async {
      when(() => mockStorage.readRefreshToken())
          .thenAnswer((_) async => 'old-refresh-token');
      when(() => mockStorage.clear()).thenAnswer((_) async {});

      final interceptor = buildInterceptor(
        refreshFn: (_) async => throw const SessionInvalidError(),
      );

      final error = DioException(
        requestOptions: RequestOptions(path: '/api/quests'),
        response: Response(
          requestOptions: RequestOptions(path: '/api/quests'),
          statusCode: 401,
          data: {'code': 'SESSION_INVALID'},
        ),
        type: DioExceptionType.badResponse,
      );

      final handler = ErrorInterceptorHandler();
      await interceptor.onError(error, handler);

      expect(sessionExpiredCalls, ['expired']);
      verify(() => mockStorage.clear()).called(1);
    });
  });
}
```

- [ ] **Step 9.2: Run tests — expect compile failure**

```bash
cd apps/mobile
flutter test test/core/network/auth_interceptor_test.dart 2>&1 | head -20
```

Expected: `auth_interceptor.dart` not found.

- [ ] **Step 9.3: Create AuthInterceptor**

Create `apps/mobile/lib/core/network/auth_interceptor.dart`:

```dart
import 'package:dio/dio.dart';
import '../errors/app_error.dart';
import '../storage/secure_token_storage.dart';
import '../../features/auth/data/dtos/auth_response_dto.dart';

class AuthInterceptor extends Interceptor {
  const AuthInterceptor({
    required SecureTokenStorage storage,
    required Future<AuthResponseDto> Function(String refreshToken) refreshFn,
    required void Function() onSessionExpired,
  })  : _storage = storage,
        _refreshFn = refreshFn,
        _onSessionExpired = onSessionExpired;

  final SecureTokenStorage _storage;
  final Future<AuthResponseDto> Function(String) _refreshFn;
  final void Function() _onSessionExpired;

  @override
  Future<void> onRequest(
      RequestOptions options, RequestInterceptorHandler handler) async {
    final token = await _storage.readAccessToken();
    if (token != null) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  }

  @override
  Future<void> onError(
      DioException err, ErrorInterceptorHandler handler) async {
    if (err.response?.statusCode != 401) {
      handler.next(err);
      return;
    }

    // Avoid infinite loop: don't retry the refresh endpoint itself
    if (err.requestOptions.path.contains('/api/auth/refresh-token')) {
      await _clearAndExpire();
      handler.next(err);
      return;
    }

    final refreshToken = await _storage.readRefreshToken();
    if (refreshToken == null) {
      await _clearAndExpire();
      handler.next(err);
      return;
    }

    try {
      final newAuth = await _refreshFn(refreshToken);
      await _storage.saveTokens(
        accessToken: newAuth.accessToken,
        refreshToken: newAuth.refreshToken,
      );
      await _storage.saveSessionMetadata(
        onboardingCompleted: newAuth.user.isOnboardingComplete,
        accessStatus: newAuth.user.accessStatus?.storageValue,
      );

      // Retry original request with new token
      final retryOptions = err.requestOptions;
      retryOptions.headers['Authorization'] = 'Bearer ${newAuth.accessToken}';
      final dio = Dio(BaseOptions(baseUrl: retryOptions.baseUrl));
      final retryResponse = await dio.fetch(retryOptions);
      handler.resolve(retryResponse);
    } on AppError {
      await _clearAndExpire();
      handler.next(err);
    }
  }

  Future<void> _clearAndExpire() async {
    await _storage.clear();
    _onSessionExpired();
  }
}
```

- [ ] **Step 9.4: Update dio_client.dart — rename + add authenticated provider**

Replace the full content of `apps/mobile/lib/core/network/dio_client.dart`:

```dart
import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../auth/access_status.dart';
import '../auth/session_provider.dart';
import '../auth/session_state.dart';
import '../config/app_config.dart';
import '../storage/secure_token_storage.dart';
import '../../features/auth/data/datasources/auth_remote_data_source.dart';
import 'auth_interceptor.dart';

BaseOptions _baseOptions() => BaseOptions(
      baseUrl: AppConfig.baseUrl,
      connectTimeout: const Duration(seconds: 15),
      receiveTimeout: const Duration(seconds: 15),
      contentType: 'application/json',
    );

/// Plain Dio — no auth headers. Used for auth endpoints (login, register, refresh-token).
final unauthenticatedDioProvider = Provider<Dio>((ref) {
  return Dio(_baseOptions());
});

/// Dio with AuthInterceptor — attaches Bearer token and handles 401 refresh.
/// Use this for all protected API calls.
final authenticatedDioProvider = Provider<Dio>((ref) {
  final storage = ref.read(secureTokenStorageProvider);
  final unauthDio = ref.read(unauthenticatedDioProvider);
  final authDs = AuthRemoteDataSource(unauthDio);

  final dio = Dio(_baseOptions());
  dio.interceptors.add(AuthInterceptor(
    storage: storage,
    refreshFn: authDs.refreshToken,
    onSessionExpired: () => ref
        .read(currentSessionStateProvider.notifier)
        .set(const SessionState.visitor()),
  ));
  return dio;
});

/// Kept for backwards compatibility — resolves to [unauthenticatedDioProvider].
/// @deprecated Use [unauthenticatedDioProvider] or [authenticatedDioProvider] explicitly.
final dioClientProvider = unauthenticatedDioProvider;
```

- [ ] **Step 9.5: Update auth_providers.dart to use unauthenticatedDioProvider explicitly**

In `apps/mobile/lib/features/auth/presentation/providers/auth_providers.dart`:

Change:
```dart
import '../../../../core/network/dio_client.dart';
...
return AuthRemoteDataSource(ref.watch(dioClientProvider));
```
To:
```dart
import '../../../../core/network/dio_client.dart';
...
return AuthRemoteDataSource(ref.watch(unauthenticatedDioProvider));
```

- [ ] **Step 9.6: Run interceptor tests**

```bash
flutter test test/core/network/auth_interceptor_test.dart
```

Expected: 5 tests pass.

- [ ] **Step 9.7: Run analyze**

```bash
flutter analyze lib/
```

Expected: No issues.

- [ ] **Step 9.8: Commit**

```bash
git add apps/mobile/lib/core/network/ \
         apps/mobile/lib/features/auth/presentation/providers/auth_providers.dart \
         apps/mobile/test/core/network/auth_interceptor_test.dart
git commit -m "feat(auth): add AuthInterceptor + authenticatedDioProvider for protected API calls"
```

---

## Task 10: Flutter — SecureSessionRepository backend validation

**Files:**
- Modify: `apps/mobile/lib/core/auth/session_repository.dart`
- Modify: `apps/mobile/lib/core/auth/fake_session_repository.dart`
- Modify: `apps/mobile/lib/core/auth/secure_session_repository.dart`
- Modify: `apps/mobile/lib/main.dart`
- Create: `apps/mobile/test/core/auth/secure_session_repository_test.dart`

- [ ] **Step 10.1: Add hasLocalSession() to SessionRepository**

Replace content of `apps/mobile/lib/core/auth/session_repository.dart`:

```dart
import 'session_state.dart';

abstract class SessionRepository {
  Future<SessionState> getSessionState();
  Future<bool> hasLocalSession();
}
```

- [ ] **Step 10.2: Implement hasLocalSession() in FakeSessionRepository**

Replace content of `apps/mobile/lib/core/auth/fake_session_repository.dart`:

```dart
import 'session_repository.dart';
import 'session_state.dart';

class FakeSessionRepository implements SessionRepository {
  const FakeSessionRepository();

  @override
  Future<SessionState> getSessionState() async => const SessionState.visitor();

  @override
  Future<bool> hasLocalSession() async => false;
}
```

- [ ] **Step 10.3: Write failing tests for SecureSessionRepository**

Create `apps/mobile/test/core/auth/secure_session_repository_test.dart`:

```dart
import 'package:awaken/core/auth/access_status.dart';
import 'package:awaken/core/auth/secure_session_repository.dart';
import 'package:awaken/core/auth/session_state.dart';
import 'package:awaken/core/errors/app_error.dart';
import 'package:awaken/core/storage/secure_token_storage.dart';
import 'package:awaken/features/auth/data/datasources/auth_remote_data_source.dart';
import 'package:awaken/features/auth/data/dtos/auth_response_dto.dart';
import 'package:awaken/features/auth/data/dtos/session_response_dto.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class MockSecureTokenStorage extends Mock implements SecureTokenStorage {}
class MockAuthRemoteDataSource extends Mock implements AuthRemoteDataSource {}

void main() {
  late MockSecureTokenStorage mockStorage;
  late MockAuthRemoteDataSource mockDataSource;

  setUp(() {
    mockStorage = MockSecureTokenStorage();
    mockDataSource = MockAuthRemoteDataSource();
  });

  SecureSessionRepository buildRepo() =>
      SecureSessionRepository(mockStorage, mockDataSource);

  group('hasLocalSession', () {
    test('returns true when access token exists in storage', () async {
      when(() => mockStorage.readAccessToken())
          .thenAnswer((_) async => 'some-token');
      expect(await buildRepo().hasLocalSession(), isTrue);
    });

    test('returns false when no access token', () async {
      when(() => mockStorage.readAccessToken()).thenAnswer((_) async => null);
      expect(await buildRepo().hasLocalSession(), isFalse);
    });
  });

  group('getSessionState — no local tokens', () {
    test('CA-001 variant: returns visitor when no tokens in storage', () async {
      when(() => mockStorage.readAccessToken()).thenAnswer((_) async => null);
      when(() => mockStorage.readRefreshToken()).thenAnswer((_) async => null);

      final state = await buildRepo().getSessionState();

      expect(state.hasSession, isFalse);
      verifyNever(() => mockDataSource.validateSession(any()));
    });
  });

  group('getSessionState — backend validation success', () {
    test('CA-001: returns valid session with fresh data from backend', () async {
      when(() => mockStorage.readAccessToken())
          .thenAnswer((_) async => 'valid-token');
      when(() => mockStorage.readRefreshToken())
          .thenAnswer((_) async => 'valid-refresh');
      when(() => mockDataSource.validateSession('valid-token'))
          .thenAnswer((_) async => const SessionResponseDto(
                userId: 'user-id',
                isAuthenticated: true,
                accessStatus: 'trial_active',
                onboardingCompleted: true,
              ));
      when(() => mockStorage.saveSessionMetadata(
            onboardingCompleted: any(named: 'onboardingCompleted'),
            accessStatus: any(named: 'accessStatus'),
          )).thenAnswer((_) async {});

      final state = await buildRepo().getSessionState();

      expect(state.hasSession, isTrue);
      expect(state.accessStatus, AccessStatus.trialActive);
      expect(state.onboardingCompleted, isTrue);
    });
  });

  group('getSessionState — 401 → refresh flow', () {
    test('CA-002: clears session and returns visitor when refresh fails', () async {
      when(() => mockStorage.readAccessToken())
          .thenAnswer((_) async => 'expired-token');
      when(() => mockStorage.readRefreshToken())
          .thenAnswer((_) async => 'expired-refresh');
      when(() => mockDataSource.validateSession('expired-token'))
          .thenThrow(const SessionInvalidError());
      when(() => mockDataSource.refreshToken('expired-refresh'))
          .thenThrow(const SessionInvalidError());
      when(() => mockStorage.clear()).thenAnswer((_) async {});

      final state = await buildRepo().getSessionState();

      expect(state.hasSession, isFalse);
      verify(() => mockStorage.clear()).called(1);
    });

    test('CA-001: refreshes and returns active session when access token expired but refresh valid', () async {
      when(() => mockStorage.readAccessToken())
          .thenAnswer((_) async => 'expired-access');
      when(() => mockStorage.readRefreshToken())
          .thenAnswer((_) async => 'valid-refresh');
      when(() => mockDataSource.validateSession('expired-access'))
          .thenThrow(const SessionInvalidError());

      final refreshedAuth = AuthResponseDto(
        accessToken: 'new-access',
        refreshToken: 'new-refresh',
        expiresAtUtc: DateTime.now().toUtc(),
        user: const AuthUserDto(
          id: 'user-id',
          email: 'hunter@awaken.app',
          displayName: null,
          avatarUrl: null,
          preferredLanguage: 'pt-BR',
          isOnboardingComplete: false,
          accessStatus: AccessStatus.trialActive,
        ),
      );

      when(() => mockDataSource.refreshToken('valid-refresh'))
          .thenAnswer((_) async => refreshedAuth);
      when(() => mockStorage.saveTokens(
            accessToken: any(named: 'accessToken'),
            refreshToken: any(named: 'refreshToken'),
          )).thenAnswer((_) async {});
      when(() => mockStorage.saveSessionMetadata(
            onboardingCompleted: any(named: 'onboardingCompleted'),
            accessStatus: any(named: 'accessStatus'),
          )).thenAnswer((_) async {});

      final state = await buildRepo().getSessionState();

      expect(state.hasSession, isTrue);
      expect(state.accessStatus, AccessStatus.trialActive);
    });
  });

  group('getSessionState — network error (offline)', () {
    test('RN-005: falls back to local session on network error', () async {
      when(() => mockStorage.readAccessToken())
          .thenAnswer((_) async => 'token');
      when(() => mockStorage.readRefreshToken())
          .thenAnswer((_) async => 'refresh');
      when(() => mockDataSource.validateSession('token'))
          .thenThrow(const NetworkError());
      when(() => mockStorage.readOnboardingCompleted())
          .thenAnswer((_) async => true);
      when(() => mockStorage.readAccessStatus())
          .thenAnswer((_) async => 'trial_active');

      final state = await buildRepo().getSessionState();

      expect(state.hasSession, isTrue);
      expect(state.accessStatus, AccessStatus.trialActive);
      expect(state.onboardingCompleted, isTrue);
    });
  });
}
```

- [ ] **Step 10.4: Run tests — expect failure**

```bash
flutter test test/core/auth/secure_session_repository_test.dart 2>&1 | head -20
```

Expected: Constructor mismatch or missing methods.

- [ ] **Step 10.5: Update SecureSessionRepository**

Replace full content of `apps/mobile/lib/core/auth/secure_session_repository.dart`:

```dart
import '../errors/app_error.dart';
import '../storage/secure_token_storage.dart';
import '../../features/auth/data/datasources/auth_remote_data_source.dart';
import 'access_status.dart';
import 'session_repository.dart';
import 'session_state.dart';

class SecureSessionRepository implements SessionRepository {
  const SecureSessionRepository(this._tokenStorage, this._authDataSource);

  final SecureTokenStorage _tokenStorage;
  final AuthRemoteDataSource _authDataSource;

  @override
  Future<bool> hasLocalSession() async =>
      (await _tokenStorage.readAccessToken()) != null;

  @override
  Future<SessionState> getSessionState() async {
    final accessToken = await _tokenStorage.readAccessToken();
    final refreshToken = await _tokenStorage.readRefreshToken();
    if (accessToken == null || refreshToken == null) {
      return const SessionState.visitor();
    }

    try {
      final dto = await _authDataSource.validateSession(accessToken);
      await _tokenStorage.saveSessionMetadata(
        onboardingCompleted: dto.onboardingCompleted,
        accessStatus: dto.accessStatus,
      );
      return SessionState(
        hasSession: true,
        onboardingCompleted: dto.onboardingCompleted,
        accessStatus: parseAccessStatus(dto.accessStatus),
      );
    } on SessionInvalidError {
      return _tryRefresh(refreshToken);
    } on NetworkError {
      return _localFallback();
    }
  }

  Future<SessionState> _tryRefresh(String refreshToken) async {
    try {
      final newAuth = await _authDataSource.refreshToken(refreshToken);
      await _tokenStorage.saveTokens(
        accessToken: newAuth.accessToken,
        refreshToken: newAuth.refreshToken,
      );
      await _tokenStorage.saveSessionMetadata(
        onboardingCompleted: newAuth.user.isOnboardingComplete,
        accessStatus: newAuth.user.accessStatus?.storageValue,
      );
      return SessionState(
        hasSession: true,
        onboardingCompleted: newAuth.user.isOnboardingComplete,
        accessStatus: newAuth.user.accessStatus,
      );
    } catch (_) {
      await _tokenStorage.clear();
      return const SessionState.visitor();
    }
  }

  Future<SessionState> _localFallback() async {
    final onboardingCompleted = await _tokenStorage.readOnboardingCompleted();
    final accessStatus = parseAccessStatus(await _tokenStorage.readAccessStatus());
    return SessionState(
      hasSession: true,
      onboardingCompleted: onboardingCompleted,
      accessStatus: accessStatus,
    );
  }
}
```

- [ ] **Step 10.6: Update main.dart to pass AuthRemoteDataSource to SecureSessionRepository**

In `apps/mobile/lib/main.dart`, add imports:

```dart
import 'core/network/dio_client.dart';
import 'features/auth/data/datasources/auth_remote_data_source.dart';
```

Update the `sessionRepositoryProvider` override:

```dart
sessionRepositoryProvider.overrideWith(
  (ref) => SecureSessionRepository(
    ref.watch(secureTokenStorageProvider),
    AuthRemoteDataSource(ref.watch(unauthenticatedDioProvider)),
  ),
),
```

- [ ] **Step 10.7: Run repository tests**

```bash
flutter test test/core/auth/secure_session_repository_test.dart
```

Expected: All tests pass.

- [ ] **Step 10.8: Run analyze**

```bash
flutter analyze lib/
```

Expected: No issues.

- [ ] **Step 10.9: Commit**

```bash
git add apps/mobile/lib/core/auth/ \
         apps/mobile/lib/main.dart \
         apps/mobile/test/core/auth/secure_session_repository_test.dart
git commit -m "feat(auth): SecureSessionRepository validates session against backend at splash"
```

---

## Task 11: Flutter — SplashController session analytics + tests

**Files:**
- Modify: `apps/mobile/lib/features/splash/presentation/controllers/splash_controller.dart`
- Modify: `apps/mobile/test/features/splash/presentation/controllers/splash_controller_test.dart`

- [ ] **Step 11.1: Add session_restored / session_expired analytics to SplashController**

Replace the full `initialize()` method in `apps/mobile/lib/features/splash/presentation/controllers/splash_controller.dart`:

```dart
Future<String?> initialize() async {
  final analytics = ref.read(analyticsServiceProvider);
  await analytics.logEvent('app_opened');
  if (_delay > Duration.zero) {
    await Future.delayed(_delay);
  }

  try {
    final repository = ref.read(sessionRepositoryProvider);
    final hadLocalSession = await repository.hasLocalSession();
    final session = await repository.getSessionState();
    ref.read(currentSessionStateProvider.notifier).set(session);

    if (hadLocalSession && !session.hasSession) {
      await analytics.logEvent('session_expired');
    } else if (session.hasSession) {
      await analytics.logEvent('session_restored');
    }

    if (isAccessBlocked(session)) {
      await analytics.logEvent('access_blocked');
    }
    await analytics.logEvent('splash_viewed');

    final route = resolveInitialRoute(session);
    state = SplashStatus.navigating;
    return route;
  } catch (error) {
    _lastError = error;
    await analytics.logEvent('splash_viewed');
    state = SplashStatus.error;
    return null;
  }
}
```

- [ ] **Step 11.2: Run existing splash tests — verify they still pass**

```bash
flutter test test/features/splash/presentation/controllers/splash_controller_test.dart
```

Expected: All existing tests still pass (the mock session repo returns visitor, `hadLocalSession` returns `false`).

But wait — `MockSessionRepository` in the test doesn't implement `hasLocalSession()` yet. The test will fail because `SessionRepository` now has that abstract method.

Fix: In the test file `splash_controller_test.dart`, the `MockSessionRepository` class is:
```dart
class MockSessionRepository extends Mock implements SessionRepository {}
```

With mocktail, unimplemented methods need stubs. Add the following in `setUp()`:
```dart
when(() => mockSessionRepository.hasLocalSession()).thenAnswer((_) async => false);
```

But the existing tests use `MockSessionRepository` per-test. Find every test that constructs a `MockSessionRepository` and add:
```dart
when(() => repo.hasLocalSession()).thenAnswer((_) async => false);
```

- [ ] **Step 11.3: Add new analytics tests to splash_controller_test.dart**

In `apps/mobile/test/features/splash/presentation/controllers/splash_controller_test.dart`, add inside the `group('SplashController', ...)`:

```dart
test('session_restored disparado quando sessão ativa ao abrir', () async {
  final events = <String>[];
  when(() => mockAnalytics.logEvent(any(), params: any(named: 'params')))
      .thenAnswer((inv) async {
    events.add(inv.positionalArguments.first as String);
  });

  final repo = MockSessionRepository();
  when(() => repo.hasLocalSession()).thenAnswer((_) async => true);
  when(() => repo.getSessionState()).thenAnswer(
    (_) async => const SessionState(
      hasSession: true,
      accessStatus: AccessStatus.trialActive,
      onboardingCompleted: true,
    ),
  );
  final container = buildContainer(sessionRepository: repo);
  addTearDown(container.dispose);

  await container.read(splashControllerProvider.notifier).initialize();

  expect(events, containsAll(['session_restored', 'splash_viewed']));
  expect(events, isNot(contains('session_expired')));
});

test('session_expired disparado quando sessão local existia mas foi invalidada', () async {
  final events = <String>[];
  when(() => mockAnalytics.logEvent(any(), params: any(named: 'params')))
      .thenAnswer((inv) async {
    events.add(inv.positionalArguments.first as String);
  });

  final repo = MockSessionRepository();
  when(() => repo.hasLocalSession()).thenAnswer((_) async => true);
  when(() => repo.getSessionState())
      .thenAnswer((_) async => const SessionState.visitor());
  final container = buildContainer(sessionRepository: repo);
  addTearDown(container.dispose);

  await container.read(splashControllerProvider.notifier).initialize();

  expect(events, containsAll(['session_expired', 'splash_viewed']));
  expect(events, isNot(contains('session_restored')));
});

test('sem session_restored nem session_expired quando não havia sessão local', () async {
  final events = <String>[];
  when(() => mockAnalytics.logEvent(any(), params: any(named: 'params')))
      .thenAnswer((inv) async {
    events.add(inv.positionalArguments.first as String);
  });

  final container = buildContainer(); // FakeSessionRepository — hasLocalSession = false
  addTearDown(container.dispose);

  await container.read(splashControllerProvider.notifier).initialize();

  expect(events, isNot(contains('session_restored')));
  expect(events, isNot(contains('session_expired')));
  expect(events, contains('splash_viewed'));
});
```

Also update any `buildContainer()` that uses a `MockSessionRepository` to stub `hasLocalSession`:
In `buildContainer()`, the `sessionRepository` parameter is optional. In tests that pass a mock repo, add the stub in that test's setup.

For the default container (no sessionRepository): `FakeSessionRepository` is the default, which already returns `false` from `hasLocalSession()`.

- [ ] **Step 11.4: Run all splash controller tests**

```bash
flutter test test/features/splash/presentation/controllers/splash_controller_test.dart -v
```

Expected: All pass (11+ tests).

- [ ] **Step 11.5: Run all Flutter tests**

```bash
flutter test
```

Expected: All tests pass.

- [ ] **Step 11.6: Run flutter analyze**

```bash
flutter analyze lib/
```

Expected: No issues.

- [ ] **Step 11.7: Commit**

```bash
git add apps/mobile/lib/features/splash/presentation/controllers/splash_controller.dart \
         apps/mobile/test/features/splash/presentation/controllers/splash_controller_test.dart
git commit -m "feat(auth): fire session_restored / session_expired analytics in SplashController"
```

---

## Task 12: Final verification

- [ ] **Step 12.1: Run full backend test suite**

```bash
cd backend
dotnet test -v q
```

Expected: All tests pass (unit + integration).

- [ ] **Step 12.2: Run full Flutter test suite**

```bash
cd apps/mobile
flutter test
```

Expected: All tests pass.

- [ ] **Step 12.3: Run flutter analyze**

```bash
flutter analyze lib/
```

Expected: No issues.

- [ ] **Step 12.4: Run dotnet build (full solution)**

```bash
cd backend
dotnet build
```

Expected: Build succeeded, 0 error(s), 0 warning(s).

- [ ] **Step 12.5: Run ARB parity test (verifies all 3 languages have same keys)**

```bash
cd apps/mobile
flutter test test/l10n/arb_parity_test.dart
```

Expected: Pass. (No new ARB keys added in this US — all error/session messages use existing keys.)

---

## Self-Review Checklist

**Spec coverage:**

| Requirement | Covered by |
|-------------|-----------|
| RN-001: Sessão válida permite entrada sem login | Tasks 10-11 (backend validation → active session → home) |
| RN-002: Sessão inválida → login | Task 10 (refresh fails → clear → visitor → route guard → login) |
| RN-003: Sessão com trial expirado → bloqueio | Task 4 (ComputeAccessStatus returns trial_expired after 14 days) |
| RN-004: Logout limpa dados locais | Task 9 (AuthInterceptor calls storage.clear() on session expire) |
| RN-005: Falha de validação exibe erro controlado | Task 10 (NetworkError → local fallback) |
| RN-006: Dados locais protegidos | flutter_secure_storage already in use (unchanged) |
| CA-001: Sessão válida → sem login pedido | Tasks 10-11 |
| CA-002: Sessão inválida → login | Task 10 |
| CA-003: Trial expirado → paywall | Task 4 + route_resolver (already handles isAccessExpired) |
| Analytics: session_restored | Task 11 |
| Analytics: session_expired | Task 11 |
| Analytics: access_blocked | Already in SplashController (unchanged) |
| Backend: GET /api/auth/session | Tasks 6-7 |
| Backend: POST /api/auth/refresh-token | Tasks 5, 7 |
| AccessStatus in auth responses | Task 4 |
| Refresh token stored in DB | Tasks 1-4 |
| Token rotation | Task 5 |
| Offline graceful fallback | Task 10 |
| i18n (pt-BR, en, es) | No new user-visible messages in this US; existing keys cover all cases |

**No placeholders:** All steps include complete code.

**Type consistency:**
- `RefreshToken.Create(Guid, string, DateTime)` — used identically in Tasks 2, 4, 5
- `user.ComputeAccessStatus(DateTime)` returns `string` — used in Tasks 4, 5, 6
- `AuthRemoteDataSource.validateSession(String)` → `SessionResponseDto` — used in Tasks 8, 10
- `AuthRemoteDataSource.refreshToken(String)` → `AuthResponseDto` — used in Tasks 8, 9, 10
- `SessionRepository.hasLocalSession()` → `Future<bool>` — used in Tasks 10, 11
