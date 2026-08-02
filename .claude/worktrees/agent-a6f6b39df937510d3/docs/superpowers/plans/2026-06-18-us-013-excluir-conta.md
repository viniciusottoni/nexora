# US-013 — Excluir Conta — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the full delete account flow (US-013): soft-delete user on backend + local session cleanup + dedicated confirmation page in Flutter.

**Architecture:** Backend soft-deletes the `User` entity (using existing `BaseEntity.SoftDelete`) and revokes all refresh tokens via `DeleteAccountCommandHandler`. Flutter shows a dedicated page with a confirmation dialog; the controller clears the local session on success or failure (RN-005). Subscription warning shown if `AccessStatus.subscriptionActive`.

**Tech Stack:** ASP.NET Core 10 / MediatR / FluentValidation / xUnit / FluentAssertions / Moq / Testcontainers; Flutter / Riverpod / Mocktail / go_router.

---

## File Map

**New backend files:**
- `backend/src/Awaken.Contracts/Auth/DeleteAccountRequest.cs`
- `backend/src/Awaken.Application/Auth/Commands/DeleteAccount/DeleteAccountCommand.cs`
- `backend/src/Awaken.Application/Auth/Commands/DeleteAccount/DeleteAccountCommandHandler.cs`
- `backend/src/Awaken.Application/Auth/Commands/DeleteAccount/DeleteAccountValidator.cs`
- `backend/tests/Awaken.UnitTests/Auth/DeleteAccountCommandHandlerTests.cs`
- `backend/tests/Awaken.UnitTests/Auth/DeleteAccountValidatorTests.cs`
- `backend/tests/Awaken.IntegrationTests/AuthDeleteAccountEndpointTests.cs`

**Modified backend files:**
- `backend/src/Awaken.Api/Controllers/V1/AuthController.cs` — add `[HttpDelete("delete-account")]`
- `backend/tests/Awaken.UnitTests/Api/AuthControllerTests.cs` — add controller unit test

**New Flutter files:**
- `apps/mobile/lib/features/auth/data/dtos/delete_account_request_dto.dart`
- `apps/mobile/lib/features/auth/presentation/providers/delete_account_state.dart`
- `apps/mobile/lib/features/auth/presentation/providers/delete_account_controller.dart`
- `apps/mobile/lib/features/auth/presentation/pages/delete_account_page.dart`

**Modified Flutter files:**
- `apps/mobile/lib/features/auth/domain/repositories/auth_repository.dart` — add `deleteAccount()`
- `apps/mobile/lib/features/auth/data/repositories/auth_repository_impl.dart` — implement `deleteAccount()`
- `apps/mobile/lib/features/auth/data/datasources/auth_remote_data_source.dart` — add `deleteAccount()`
- `apps/mobile/lib/features/settings/presentation/pages/settings_page.dart` — add delete account tile
- `apps/mobile/lib/app/app_router.dart` — add route + import
- `apps/mobile/lib/l10n/app_pt.arb` — add 10 keys
- `apps/mobile/lib/l10n/app_en.arb` — add 10 keys
- `apps/mobile/lib/l10n/app_es.arb` — add 10 keys

**New Flutter test files:**
- `apps/mobile/test/features/auth/presentation/providers/delete_account_controller_test.dart`
- `apps/mobile/test/features/auth/presentation/pages/delete_account_page_test.dart`

**Modified Flutter test files:**
- `apps/mobile/test/features/auth/data/repositories/auth_repository_impl_test.dart` — add deleteAccount tests
- `apps/mobile/test/features/settings/presentation/pages/settings_page_test.dart` — add delete account tile tests

---

## Task 1 — ARB localization keys

**Files:**
- Modify: `apps/mobile/lib/l10n/app_pt.arb`
- Modify: `apps/mobile/lib/l10n/app_en.arb`
- Modify: `apps/mobile/lib/l10n/app_es.arb`

- [ ] **Step 1: Add keys to app_pt.arb**

Add after the `"logoutFailedError"` entry (before the `"languageSelectorTitle"` line):

```json
  "deleteAccountTitle": "Excluir conta",
  "deleteAccountWarning": "Seu progresso e dados serão removidos ou anonimizados. Esta ação não pode ser desfeita.",
  "deleteAccountSubscriptionWarning": "Você possui uma assinatura ativa. O cancelamento financeiro deve ser feito pela loja ou plataforma.",
  "deleteAccountDialogTitle": "Excluir sua conta?",
  "deleteAccountDialogMessage": "Todos os seus dados serão removidos ou anonimizados. Esta ação não pode ser desfeita.",
  "deleteAccountDialogConfirm": "Excluir conta",
  "deleteAccountDialogCancel": "Cancelar",
  "deleteAccountButton": "Excluir minha conta",
  "deleteAccountConnectionError": "Verifique sua conexão e tente novamente.",
  "deleteAccountUnexpectedError": "Não foi possível excluir a conta agora. Tente novamente.",
```

- [ ] **Step 2: Add keys to app_en.arb**

Add after the `"logoutFailedError"` entry (before the `"languageSelectorTitle"` line):

```json
  "deleteAccountTitle": "Delete account",
  "deleteAccountWarning": "Your progress and data will be removed or anonymized. This action cannot be undone.",
  "deleteAccountSubscriptionWarning": "You have an active subscription. Financial cancellation must be done through the store or platform.",
  "deleteAccountDialogTitle": "Delete your account?",
  "deleteAccountDialogMessage": "All your data will be removed or anonymized. This action cannot be undone.",
  "deleteAccountDialogConfirm": "Delete account",
  "deleteAccountDialogCancel": "Cancel",
  "deleteAccountButton": "Delete my account",
  "deleteAccountConnectionError": "Check your connection and try again.",
  "deleteAccountUnexpectedError": "Could not delete the account right now. Please try again.",
```

- [ ] **Step 3: Add keys to app_es.arb**

Add after the `"logoutFailedError"` entry (before the `"languageSelectorTitle"` line):

