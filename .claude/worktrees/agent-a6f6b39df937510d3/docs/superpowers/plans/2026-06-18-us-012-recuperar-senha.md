# US-012 Recuperar Senha — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement password recovery (forgot-password) flow — backend endpoint that generates a reset token and sends email (no-op stub for MVP), plus Flutter screen with l10n in pt-BR, EN, and ES, navigable from the login page.

**Architecture:** Backend uses a new `PasswordResetRequest` domain entity (stores hashed token + expiry); handler always returns generic success (RN-002 — never reveals whether email exists); email delivery is abstracted behind `IEmailService` (no-op stub logs the token, real SMTP/Firebase can be wired later). Flutter has a dedicated `ForgotPasswordPage` reached from the login screen; state machine mirrors the LogoutController/RegisterController patterns.

**Tech Stack:** .NET 10, MediatR, FluentValidation, EF Core + PostgreSQL, xUnit + Moq + FluentAssertions + Testcontainers; Flutter + Riverpod (Notifier) + Dio + go_router + mocktail.

---

## File Map

### Backend — create
| Path | Responsibility |
|---|---|
| `backend/src/Awaken.Domain/Entities/Auth/PasswordResetRequest.cs` | Domain entity (token hash, expiry, used-at) |
| `backend/src/Awaken.Domain/Repositories/IPasswordResetRepository.cs` | Repository interface |
| `backend/src/Awaken.Application/Common/Interfaces/IEmailService.cs` | Email abstraction |
| `backend/src/Awaken.Application/Auth/Commands/ForgotPassword/ForgotPasswordCommand.cs` | MediatR command |
| `backend/src/Awaken.Application/Auth/Commands/ForgotPassword/ForgotPasswordCommandHandler.cs` | Handler |
| `backend/src/Awaken.Application/Auth/Commands/ForgotPassword/ForgotPasswordValidator.cs` | FluentValidation |
| `backend/src/Awaken.Contracts/Auth/ForgotPasswordRequest.cs` | Request DTO |
| `backend/src/Awaken.Infrastructure/Persistence/Configurations/PasswordResetConfiguration.cs` | EF config |
| `backend/src/Awaken.Infrastructure/Persistence/Repositories/PasswordResetRepository.cs` | EF repository |
| `backend/src/Awaken.Infrastructure/Services/EmailService.cs` | No-op stub |
| `backend/tests/Awaken.UnitTests/Auth/ForgotPasswordValidatorTests.cs` | Validator unit tests |
| `backend/tests/Awaken.UnitTests/Auth/ForgotPasswordCommandHandlerTests.cs` | Handler unit tests |
| `backend/tests/Awaken.IntegrationTests/AuthForgotPasswordEndpointTests.cs` | API integration tests |

### Backend — modify
| Path | Change |
|---|---|
| `backend/src/Awaken.Infrastructure/Persistence/AwakenDbContext.cs` | Add `DbSet<PasswordResetRequest>` |
| `backend/src/Awaken.Infrastructure/DependencyInjection.cs` | Register `IPasswordResetRepository` + `IEmailService` |
| `backend/src/Awaken.Api/Controllers/V1/AuthController.cs` | Add `POST /api/auth/forgot-password` |

### Flutter — create
| Path | Responsibility |
|---|---|
| `apps/mobile/lib/features/auth/data/dtos/forgot_password_request_dto.dart` | Request DTO |
| `apps/mobile/lib/features/auth/presentation/providers/forgot_password_state.dart` | Sealed state classes |
| `apps/mobile/lib/features/auth/presentation/providers/forgot_password_controller.dart` | Riverpod Notifier |
| `apps/mobile/lib/features/auth/presentation/pages/forgot_password_page.dart` | UI page |
| `apps/mobile/test/features/auth/data/dtos/forgot_password_request_dto_test.dart` | DTO test |
| `apps/mobile/test/features/auth/presentation/providers/forgot_password_controller_test.dart` | Controller test |
| `apps/mobile/test/features/auth/presentation/pages/forgot_password_page_test.dart` | Page widget test |

### Flutter — modify
| Path | Change |
|---|---|
| `apps/mobile/lib/features/auth/domain/repositories/auth_repository.dart` | Add `forgotPassword()` |
| `apps/mobile/lib/features/auth/data/datasources/auth_remote_data_source.dart` | Add `forgotPassword()` + `_mapForgotPasswordError()` |
| `apps/mobile/lib/features/auth/data/repositories/auth_repository_impl.dart` | Implement `forgotPassword()` |
| `apps/mobile/lib/app/app_router.dart` | Add `/forgot-password` route |
| `apps/mobile/lib/features/auth/presentation/pages/login_page.dart` | Navigate to forgot-password instead of snackbar |
| `apps/mobile/lib/l10n/app_pt.arb` | New keys |
| `apps/mobile/lib/l10n/app_en.arb` | New keys |
| `apps/mobile/lib/l10n/app_es.arb` | New keys |

---

## Task 1: Domain Entity + Repository Interface

**Files:**
- Create: `backend/src/Awaken.Domain/Entities/Auth/PasswordResetRequest.cs`
- Create: `backend/src/Awaken.Domain/Repositories/IPasswordResetRepository.cs`

- [ ] **Step 1: Create PasswordResetRequest entity**

```csharp
// backend/src/Awaken.Domain/Entities/Auth/PasswordResetRequest.cs
using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Auth;

public class PasswordResetRequest : BaseEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }

    private PasswordResetRequest() { }

    public static PasswordResetRequest Create(
        Guid userId,
        string tokenHash,
        DateTime requestedAtUtc,
        DateTime expiresAtUtc) =>
        new()
        {
            UserId = userId,
            TokenHash = tokenHash,
            RequestedAtUtc = requestedAtUtc,
            ExpiresAtUtc = expiresAtUtc,
        };

    public bool IsExpired(DateTime utcNow) => ExpiresAtUtc < utcNow;

    public void MarkUsed(DateTime utcNow)
    {
        UsedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }
}
```

- [ ] **Step 2: Create IPasswordResetRepository**

```csharp
// backend/src/Awaken.Domain/Repositories/IPasswordResetRepository.cs
using Awaken.Domain.Common;
using Awaken.Domain.Entities.Auth;

namespace Awaken.Domain.Repositories;

public interface IPasswordResetRepository : IRepository<PasswordResetRequest>
{
    Task<PasswordResetRequest?> GetActiveByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);
}
```

- [ ] **Step 3: Build domain project to verify compilation**

```bash
dotnet build backend/src/Awaken.Domain/Awaken.Domain.csproj
```

Expected: Build succeeded, 0 error(s).

---

## Task 2: IEmailService Interface

**Files:**
- Create: `backend/src/Awaken.Application/Common/Interfaces/IEmailService.cs`

- [ ] **Step 1: Create interface**

```csharp
// backend/src/Awaken.Application/Common/Interfaces/IEmailService.cs
namespace Awaken.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetAsync(
        string toEmail,
        string rawToken,
        CancellationToken cancellationToken = default);
}
```

---

## Task 3: Application Layer — Validator (TDD)

**Files:**
- Create: `backend/src/Awaken.Application/Auth/Commands/ForgotPassword/ForgotPasswordCommand.cs`
- Create: `backend/src/Awaken.Application/Auth/Commands/ForgotPassword/ForgotPasswordValidator.cs`
- Create: `backend/tests/Awaken.UnitTests/Auth/ForgotPasswordValidatorTests.cs`

- [ ] **Step 1: Create ForgotPasswordCommand**

```csharp
// backend/src/Awaken.Application/Auth/Commands/ForgotPassword/ForgotPasswordCommand.cs
using MediatR;

namespace Awaken.Application.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest<Unit>;
```

- [ ] **Step 2: Write failing validator tests**

```csharp
// backend/tests/Awaken.UnitTests/Auth/ForgotPasswordValidatorTests.cs
using Awaken.Application.Auth.Commands.ForgotPassword;
using FluentValidation.TestHelper;

namespace Awaken.UnitTests.Auth;

public class ForgotPasswordValidatorTests
{
    private readonly ForgotPasswordValidator _sut = new();

    [Fact]
    public void ValidEmailPassesValidation()
    {
        var result = _sut.TestValidate(new ForgotPasswordCommand("hunter@awaken.app"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void EmptyEmailFailsValidation(string? email)
    {
        var result = _sut.TestValidate(new ForgotPasswordCommand(email!));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    public void InvalidEmailFormatFailsValidation(string email)
    {
        var result = _sut.TestValidate(new ForgotPasswordCommand(email));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void EmailExceeding256CharsFailsValidation()
    {
        var longEmail = new string('a', 248) + "@b.com";
        var result = _sut.TestValidate(new ForgotPasswordCommand(longEmail));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

```bash
dotnet test backend/tests/Awaken.UnitTests/Awaken.UnitTests.csproj --filter "ForgotPasswordValidatorTests" --no-build 2>&1 | tail -5
```

Expected: build error or test failure (validator not yet created).

- [ ] **Step 4: Create ForgotPasswordValidator**

```csharp
// backend/src/Awaken.Application/Auth/Commands/ForgotPassword/ForgotPasswordValidator.cs
using FluentValidation;

namespace Awaken.Application.Auth.Commands.ForgotPassword;

public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);
    }
}
```

- [ ] **Step 5: Run validator tests — expect green**

```bash
dotnet test backend/tests/Awaken.UnitTests/Awaken.UnitTests.csproj --filter "ForgotPasswordValidatorTests"
```

Expected: 6 tests passed.

---

## Task 4: Application Layer — Handler (TDD)

**Files:**
- Create: `backend/src/Awaken.Application/Auth/Commands/ForgotPassword/ForgotPasswordCommandHandler.cs`
- Create: `backend/tests/Awaken.UnitTests/Auth/ForgotPasswordCommandHandlerTests.cs`

- [ ] **Step 1: Write failing handler tests**

```csharp
// backend/tests/Awaken.UnitTests/Auth/ForgotPasswordCommandHandlerTests.cs
using Awaken.Application.Auth.Commands.ForgotPassword;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using FluentAssertions;
using MediatR;
using Moq;

namespace Awaken.UnitTests.Auth;