```json
  "deleteAccountTitle": "Eliminar cuenta",
  "deleteAccountWarning": "Tu progreso y datos serán eliminados o anonimizados. Esta acción no se puede deshacer.",
  "deleteAccountSubscriptionWarning": "Tienes una suscripción activa. La cancelación financiera debe realizarse a través de la tienda o plataforma.",
  "deleteAccountDialogTitle": "¿Eliminar tu cuenta?",
  "deleteAccountDialogMessage": "Todos tus datos serán eliminados o anonimizados. Esta acción no se puede deshacer.",
  "deleteAccountDialogConfirm": "Eliminar cuenta",
  "deleteAccountDialogCancel": "Cancelar",
  "deleteAccountButton": "Eliminar mi cuenta",
  "deleteAccountConnectionError": "Verifica tu conexión e intenta de nuevo.",
  "deleteAccountUnexpectedError": "No fue posible eliminar la cuenta ahora. Inténtalo de nuevo.",
```

- [ ] **Step 4: Gerar localizations**

```bash
cd apps/mobile && flutter gen-l10n
```

Expected: no errors, `app_localizations.dart` updated with 10 new getter methods.

- [ ] **Step 5: Run analyze**

```bash
cd apps/mobile && flutter analyze
```

Expected: no issues.

- [ ] **Step 6: Commit**

```bash
git add apps/mobile/lib/l10n/app_pt.arb apps/mobile/lib/l10n/app_en.arb apps/mobile/lib/l10n/app_es.arb
git commit -m "feat(i18n): add delete account localization keys (US-013)"
```

---

## Task 2 — Backend: Contract + Command + Validator + Handler (TDD)

**Files:**
- Create: `backend/src/Awaken.Contracts/Auth/DeleteAccountRequest.cs`
- Create: `backend/src/Awaken.Application/Auth/Commands/DeleteAccount/DeleteAccountCommand.cs`
- Create: `backend/src/Awaken.Application/Auth/Commands/DeleteAccount/DeleteAccountCommandHandler.cs`
- Create: `backend/src/Awaken.Application/Auth/Commands/DeleteAccount/DeleteAccountValidator.cs`
- Create: `backend/tests/Awaken.UnitTests/Auth/DeleteAccountCommandHandlerTests.cs`
- Create: `backend/tests/Awaken.UnitTests/Auth/DeleteAccountValidatorTests.cs`

- [ ] **Step 1: Write failing handler tests**

Create `backend/tests/Awaken.UnitTests/Auth/DeleteAccountCommandHandlerTests.cs`:

```csharp
using Awaken.Application.Auth.Commands.DeleteAccount;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using FluentAssertions;
using MediatR;
using Moq;

namespace Awaken.UnitTests.Auth;

public class DeleteAccountCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private DeleteAccountCommandHandler CreateHandler() => new(
        _currentUserService.Object,
        _userRepository.Object,
        _refreshTokenRepository.Object,
        _unitOfWork.Object);

    [Fact]
    public async Task HandleSoftDeletesUserRevokesTokensAndSaves()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hashed-pw", "Hunter");
        _currentUserService.Setup(s => s.UserId).Returns(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await CreateHandler().Handle(new DeleteAccountCommand(), CancellationToken.None);

        result.Should().Be(Unit.Value);
        user.IsDeleted.Should().BeTrue();
        user.DeletedAtUtc.Should().NotBeNull();
        _refreshTokenRepository.Verify(
            r => r.RevokeAllByUserIdAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleSoftDeletesUserBeforeRevokingTokens()
    {
        var callOrder = new List<string>();
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hashed-pw", "Hunter");
        _currentUserService.Setup(s => s.UserId).Returns(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepository
            .Setup(r => r.Update(It.IsAny<User>()))
            .Callback(() => callOrder.Add("update"));
        _refreshTokenRepository
            .Setup(r => r.RevokeAllByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("revoke"))
            .Returns(Task.CompletedTask);
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("save"))
            .ReturnsAsync(1);

        await CreateHandler().Handle(new DeleteAccountCommand(), CancellationToken.None);

        callOrder.Should().ContainInOrder("update", "revoke", "save");
    }

    [Fact]
    public async Task HandleSetsDeletedAtUtcOnUser()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("hunter@awaken.app", "hashed-pw", "Hunter");
        _currentUserService.Setup(s => s.UserId).Returns(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var before = DateTime.UtcNow;
        await CreateHandler().Handle(new DeleteAccountCommand(), CancellationToken.None);
        var after = DateTime.UtcNow;

        user.DeletedAtUtc.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task HandleThrowsWhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        _currentUserService.Setup(s => s.UserId).Returns(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = async () => await CreateHandler().Handle(new DeleteAccountCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
```

- [ ] **Step 2: Run handler tests to verify they fail**

```bash
cd backend && dotnet test tests/Awaken.UnitTests/Awaken.UnitTests.csproj --filter "FullyQualifiedName~DeleteAccountCommandHandlerTests" --no-build 2>&1 | tail -5
```

Expected: FAIL — `DeleteAccountCommandHandler` not found.

- [ ] **Step 3: Write failing validator tests**

Create `backend/tests/Awaken.UnitTests/Auth/DeleteAccountValidatorTests.cs`:

```csharp
using Awaken.Application.Auth.Commands.DeleteAccount;
using FluentAssertions;

namespace Awaken.UnitTests.Auth;

public class DeleteAccountValidatorTests
{
    private readonly DeleteAccountValidator _validator = new();

    [Fact]
    public async Task ValidateSucceedsWhenConfirmationIsTrue()
    {
        var result = await _validator.ValidateAsync(new DeleteAccountCommand());

        result.IsValid.Should().BeTrue();
    }
}
```

- [ ] **Step 4: Create the contract**

Create `backend/src/Awaken.Contracts/Auth/DeleteAccountRequest.cs`:

```csharp
namespace Awaken.Contracts.Auth;

public record DeleteAccountRequest(bool Confirmation);
```

- [ ] **Step 5: Create the command**

Create `backend/src/Awaken.Application/Auth/Commands/DeleteAccount/DeleteAccountCommand.cs`:

```csharp
using MediatR;

namespace Awaken.Application.Auth.Commands.DeleteAccount;

public record DeleteAccountCommand : IRequest<Unit>;
```

- [ ] **Step 6: Create the validator**

Create `backend/src/Awaken.Application/Auth/Commands/DeleteAccount/DeleteAccountValidator.cs`:

```csharp
using FluentValidation;

namespace Awaken.Application.Auth.Commands.DeleteAccount;

public class DeleteAccountValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountValidator()
    {
    }
}
```

- [ ] **Step 7: Create the handler**

Create `backend/src/Awaken.Application/Auth/Commands/DeleteAccount/DeleteAccountCommandHandler.cs`:

```csharp
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Auth.Commands.DeleteAccount;

public class DeleteAccountCommandHandler(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteAccountCommand, Unit>
{
    public async Task<Unit> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"User {userId} not found.");

        user.SoftDelete(userId);
        userRepository.Update(user);

        await refreshTokenRepository.RevokeAllByUserIdAsync(userId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
```

- [ ] **Step 8: Build and run all new unit tests**

```bash
cd backend && dotnet build --no-incremental -q && dotnet test tests/Awaken.UnitTests/Awaken.UnitTests.csproj --filter "FullyQualifiedName~DeleteAccount" -v minimal
```

Expected: 5 tests PASS.

- [ ] **Step 9: Commit**

```bash
git add backend/src/Awaken.Contracts/Auth/DeleteAccountRequest.cs \
        backend/src/Awaken.Application/Auth/Commands/DeleteAccount/ \
        backend/tests/Awaken.UnitTests/Auth/DeleteAccountCommandHandlerTests.cs \
        backend/tests/Awaken.UnitTests/Auth/DeleteAccountValidatorTests.cs
git commit -m "feat(backend): add DeleteAccountCommand handler and validator (US-013)"
```

---

## Task 3 — Backend: Controller endpoint + unit test

**Files:**
- Modify: `backend/src/Awaken.Api/Controllers/V1/AuthController.cs`
- Modify: `backend/tests/Awaken.UnitTests/Api/AuthControllerTests.cs`

- [ ] **Step 1: Write failing controller unit test**

Add at the end of the `AuthControllerTests` class in `backend/tests/Awaken.UnitTests/Api/AuthControllerTests.cs`:

```csharp
[Fact]
public async Task DeleteAccount_SendsCommand_AndReturnsOkResult()
{
    _mediator.Setup(m => m.Send(
            It.IsAny<DeleteAccountCommand>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(Unit.Value);

    var httpContext = new DefaultHttpContext();
    httpContext.Items["CorrelationId"] = "delete-account-corr-unit";
    var controller = new AuthController(_mediator.Object)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        }
    };

    var result = await controller.DeleteAccount(
        new DeleteAccountRequest(true),
        CancellationToken.None);

    var ok = result.Should().BeOfType<OkObjectResult>().Subject;
    ok.Value.Should().BeEquivalentTo(new
    {
        success = true,
        accountStatus = "deleted",
        correlationId = "delete-account-corr-unit"
    });
    _mediator.Verify(m => m.Send(It.IsAny<DeleteAccountCommand>(), It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
public async Task DeleteAccount_ReturnsBadRequest_WhenConfirmationIsFalse()
{
    var controller = new AuthController(_mediator.Object);

    var result = await controller.DeleteAccount(
        new DeleteAccountRequest(false),
        CancellationToken.None);

    result.Should().BeOfType<BadRequestObjectResult>();
    _mediator.Verify(m => m.Send(It.IsAny<DeleteAccountCommand>(), It.IsAny<CancellationToken>()), Times.Never);
}
```

Also add the required `using` at the top:

```csharp
using Awaken.Application.Auth.Commands.DeleteAccount;
```

- [ ] **Step 2: Run controller test to verify it fails**

```bash
cd backend && dotnet test tests/Awaken.UnitTests/Awaken.UnitTests.csproj --filter "FullyQualifiedName~AuthControllerTests.DeleteAccount" --no-build 2>&1 | tail -5
```

Expected: FAIL — `DeleteAccount` method not found on `AuthController`.

- [ ] **Step 3: Add endpoint to AuthController**

In `backend/src/Awaken.Api/Controllers/V1/AuthController.cs`, add the using at the top:

```csharp
using Awaken.Application.Auth.Commands.DeleteAccount;
```

And add the endpoint method:

```csharp
[HttpDelete("delete-account")]
[Authorize]
public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request, CancellationToken ct)
{
    if (request.Confirmation != true)
        return BadRequest(new { code = "CONFIRMATION_REQUIRED", message = "Confirmation must be true." });

    await mediator.Send(new DeleteAccountCommand(), ct);
    var correlationId = ControllerContext.HttpContext?.Items["CorrelationId"]?.ToString()
                        ?? Guid.NewGuid().ToString();
    return Ok(new { success = true, accountStatus = "deleted", correlationId });
}
```

- [ ] **Step 4: Build and run controller tests**

```bash
cd backend && dotnet build --no-incremental -q && dotnet test tests/Awaken.UnitTests/Awaken.UnitTests.csproj --filter "FullyQualifiedName~AuthControllerTests" -v minimal
```

Expected: all `AuthControllerTests` PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/src/Awaken.Api/Controllers/V1/AuthController.cs \
        backend/tests/Awaken.UnitTests/Api/AuthControllerTests.cs
git commit -m "feat(api): add DELETE /api/auth/delete-account endpoint (US-013)"
```

---

## Task 4 — Backend: Integration tests

**Files:**
- Create: `backend/tests/Awaken.IntegrationTests/AuthDeleteAccountEndpointTests.cs`

- [ ] **Step 1: Write integration tests**

Create `backend/tests/Awaken.IntegrationTests/AuthDeleteAccountEndpointTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Awaken.Contracts.Auth;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Awaken.IntegrationTests;