public class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordResetRepository> _passwordResetRepository = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static readonly DateTime UtcNow = new(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc);

    private ForgotPasswordCommandHandler CreateHandler() => new(
        _userRepository.Object,
        _passwordResetRepository.Object,
        _emailService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object);

    public ForgotPasswordCommandHandlerTests()
    {
        _dateTimeService.Setup(d => d.UtcNow).Returns(UtcNow);
    }

    [Fact]
    public async Task HandleReturnsUnitRegardlessOfWhetherEmailExists()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await CreateHandler().Handle(
            new ForgotPasswordCommand("nobody@awaken.app"),
            CancellationToken.None);

        result.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task HandleDoesNotSendEmailWhenEmailDoesNotExist()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await CreateHandler().Handle(
            new ForgotPasswordCommand("nobody@awaken.app"),
            CancellationToken.None);

        _emailService.Verify(e => e.SendPasswordResetAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleCreatesResetRequestAndSendsEmailWhenEmailExists()
    {
        var user = CreateUser("hunter@awaken.app");
        _userRepository.Setup(r => r.GetByEmailAsync("hunter@awaken.app", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        PasswordResetRequest? capturedRequest = null;
        _passwordResetRepository
            .Setup(r => r.AddAsync(It.IsAny<PasswordResetRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PasswordResetRequest, CancellationToken>((req, _) => capturedRequest = req)
            .Returns(Task.CompletedTask);

        await CreateHandler().Handle(
            new ForgotPasswordCommand("hunter@awaken.app"),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.UserId.Should().Be(user.Id);
        capturedRequest.ExpiresAtUtc.Should().Be(UtcNow.AddHours(1));
        capturedRequest.RequestedAtUtc.Should().Be(UtcNow);
        capturedRequest.UsedAtUtc.Should().BeNull();
        capturedRequest.TokenHash.Should().NotBeNullOrWhiteSpace();

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _emailService.Verify(e => e.SendPasswordResetAsync(
            "hunter@awaken.app",
            It.IsNotNull<string>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleStoresHashedTokenNotRawToken()
    {
        var user = CreateUser("hunter@awaken.app");
        _userRepository.Setup(r => r.GetByEmailAsync("hunter@awaken.app", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        string? capturedRawToken = null;
        PasswordResetRequest? capturedRequest = null;

        _passwordResetRepository
            .Setup(r => r.AddAsync(It.IsAny<PasswordResetRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PasswordResetRequest, CancellationToken>((req, _) => capturedRequest = req)
            .Returns(Task.CompletedTask);
        _emailService
            .Setup(e => e.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, raw, _) => capturedRawToken = raw)
            .Returns(Task.CompletedTask);

        await CreateHandler().Handle(
            new ForgotPasswordCommand("hunter@awaken.app"),
            CancellationToken.None);

        capturedRawToken.Should().NotBeNullOrWhiteSpace();
        capturedRequest!.TokenHash.Should().NotBe(capturedRawToken);
    }

    private static User CreateUser(string email)
    {
        var user = User.Create(email, "hashed-password", "Hunter");
        return user;
    }
}
```

- [ ] **Step 2: Run tests — expect build failure (handler not yet created)**

```bash
dotnet test backend/tests/Awaken.UnitTests/Awaken.UnitTests.csproj --filter "ForgotPasswordCommandHandlerTests" 2>&1 | tail -8
```

Expected: build error — `ForgotPasswordCommandHandler` not found.

- [ ] **Step 3: Create ForgotPasswordCommandHandler**

```csharp
// backend/src/Awaken.Application/Auth/Commands/ForgotPassword/ForgotPasswordCommandHandler.cs
using System.Security.Cryptography;
using System.Text;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordResetRepository passwordResetRepository,
    IEmailService emailService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork) : IRequestHandler<ForgotPasswordCommand, Unit>
{
    public async Task<Unit> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var utcNow = dateTimeService.UtcNow;
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is not null)
        {
            var rawToken = Guid.NewGuid().ToString("N");
            var tokenHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

            var resetRequest = PasswordResetRequest.Create(
                user.Id,
                tokenHash,
                utcNow,
                utcNow.AddHours(1));

            await passwordResetRepository.AddAsync(resetRequest, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await emailService.SendPasswordResetAsync(user.Email, rawToken, cancellationToken);
        }

        return Unit.Value;
    }
}
```

- [ ] **Step 4: Run handler tests — expect green**

```bash
dotnet test backend/tests/Awaken.UnitTests/Awaken.UnitTests.csproj --filter "ForgotPasswordCommandHandlerTests"
```

Expected: 4 tests passed.

---

## Task 5: Contracts DTO

**Files:**
- Create: `backend/src/Awaken.Contracts/Auth/ForgotPasswordRequest.cs`

- [ ] **Step 1: Create DTO**

```csharp
// backend/src/Awaken.Contracts/Auth/ForgotPasswordRequest.cs
namespace Awaken.Contracts.Auth;

public record ForgotPasswordRequest(string Email);
```

---

## Task 6: Infrastructure — EF Configuration + Repository + Email Stub

**Files:**
- Create: `backend/src/Awaken.Infrastructure/Persistence/Configurations/PasswordResetConfiguration.cs`
- Create: `backend/src/Awaken.Infrastructure/Persistence/Repositories/PasswordResetRepository.cs`
- Create: `backend/src/Awaken.Infrastructure/Services/EmailService.cs`
- Modify: `backend/src/Awaken.Infrastructure/Persistence/AwakenDbContext.cs`
- Modify: `backend/src/Awaken.Infrastructure/DependencyInjection.cs`

- [ ] **Step 1: Create EF configuration**

```csharp
// backend/src/Awaken.Infrastructure/Persistence/Configurations/PasswordResetConfiguration.cs
using Awaken.Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awaken.Infrastructure.Persistence.Configurations;

public class PasswordResetConfiguration : IEntityTypeConfiguration<PasswordResetRequest>
{
    public void Configure(EntityTypeBuilder<PasswordResetRequest> builder)
    {
        builder.ToTable("password_reset_requests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.UserId).IsRequired();
        builder.Property(r => r.TokenHash).IsRequired().HasMaxLength(128);
        builder.HasIndex(r => r.TokenHash).IsUnique();
        builder.Property(r => r.RequestedAtUtc).IsRequired();
        builder.Property(r => r.ExpiresAtUtc).IsRequired();
    }
}
```

- [ ] **Step 2: Create PasswordResetRepository**

```csharp
// backend/src/Awaken.Infrastructure/Persistence/Repositories/PasswordResetRepository.cs
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

public class PasswordResetRepository(AwakenDbContext context) : IPasswordResetRepository
{
    public async Task<PasswordResetRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.PasswordResetRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IEnumerable<PasswordResetRequest>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.PasswordResetRequests.ToListAsync(cancellationToken);

    public async Task AddAsync(PasswordResetRequest entity, CancellationToken cancellationToken = default) =>
        await context.PasswordResetRequests.AddAsync(entity, cancellationToken);

    public void Update(PasswordResetRequest entity) =>
        context.PasswordResetRequests.Update(entity);

    public void Remove(PasswordResetRequest entity) =>
        context.PasswordResetRequests.Remove(entity);

    public async Task<PasswordResetRequest?> GetActiveByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        await context.PasswordResetRequests
            .FirstOrDefaultAsync(
                r => r.TokenHash == tokenHash && r.UsedAtUtc == null,
                cancellationToken);
}
```

- [ ] **Step 3: Create no-op EmailService**

```csharp
// backend/src/Awaken.Infrastructure/Services/EmailService.cs
using Awaken.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Awaken.Infrastructure.Services;

public class EmailService(ILogger<EmailService> logger) : IEmailService
{
    public Task SendPasswordResetAsync(
        string toEmail,
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "PASSWORD_RESET_TOKEN_GENERATED for {Email} — integrate real email provider before production",
            toEmail);
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 4: Add DbSet to AwakenDbContext**

In `backend/src/Awaken.Infrastructure/Persistence/AwakenDbContext.cs`, add after `DbSet<RefreshToken>` line:

```csharp
public DbSet<PasswordResetRequest> PasswordResetRequests => Set<PasswordResetRequest>();
```

The updated file should look like:

```csharp
using System.Reflection;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Common;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Entities.Quests;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence;

public class AwakenDbContext(DbContextOptions<AwakenDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetRequest> PasswordResetRequests => Set<PasswordResetRequest>();
    public DbSet<HunterProgression> HunterProgressions => Set<HunterProgression>();
    public DbSet<Quest> Quests => Set<Quest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.GetType().GetProperty("UpdatedAtUtc")?.SetValue(entry.Entity, DateTime.UtcNow);
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var entity in ChangeTracker.Entries<BaseEntity>().Select(e => e.Entity))
            entity.ClearDomainEvents();

        return result;
    }
}
```

- [ ] **Step 5: Register services in DependencyInjection**

In `backend/src/Awaken.Infrastructure/DependencyInjection.cs`, add two lines after `IRefreshTokenRepository` registration:

```csharp
services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
services.AddScoped<IEmailService, EmailService>();
```

- [ ] **Step 6: Build infrastructure to verify compilation**

```bash
dotnet build backend/src/Awaken.Infrastructure/Awaken.Infrastructure.csproj
```

Expected: Build succeeded, 0 error(s).

- [ ] **Step 7: Generate EF migration**

```bash
dotnet ef migrations add AddPasswordResetRequests -p backend/src/Awaken.Infrastructure -s backend/src/Awaken.Api
```

Expected: Migration file created in `Awaken.Infrastructure/Persistence/Migrations/`.

---

## Task 7: API Endpoint

**Files:**
- Modify: `backend/src/Awaken.Api/Controllers/V1/AuthController.cs`

- [ ] **Step 1: Add ForgotPassword endpoint**

Add the following using at the top of `AuthController.cs`:

```csharp
using Awaken.Application.Auth.Commands.ForgotPassword;
```

Add the following method in `AuthController` after the `Login` endpoint:

```csharp
[HttpPost("forgot-password")]
public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
{
    await mediator.Send(new ForgotPasswordCommand(request.Email), ct);
    var correlationId = ControllerContext.HttpContext?.Items["CorrelationId"]?.ToString()
                        ?? Guid.NewGuid().ToString();
    return Ok(new { success = true, correlationId });
}
```

---

## Task 8: Integration Tests — Backend

**Files:**
- Create: `backend/tests/Awaken.IntegrationTests/AuthForgotPasswordEndpointTests.cs`

- [ ] **Step 1: Create integration tests**

```csharp
// backend/tests/Awaken.IntegrationTests/AuthForgotPasswordEndpointTests.cs
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Awaken.IntegrationTests;

public class AuthForgotPasswordEndpointTests : IAsyncLifetime
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

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task RegisterAsync(string email, string password = "Str0ngPass!")
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            name = "Hunter",
            language = "pt-BR"
        });
    }

    [Fact]
    public async Task ForgotPasswordReturnsOkWithGenericSuccessForExistingEmail()
    {
        await RegisterAsync("hunter@awaken.app");
        _client.DefaultRequestHeaders.Add("X-Correlation-Id", "fp-corr-1");

        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new { email = "hunter@awaken.app" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        document.RootElement.GetProperty("correlationId").GetString().Should().Be("fp-corr-1");
    }

    [Fact]
    public async Task ForgotPasswordReturnsOkWithGenericSuccessForNonExistingEmail()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new { email = "nobody@awaken.app" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task ForgotPasswordReturnsValidationErrorForInvalidEmail()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new { email = "not-an-email" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ForgotPasswordReturnsValidationErrorForMissingEmail()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new { });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ForgotPasswordDoesNotRevealWhetherEmailExistsInResponse()
    {
        await RegisterAsync("exists@awaken.app");

        var responseExists = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new { email = "exists@awaken.app" });
        var responseNotExists = await _client.PostAsJsonAsync("/api/auth/forgot-password",
            new { email = "notexists@awaken.app" });

        var bodyExists = await responseExists.Content.ReadAsStringAsync();
        var bodyNotExists = await responseNotExists.Content.ReadAsStringAsync();

        responseExists.StatusCode.Should().Be(HttpStatusCode.OK);
        responseNotExists.StatusCode.Should().Be(HttpStatusCode.OK);

        using var docExists = JsonDocument.Parse(bodyExists);
        using var docNotExists = JsonDocument.Parse(bodyNotExists);

        docExists.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        docNotExists.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run all backend tests**

```bash
dotnet test backend/
```

Expected: all existing tests still pass + new tests pass.

---

## Task 9: Flutter — l10n Keys

**Files:**
- Modify: `apps/mobile/lib/l10n/app_pt.arb`
- Modify: `apps/mobile/lib/l10n/app_en.arb`
- Modify: `apps/mobile/lib/l10n/app_es.arb`

- [ ] **Step 1: Add pt-BR keys** (add after `"loginForgotPasswordComingSoon"` line in `app_pt.arb`)

```json
"forgotPasswordTitle": "Recuperar senha",
"forgotPasswordSubtitle": "Informe seu e-mail para receber as instruções de recuperação.",
"forgotPasswordEmailLabel": "E-mail",
"forgotPasswordButton": "Enviar instruções",
"forgotPasswordSuccessTitle": "Instruções enviadas",
"forgotPasswordSuccessMessage": "Se existir uma conta com este e-mail, você receberá as instruções de recuperação em breve.",
"forgotPasswordBackToLogin": "Voltar ao login",
"forgotPasswordEmailRequiredError": "Informe seu e-mail.",
"forgotPasswordEmailInvalidError": "Informe um e-mail válido.",
"forgotPasswordConnectionError": "Verifique sua conexão e tente novamente.",
"forgotPasswordUnexpectedError": "Não foi possível processar a solicitação agora."
```

- [ ] **Step 2: Add EN keys** (add after `"loginForgotPasswordComingSoon"` line in `app_en.arb`)

```json
"forgotPasswordTitle": "Recover password",
"forgotPasswordSubtitle": "Enter your email to receive recovery instructions.",
"forgotPasswordEmailLabel": "Email",
"forgotPasswordButton": "Send instructions",
"forgotPasswordSuccessTitle": "Instructions sent",
"forgotPasswordSuccessMessage": "If an account exists for this email, you will receive recovery instructions shortly.",
"forgotPasswordBackToLogin": "Back to sign in",
"forgotPasswordEmailRequiredError": "Enter your email.",
"forgotPasswordEmailInvalidError": "Enter a valid email.",
"forgotPasswordConnectionError": "Check your connection and try again.",
"forgotPasswordUnexpectedError": "Could not process the request right now."
```

- [ ] **Step 3: Add ES keys** (add after `"loginForgotPasswordComingSoon"` line in `app_es.arb`)

```json
"forgotPasswordTitle": "Recuperar contraseña",
"forgotPasswordSubtitle": "Ingresa tu correo para recibir las instrucciones de recuperación.",
"forgotPasswordEmailLabel": "Correo electrónico",
"forgotPasswordButton": "Enviar instrucciones",
"forgotPasswordSuccessTitle": "Instrucciones enviadas",
"forgotPasswordSuccessMessage": "Si existe una cuenta con este correo, recibirás las instrucciones de recuperación en breve.",
"forgotPasswordBackToLogin": "Volver al inicio de sesión",
"forgotPasswordEmailRequiredError": "Ingresa tu correo electrónico.",
"forgotPasswordEmailInvalidError": "Ingresa un correo electrónico válido.",
"forgotPasswordConnectionError": "Verifica tu conexión e intenta de nuevo.",
"forgotPasswordUnexpectedError": "No fue posible procesar la solicitud ahora."
```

- [ ] **Step 4: Generate l10n**

```bash
cd apps/mobile && flutter gen-l10n
```

Expected: no errors; generated files updated in `.dart_tool/flutter_gen/`.

---

## Task 10: Flutter — DTO

**Files:**
- Create: `apps/mobile/lib/features/auth/data/dtos/forgot_password_request_dto.dart`
- Create: `apps/mobile/test/features/auth/data/dtos/forgot_password_request_dto_test.dart`

- [ ] **Step 1: Write failing DTO test**

```dart
// apps/mobile/test/features/auth/data/dtos/forgot_password_request_dto_test.dart
import 'package:flutter_test/flutter_test.dart';
import 'package:awaken/features/auth/data/dtos/forgot_password_request_dto.dart';

void main() {
  group('ForgotPasswordRequestDto', () {
    test('toJson returns correct map', () {
      const dto = ForgotPasswordRequestDto(email: 'hunter@awaken.app');
      expect(dto.toJson(), {'email': 'hunter@awaken.app'});
    });
  });
}
```

- [ ] **Step 2: Run test — expect failure**

```bash
cd apps/mobile && flutter test test/features/auth/data/dtos/forgot_password_request_dto_test.dart
```

Expected: compilation error — class not found.

- [ ] **Step 3: Create DTO**

```dart
// apps/mobile/lib/features/auth/data/dtos/forgot_password_request_dto.dart
class ForgotPasswordRequestDto {
  const ForgotPasswordRequestDto({required this.email});

  final String email;

  Map<String, dynamic> toJson() => {'email': email};
}
```

- [ ] **Step 4: Run test — expect green**

```bash
cd apps/mobile && flutter test test/features/auth/data/dtos/forgot_password_request_dto_test.dart
```

Expected: 1 passed.

---

## Task 11: Flutter — Domain + Data Layer

**Files:**
- Modify: `apps/mobile/lib/features/auth/domain/repositories/auth_repository.dart`
- Modify: `apps/mobile/lib/features/auth/data/datasources/auth_remote_data_source.dart`
- Modify: `apps/mobile/lib/features/auth/data/repositories/auth_repository_impl.dart`

- [ ] **Step 1: Add forgotPassword to AuthRepository**

```dart
// apps/mobile/lib/features/auth/domain/repositories/auth_repository.dart
import '../entities/auth_session.dart';

abstract class AuthRepository {
  Future<AuthSession> register({
    required String name,
    required String email,
    required String password,
    required String language,
  });

  Future<AuthSession> login({
    required String email,
    required String password,
  });

  Future<AuthSession> loginWithGoogle({
    required String idToken,
  });

  Future<void> logout();

  Future<void> forgotPassword({required String email});
}
```

- [ ] **Step 2: Add forgotPassword to AuthRemoteDataSource**

Add to `AuthRemoteDataSource` class (after `logout()` method):

```dart
Future<void> forgotPassword(ForgotPasswordRequestDto request) async {
  try {
    await _dio.post('/api/auth/forgot-password', data: request.toJson());
  } on DioException catch (e) {
    throw _mapForgotPasswordError(e);
  }
}

AppError _mapForgotPasswordError(DioException e) {
  switch (e.type) {
    case DioExceptionType.connectionTimeout:
    case DioExceptionType.sendTimeout:
    case DioExceptionType.receiveTimeout:
    case DioExceptionType.connectionError:
      return const NetworkError();
    default:
      break;
  }
  return const UnexpectedError();
}
```

Also add the import at the top of the file:
```dart
import '../dtos/forgot_password_request_dto.dart';
```

- [ ] **Step 3: Implement forgotPassword in AuthRepositoryImpl**

Add to `AuthRepositoryImpl` class (after `logout()` method):

```dart
@override
Future<void> forgotPassword({required String email}) async {
  await _remoteDataSource.forgotPassword(ForgotPasswordRequestDto(email: email));
}
```

Also add the import at the top of `auth_repository_impl.dart`:
```dart
import '../dtos/forgot_password_request_dto.dart';
```

- [ ] **Step 4: Analyze to verify no errors**

```bash
cd apps/mobile && flutter analyze lib/features/auth/
```

Expected: No issues found.

---

## Task 12: Flutter — State + Controller

**Files:**
- Create: `apps/mobile/lib/features/auth/presentation/providers/forgot_password_state.dart`
- Create: `apps/mobile/lib/features/auth/presentation/providers/forgot_password_controller.dart`
- Create: `apps/mobile/test/features/auth/presentation/providers/forgot_password_controller_test.dart`

- [ ] **Step 1: Write failing controller tests**

```dart
// apps/mobile/test/features/auth/presentation/providers/forgot_password_controller_test.dart
import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:awaken/core/analytics/analytics_provider.dart';
import 'package:awaken/core/analytics/analytics_service.dart';
import 'package:awaken/core/errors/app_error.dart';
import 'package:awaken/features/auth/domain/repositories/auth_repository.dart';
import 'package:awaken/features/auth/presentation/providers/auth_providers.dart';
import 'package:awaken/features/auth/presentation/providers/forgot_password_controller.dart';
import 'package:awaken/features/auth/presentation/providers/forgot_password_state.dart';

class MockAnalyticsService extends Mock implements AnalyticsService {}

class MockAuthRepository extends Mock implements AuthRepository {}

void main() {
  late MockAnalyticsService mockAnalytics;
  late MockAuthRepository mockAuthRepository;

  setUp(() {
    mockAnalytics = MockAnalyticsService();
    mockAuthRepository = MockAuthRepository();
    when(() => mockAnalytics.logEvent(any(), params: any(named: 'params')))
        .thenAnswer((_) async {});
  });

  ProviderContainer buildContainer() => ProviderContainer(overrides: [
        analyticsServiceProvider.overrideWithValue(mockAnalytics),
        authRepositoryProvider.overrideWithValue(mockAuthRepository),
      ]);

  group('ForgotPasswordController', () {
    test('estado inicial é ForgotPasswordInitial', () {
      final container = buildContainer();
      addTearDown(container.dispose);

      expect(container.read(forgotPasswordControllerProvider),
          isA<ForgotPasswordInitial>());
    });

    test('CA-002: e-mail inválido resulta em ForgotPasswordValidationError sem chamar backend',
        () async {
      final container = buildContainer();
      addTearDown(container.dispose);

      await container
          .read(forgotPasswordControllerProvider.notifier)
          .submit(email: 'not-an-email');

      expect(container.read(forgotPasswordControllerProvider),
          isA<ForgotPasswordValidationError>());
      verifyNever(() => mockAuthRepository.forgotPassword(
            email: any(named: 'email'),
          ));
    });

    test('CA-002: e-mail vazio resulta em ForgotPasswordValidationError', () async {
      final container = buildContainer();
      addTearDown(container.dispose);

      await container
          .read(forgotPasswordControllerProvider.notifier)
          .submit(email: '');

      expect(container.read(forgotPasswordControllerProvider),
          isA<ForgotPasswordValidationError>());
    });

    test('CA-001 + CA-003: e-mail válido retorna ForgotPasswordSuccess com mensagem genérica',
        () async {
      when(() => mockAuthRepository.forgotPassword(
            email: any(named: 'email'),
          )).thenAnswer((_) async {});

      final container = buildContainer();
      addTearDown(container.dispose);

      await container
          .read(forgotPasswordControllerProvider.notifier)
          .submit(email: 'hunter@awaken.app');

      expect(container.read(forgotPasswordControllerProvider),
          isA<ForgotPasswordSuccess>());
      verify(() => mockAnalytics.logEvent('password_reset_requested')).called(1);
    });

    test('falha de rede resulta em ForgotPasswordConnectionError', () async {
      when(() => mockAuthRepository.forgotPassword(
            email: any(named: 'email'),
          )).thenThrow(const NetworkError());

      final container = buildContainer();
      addTearDown(container.dispose);

      await container
          .read(forgotPasswordControllerProvider.notifier)
          .submit(email: 'hunter@awaken.app');

      expect(container.read(forgotPasswordControllerProvider),
          isA<ForgotPasswordConnectionError>());
      verify(() => mockAnalytics.logEvent('password_reset_failed')).called(1);
    });

    test('erro inesperado resulta em ForgotPasswordUnexpectedError', () async {
      when(() => mockAuthRepository.forgotPassword(
            email: any(named: 'email'),
          )).thenThrow(const UnexpectedError());

      final container = buildContainer();
      addTearDown(container.dispose);

      await container
          .read(forgotPasswordControllerProvider.notifier)
          .submit(email: 'hunter@awaken.app');

      expect(container.read(forgotPasswordControllerProvider),
          isA<ForgotPasswordUnexpectedError>());
    });

    test('estado é ForgotPasswordSubmitting durante o processo', () async {
      final completer = Completer<void>();
      when(() => mockAuthRepository.forgotPassword(
            email: any(named: 'email'),
          )).thenAnswer((_) => completer.future);

      final container = buildContainer();
      addTearDown(container.dispose);

      final submitFuture = container
          .read(forgotPasswordControllerProvider.notifier)
          .submit(email: 'hunter@awaken.app');

      expect(container.read(forgotPasswordControllerProvider),
          isA<ForgotPasswordSubmitting>());

      completer.complete();
      await submitFuture;

      expect(container.read(forgotPasswordControllerProvider),
          isA<ForgotPasswordSuccess>());
    });
  });
}
```

- [ ] **Step 2: Run tests — expect compilation failure**

```bash
cd apps/mobile && flutter test test/features/auth/presentation/providers/forgot_password_controller_test.dart 2>&1 | tail -10
```

Expected: error — classes not found.

- [ ] **Step 3: Create ForgotPasswordState**

```dart
// apps/mobile/lib/features/auth/presentation/providers/forgot_password_state.dart
sealed class ForgotPasswordState {
  const ForgotPasswordState();
}

final class ForgotPasswordInitial extends ForgotPasswordState {
  const ForgotPasswordInitial();
}

final class ForgotPasswordSubmitting extends ForgotPasswordState {
  const ForgotPasswordSubmitting();
}

final class ForgotPasswordSuccess extends ForgotPasswordState {
  const ForgotPasswordSuccess();
}

final class ForgotPasswordValidationError extends ForgotPasswordState {
  const ForgotPasswordValidationError({required this.emailError});
  final String? emailError;
}

final class ForgotPasswordConnectionError extends ForgotPasswordState {
  const ForgotPasswordConnectionError();
}

final class ForgotPasswordUnexpectedError extends ForgotPasswordState {
  const ForgotPasswordUnexpectedError();
}
```

- [ ] **Step 4: Create ForgotPasswordController**

```dart
// apps/mobile/lib/features/auth/presentation/providers/forgot_password_controller.dart
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/analytics/analytics_provider.dart';
import '../../../../core/errors/app_error.dart';
import 'auth_providers.dart';
import 'forgot_password_state.dart';

class ForgotPasswordController extends Notifier<ForgotPasswordState> {
  @override
  ForgotPasswordState build() => const ForgotPasswordInitial();

  Future<void> submit({required String email}) async {
    if (!_isValidEmail(email)) {
      state = ForgotPasswordValidationError(
        emailError: email.isEmpty ? 'required' : 'invalid',
      );
      return;
    }

    state = const ForgotPasswordSubmitting();

    try {
      await ref.read(authRepositoryProvider).forgotPassword(email: email);
      state = const ForgotPasswordSuccess();
      await ref.read(analyticsServiceProvider).logEvent('password_reset_requested');
    } on NetworkError {
      state = const ForgotPasswordConnectionError();
      await ref.read(analyticsServiceProvider).logEvent('password_reset_failed');
    } catch (_) {
      state = const ForgotPasswordUnexpectedError();
      await ref.read(analyticsServiceProvider).logEvent('password_reset_failed');
    }
  }

  bool _isValidEmail(String email) {
    if (email.isEmpty) return false;
    final emailRegex = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');
    return emailRegex.hasMatch(email);
  }
}

final forgotPasswordControllerProvider =
    NotifierProvider<ForgotPasswordController, ForgotPasswordState>(
        ForgotPasswordController.new);
```

- [ ] **Step 5: Run controller tests — expect green**

```bash
cd apps/mobile && flutter test test/features/auth/presentation/providers/forgot_password_controller_test.dart
```

Expected: 6 tests passed.

---

## Task 13: Flutter — Page + Router

**Files:**
- Create: `apps/mobile/lib/features/auth/presentation/pages/forgot_password_page.dart`
- Modify: `apps/mobile/lib/app/app_router.dart`

- [ ] **Step 1: Create ForgotPasswordPage**

```dart
// apps/mobile/lib/features/auth/presentation/pages/forgot_password_page.dart
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:awaken/l10n/app_localizations.dart';

import '../../../../design_system/components/awaken_button.dart';
import '../../../../design_system/components/awaken_text_field.dart';
import '../../../../design_system/tokens/colors.dart';
import '../../../../design_system/tokens/spacing.dart';
import '../../../../design_system/tokens/typography.dart';
import '../providers/forgot_password_controller.dart';
import '../providers/forgot_password_state.dart';

class ForgotPasswordPage extends ConsumerStatefulWidget {
  const ForgotPasswordPage({super.key});

  @override
  ConsumerState<ForgotPasswordPage> createState() => _ForgotPasswordPageState();
}

class _ForgotPasswordPageState extends ConsumerState<ForgotPasswordPage> {
  final _emailController = TextEditingController();

  @override
  void dispose() {
    _emailController.dispose();
    super.dispose();
  }

  String? _emailError(AppLocalizations l10n, ForgotPasswordState state) {
    if (state is! ForgotPasswordValidationError) return null;
    return switch (state.emailError) {
      'required' => l10n.forgotPasswordEmailRequiredError,
      'invalid' => l10n.forgotPasswordEmailInvalidError,
      _ => null,
    };
  }

  Future<void> _submit() async {
    await ref
        .read(forgotPasswordControllerProvider.notifier)
        .submit(email: _emailController.text);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final state = ref.watch(forgotPasswordControllerProvider);
    final isSubmitting = state is ForgotPasswordSubmitting;
    final isSuccess = state is ForgotPasswordSuccess;

    ref.listen<ForgotPasswordState>(forgotPasswordControllerProvider, (_, next) {
      if (next is ForgotPasswordConnectionError) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(l10n.forgotPasswordConnectionError)),
        );
      } else if (next is ForgotPasswordUnexpectedError) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(l10n.forgotPasswordUnexpectedError)),
        );
      }
    });

    return Scaffold(
      backgroundColor: AwakenColors.backgroundPrimary,
      appBar: AppBar(
        backgroundColor: Colors.transparent,
        elevation: 0,
        leading: IconButton(
          key: const Key('forgot-password-back'),
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.pop(),
        ),
      ),
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(AwakenSpacing.lg),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: isSuccess
                  ? _SuccessContent(l10n: l10n, onBack: () => context.pop())
                  : _FormContent(
                      l10n: l10n,
                      emailController: _emailController,
                      emailError: _emailError(l10n, state),
                      isSubmitting: isSubmitting,
                      onSubmit: isSubmitting ? null : _submit,
                    ),
            ),
          ),
        ),
      ),
    );
  }
}

class _FormContent extends StatelessWidget {
  const _FormContent({
    required this.l10n,
    required this.emailController,
    required this.emailError,
    required this.isSubmitting,
    required this.onSubmit,
  });

  final AppLocalizations l10n;
  final TextEditingController emailController;
  final String? emailError;
  final bool isSubmitting;
  final VoidCallback? onSubmit;

  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          l10n.forgotPasswordTitle,
          textAlign: TextAlign.center,
          style: AwakenTypography.displayMedium,
        ),
        const SizedBox(height: AwakenSpacing.sm),
        Text(
          l10n.forgotPasswordSubtitle,
          textAlign: TextAlign.center,
          style: AwakenTypography.bodyMedium,
        ),
        const SizedBox(height: AwakenSpacing.xl),
        AwakenTextField(
          key: const Key('forgot-password-email'),
          label: l10n.forgotPasswordEmailLabel,
          controller: emailController,
          keyboardType: TextInputType.emailAddress,
          textInputAction: TextInputAction.done,
          errorText: emailError,
        ),
        const SizedBox(height: AwakenSpacing.lg),
        AwakenButton(
          key: const Key('forgot-password-submit'),
          label: l10n.forgotPasswordButton,
          isLoading: isSubmitting,
          onPressed: onSubmit,
        ),
      ],
    );
  }
}