public class AuthDeleteAccountEndpointTests : IAsyncLifetime
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

    private async Task<AuthResponse> RegisterAndLoginAsync(
        string email = "hunter@awaken.app",
        string password = "Str0ngPass!")
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            name = "Hunter",
            language = "pt-BR"
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        return (await loginResponse.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    [Fact]
    public async Task DeleteAccountReturnsOkWhenAuthenticatedAndConfirmed()
    {
        var auth = await RegisterAndLoginAsync("delete1@awaken.app");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        _client.DefaultRequestHeaders.Add("X-Correlation-Id", "delete-corr-1");

        var response = await _client.DeleteAsync("/api/auth/delete-account");
        // Nota: DeleteAsync não envia body; usar SendAsync para enviar body
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/auth/delete-account")
        {
            Content = JsonContent.Create(new { confirmation = true })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        request.Headers.Add("X-Correlation-Id", "delete-corr-1");
        _client.DefaultRequestHeaders.Authorization = null;
        _client.DefaultRequestHeaders.Remove("X-Correlation-Id");

        response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        document.RootElement.GetProperty("accountStatus").GetString().Should().Be("deleted");
        document.RootElement.GetProperty("correlationId").GetString().Should().Be("delete-corr-1");
    }

    [Fact]
    public async Task DeleteAccountReturnsUnauthorizedWithoutToken()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/auth/delete-account")
        {
            Content = JsonContent.Create(new { confirmation = true })
        };

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAccountReturnsBadRequestWhenConfirmationIsFalse()
    {
        var auth = await RegisterAndLoginAsync("delete-bad@awaken.app");
        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/auth/delete-account")
        {
            Content = JsonContent.Create(new { confirmation = false })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteAccountRevokesRefreshTokenSoItCannotBeReused()
    {
        var auth = await RegisterAndLoginAsync("delete-revoke@awaken.app");
        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/auth/delete-account")
        {
            Content = JsonContent.Create(new { confirmation = true })
        };
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        await _client.SendAsync(deleteRequest);

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh-token", new
        {
            refreshToken = auth.RefreshToken
        });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAccountSoftDeletesUserSoLoginFails()
    {
        var email = "delete-login@awaken.app";
        var password = "Str0ngPass!";
        var auth = await RegisterAndLoginAsync(email, password);
        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/auth/delete-account")
        {
            Content = JsonContent.Create(new { confirmation = true })
        };
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        await _client.SendAsync(deleteRequest);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

- [ ] **Step 2: Run integration tests**

```bash
cd backend && dotnet test tests/Awaken.IntegrationTests/Awaken.IntegrationTests.csproj --filter "FullyQualifiedName~AuthDeleteAccountEndpointTests" -v minimal
```

Expected: 5 tests PASS (may take ~30s for Testcontainers).

> **Note about `DeleteAccountSoftDeletesUserSoLoginFails`:** This test requires that the login handler rejects deleted users. If `LoginUserCommandHandler` does not check `user.IsDeleted`, you need to add that check:
> In `backend/src/Awaken.Application/Auth/Commands/Login/LoginUserCommandHandler.cs`, after fetching the user, add:
> ```csharp
> if (user.IsDeleted)
>     throw new UnauthorizedException("INVALID_CREDENTIALS");
> ```

- [ ] **Step 3: Commit**

```bash
git add backend/tests/Awaken.IntegrationTests/AuthDeleteAccountEndpointTests.cs
git commit -m "test(backend): integration tests for DELETE /api/auth/delete-account (US-013)"
```

---

## Task 5 — Flutter: DTO + DataSource + Repository (TDD)

**Files:**
- Create: `apps/mobile/lib/features/auth/data/dtos/delete_account_request_dto.dart`
- Modify: `apps/mobile/lib/features/auth/data/datasources/auth_remote_data_source.dart`
- Modify: `apps/mobile/lib/features/auth/domain/repositories/auth_repository.dart`
- Modify: `apps/mobile/lib/features/auth/data/repositories/auth_repository_impl.dart`
- Modify: `apps/mobile/test/features/auth/data/repositories/auth_repository_impl_test.dart`

- [ ] **Step 1: Write failing repository tests**

Add to `apps/mobile/test/features/auth/data/repositories/auth_repository_impl_test.dart`, inside the `void main()` body, add a new group after the existing `group('AuthRepositoryImpl.logout', ...)`:

```dart
  group('AuthRepositoryImpl.deleteAccount', () {
    test('deleteAccount chama datasource e limpa tokens locais', () async {
      when(() => remoteDataSource.deleteAccount()).thenAnswer((_) async {});
      when(() => tokenStorage.clear()).thenAnswer((_) async {});

      await repository.deleteAccount();

      verify(() => remoteDataSource.deleteAccount()).called(1);
      verify(() => tokenStorage.clear()).called(1);
    });

    test(
        'RN-005: deleteAccount limpa tokens locais mesmo quando datasource lança NetworkError',
        () async {
      when(() => remoteDataSource.deleteAccount()).thenThrow(const NetworkError());
      when(() => tokenStorage.clear()).thenAnswer((_) async {});

      await expectLater(repository.deleteAccount(), throwsA(isA<NetworkError>()));

      verify(() => tokenStorage.clear()).called(1);
    });

    test(
        'RN-005: deleteAccount limpa tokens locais mesmo quando datasource lança UnexpectedError',
        () async {
      when(() => remoteDataSource.deleteAccount()).thenThrow(const UnexpectedError());
      when(() => tokenStorage.clear()).thenAnswer((_) async {});

      await expectLater(repository.deleteAccount(), throwsA(isA<UnexpectedError>()));

      verify(() => tokenStorage.clear()).called(1);
    });
  });
```

- [ ] **Step 2: Run repository tests to verify they fail**

```bash
cd apps/mobile && flutter test test/features/auth/data/repositories/auth_repository_impl_test.dart 2>&1 | tail -10
```

Expected: FAIL — `deleteAccount` not found.

- [ ] **Step 3: Create DTO**

Create `apps/mobile/lib/features/auth/data/dtos/delete_account_request_dto.dart`:

```dart
class DeleteAccountRequestDto {
  const DeleteAccountRequestDto();

  Map<String, dynamic> toJson() => {'confirmation': true};
}
```

- [ ] **Step 4: Add deleteAccount to domain repository**

In `apps/mobile/lib/features/auth/domain/repositories/auth_repository.dart`, add:

```dart
  Future<void> deleteAccount();
```

- [ ] **Step 5: Add deleteAccount to AuthRemoteDataSource**

In `apps/mobile/lib/features/auth/data/datasources/auth_remote_data_source.dart`, add the import and method. Also add an error mapper method:

Add this import at the top (already in file):
(no new import needed — `dio` and `app_error` are already imported)

Add after `forgotPassword`:

```dart
  Future<void> deleteAccount() async {
    try {
      await _dio.delete('/api/auth/delete-account',
          data: const DeleteAccountRequestDto().toJson());
    } on DioException catch (e) {
      throw _mapDeleteAccountError(e);
    }
  }

  AppError _mapDeleteAccountError(DioException e) {
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

Also add the import for the DTO:

```dart
import '../dtos/delete_account_request_dto.dart';
```

- [ ] **Step 6: Implement deleteAccount in AuthRepositoryImpl**

In `apps/mobile/lib/features/auth/data/repositories/auth_repository_impl.dart`, add:

```dart
  @override
  Future<void> deleteAccount() async {
    try {
      await _remoteDataSource.deleteAccount();
    } finally {
      // RN-005: limpar sessão local mesmo se o backend falhar
      await _tokenStorage.clear();
    }
  }
```

- [ ] **Step 7: Run repository tests**

```bash
cd apps/mobile && flutter test test/features/auth/data/repositories/auth_repository_impl_test.dart 2>&1 | tail -10
```

Expected: all tests PASS.

- [ ] **Step 8: Run analyze**

```bash
cd apps/mobile && flutter analyze
```

Expected: no issues.

- [ ] **Step 9: Commit**

```bash
git add apps/mobile/lib/features/auth/data/dtos/delete_account_request_dto.dart \
        apps/mobile/lib/features/auth/data/datasources/auth_remote_data_source.dart \
        apps/mobile/lib/features/auth/domain/repositories/auth_repository.dart \
        apps/mobile/lib/features/auth/data/repositories/auth_repository_impl.dart \
        apps/mobile/test/features/auth/data/repositories/auth_repository_impl_test.dart
git commit -m "feat(flutter/data): add deleteAccount to datasource and repository (US-013)"
```

---

## Task 6 — Flutter: State + Controller (TDD)

**Files:**
- Create: `apps/mobile/lib/features/auth/presentation/providers/delete_account_state.dart`
- Create: `apps/mobile/lib/features/auth/presentation/providers/delete_account_controller.dart`
- Create: `apps/mobile/test/features/auth/presentation/providers/delete_account_controller_test.dart`

- [ ] **Step 1: Write failing controller tests**

Create `apps/mobile/test/features/auth/presentation/providers/delete_account_controller_test.dart`:

```dart
import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:awaken/core/analytics/analytics_provider.dart';
import 'package:awaken/core/analytics/analytics_service.dart';
import 'package:awaken/core/auth/session_provider.dart';
import 'package:awaken/core/errors/app_error.dart';
import 'package:awaken/features/auth/domain/repositories/auth_repository.dart';
import 'package:awaken/features/auth/presentation/providers/auth_providers.dart';
import 'package:awaken/features/auth/presentation/providers/delete_account_controller.dart';
import 'package:awaken/features/auth/presentation/providers/delete_account_state.dart';

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

  group('DeleteAccountController', () {
    test('estado inicial é DeleteAccountInitial', () {
      final container = buildContainer();
      addTearDown(container.dispose);

      expect(container.read(deleteAccountControllerProvider),
          isA<DeleteAccountInitial>());
    });

    test(
        'CA-001: exclusão bem-sucedida encerra sessão local e dispara analytics',
        () async {
      when(() => mockAuthRepository.deleteAccount()).thenAnswer((_) async {});

      final container = buildContainer();
      addTearDown(container.dispose);

      await container
          .read(deleteAccountControllerProvider.notifier)
          .deleteAccount();

      expect(container.read(deleteAccountControllerProvider),
          isA<DeleteAccountSuccess>());
      final session = container.read(currentSessionStateProvider);
      expect(session?.hasSession, isFalse);
      verify(() => mockAnalytics.logEvent('account_delete_started')).called(1);
      verify(() => mockAnalytics.logEvent('account_delete_completed')).called(1);
    });

    test(
        'RN-005: falha de rede ainda limpa sessão local e termina em DeleteAccountConnectionError',
        () async {
      when(() => mockAuthRepository.deleteAccount())
          .thenThrow(const NetworkError());

      final container = buildContainer();
      addTearDown(container.dispose);

      await container
          .read(deleteAccountControllerProvider.notifier)
          .deleteAccount();

      expect(container.read(deleteAccountControllerProvider),
          isA<DeleteAccountConnectionError>());
      final session = container.read(currentSessionStateProvider);
      expect(session?.hasSession, isFalse);
      verify(() => mockAnalytics.logEvent('account_delete_failed')).called(1);
    });

    test(
        'RN-005: erro inesperado ainda limpa sessão local e termina em DeleteAccountUnexpectedError',
        () async {
      when(() => mockAuthRepository.deleteAccount())
          .thenThrow(const UnexpectedError());

      final container = buildContainer();
      addTearDown(container.dispose);

      await container
          .read(deleteAccountControllerProvider.notifier)
          .deleteAccount();

      expect(container.read(deleteAccountControllerProvider),
          isA<DeleteAccountUnexpectedError>());
      final session = container.read(currentSessionStateProvider);
      expect(session?.hasSession, isFalse);
    });

    test('estado é DeleteAccountLoading durante o processo', () async {
      final completer = Completer<void>();
      when(() => mockAuthRepository.deleteAccount())
          .thenAnswer((_) => completer.future);

      final container = buildContainer();
      addTearDown(container.dispose);

      final future = container
          .read(deleteAccountControllerProvider.notifier)
          .deleteAccount();

      expect(container.read(deleteAccountControllerProvider),
          isA<DeleteAccountLoading>());

      completer.complete();
      await future;

      expect(container.read(deleteAccountControllerProvider),
          isA<DeleteAccountSuccess>());
    });

    test('CA-002: cancelamento não chama o repositório', () {
      final container = buildContainer();
      addTearDown(container.dispose);

      verifyNever(() => mockAuthRepository.deleteAccount());
    });
  });
}
```

- [ ] **Step 2: Run controller tests to verify they fail**

```bash
cd apps/mobile && flutter test test/features/auth/presentation/providers/delete_account_controller_test.dart 2>&1 | tail -10
```

Expected: FAIL — `DeleteAccountController` not found.

- [ ] **Step 3: Create delete_account_state.dart**

Create `apps/mobile/lib/features/auth/presentation/providers/delete_account_state.dart`:

```dart
sealed class DeleteAccountState {
  const DeleteAccountState();
}

final class DeleteAccountInitial extends DeleteAccountState {
  const DeleteAccountInitial();
}

final class DeleteAccountLoading extends DeleteAccountState {
  const DeleteAccountLoading();
}

final class DeleteAccountSuccess extends DeleteAccountState {
  const DeleteAccountSuccess();
}

final class DeleteAccountConnectionError extends DeleteAccountState {
  const DeleteAccountConnectionError();
}

final class DeleteAccountUnexpectedError extends DeleteAccountState {
  const DeleteAccountUnexpectedError();
}
```

- [ ] **Step 4: Create delete_account_controller.dart**

Create `apps/mobile/lib/features/auth/presentation/providers/delete_account_controller.dart`:

```dart
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/analytics/analytics_provider.dart';
import '../../../../core/auth/session_provider.dart';
import '../../../../core/auth/session_state.dart';
import '../../../../core/errors/app_error.dart';
import 'auth_providers.dart';
import 'delete_account_state.dart';

class DeleteAccountController extends Notifier<DeleteAccountState> {
  @override
  DeleteAccountState build() => const DeleteAccountInitial();

  Future<void> deleteAccount() async {
    state = const DeleteAccountLoading();

    final analytics = ref.read(analyticsServiceProvider);
    await analytics.logEvent('account_delete_started');

    try {
      await ref.read(authRepositoryProvider).deleteAccount();
    } on NetworkError {
      _clearSession();
      state = const DeleteAccountConnectionError();
      await analytics.logEvent('account_delete_failed');
      return;
    } catch (_) {
      _clearSession();
      state = const DeleteAccountUnexpectedError();
      await analytics.logEvent('account_delete_failed');
      return;
    }

    _clearSession();
    state = const DeleteAccountSuccess();
    await analytics.logEvent('account_delete_completed');
  }

  void _clearSession() {
    ref
        .read(currentSessionStateProvider.notifier)
        .set(const SessionState.visitor());
  }
}

final deleteAccountControllerProvider =
    NotifierProvider<DeleteAccountController, DeleteAccountState>(
        DeleteAccountController.new);
```

- [ ] **Step 5: Run controller tests**

```bash
cd apps/mobile && flutter test test/features/auth/presentation/providers/delete_account_controller_test.dart 2>&1 | tail -10
```

Expected: 6 tests PASS.

- [ ] **Step 6: Run analyze**

```bash
cd apps/mobile && flutter analyze
```

Expected: no issues.

- [ ] **Step 7: Commit**

```bash
git add apps/mobile/lib/features/auth/presentation/providers/delete_account_state.dart \
        apps/mobile/lib/features/auth/presentation/providers/delete_account_controller.dart \
        apps/mobile/test/features/auth/presentation/providers/delete_account_controller_test.dart
git commit -m "feat(flutter/state): add DeleteAccountController and state (US-013)"
```

---

## Task 7 — Flutter: Page + Router + Settings tile (TDD)

**Files:**
- Create: `apps/mobile/lib/features/auth/presentation/pages/delete_account_page.dart`
- Modify: `apps/mobile/lib/app/app_router.dart`
- Modify: `apps/mobile/lib/features/settings/presentation/pages/settings_page.dart`
- Create: `apps/mobile/test/features/auth/presentation/pages/delete_account_page_test.dart`
- Modify: `apps/mobile/test/features/settings/presentation/pages/settings_page_test.dart`

- [ ] **Step 1: Write failing page tests**

Create `apps/mobile/test/features/auth/presentation/pages/delete_account_page_test.dart`:

```dart
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mocktail/mocktail.dart';
import 'package:awaken/core/analytics/analytics_provider.dart';
import 'package:awaken/core/analytics/analytics_service.dart';
import 'package:awaken/core/errors/app_error.dart';
import 'package:awaken/features/auth/domain/repositories/auth_repository.dart';
import 'package:awaken/features/auth/presentation/pages/delete_account_page.dart';
import 'package:awaken/features/auth/presentation/providers/auth_providers.dart';
import 'package:awaken/l10n/app_localizations.dart';

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

  Widget buildTestApp() {
    final router = GoRouter(
      initialLocation: '/delete-account',
      routes: [
        GoRoute(
          path: '/delete-account',
          builder: (_, __) => const DeleteAccountPage(),
        ),
      ],
    );

    return ProviderScope(
      overrides: [
        analyticsServiceProvider.overrideWithValue(mockAnalytics),
        authRepositoryProvider.overrideWithValue(mockAuthRepository),
      ],
      child: MaterialApp.router(
        routerConfig: router,
        localizationsDelegates: const [
          AppLocalizations.delegate,
          GlobalMaterialLocalizations.delegate,
          GlobalWidgetsLocalizations.delegate,
          GlobalCupertinoLocalizations.delegate,
        ],
        supportedLocales: const [
          Locale('pt', 'BR'),
          Locale('en'),
          Locale('es'),
        ],
        locale: const Locale('pt', 'BR'),
      ),
    );
  }

  testWidgets('CA-001: exibe título e botão de exclusão', (tester) async {
    await tester.pumpWidget(buildTestApp());
    await tester.pumpAndSettle();

    expect(find.text('Excluir conta'), findsWidgets);
    expect(find.byKey(const Key('delete-account-button')), findsOneWidget);
  });

  testWidgets('exibe aviso sobre consequências', (tester) async {
    await tester.pumpWidget(buildTestApp());
    await tester.pumpAndSettle();

    expect(
        find.textContaining('removidos ou anonimizados'), findsOneWidget);
  });

  testWidgets('toque no botão exibe dialog de confirmação', (tester) async {
    await tester.pumpWidget(buildTestApp());
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('delete-account-button')));
    await tester.pumpAndSettle();

    expect(find.text('Excluir sua conta?'), findsOneWidget);
    expect(find.text('Cancelar'), findsOneWidget);
  });

  testWidgets('CA-002: cancelar dialog não chama o repositório', (tester) async {
    await tester.pumpWidget(buildTestApp());
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('delete-account-button')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Cancelar'));
    await tester.pumpAndSettle();

    verifyNever(() => mockAuthRepository.deleteAccount());
  });

  testWidgets('CA-001: confirmar exclusão chama o repositório', (tester) async {
    when(() => mockAuthRepository.deleteAccount()).thenAnswer((_) async {});

    await tester.pumpWidget(buildTestApp());
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('delete-account-button')));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('delete-account-dialog-confirm')));
    await tester.pumpAndSettle();

    verify(() => mockAuthRepository.deleteAccount()).called(1);
  });

  testWidgets('CA-003: erro de conexão exibe snackbar', (tester) async {
    when(() => mockAuthRepository.deleteAccount())
        .thenThrow(const NetworkError());

    await tester.pumpWidget(buildTestApp());
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('delete-account-button')));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('delete-account-dialog-confirm')));
    await tester.pumpAndSettle();

    expect(find.byType(SnackBar), findsOneWidget);
  });

  testWidgets('estado inicial não exibe indicador de progresso', (tester) async {
    await tester.pumpWidget(buildTestApp());
    await tester.pumpAndSettle();

    expect(find.byType(CircularProgressIndicator), findsNothing);
  });
}
```

- [ ] **Step 2: Write failing settings page tests**

Add to `apps/mobile/test/features/settings/presentation/pages/settings_page_test.dart`, at the end of `void main()`:

```dart
  testWidgets('exibe opção Excluir conta', (tester) async {
    await tester.pumpWidget(buildTestApp());
    await tester.pumpAndSettle();

    expect(find.text('Excluir conta'), findsOneWidget);
  });