class _SuccessContent extends StatelessWidget {
  const _SuccessContent({required this.l10n, required this.onBack});

  final AppLocalizations l10n;
  final VoidCallback onBack;

  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const Icon(
          Icons.mark_email_read_outlined,
          key: Key('forgot-password-success-icon'),
          size: 64,
          color: AwakenColors.primary,
        ),
        const SizedBox(height: AwakenSpacing.lg),
        Text(
          l10n.forgotPasswordSuccessTitle,
          textAlign: TextAlign.center,
          style: AwakenTypography.displayMedium,
        ),
        const SizedBox(height: AwakenSpacing.sm),
        Text(
          l10n.forgotPasswordSuccessMessage,
          textAlign: TextAlign.center,
          style: AwakenTypography.bodyMedium,
        ),
        const SizedBox(height: AwakenSpacing.xl),
        AwakenButton(
          key: const Key('forgot-password-back-button'),
          label: l10n.forgotPasswordBackToLogin,
          onPressed: onBack,
        ),
      ],
    );
  }
}
```

- [ ] **Step 2: Add route to app_router.dart**

Add import at top of `app_router.dart`:
```dart
import '../features/auth/presentation/pages/forgot_password_page.dart';
```

Add route constant in `AppRoutes`:
```dart
static const forgotPassword = '/forgot-password';
```

Add route in `GoRouter` routes list (after `register` route):
```dart
GoRoute(
  path: AppRoutes.forgotPassword,
  pageBuilder: (ctx, state) => _buildPage(
    state: state,
    child: const ForgotPasswordPage(),
  ),
),
```

---

## Task 14: Flutter — Update LoginPage

**Files:**
- Modify: `apps/mobile/lib/features/auth/presentation/pages/login_page.dart`

- [ ] **Step 1: Update "Esqueci minha senha" button to navigate to forgot-password**

In `login_page.dart`, replace the `onPressed` for the forgot-password `TextButton` (currently shows a snackbar):

```dart
// BEFORE
TextButton(
  onPressed: isSubmitting
      ? null
      : () => _showSnackBar(
            context,
            l10n.loginForgotPasswordComingSoon,
          ),
  child: Text(l10n.loginForgotPassword),
),