```

- [ ] **Step 3: Run page tests to verify they fail**

```bash
cd apps/mobile && flutter test test/features/auth/presentation/pages/delete_account_page_test.dart test/features/settings/presentation/pages/settings_page_test.dart 2>&1 | tail -10
```

Expected: FAIL — `DeleteAccountPage` not found.

- [ ] **Step 4: Create delete_account_page.dart**

Create `apps/mobile/lib/features/auth/presentation/pages/delete_account_page.dart`:

```dart
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:awaken/l10n/app_localizations.dart';

import '../../../../core/auth/access_status.dart';
import '../../../../core/auth/session_provider.dart';
import '../../../../design_system/components/awaken_button.dart';
import '../../../../design_system/tokens/colors.dart';
import '../../../../design_system/tokens/spacing.dart';
import '../../../../design_system/tokens/typography.dart';
import '../providers/delete_account_controller.dart';
import '../providers/delete_account_state.dart';

class DeleteAccountPage extends ConsumerWidget {
  const DeleteAccountPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final state = ref.watch(deleteAccountControllerProvider);
    final isLoading = state is DeleteAccountLoading;
    final session = ref.watch(currentSessionStateProvider);
    final hasActiveSubscription =
        session?.accessStatus == AccessStatus.subscriptionActive;

    ref.listen<DeleteAccountState>(deleteAccountControllerProvider, (_, next) {
      if (next is DeleteAccountConnectionError) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(l10n.deleteAccountConnectionError)),
        );
      } else if (next is DeleteAccountUnexpectedError) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(l10n.deleteAccountUnexpectedError)),
        );
      }
    });

    return Scaffold(
      backgroundColor: AwakenColors.backgroundPrimary,
      appBar: AppBar(
        backgroundColor: Colors.transparent,
        elevation: 0,
        leading: IconButton(
          key: const Key('delete-account-back'),
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
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const Icon(
                    Icons.delete_forever_outlined,
                    size: 64,
                    color: AwakenColors.error,
                  ),
                  const SizedBox(height: AwakenSpacing.lg),
                  Text(
                    l10n.deleteAccountTitle,
                    textAlign: TextAlign.center,
                    style: AwakenTypography.displayMedium,
                  ),
                  const SizedBox(height: AwakenSpacing.sm),
                  Text(
                    l10n.deleteAccountWarning,
                    textAlign: TextAlign.center,
                    style: AwakenTypography.bodyMedium,
                  ),
                  if (hasActiveSubscription) ...[
                    const SizedBox(height: AwakenSpacing.md),
                    Container(
                      padding: const EdgeInsets.all(AwakenSpacing.md),
                      decoration: BoxDecoration(
                        color: AwakenColors.warning.withOpacity(0.12),
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(
                            color: AwakenColors.warning.withOpacity(0.4)),
                      ),
                      child: Text(
                        l10n.deleteAccountSubscriptionWarning,
                        textAlign: TextAlign.center,
                        style: AwakenTypography.bodySmall,
                      ),
                    ),
                  ],
                  const SizedBox(height: AwakenSpacing.xl),
                  AwakenButton(
                    key: const Key('delete-account-button'),
                    label: l10n.deleteAccountButton,
                    isLoading: isLoading,
                    isDestructive: true,
                    onPressed:
                        isLoading ? null : () => _confirmDelete(context, ref, l10n),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  Future<void> _confirmDelete(
    BuildContext context,
    WidgetRef ref,
    AppLocalizations l10n,
  ) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: AwakenColors.backgroundSecondary,
        title: Text(
          l10n.deleteAccountDialogTitle,
          style: AwakenTypography.titleMedium,
        ),
        content: Text(
          l10n.deleteAccountDialogMessage,
          style: AwakenTypography.bodyMedium,
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
            child: Text(l10n.deleteAccountDialogCancel),
          ),
          TextButton(
            key: const Key('delete-account-dialog-confirm'),
            onPressed: () => Navigator.of(ctx).pop(true),
            child: Text(
              l10n.deleteAccountDialogConfirm,
              style: const TextStyle(color: AwakenColors.error),
            ),
          ),
        ],
      ),
    );

    if (confirmed == true) {
      await ref.read(deleteAccountControllerProvider.notifier).deleteAccount();
    }
  }
}
```

> **Note:** `AwakenButton` may not have an `isDestructive` parameter. Check `apps/mobile/lib/design_system/components/awaken_button.dart`. If it doesn't, remove that parameter and instead wrap in a `Theme` with `colorScheme.primary = AwakenColors.error`, or simply omit the styling difference.

> **Note:** `AwakenColors.warning` may not exist. Check the tokens file. If it doesn't, replace `AwakenColors.warning` with `Colors.orange`.

- [ ] **Step 5: Add route to app_router.dart**

In `apps/mobile/lib/app/app_router.dart`:

Add to the `AppRoutes` abstract class:
```dart
  static const deleteAccount = '/settings/delete-account';