// AFTER
TextButton(
  onPressed: isSubmitting
      ? null
      : () => context.push(AppRoutes.forgotPassword),
  child: Text(l10n.loginForgotPassword),
),
```

---

## Task 15: Flutter — Page Widget Tests

**Files:**
- Create: `apps/mobile/test/features/auth/presentation/pages/forgot_password_page_test.dart`

- [ ] **Step 1: Write failing page tests**

```dart
// apps/mobile/test/features/auth/presentation/pages/forgot_password_page_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mocktail/mocktail.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:awaken/l10n/app_localizations.dart';
import 'package:awaken/app/app_router.dart';
import 'package:awaken/core/analytics/analytics_provider.dart';
import 'package:awaken/core/analytics/analytics_service.dart';
import 'package:awaken/core/errors/app_error.dart';
import 'package:awaken/features/auth/domain/repositories/auth_repository.dart';
import 'package:awaken/features/auth/presentation/pages/forgot_password_page.dart';
import 'package:awaken/features/auth/presentation/providers/auth_providers.dart';

class MockAnalyticsService extends Mock implements AnalyticsService {}
class MockAuthRepository extends Mock implements AuthRepository {}

GoRouter _buildRouter() => GoRouter(
      initialLocation: AppRoutes.forgotPassword,
      routes: [
        GoRoute(
          path: AppRoutes.forgotPassword,
          builder: (_, __) => const ForgotPasswordPage(),
        ),
        GoRoute(
          path: AppRoutes.login,
          builder: (_, __) => const Scaffold(body: Text('Login')),
        ),
      ],
    );