```

Add import at top:
```dart
import '../features/auth/presentation/pages/delete_account_page.dart';
```

Add route in `buildAppRouter` after `settingsLanguage`:
```dart
    GoRoute(
      path: AppRoutes.deleteAccount,
      pageBuilder: (ctx, state) => _buildPage(
        state: state,
        child: const DeleteAccountPage(),
      ),
    ),
```

- [ ] **Step 6: Add tile to settings_page.dart**

In `apps/mobile/lib/features/settings/presentation/pages/settings_page.dart`:

Add import:
```dart
import '../../../auth/presentation/providers/delete_account_controller.dart';
import '../../../auth/presentation/providers/delete_account_state.dart';
```

In `build`, add after the `logoutState` watchers:
```dart
    final deleteAccountState = ref.watch(deleteAccountControllerProvider);
    final isDeletingAccount = deleteAccountState is DeleteAccountLoading;
```

Add listener:
```dart
    ref.listen<DeleteAccountState>(deleteAccountControllerProvider, (_, next) {
      if (next is DeleteAccountConnectionError) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(l10n.deleteAccountConnectionError)),
        );
      } else if (next is DeleteAccountUnexpectedError) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(l10n.deleteAccountUnexpectedError)),
        );
      }
    });
```

In the `ListView`, add after the logout tile:
```dart
          const Divider(height: 1, indent: AwakenSpacing.lg),
          _SettingsTile(
            label: l10n.settingsDeleteAccount,
            icon: Icons.delete_outline,
            isDestructive: true,
            isLoading: isDeletingAccount,
            onTap: isDeletingAccount
                ? null
                : () => context.push(AppRoutes.deleteAccount),
          ),
```

- [ ] **Step 7: Check AwakenButton and AwakenColors for isDestructive/warning**

```bash
cd apps/mobile && grep -n "isDestructive\|warning" lib/design_system/components/awaken_button.dart lib/design_system/tokens/colors.dart 2>&1
```

If `AwakenButton` doesn't have `isDestructive`, remove that param from the page and use default styling. If `AwakenColors.warning` doesn't exist, replace with `Colors.orange` in the page.

- [ ] **Step 8: Generate l10n and run analyze**

```bash
cd apps/mobile && flutter gen-l10n && flutter analyze
```

Expected: no issues.

- [ ] **Step 9: Run page + settings tests**

```bash
cd apps/mobile && flutter test test/features/auth/presentation/pages/delete_account_page_test.dart test/features/settings/presentation/pages/settings_page_test.dart 2>&1 | tail -15
```

Expected: all tests PASS.

- [ ] **Step 10: Commit**

```bash
git add apps/mobile/lib/features/auth/presentation/pages/delete_account_page.dart \
        apps/mobile/lib/app/app_router.dart \
        apps/mobile/lib/features/settings/presentation/pages/settings_page.dart \
        apps/mobile/test/features/auth/presentation/pages/delete_account_page_test.dart \
        apps/mobile/test/features/settings/presentation/pages/settings_page_test.dart
git commit -m "feat(flutter/ui): add delete account page, route, and settings tile (US-013)"
```

---

## Task 8 — Verificação final

- [ ] **Step 1: Run all backend tests**

```bash
cd backend && dotnet test -v minimal
```

Expected: all tests PASS (no failures).

- [ ] **Step 2: Run all Flutter tests**

```bash
cd apps/mobile && flutter test 2>&1 | tail -20
```

Expected: all tests PASS.

- [ ] **Step 3: Run Flutter analyze**

```bash
cd apps/mobile && flutter analyze
```

Expected: no issues.

---

## Self-Review: Spec Coverage

| Requirement | Covered by |
|---|---|
| RN-001: Only authenticated users | `[Authorize]` on endpoint; Flutter requires session |
| RN-002: Explicit confirmation | Confirmation dialog in page + `confirmation` field in API |
| RN-003: Explain consequences | `deleteAccountWarning` text on page |
| RN-004: Subscription cancellation via store | `deleteAccountSubscriptionWarning` shown when `subscriptionActive` |
| RN-005: Clear local session after | `_clearSession()` in controller + `finally` in repo impl |
| RN-006: Backend policy (soft-delete) | `user.SoftDelete()` in handler |
| CA-001: Delete confirmed → policy applied + session cleared | Handler + controller |
| CA-002: User cancels → no changes | Dialog cancel; controller never called |
| CA-003: Active subscription → inform | Warning block shown when `subscriptionActive` |
| Analytics: account_delete_started | Fired before request |
| Analytics: account_delete_completed | Fired on success |
| Analytics: account_delete_failed | Fired on network/unexpected error |
| PT-BR, EN, ES | ARB keys in all 3 files |
| settingsDeleteAccount in settings | Tile added to settings_page |