Widget _buildTestApp(List<Override> overrides) => ProviderScope(
      overrides: overrides,
      child: MaterialApp.router(
        routerConfig: _buildRouter(),
        localizationsDelegates: const [
          AppLocalizations.delegate,
          GlobalMaterialLocalizations.delegate,
          GlobalWidgetsLocalizations.delegate,
          GlobalCupertinoLocalizations.delegate,
        ],
        supportedLocales: const [Locale('pt', 'BR'), Locale('en'), Locale('es')],
        locale: const Locale('pt', 'BR'),
      ),
    );

void main() {
  late MockAnalyticsService mockAnalytics;
  late MockAuthRepository mockAuthRepository;

  setUp(() {
    SharedPreferences.setMockInitialValues({});
    mockAnalytics = MockAnalyticsService();
    mockAuthRepository = MockAuthRepository();
    when(() => mockAnalytics.logEvent(any(), params: any(named: 'params')))
        .thenAnswer((_) async {});
  });

  List<Override> defaultOverrides() => [
        analyticsServiceProvider.overrideWithValue(mockAnalytics),
        authRepositoryProvider.overrideWithValue(mockAuthRepository),
      ];

  group('ForgotPasswordPage', () {
    testWidgets('exibe título, subtítulo, campo de e-mail e botão de envio',
        (tester) async {
      await tester.pumpWidget(_buildTestApp(defaultOverrides()));
      await tester.pump();

      expect(find.text('Recuperar senha'), findsOneWidget);
      expect(find.byKey(const Key('forgot-password-email')), findsOneWidget);
      expect(find.byKey(const Key('forgot-password-submit')), findsOneWidget);
    });

    testWidgets('CA-002: envio com e-mail vazio exibe erro de validação',
        (tester) async {
      await tester.pumpWidget(_buildTestApp(defaultOverrides()));
      await tester.pump();

      await tester.tap(find.byKey(const Key('forgot-password-submit')));
      await tester.pumpAndSettle();

      expect(find.text('Informe seu e-mail.'), findsOneWidget);
      verifyNever(() =>
          mockAuthRepository.forgotPassword(email: any(named: 'email')));
    });

    testWidgets('CA-002: e-mail com formato inválido exibe erro de validação',
        (tester) async {
      await tester.pumpWidget(_buildTestApp(defaultOverrides()));
      await tester.pump();

      await tester.enterText(
          find.byKey(const Key('forgot-password-email')), 'not-an-email');
      await tester.tap(find.byKey(const Key('forgot-password-submit')));
      await tester.pumpAndSettle();

      expect(find.text('Informe um e-mail válido.'), findsOneWidget);
    });

    testWidgets('CA-001 + CA-003: e-mail válido exibe tela de confirmação genérica',
        (tester) async {
      when(() => mockAuthRepository.forgotPassword(
            email: any(named: 'email'),
          )).thenAnswer((_) async {});

      await tester.pumpWidget(_buildTestApp(defaultOverrides()));
      await tester.pump();

      await tester.enterText(
          find.byKey(const Key('forgot-password-email')), 'hunter@awaken.app');
      await tester.tap(find.byKey(const Key('forgot-password-submit')));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('forgot-password-success-icon')), findsOneWidget);
      expect(find.text('Instruções enviadas'), findsOneWidget);
      expect(find.byKey(const Key('forgot-password-back-button')), findsOneWidget);
    });

    testWidgets('CA-003: tela de sucesso mostra mensagem genérica',
        (tester) async {
      when(() => mockAuthRepository.forgotPassword(
            email: any(named: 'email'),
          )).thenAnswer((_) async {});

      await tester.pumpWidget(_buildTestApp(defaultOverrides()));
      await tester.pump();

      await tester.enterText(
          find.byKey(const Key('forgot-password-email')), 'unknown@awaken.app');
      await tester.tap(find.byKey(const Key('forgot-password-submit')));
      await tester.pumpAndSettle();

      expect(
        find.text(
          'Se existir uma conta com este e-mail, você receberá as instruções de recuperação em breve.',
        ),
        findsOneWidget,
      );
    });

    testWidgets('falha de conexão exibe snackbar de erro de rede', (tester) async {
      when(() => mockAuthRepository.forgotPassword(
            email: any(named: 'email'),
          )).thenThrow(const NetworkError());

      await tester.pumpWidget(_buildTestApp(defaultOverrides()));
      await tester.pump();

      await tester.enterText(
          find.byKey(const Key('forgot-password-email')), 'hunter@awaken.app');
      await tester.tap(find.byKey(const Key('forgot-password-submit')));
      await tester.pump();

      expect(
        find.text('Verifique sua conexão e tente novamente.'),
        findsOneWidget,
      );
    });
  });
}
```

- [ ] **Step 2: Run page tests — check for failures caused by missing implementations**

```bash
cd apps/mobile && flutter test test/features/auth/presentation/pages/forgot_password_page_test.dart
```

Expected: tests run (some may fail if page is incomplete; fix any issues).

---

## Task 16: Flutter — Update LoginPage Test

**Files:**
- Modify: `apps/mobile/test/features/auth/presentation/pages/login_page_test.dart`

- [ ] **Step 1: Add route stub + update test**

In `_buildRouter()` in `login_page_test.dart`, add the forgot-password route:

```dart
GoRoute(
  path: AppRoutes.forgotPassword,
  builder: (_, __) => const Scaffold(body: Text('ForgotPassword')),
),
```

Then add a new test in the `LoginPage` group:

```dart
testWidgets('toque em "Esqueci minha senha" navega para forgot-password',
    (tester) async {
  await tester.pumpWidget(_buildTestApp(defaultOverrides()));
  await tester.pump();

  await tester.ensureVisible(find.text('Esqueci minha senha'));
  await tester.tap(find.text('Esqueci minha senha'));
  await tester.pumpAndSettle();

  expect(find.text('ForgotPassword'), findsOneWidget);
});
```

- [ ] **Step 2: Run updated login page tests**

```bash
cd apps/mobile && flutter test test/features/auth/presentation/pages/login_page_test.dart
```

Expected: all tests pass including the new one.

---

## Task 17: Final Verification

- [ ] **Step 1: Run all backend tests**

```bash
dotnet test backend/
```

Expected: all tests pass (0 failures).

- [ ] **Step 2: Run flutter analyze**

```bash
cd apps/mobile && flutter analyze
```

Expected: No issues found.

- [ ] **Step 3: Run all Flutter tests**

```bash
cd apps/mobile && flutter test
```

Expected: all tests pass (0 failures).

- [ ] **Step 4: Check ARB parity (all 3 languages have the same keys)**

```bash
cd apps/mobile && flutter test test/l10n/arb_parity_test.dart
```

Expected: test passes confirming pt/en/es have identical key sets.

---

## Spec Coverage Checklist

| Requirement | Covered by |
|---|---|
| Link "Esqueci minha senha" na tela de login | Task 14 |
| Tela de solicitação com e-mail | Task 13 |
| Validação de e-mail (RN-001) | Task 12 (controller), Task 15 (page) |
| Solicitação de recuperação ao backend (POST /api/auth/forgot-password) | Task 7 |
| Mensagem genérica de confirmação (RN-002, CA-003) | Task 13 (success screen), Task 15 |
| Não revelar existência do e-mail (RN-002) | Task 4 (handler), Task 8 (integration test) |
| Validade limitada (RN-005) | Task 1 (entity expiresAtUtc, 1h) |
| Não altera progresso nem assinatura (RN-004) | Handler only touches PasswordResetRequest |
| Estados de tela: inicial, enviando, confirmação, erro validação, erro conexão, erro inesperado | Task 12 (state sealed classes) |
| Analytics: password_reset_requested + password_reset_failed | Task 12 (controller) |
| Internacionalização PT-BR, EN, ES | Task 9 |
| Testes backend: unit + integration | Tasks 3–4, 8 |
| Testes Flutter: controller + page + DTO | Tasks 10, 12, 15 |
