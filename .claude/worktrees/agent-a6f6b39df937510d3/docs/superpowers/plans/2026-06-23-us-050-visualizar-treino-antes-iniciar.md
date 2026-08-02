# US-050 — Visualizar treino antes de iniciar (EPIC-007)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implementar a tela de pré-treino (somente leitura) com endpoint `GET /api/quests/{questId}/preview`, exibindo exercícios, tipo de treino, XP estimado e duração, com estados de loading, sucesso, acesso bloqueado, quest já iniciada e erro.

**Architecture:** Backend expõe `GET /api/quests/{questId}/preview` via query MediatR (`GetQuestPreviewQuery`), mapeando o quest existente para `QuestPreviewResponse`. Flutter adiciona `getQuestPreview(questId)` ao `QuestsRepository`, um `PreQuestController` (`FamilyNotifier` por `questId`), e a `PreQuestPage` com route `/pre-quest/:questId`. O acesso expirado é bloqueado pelo `ActiveAccessMiddleware` existente (retorna 403), sem lógica adicional no handler.

**Tech Stack:** ASP.NET Core + MediatR + EF Core (backend) · Flutter + Riverpod + Dio (frontend) · xUnit + FluentAssertions + Testcontainers (backend tests) · flutter_test + mocktail (frontend tests)

---

## Mapa de arquivos

### Criar
| Arquivo | Responsabilidade |
|---|---|
| `Awaken.Contracts/Quests/QuestPreviewResponse.cs` | DTO de resposta do preview |
| `Awaken.Application/Quests/Queries/GetQuestPreview/GetQuestPreviewQuery.cs` | Record do query MediatR |
| `Awaken.Application/Quests/Queries/GetQuestPreview/GetQuestPreviewQueryHandler.cs` | Handler: busca quest, verifica ownership, mapeia |
| `backend/tests/Awaken.UnitTests/Quests/GetQuestPreviewQueryHandlerTests.cs` | Testes unitários do handler |
| `backend/tests/Awaken.IntegrationTests/QuestPreviewEndpointTests.cs` | Testes de integração do endpoint |
| `apps/mobile/lib/features/quests/domain/entities/quest_preview.dart` | Entidade de domínio + enum TrainingType |
| `apps/mobile/lib/features/quests/data/dtos/quest_preview_response_dto.dart` | DTO Flutter + fromJson |
| `apps/mobile/lib/features/quests/presentation/providers/pre_quest_state.dart` | Estados sealed da pré-quest |
| `apps/mobile/lib/features/quests/presentation/providers/pre_quest_controller.dart` | FamilyNotifier + provider |
| `apps/mobile/lib/features/quests/presentation/widgets/pre_quest_exercise_card.dart` | Card de exercício somente leitura |
| `apps/mobile/lib/features/quests/presentation/pages/pre_quest_page.dart` | Tela de pré-treino completa |
| `apps/mobile/test/features/quests/data/datasources/quest_preview_data_source_test.dart` | Testes do data source |
| `apps/mobile/test/features/quests/presentation/providers/pre_quest_controller_test.dart` | Testes do controller |

### Modificar
| Arquivo | O que muda |
|---|---|
| `Awaken.Application/Quests/Common/QuestResponseMapper.cs` | Adicionar método `ToPreviewResponse(Quest)` |
| `Awaken.Api/Controllers/V1/QuestsController.cs` | Adicionar `GET /{questId:guid}/preview` |
| `apps/mobile/lib/features/quests/data/datasources/quests_remote_data_source.dart` | Adicionar `getQuestPreview(questId)` |
| `apps/mobile/lib/features/quests/domain/repositories/quests_repository.dart` | Adicionar `getQuestPreview(questId)` |
| `apps/mobile/lib/features/quests/data/repositories/quests_repository_impl.dart` | Implementar `getQuestPreview` |
| `apps/mobile/lib/app/app_router.dart` | Adicionar rota `/pre-quest/:questId` |
| `apps/mobile/lib/l10n/app_pt.arb` | Adicionar chaves de pré-treino |
| `apps/mobile/lib/l10n/app_en.arb` | Adicionar chaves de pré-treino |
| `apps/mobile/lib/l10n/app_es.arb` | Adicionar chaves de pré-treino |
| `apps/mobile/lib/l10n/app_fr.arb` | Adicionar chaves de pré-treino |

---

## Task 1: Backend — QuestPreviewResponse DTO

**Files:**
- Create: `backend/src/Awaken.Contracts/Quests/QuestPreviewResponse.cs`

- [ ] **Step 1: Criar o DTO**

```csharp
// backend/src/Awaken.Contracts/Quests/QuestPreviewResponse.cs
namespace Awaken.Contracts.Quests;

public record QuestPreviewResponse(
    Guid QuestId,
    string TrainingType,
    long EstimatedXp,
    int EstimatedDurationMinutes,
    bool CanChangeTrainingType,
    WorkoutDto? Workout);
```

> `WorkoutDto` já está no namespace `Awaken.Contracts.Quests` (arquivo `QuestResponse.cs`), sem import necessário.

- [ ] **Step 2: Verificar compilação**

```bash
cd backend/src
dotnet build Awaken.Contracts/Awaken.Contracts.csproj
```

Expected: compilação sem erros.

---

## Task 2: Backend — QuestResponseMapper.ToPreviewResponse

**Files:**
- Modify: `backend/src/Awaken.Application/Quests/Common/QuestResponseMapper.cs`

- [ ] **Step 1: Adicionar método `ToPreviewResponse` no mapper**

No arquivo `QuestResponseMapper.cs`, logo após o método `ToResponse`, adicionar:

```csharp
public static QuestPreviewResponse ToPreviewResponse(Quest quest)
{
    var workout = ParseWorkout(quest.WorkoutJson);
    var estimatedDurationMinutes = workout?.DurationMinutes ?? 0;
    var estimatedXp = (long)Math.Round(estimatedDurationMinutes * 4.0);
    var trainingType = quest.IsPersonalized ? "personalized_individual" : "fallback";
    var canChangeTrainingType = quest.Status is not ("in_progress" or "completed");

    return new QuestPreviewResponse(
        QuestId: quest.Id,
        TrainingType: trainingType,
        EstimatedXp: estimatedXp,
        EstimatedDurationMinutes: estimatedDurationMinutes,
        CanChangeTrainingType: canChangeTrainingType,
        Workout: workout);
}
```

> `ParseWorkout` já é private static no mesmo arquivo — acessível pelo novo método público.

- [ ] **Step 2: Verificar compilação**

```bash
cd backend/src
dotnet build Awaken.Application/Awaken.Application.csproj
```

Expected: sem erros.

---

## Task 3: Backend — GetQuestPreviewQuery + Handler + Unit Tests

**Files:**
- Create: `backend/src/Awaken.Application/Quests/Queries/GetQuestPreview/GetQuestPreviewQuery.cs`
- Create: `backend/src/Awaken.Application/Quests/Queries/GetQuestPreview/GetQuestPreviewQueryHandler.cs`
- Create: `backend/tests/Awaken.UnitTests/Quests/GetQuestPreviewQueryHandlerTests.cs`

- [ ] **Step 1: Criar o record do query**

```csharp
// backend/src/Awaken.Application/Quests/Queries/GetQuestPreview/GetQuestPreviewQuery.cs
using Awaken.Contracts.Quests;
using MediatR;

namespace Awaken.Application.Quests.Queries.GetQuestPreview;

public record GetQuestPreviewQuery(Guid QuestId) : IRequest<QuestPreviewResponse>;
```

- [ ] **Step 2: Escrever os testes unitários do handler (failing)**

```csharp
// backend/tests/Awaken.UnitTests/Quests/GetQuestPreviewQueryHandlerTests.cs
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Queries.GetQuestPreview;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Quests;

public class GetQuestPreviewQueryHandlerTests
{
    private readonly Mock<IQuestRepository> _questRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid QuestId = Guid.NewGuid();

    private const string WorkoutJson = """
    {
      "title": "Daily Quest",
      "description": "Full body",
      "durationMinutes": 30,
      "exercises": [
        { "name": "Squat", "sets": 3, "repsMin": 10, "repsMax": 15, "restSeconds": 90, "targetRpe": "6-8" }
      ]
    }
    """;

    public GetQuestPreviewQueryHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
    }

    private GetQuestPreviewQueryHandler CreateHandler() => new(
        _questRepository.Object,
        _currentUserService.Object);

    private Quest BuildPendingPersonalizedQuest()
    {
        var quest = Quest.Create(UserId, DateTime.UtcNow.Date, "pt-BR", "idem-key");
        quest.AssignWorkout(WorkoutJson, isPersonalized: true);
        return quest;
    }

    private Quest BuildInProgressQuest()
    {
        var quest = Quest.Create(UserId, DateTime.UtcNow.Date, "pt-BR", "idem-key");
        quest.AssignWorkout(WorkoutJson, isPersonalized: true);
        quest.Start();
        return quest;
    }

    private Quest BuildFallbackPendingQuest()
    {
        var quest = Quest.Create(UserId, DateTime.UtcNow.Date, "pt-BR", "idem-key");
        quest.AssignWorkout(WorkoutJson, isPersonalized: false);
        return quest;
    }

    [Fact]
    public async Task CA001_ReturnsPreview_WhenQuestBelongsToUserAndIsPending()
    {
        var quest = BuildPendingPersonalizedQuest();
        _questRepository.Setup(r => r.GetByIdAsync(QuestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(new GetQuestPreviewQuery(QuestId), CancellationToken.None);

        result.QuestId.Should().Be(quest.Id);
        result.TrainingType.Should().Be("personalized_individual");
        result.CanChangeTrainingType.Should().BeTrue();
        result.EstimatedDurationMinutes.Should().Be(30);
        result.EstimatedXp.Should().Be(120); // 30 * 4
        result.Workout.Should().NotBeNull();
        result.Workout!.Exercises.Should().HaveCount(1);
        result.Workout.Exercises.First().Name.Should().Be("Squat");
    }

    [Fact]
    public async Task CA001_CanChangeTrainingType_IsFalse_WhenQuestIsInProgress()
    {
        var quest = BuildInProgressQuest();
        _questRepository.Setup(r => r.GetByIdAsync(QuestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(new GetQuestPreviewQuery(QuestId), CancellationToken.None);

        result.CanChangeTrainingType.Should().BeFalse();
    }

    [Fact]
    public async Task CA001_CanChangeTrainingType_IsFalse_WhenQuestIsCompleted()
    {
        var quest = BuildPendingPersonalizedQuest();
        quest.Start();
        quest.Complete(xpAwarded: 120);
        _questRepository.Setup(r => r.GetByIdAsync(QuestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(new GetQuestPreviewQuery(QuestId), CancellationToken.None);

        result.CanChangeTrainingType.Should().BeFalse();
    }

    [Fact]
    public async Task CA001_TrainingType_IsFallback_WhenQuestIsNotPersonalized()
    {
        var quest = BuildFallbackPendingQuest();
        _questRepository.Setup(r => r.GetByIdAsync(QuestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        var result = await CreateHandler().Handle(new GetQuestPreviewQuery(QuestId), CancellationToken.None);

        result.TrainingType.Should().Be("fallback");
    }

    [Fact]
    public async Task RN001_Throws_NotFoundException_WhenQuestDoesNotExist()
    {
        _questRepository.Setup(r => r.GetByIdAsync(QuestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Quest?)null);

        await CreateHandler()
            .Invoking(h => h.Handle(new GetQuestPreviewQuery(QuestId), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RN001_Throws_UnauthorizedException_WhenQuestBelongsToAnotherUser()
    {
        var otherUserId = Guid.NewGuid();
        var quest = Quest.Create(otherUserId, DateTime.UtcNow.Date, "pt-BR", "idem-key");
        quest.AssignWorkout(WorkoutJson, isPersonalized: true);
        _questRepository.Setup(r => r.GetByIdAsync(QuestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(quest);

        await CreateHandler()
            .Invoking(h => h.Handle(new GetQuestPreviewQuery(QuestId), CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>();
    }
}
```

- [ ] **Step 3: Rodar os testes para confirmar falha**

```bash
cd backend
dotnet test tests/Awaken.UnitTests/Awaken.UnitTests.csproj --filter "GetQuestPreviewQueryHandlerTests" -v minimal
```

Expected: falha com "type or namespace not found" (handler não existe ainda).

- [ ] **Step 4: Criar o handler**

```csharp
// backend/src/Awaken.Application/Quests/Queries/GetQuestPreview/GetQuestPreviewQueryHandler.cs
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Quests.Common;
using Awaken.Contracts.Quests;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Quests.Queries.GetQuestPreview;

public class GetQuestPreviewQueryHandler(
    IQuestRepository questRepository,
    ICurrentUserService currentUserService) : IRequestHandler<GetQuestPreviewQuery, QuestPreviewResponse>
{
    public async Task<QuestPreviewResponse> Handle(
        GetQuestPreviewQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var quest = await questRepository.GetByIdAsync(request.QuestId, cancellationToken)
            ?? throw new NotFoundException("Quest", request.QuestId);

        if (quest.UserId != userId)
            throw new UnauthorizedException("QUEST_NOT_OWNED", "Quest nao pertence ao usuario atual.");

        return QuestResponseMapper.ToPreviewResponse(quest);
    }
}
```

- [ ] **Step 5: Rodar os testes para confirmar aprovação**

```bash
cd backend
dotnet test tests/Awaken.UnitTests/Awaken.UnitTests.csproj --filter "GetQuestPreviewQueryHandlerTests" -v minimal
```

Expected: todos os 6 testes passando.

---

## Task 4: Backend — Controller endpoint

**Files:**
- Modify: `backend/src/Awaken.Api/Controllers/V1/QuestsController.cs`

- [ ] **Step 1: Adicionar using e endpoint**

Adicionar using no topo do arquivo:
```csharp
using Awaken.Application.Quests.Queries.GetQuestPreview;
```

Adicionar método ao `QuestsController`:
```csharp
[HttpGet("{questId:guid}/preview")]
public async Task<IActionResult> GetPreview(Guid questId, CancellationToken ct)
{
    var result = await mediator.Send(new GetQuestPreviewQuery(questId), ct);
    return Ok(result);
}
```

- [ ] **Step 2: Verificar build do projeto API**

```bash
cd backend/src
dotnet build Awaken.Api/Awaken.Api.csproj
```

Expected: sem erros.

---

## Task 5: Backend — Integration Tests

**Files:**
- Create: `backend/tests/Awaken.IntegrationTests/QuestPreviewEndpointTests.cs`

- [ ] **Step 1: Escrever os testes de integração**

```csharp
// backend/tests/Awaken.IntegrationTests/QuestPreviewEndpointTests.cs
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Quests;
using Awaken.Domain.Entities.Exercises;
using Awaken.Domain.Entities.Progression;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Awaken.IntegrationTests;

public class QuestPreviewEndpointTests : IAsyncLifetime
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

    private async Task<string> RegisterAndGetTokenAsync(string email)
    {
        var payload = new { email, password = "Str0ngPass!", name = "Hunter", language = "pt-BR" };
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
    }

    private async Task SeedExercisesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        if (!await db.Set<ExerciseCatalog>().AnyAsync())
        {
            db.Set<ExerciseCatalog>().Add(ExerciseCatalog.Create(
                "Squat", "Legs", "beginner", "barbell",
                muscleGroups: ["quadriceps"], equipment: ["barbell"]));
            await db.SaveChangesAsync();
        }
    }

    private async Task SeedProgressionAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();

        if (!await db.Set<HunterProgression>().AnyAsync(p => p.UserId == userId))
        {
            db.Set<HunterProgression>().Add(HunterProgression.Initialize(userId));
            await db.SaveChangesAsync();
        }
    }

    private async Task<Guid> GetUserIdAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        return (await db.Users.SingleAsync(u => u.Email == email)).Id;
    }

    private async Task<Guid> GenerateQuestAndGetIdAsync(HttpClient client)
    {
        var response = await client.PostAsync("/api/quests/daily/generate", null);
        response.EnsureSuccessStatusCode();
        var quest = await response.Content.ReadFromJsonAsync<QuestResponse>();
        return quest!.Id;
    }

    [Fact]
    public async Task CA001_Returns200_WithPreviewData_WhenQuestExists()
    {
        var token = await RegisterAndGetTokenAsync("preview_ok@test.com");
        var userId = await GetUserIdAsync("preview_ok@test.com");
        await SeedExercisesAsync();
        await SeedProgressionAsync(userId);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var questId = await GenerateQuestAndGetIdAsync(_client);

        var response = await _client.GetAsync($"/api/quests/{questId}/preview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var preview = await response.Content.ReadFromJsonAsync<QuestPreviewResponse>();
        preview.Should().NotBeNull();
        preview!.QuestId.Should().Be(questId);
        preview.TrainingType.Should().NotBeNullOrEmpty();
        preview.CanChangeTrainingType.Should().BeTrue();
        preview.EstimatedDurationMinutes.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RN001_Returns401_WhenUnauthenticated()
    {
        var response = await _client.GetAsync($"/api/quests/{Guid.NewGuid()}/preview");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RN001_Returns404_WhenQuestDoesNotExist()
    {
        var token = await RegisterAndGetTokenAsync("preview_notfound@test.com");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/quests/{Guid.NewGuid()}/preview");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RN001_Returns403_WhenUserTriesToViewAnotherUsersQuest()
    {
        // Register user A and generate a quest
        var tokenA = await RegisterAndGetTokenAsync("preview_userA@test.com");
        var userAId = await GetUserIdAsync("preview_userA@test.com");
        await SeedExercisesAsync();
        await SeedProgressionAsync(userAId);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenA);
        var questId = await GenerateQuestAndGetIdAsync(_client);

        // Register user B and try to view user A's quest
        var tokenB = await RegisterAndGetTokenAsync("preview_userB@test.com");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenB);

        var response = await _client.GetAsync($"/api/quests/{questId}/preview");

        // UnauthorizedException is mapped to 403 by ExceptionHandlingMiddleware
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
```

- [ ] **Step 2: Rodar os testes de integração**

```bash
cd backend
dotnet test tests/Awaken.IntegrationTests/Awaken.IntegrationTests.csproj --filter "QuestPreviewEndpointTests" -v minimal
```

Expected: 4 testes passando.

> Nota: `CA001_Returns403_WhenUserTriesToViewAnotherUsersQuest` depende de como o `ExceptionHandlingMiddleware` mapeia `UnauthorizedException`. Verifique o middleware — se mapeia para 401 (não 403), ajuste o assert.

---

## Task 6: Flutter — L10n keys (todas as 4 línguas)

**Files:**
- Modify: `apps/mobile/lib/l10n/app_pt.arb`
- Modify: `apps/mobile/lib/l10n/app_en.arb`
- Modify: `apps/mobile/lib/l10n/app_es.arb`
- Modify: `apps/mobile/lib/l10n/app_fr.arb`

- [ ] **Step 1: Adicionar chaves em `app_pt.arb`**

Inserir antes do `}` final:

```json
,

  "preQuestTitle": "Pré-treino",
  "@preQuestTitle": { "description": "Título da tela de pré-treino (US-050)" },
  "preQuestTrainingTypeLabel": "Tipo de treino",
  "@preQuestTrainingTypeLabel": { "description": "Rótulo do tipo de treino no pré-treino" },
  "preQuestTrainingTypePersonalized": "Personalizado Individual",
  "@preQuestTrainingTypePersonalized": { "description": "Tipo de treino: Personalizado Individual" },
  "preQuestTrainingTypeRegeneration": "Treino de Regeneração",
  "@preQuestTrainingTypeRegeneration": { "description": "Tipo de treino: Regeneração" },
  "preQuestTrainingTypeProgram": "Programa",
  "@preQuestTrainingTypeProgram": { "description": "Tipo de treino: Programa (Caminho de Saitama, Perfect 2, etc.)" },
  "preQuestTrainingTypeFallback": "Treino padrão",
  "@preQuestTrainingTypeFallback": { "description": "Tipo de treino: fallback genérico" },
  "preQuestEstimatedXpLabel": "{xp} XP estimado",
  "@preQuestEstimatedXpLabel": { "description": "XP estimado exibido no pré-treino", "placeholders": { "xp": { "type": "int" } } },
  "preQuestEstimatedDurationLabel": "{minutes} min",
  "@preQuestEstimatedDurationLabel": { "description": "Duração estimada do treino no pré-treino", "placeholders": { "minutes": { "type": "int" } } },
  "preQuestExercisesSection": "Exercícios",
  "@preQuestExercisesSection": { "description": "Título da seção de exercícios no pré-treino (somente leitura)" },
  "preQuestReadOnlyBadge": "Somente leitura",
  "@preQuestReadOnlyBadge": { "description": "Selo indicando que o pré-treino está em modo somente leitura" },
  "preQuestChangeTypeButton": "Alterar tipo de treino",
  "@preQuestChangeTypeButton": { "description": "Botão para abrir o seletor de tipo de treino (US-051)" },
  "preQuestStartButton": "Iniciar quest",
  "@preQuestStartButton": { "description": "Botão para confirmar e iniciar a quest" },
  "preQuestLoadErrorMessage": "Não foi possível carregar o pré-treino.",
  "@preQuestLoadErrorMessage": { "description": "Mensagem de erro ao falhar o carregamento do pré-treino" },
  "preQuestAccessExpiredMessage": "Seu acesso expirou. Renove sua assinatura para continuar.",
  "@preQuestAccessExpiredMessage": { "description": "Mensagem exibida quando o acesso expirou ao tentar abrir o pré-treino" },
  "preQuestAlreadyStartedMessage": "Esta quest já foi iniciada.",
  "@preQuestAlreadyStartedMessage": { "description": "Mensagem exibida quando a quest já foi iniciada e não permite alteração" },
  "preQuestRetryButton": "Tentar novamente",
  "@preQuestRetryButton": { "description": "Botão para retentar o carregamento do pré-treino após erro" },
  "preQuestGoToSubscriptionButton": "Ver planos",
  "@preQuestGoToSubscriptionButton": { "description": "Botão para ir à tela de assinatura quando o acesso expirou" }
```

- [ ] **Step 2: Adicionar chaves em `app_en.arb`**

Inserir antes do `}` final:

```json
,

  "preQuestTitle": "Pre-workout",
  "@preQuestTitle": { "description": "Pre-workout screen title (US-050)" },
  "preQuestTrainingTypeLabel": "Training type",
  "@preQuestTrainingTypeLabel": { "description": "Training type label in pre-workout" },
  "preQuestTrainingTypePersonalized": "Individual Personalized",
  "@preQuestTrainingTypePersonalized": { "description": "Training type: Individual Personalized" },
  "preQuestTrainingTypeRegeneration": "Recovery Workout",
  "@preQuestTrainingTypeRegeneration": { "description": "Training type: Recovery/Regeneration" },
  "preQuestTrainingTypeProgram": "Program",
  "@preQuestTrainingTypeProgram": { "description": "Training type: Program (Path of Saitama, Perfect 2, etc.)" },
  "preQuestTrainingTypeFallback": "Standard workout",
  "@preQuestTrainingTypeFallback": { "description": "Training type: generic fallback" },
  "preQuestEstimatedXpLabel": "{xp} estimated XP",
  "@preQuestEstimatedXpLabel": { "description": "Estimated XP shown in pre-workout", "placeholders": { "xp": { "type": "int" } } },
  "preQuestEstimatedDurationLabel": "{minutes} min",
  "@preQuestEstimatedDurationLabel": { "description": "Estimated workout duration in pre-workout", "placeholders": { "minutes": { "type": "int" } } },
  "preQuestExercisesSection": "Exercises",
  "@preQuestExercisesSection": { "description": "Exercises section title in pre-workout (read-only)" },
  "preQuestReadOnlyBadge": "Read only",
  "@preQuestReadOnlyBadge": { "description": "Badge indicating the pre-workout is in read-only mode" },
  "preQuestChangeTypeButton": "Change training type",
  "@preQuestChangeTypeButton": { "description": "Button to open the training type selector (US-051)" },
  "preQuestStartButton": "Start quest",
  "@preQuestStartButton": { "description": "Button to confirm and start the quest" },
  "preQuestLoadErrorMessage": "Couldn't load the pre-workout.",
  "@preQuestLoadErrorMessage": { "description": "Error message when pre-workout loading fails" },
  "preQuestAccessExpiredMessage": "Your access has expired. Renew your subscription to continue.",
  "@preQuestAccessExpiredMessage": { "description": "Message shown when access has expired when trying to open pre-workout" },
  "preQuestAlreadyStartedMessage": "This quest has already started.",
  "@preQuestAlreadyStartedMessage": { "description": "Message shown when the quest has already started and can no longer be changed" },
  "preQuestRetryButton": "Try again",
  "@preQuestRetryButton": { "description": "Button to retry loading the pre-workout after an error" },
  "preQuestGoToSubscriptionButton": "View plans",
  "@preQuestGoToSubscriptionButton": { "description": "Button to go to subscription screen when access has expired" }
```

- [ ] **Step 3: Adicionar chaves em `app_es.arb`**

Inserir antes do `}` final:

```json
,

  "preQuestTitle": "Pré-entreno",
  "@preQuestTitle": { "description": "Titulo de la pantalla de pré-entreno (US-050)" },
  "preQuestTrainingTypeLabel": "Tipo de entrenamiento",
  "@preQuestTrainingTypeLabel": { "description": "Etiqueta del tipo de entrenamiento en el pre-entreno" },
  "preQuestTrainingTypePersonalized": "Personalizado Individual",
  "@preQuestTrainingTypePersonalized": { "description": "Tipo de entrenamiento: Personalizado Individual" },
  "preQuestTrainingTypeRegeneration": "Entrenamiento de Recuperación",
  "@preQuestTrainingTypeRegeneration": { "description": "Tipo de entrenamiento: Recuperación/Regeneración" },
  "preQuestTrainingTypeProgram": "Programa",
  "@preQuestTrainingTypeProgram": { "description": "Tipo de entrenamiento: Programa" },
  "preQuestTrainingTypeFallback": "Entrenamiento estándar",
  "@preQuestTrainingTypeFallback": { "description": "Tipo de entrenamiento: plantilla generica" },
  "preQuestEstimatedXpLabel": "{xp} XP estimado",
  "@preQuestEstimatedXpLabel": { "description": "XP estimado mostrado en el pre-entreno", "placeholders": { "xp": { "type": "int" } } },
  "preQuestEstimatedDurationLabel": "{minutes} min",
  "@preQuestEstimatedDurationLabel": { "description": "Duracion estimada del entrenamiento en el pre-entreno", "placeholders": { "minutes": { "type": "int" } } },
  "preQuestExercisesSection": "Ejercicios",
  "@preQuestExercisesSection": { "description": "Titulo de la seccion de ejercicios en el pre-entreno (solo lectura)" },
  "preQuestReadOnlyBadge": "Solo lectura",
  "@preQuestReadOnlyBadge": { "description": "Insignia que indica que el pre-entreno esta en modo solo lectura" },
  "preQuestChangeTypeButton": "Cambiar tipo de entrenamiento",
  "@preQuestChangeTypeButton": { "description": "Boton para abrir el selector de tipo de entrenamiento (US-051)" },
  "preQuestStartButton": "Iniciar misión",
  "@preQuestStartButton": { "description": "Boton para confirmar e iniciar la quest" },
  "preQuestLoadErrorMessage": "No se pudo cargar el pre-entreno.",
  "@preQuestLoadErrorMessage": { "description": "Mensaje de error cuando falla la carga del pre-entreno" },
  "preQuestAccessExpiredMessage": "Tu acceso ha expirado. Renueva tu suscripción para continuar.",
  "@preQuestAccessExpiredMessage": { "description": "Mensaje mostrado cuando el acceso expiro al intentar abrir el pre-entreno" },
  "preQuestAlreadyStartedMessage": "Esta misión ya ha comenzado.",
  "@preQuestAlreadyStartedMessage": { "description": "Mensaje mostrado cuando la quest ya fue iniciada y no permite cambios" },
  "preQuestRetryButton": "Intentar de nuevo",
  "@preQuestRetryButton": { "description": "Boton para reintentar la carga del pre-entreno despues de un error" },
  "preQuestGoToSubscriptionButton": "Ver planes",
  "@preQuestGoToSubscriptionButton": { "description": "Boton para ir a la pantalla de suscripcion cuando el acceso expiro" }
```

- [ ] **Step 4: Adicionar chaves em `app_fr.arb`**

Inserir antes do `}` final:

```json
,

  "preQuestTitle": "Pré-entraînement",
  "@preQuestTitle": { "description": "Titre de l'ecran pre-entrainement (US-050)" },
  "preQuestTrainingTypeLabel": "Type d'entraînement",
  "@preQuestTrainingTypeLabel": { "description": "Etiquette du type d'entrainement dans le pre-entrainement" },
  "preQuestTrainingTypePersonalized": "Personnalisé Individuel",
  "@preQuestTrainingTypePersonalized": { "description": "Type d'entrainement: Personnalise Individuel" },
  "preQuestTrainingTypeRegeneration": "Entraînement de Récupération",
  "@preQuestTrainingTypeRegeneration": { "description": "Type d'entrainement: Recuperation/Regeneration" },
  "preQuestTrainingTypeProgram": "Programme",
  "@preQuestTrainingTypeProgram": { "description": "Type d'entrainement: Programme" },
  "preQuestTrainingTypeFallback": "Entraînement standard",
  "@preQuestTrainingTypeFallback": { "description": "Type d'entrainement: modele generique" },
  "preQuestEstimatedXpLabel": "{xp} XP estimés",
  "@preQuestEstimatedXpLabel": { "description": "XP estimes affiches dans le pre-entrainement", "placeholders": { "xp": { "type": "int" } } },
  "preQuestEstimatedDurationLabel": "{minutes} min",
  "@preQuestEstimatedDurationLabel": { "description": "Duree estimee de l'entrainement dans le pre-entrainement", "placeholders": { "minutes": { "type": "int" } } },
  "preQuestExercisesSection": "Exercices",
  "@preQuestExercisesSection": { "description": "Titre de la section exercices dans le pre-entrainement (lecture seule)" },
  "preQuestReadOnlyBadge": "Lecture seule",
  "@preQuestReadOnlyBadge": { "description": "Badge indiquant que le pre-entrainement est en mode lecture seule" },
  "preQuestChangeTypeButton": "Changer le type d'entraînement",
  "@preQuestChangeTypeButton": { "description": "Bouton pour ouvrir le selecteur de type d'entrainement (US-051)" },
  "preQuestStartButton": "Démarrer la quête",
  "@preQuestStartButton": { "description": "Bouton pour confirmer et demarrer la quete" },
  "preQuestLoadErrorMessage": "Impossible de charger le pré-entraînement.",
  "@preQuestLoadErrorMessage": { "description": "Message d'erreur quand le chargement du pre-entrainement echoue" },
  "preQuestAccessExpiredMessage": "Votre accès a expiré. Renouvelez votre abonnement pour continuer.",
  "@preQuestAccessExpiredMessage": { "description": "Message affiche quand l'acces a expire en essayant d'ouvrir le pre-entrainement" },
  "preQuestAlreadyStartedMessage": "Cette quête a déjà commencé.",
  "@preQuestAlreadyStartedMessage": { "description": "Message affiche quand la quete a deja commence et ne peut plus etre modifiee" },
  "preQuestRetryButton": "Réessayer",
  "@preQuestRetryButton": { "description": "Bouton pour relancer le chargement du pre-entrainement apres une erreur" },
  "preQuestGoToSubscriptionButton": "Voir les forfaits",
  "@preQuestGoToSubscriptionButton": { "description": "Bouton pour aller a l'ecran d'abonnement quand l'acces a expire" }
```

- [ ] **Step 5: Gerar localização**

```bash
cd apps/mobile
flutter gen-l10n
```

Expected: geração sem erros; arquivos `app_localizations_*.dart` atualizados.

---

## Task 7: Flutter — QuestPreview entity + QuestPreviewResponseDto

**Files:**
- Create: `apps/mobile/lib/features/quests/domain/entities/quest_preview.dart`
- Create: `apps/mobile/lib/features/quests/data/dtos/quest_preview_response_dto.dart`

- [ ] **Step 1: Criar a entidade de domínio**

```dart
// apps/mobile/lib/features/quests/domain/entities/quest_preview.dart

enum TrainingType {
  personalizedIndividual,
  regeneration,
  program,
  fallback;

  static TrainingType fromApi(String value) => switch (value) {
        'personalized_individual' => personalizedIndividual,
        'regeneration' => regeneration,
        'program' => program,
        _ => fallback,
      };
}

class QuestPreviewExercise {
  const QuestPreviewExercise({
    required this.name,
    required this.description,
    required this.sets,
    required this.repsMin,
    this.repsMax,
    this.restSeconds,
    this.videoUrl,
    this.targetRpe,
  });

  final String name;
  final String description;
  final int sets;
  final int repsMin;
  final int? repsMax;
  final int? restSeconds;
  final String? videoUrl;
  final String? targetRpe;

  String get repsDisplay =>
      repsMax != null ? '$repsMin–$repsMax' : '$repsMin';
}

class QuestPreview {
  const QuestPreview({
    required this.questId,
    required this.trainingType,
    required this.estimatedXp,
    required this.estimatedDurationMinutes,
    required this.canChangeTrainingType,
    required this.workoutTitle,
    required this.workoutDescription,
    required this.exercises,
  });

  final String questId;
  final TrainingType trainingType;
  final int estimatedXp;
  final int estimatedDurationMinutes;
  final bool canChangeTrainingType;
  final String workoutTitle;
  final String workoutDescription;
  final List<QuestPreviewExercise> exercises;
}
```

- [ ] **Step 2: Criar o DTO**

```dart
// apps/mobile/lib/features/quests/data/dtos/quest_preview_response_dto.dart
import 'quest_response_dto.dart';

class QuestPreviewResponseDto {
  const QuestPreviewResponseDto({
    required this.questId,
    required this.trainingType,
    required this.estimatedXp,
    required this.estimatedDurationMinutes,
    required this.canChangeTrainingType,
    this.workout,
  });

  final String questId;
  final String trainingType;
  final int estimatedXp;
  final int estimatedDurationMinutes;
  final bool canChangeTrainingType;
  final WorkoutDto? workout;

  factory QuestPreviewResponseDto.fromJson(Map<String, dynamic> json) {
    return QuestPreviewResponseDto(
      questId: json['questId'] as String,
      trainingType: json['trainingType'] as String? ?? 'personalized_individual',
      estimatedXp: json['estimatedXp'] as int? ?? 0,
      estimatedDurationMinutes: json['estimatedDurationMinutes'] as int? ?? 0,
      canChangeTrainingType: json['canChangeTrainingType'] as bool? ?? false,
      workout: json['workout'] != null
          ? WorkoutDto.fromJson(json['workout'] as Map<String, dynamic>)
          : null,
    );
  }
}
```

---

## Task 8: Flutter — Data source, Repository interface e implementation + unit tests

**Files:**
- Modify: `apps/mobile/lib/features/quests/data/datasources/quests_remote_data_source.dart`
- Modify: `apps/mobile/lib/features/quests/domain/repositories/quests_repository.dart`
- Modify: `apps/mobile/lib/features/quests/data/repositories/quests_repository_impl.dart`
- Create: `apps/mobile/test/features/quests/data/datasources/quest_preview_data_source_test.dart`

- [ ] **Step 1: Adicionar `getQuestPreview` ao data source**

No arquivo `quests_remote_data_source.dart`, adicionar import e método:

```dart
import '../dtos/quest_preview_response_dto.dart';
```

Adicionar método antes de `_mapError`:

```dart
Future<QuestPreviewResponseDto> getQuestPreview(String questId) async {
  try {
    final response = await _dio.get('/api/quests/$questId/preview');
    return QuestPreviewResponseDto.fromJson(response.data as Map<String, dynamic>);
  } on DioException catch (e) {
    throw _mapError(e);
  }
}
```

- [ ] **Step 2: Adicionar `getQuestPreview` à interface do repositório**

No arquivo `quests_repository.dart`, adicionar import e método:

```dart
import '../entities/quest_preview.dart';
```

```dart
abstract interface class QuestsRepository {
  Future<DailyQuest> generateDailyQuest();
  Future<DailyQuest?> getTodayQuest();
  Future<DailyQuest> confirmDailyQuest(String questId);
  Future<DailyQuest> regenerateDailyQuest({bool useReforgeScroll = false});

  /// US-050: busca a prévia da quest para exibição em modo somente leitura
  /// antes de iniciar a quest.
  Future<QuestPreview> getQuestPreview(String questId);
}
```

- [ ] **Step 3: Implementar `getQuestPreview` no repositório**

No arquivo `quests_repository_impl.dart`, adicionar import:

```dart
import '../../domain/entities/quest_preview.dart';
import '../dtos/quest_preview_response_dto.dart';
```

Adicionar implementação:

```dart
@override
Future<QuestPreview> getQuestPreview(String questId) async {
  final dto = await _dataSource.getQuestPreview(questId);
  return _toPreviewEntity(dto);
}

QuestPreview _toPreviewEntity(QuestPreviewResponseDto dto) {
  final workout = dto.workout;
  return QuestPreview(
    questId: dto.questId,
    trainingType: TrainingType.fromApi(dto.trainingType),
    estimatedXp: dto.estimatedXp,
    estimatedDurationMinutes: dto.estimatedDurationMinutes,
    canChangeTrainingType: dto.canChangeTrainingType,
    workoutTitle: workout?.title ?? '',
    workoutDescription: workout?.description ?? '',
    exercises: (workout?.exercises ?? [])
        .map((e) => QuestPreviewExercise(
              name: e.name,
              description: e.description,
              sets: e.sets,
              repsMin: e.repsMin,
              repsMax: e.repsMax,
              restSeconds: e.restSeconds,
              videoUrl: e.videoUrl,
              targetRpe: e.targetRpe,
            ))
        .toList(),
  );
}
```

- [ ] **Step 4: Escrever testes do data source**

```dart
// apps/mobile/test/features/quests/data/datasources/quest_preview_data_source_test.dart
import 'dart:convert';
import 'dart:typed_data';

import 'package:awaken/core/errors/app_error.dart';
import 'package:awaken/features/quests/data/datasources/quests_remote_data_source.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

class _FakeAdapter implements HttpClientAdapter {
  _FakeAdapter({required this.statusCode, required this.body});
  final int statusCode;
  final Map<String, dynamic> body;

  @override
  Future<ResponseBody> fetch(RequestOptions options, Stream<Uint8List>? req, Future<void>? cancel) async {
    return ResponseBody.fromString(
      jsonEncode(body),
      statusCode,
      headers: {Headers.contentTypeHeader: [Headers.jsonContentType]},
    );
  }

  @override
  void close({bool force = false}) {}
}

class _ErrorAdapter implements HttpClientAdapter {
  _ErrorAdapter({required this.type, this.statusCode});
  final DioExceptionType type;
  final int? statusCode;

  @override
  Future<ResponseBody> fetch(RequestOptions options, Stream<Uint8List>? req, Future<void>? cancel) async {
    throw DioException(
      requestOptions: options,
      response: statusCode == null
          ? null
          : Response(requestOptions: options, statusCode: statusCode, data: const {}),
      type: type,
    );
  }

  @override
  void close({bool force = false}) {}
}

class _CapturingAdapter implements HttpClientAdapter {
  _CapturingAdapter({required this.next, required this.onCapture});
  final HttpClientAdapter next;
  final void Function(RequestOptions) onCapture;

  @override
  Future<ResponseBody> fetch(RequestOptions options, Stream<Uint8List>? req, Future<void>? cancel) {
    onCapture(options);
    return next.fetch(options, req, cancel);
  }

  @override
  void close({bool force = false}) {}
}

Dio _buildDio(HttpClientAdapter adapter) {
  final dio = Dio(BaseOptions(baseUrl: 'https://api.test'));
  dio.httpClientAdapter = adapter;
  return dio;
}

Map<String, dynamic> _previewBody({bool canChangeTrainingType = true}) => {
      'questId': 'qst_001',
      'trainingType': 'personalized_individual',
      'estimatedXp': 120,
      'estimatedDurationMinutes': 30,
      'canChangeTrainingType': canChangeTrainingType,
      'workout': {
        'title': 'Daily Quest',
        'description': 'Full body',
        'durationMinutes': 30,
        'exercises': [
          {
            'name': 'Squat',
            'description': 'Leg press',
            'sets': 3,
            'repsMin': 10,
            'repsMax': 15,
            'restSeconds': 90,
            'targetRpe': '6-8',
          },
        ],
      },
    };

void main() {
  group('QuestsRemoteDataSource.getQuestPreview', () {
    test('sends GET to preview endpoint with questId', () async {
      RequestOptions? captured;
      final adapter = _CapturingAdapter(
        next: _FakeAdapter(statusCode: 200, body: _previewBody()),
        onCapture: (options) => captured = options,
      );
      final dataSource = QuestsRemoteDataSource(_buildDio(adapter));

      final result = await dataSource.getQuestPreview('qst_001');

      expect(captured?.method, 'GET');
      expect(captured?.path, '/api/quests/qst_001/preview');
      expect(result.questId, 'qst_001');
      expect(result.trainingType, 'personalized_individual');
      expect(result.estimatedXp, 120);
      expect(result.estimatedDurationMinutes, 30);
      expect(result.canChangeTrainingType, true);
      expect(result.workout!.exercises.single.name, 'Squat');
    });

    test('returns preview with canChangeTrainingType=false when quest is in_progress', () async {
      final dataSource = QuestsRemoteDataSource(
        _buildDio(_FakeAdapter(statusCode: 200, body: _previewBody(canChangeTrainingType: false))),
      );

      final result = await dataSource.getQuestPreview('qst_001');

      expect(result.canChangeTrainingType, false);
    });

    test('throws AccessBlockedError on 403', () async {
      final dataSource = QuestsRemoteDataSource(
        _buildDio(_ErrorAdapter(type: DioExceptionType.badResponse, statusCode: 403)),
      );

      expect(dataSource.getQuestPreview('qst_001'), throwsA(isA<AccessBlockedError>()));
    });

    test('throws NotFoundError on 404', () async {
      final dataSource = QuestsRemoteDataSource(
        _buildDio(_ErrorAdapter(type: DioExceptionType.badResponse, statusCode: 404)),
      );

      expect(dataSource.getQuestPreview('qst_001'), throwsA(isA<NotFoundError>()));
    });

    test('throws NetworkError on connection failure', () async {
      final dataSource = QuestsRemoteDataSource(
        _buildDio(_ErrorAdapter(type: DioExceptionType.connectionError)),
      );

      expect(dataSource.getQuestPreview('qst_001'), throwsA(isA<NetworkError>()));
    });
  });
}
```

- [ ] **Step 5: Rodar os testes do data source**

```bash
cd apps/mobile
flutter test test/features/quests/data/datasources/quest_preview_data_source_test.dart -v
```

Expected: todos os 5 testes passando.

- [ ] **Step 6: Verificar que os testes existentes continuam passando**

```bash
cd apps/mobile
flutter test test/features/quests/ -v
```

Expected: todos os testes passando.

---

## Task 9: Flutter — PreQuestState + PreQuestController + unit tests

**Files:**
- Create: `apps/mobile/lib/features/quests/presentation/providers/pre_quest_state.dart`
- Create: `apps/mobile/lib/features/quests/presentation/providers/pre_quest_controller.dart`
- Create: `apps/mobile/test/features/quests/presentation/providers/pre_quest_controller_test.dart`

- [ ] **Step 1: Criar os estados sealed**

```dart
// apps/mobile/lib/features/quests/presentation/providers/pre_quest_state.dart
import '../../domain/entities/quest_preview.dart';

sealed class PreQuestState {
  const PreQuestState();
}

final class PreQuestLoading extends PreQuestState {
  const PreQuestLoading();
}

final class PreQuestLoaded extends PreQuestState {
  const PreQuestLoaded(this.preview);
  final QuestPreview preview;
}

final class PreQuestAccessBlocked extends PreQuestState {
  const PreQuestAccessBlocked();
}

final class PreQuestAlreadyStarted extends PreQuestState {
  const PreQuestAlreadyStarted();
}

final class PreQuestNetworkError extends PreQuestState {
  const PreQuestNetworkError();
}

final class PreQuestUnexpectedError extends PreQuestState {
  const PreQuestUnexpectedError();
}
```

- [ ] **Step 2: Escrever os testes do controller (failing)**

```dart
// apps/mobile/test/features/quests/presentation/providers/pre_quest_controller_test.dart
import 'package:awaken/core/analytics/analytics_service.dart';
import 'package:awaken/core/analytics/analytics_provider.dart';
import 'package:awaken/core/errors/app_error.dart';
import 'package:awaken/features/quests/domain/entities/quest_preview.dart';
import 'package:awaken/features/quests/domain/repositories/quests_repository.dart';
import 'package:awaken/features/quests/presentation/providers/pre_quest_controller.dart';
import 'package:awaken/features/quests/presentation/providers/pre_quest_state.dart';
import 'package:awaken/features/quests/presentation/providers/quests_providers.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

class _MockQuestsRepository extends Mock implements QuestsRepository {}
class _MockAnalyticsService extends Mock implements AnalyticsService {}

const _questId = 'qst_001';

QuestPreview _buildPreview({bool canChangeTrainingType = true}) => QuestPreview(
      questId: _questId,
      trainingType: TrainingType.personalizedIndividual,
      estimatedXp: 120,
      estimatedDurationMinutes: 30,
      canChangeTrainingType: canChangeTrainingType,
      workoutTitle: 'Daily Quest',
      workoutDescription: 'Full body',
      exercises: const [],
    );

void main() {
  late _MockQuestsRepository mockRepository;
  late _MockAnalyticsService mockAnalytics;

  setUp(() {
    mockRepository = _MockQuestsRepository();
    mockAnalytics = _MockAnalyticsService();
    when(() => mockAnalytics.logEvent(any())).thenAnswer((_) async {});
  });

  ProviderContainer buildContainer() => ProviderContainer(
        overrides: [
          questsRepositoryProvider.overrideWithValue(mockRepository),
          analyticsServiceProvider.overrideWithValue(mockAnalytics),
        ],
      );

  group('PreQuestController', () {
    test('CA001 — starts as Loading then transitions to Loaded on success', () async {
      when(() => mockRepository.getQuestPreview(_questId))
          .thenAnswer((_) async => _buildPreview());

      final container = buildContainer();
      addTearDown(container.dispose);

      // Initially loading
      expect(
        container.read(preQuestControllerProvider(_questId)),
        isA<PreQuestLoading>(),
      );

      // Wait for the microtask to complete
      await Future<void>.delayed(Duration.zero);

      expect(
        container.read(preQuestControllerProvider(_questId)),
        isA<PreQuestLoaded>(),
      );
      verify(() => mockAnalytics.logEvent('quest_viewed')).called(1);
    });

    test('CA001 — fires access_blocked event and shows PreQuestAccessBlocked on 403', () async {
      when(() => mockRepository.getQuestPreview(_questId))
          .thenThrow(const AccessBlockedError());

      final container = buildContainer();
      addTearDown(container.dispose);

      await Future<void>.delayed(Duration.zero);

      expect(
        container.read(preQuestControllerProvider(_questId)),
        isA<PreQuestAccessBlocked>(),
      );
      verify(() => mockAnalytics.logEvent('access_blocked')).called(1);
    });

    test('CA001 — shows PreQuestAlreadyStarted when canChangeTrainingType is false', () async {
      when(() => mockRepository.getQuestPreview(_questId))
          .thenAnswer((_) async => _buildPreview(canChangeTrainingType: false));

      final container = buildContainer();
      addTearDown(container.dispose);

      await Future<void>.delayed(Duration.zero);

      expect(
        container.read(preQuestControllerProvider(_questId)),
        isA<PreQuestAlreadyStarted>(),
      );
    });

    test('shows PreQuestNetworkError on NetworkError', () async {
      when(() => mockRepository.getQuestPreview(_questId))
          .thenThrow(const NetworkError());

      final container = buildContainer();
      addTearDown(container.dispose);

      await Future<void>.delayed(Duration.zero);

      expect(
        container.read(preQuestControllerProvider(_questId)),
        isA<PreQuestNetworkError>(),
      );
    });

    test('shows PreQuestUnexpectedError on unknown error', () async {
      when(() => mockRepository.getQuestPreview(_questId))
          .thenThrow(Exception('unknown'));

      final container = buildContainer();
      addTearDown(container.dispose);

      await Future<void>.delayed(Duration.zero);

      expect(
        container.read(preQuestControllerProvider(_questId)),
        isA<PreQuestUnexpectedError>(),
      );
    });

    test('retry reloads the preview', () async {
      when(() => mockRepository.getQuestPreview(_questId))
          .thenThrow(const NetworkError());

      final container = buildContainer();
      addTearDown(container.dispose);

      await Future<void>.delayed(Duration.zero);

      expect(
        container.read(preQuestControllerProvider(_questId)),
        isA<PreQuestNetworkError>(),
      );

      when(() => mockRepository.getQuestPreview(_questId))
          .thenAnswer((_) async => _buildPreview());

      await container
          .read(preQuestControllerProvider(_questId).notifier)
          .retry(_questId);

      expect(
        container.read(preQuestControllerProvider(_questId)),
        isA<PreQuestLoaded>(),
      );
    });
  });
}
```

- [ ] **Step 3: Rodar os testes para confirmar falha**

```bash
cd apps/mobile
flutter test test/features/quests/presentation/providers/pre_quest_controller_test.dart -v
```

Expected: falha ("preQuestControllerProvider" não existe).

- [ ] **Step 4: Criar o controller**

```dart
// apps/mobile/lib/features/quests/presentation/providers/pre_quest_controller.dart
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/analytics/analytics_provider.dart';
import '../../../../core/errors/app_error.dart';
import 'pre_quest_state.dart';
import 'quests_providers.dart';

class PreQuestController extends FamilyNotifier<PreQuestState, String> {
  @override
  PreQuestState build(String questId) {
    Future.microtask(() => _load(questId));
    return const PreQuestLoading();
  }

  Future<void> _load(String questId) async {
    state = const PreQuestLoading();
    final analytics = ref.read(analyticsServiceProvider);
    final repository = ref.read(questsRepositoryProvider);

    try {
      final preview = await repository.getQuestPreview(questId);

      if (!preview.canChangeTrainingType) {
        state = const PreQuestAlreadyStarted();
        return;
      }

      state = PreQuestLoaded(preview);
      await analytics.logEvent('quest_viewed');
    } on AccessBlockedError {
      state = const PreQuestAccessBlocked();
      await analytics.logEvent('access_blocked');
    } on NetworkError {
      state = const PreQuestNetworkError();
    } catch (_) {
      state = const PreQuestUnexpectedError();
    }
  }

  Future<void> retry(String questId) => _load(questId);
}

final preQuestControllerProvider =
    NotifierProviderFamily<PreQuestController, PreQuestState, String>(
        PreQuestController.new);
```

- [ ] **Step 5: Rodar os testes para confirmar aprovação**

```bash
cd apps/mobile
flutter test test/features/quests/presentation/providers/pre_quest_controller_test.dart -v
```

Expected: todos os 6 testes passando.

---

## Task 10: Flutter — Rota + PreQuestExerciseCard widget

**Files:**
- Modify: `apps/mobile/lib/app/app_router.dart`
- Create: `apps/mobile/lib/features/quests/presentation/widgets/pre_quest_exercise_card.dart`

- [ ] **Step 1: Adicionar a rota ao `app_router.dart`**

Adicionar constante no bloco `AppRoutes`:

```dart
static const preQuest = '/pre-quest/:questId';
```

Adicionar import:

```dart
import '../features/quests/presentation/pages/pre_quest_page.dart';
```

Adicionar rota na lista `routes` (após a rota `dailyQuest`):

```dart
GoRoute(
  path: AppRoutes.preQuest,
  pageBuilder: (ctx, state) => _buildPage(
    state: state,
    child: PreQuestPage(questId: state.pathParameters['questId']!),
  ),
),
```

- [ ] **Step 2: Criar o card de exercício somente leitura**

```dart
// apps/mobile/lib/features/quests/presentation/widgets/pre_quest_exercise_card.dart
import 'package:flutter/material.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import '../../domain/entities/quest_preview.dart';

class PreQuestExerciseCard extends StatelessWidget {
  const PreQuestExerciseCard({super.key, required this.exercise, required this.index});

  final QuestPreviewExercise exercise;
  final int index;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final textTheme = Theme.of(context).textTheme;
    final colorScheme = Theme.of(context).colorScheme;

    final repsText = exercise.repsMax != null
        ? l10n.dailyQuestExerciseRepsRange(exercise.repsMin, exercise.repsMax!)
        : l10n.dailyQuestExerciseRepsFixed(exercise.repsMin);

    return Card(
      margin: const EdgeInsets.symmetric(vertical: 6),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            CircleAvatar(
              radius: 18,
              backgroundColor: colorScheme.primaryContainer,
              child: Text(
                '${index + 1}',
                style: textTheme.labelLarge?.copyWith(
                  color: colorScheme.onPrimaryContainer,
                ),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(exercise.name, style: textTheme.titleSmall),
                  if (exercise.description.isNotEmpty) ...[
                    const SizedBox(height: 2),
                    Text(
                      exercise.description,
                      style: textTheme.bodySmall?.copyWith(
                        color: colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ],
                  const SizedBox(height: 6),
                  Wrap(
                    spacing: 8,
                    children: [
                      _MetaChip(
                        icon: Icons.repeat,
                        label: '${exercise.sets}x $repsText',
                      ),
                      if (exercise.restSeconds != null)
                        _MetaChip(
                          icon: Icons.timer_outlined,
                          label: '${exercise.restSeconds}s',
                        ),
                      if (exercise.targetRpe != null)
                        _MetaChip(
                          icon: Icons.speed_outlined,
                          label: 'RPE ${exercise.targetRpe}',
                        ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _MetaChip extends StatelessWidget {
  const _MetaChip({required this.icon, required this.label});

  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 14, color: colorScheme.onSurfaceVariant),
        const SizedBox(width: 3),
        Text(
          label,
          style: Theme.of(context).textTheme.bodySmall?.copyWith(
                color: colorScheme.onSurfaceVariant,
              ),
        ),
      ],
    );
  }
}
```

- [ ] **Step 3: Verificar análise**

```bash
cd apps/mobile
flutter analyze lib/app/app_router.dart lib/features/quests/presentation/widgets/pre_quest_exercise_card.dart
```

Expected: sem erros (PreQuestPage ainda não existe; ignorar temporariamente se necessário criando um stub).

---

## Task 11: Flutter — PreQuestPage (tela completa)

**Files:**
- Create: `apps/mobile/lib/features/quests/presentation/pages/pre_quest_page.dart`

- [ ] **Step 1: Criar a página de pré-treino**

```dart
// apps/mobile/lib/features/quests/presentation/pages/pre_quest_page.dart
import 'package:flutter/material.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../domain/entities/quest_preview.dart';
import '../providers/pre_quest_controller.dart';
import '../providers/pre_quest_state.dart';
import '../widgets/pre_quest_exercise_card.dart';
import '../../../../app/app_router.dart';

class PreQuestPage extends ConsumerWidget {
  const PreQuestPage({super.key, required this.questId});

  final String questId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(preQuestControllerProvider(questId));

    return Scaffold(
      appBar: AppBar(
        title: Text(AppLocalizations.of(context)!.preQuestTitle),
      ),
      body: switch (state) {
        PreQuestLoading() => const Center(child: CircularProgressIndicator()),
        PreQuestLoaded(:final preview) => _PreQuestContent(
            questId: questId,
            preview: preview,
          ),
        PreQuestAccessBlocked() => _PreQuestAccessBlocked(questId: questId),
        PreQuestAlreadyStarted() => _PreQuestAlreadyStarted(questId: questId),
        PreQuestNetworkError() => _PreQuestError(
            questId: questId,
            ref: ref,
          ),
        PreQuestUnexpectedError() => _PreQuestError(
            questId: questId,
            ref: ref,
          ),
      },
    );
  }
}

class _PreQuestContent extends ConsumerWidget {
  const _PreQuestContent({required this.questId, required this.preview});

  final String questId;
  final QuestPreview preview;

  String _trainingTypeLabel(AppLocalizations l10n, TrainingType type) =>
      switch (type) {
        TrainingType.personalizedIndividual => l10n.preQuestTrainingTypePersonalized,
        TrainingType.regeneration => l10n.preQuestTrainingTypeRegeneration,
        TrainingType.program => l10n.preQuestTrainingTypeProgram,
        TrainingType.fallback => l10n.preQuestTrainingTypeFallback,
      };

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context)!;
    final textTheme = Theme.of(context).textTheme;
    final colorScheme = Theme.of(context).colorScheme;

    return Column(
      children: [
        Expanded(
          child: ListView(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            children: [
              // Training type + read-only badge
              Row(
                children: [
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        l10n.preQuestTrainingTypeLabel,
                        style: textTheme.labelSmall?.copyWith(
                          color: colorScheme.onSurfaceVariant,
                        ),
                      ),
                      Text(
                        _trainingTypeLabel(l10n, preview.trainingType),
                        style: textTheme.titleMedium,
                      ),
                    ],
                  ),
                  const Spacer(),
                  Chip(
                    label: Text(l10n.preQuestReadOnlyBadge),
                    labelStyle: textTheme.labelSmall,
                    padding: EdgeInsets.zero,
                  ),
                ],
              ),
              const SizedBox(height: 12),

              // XP + duration
              Row(
                children: [
                  _StatChip(
                    icon: Icons.star_outline,
                    label: l10n.preQuestEstimatedXpLabel(preview.estimatedXp),
                  ),
                  const SizedBox(width: 12),
                  _StatChip(
                    icon: Icons.timer_outlined,
                    label: l10n.preQuestEstimatedDurationLabel(
                        preview.estimatedDurationMinutes),
                  ),
                ],
              ),
              const SizedBox(height: 20),

              // Exercises section
              Text(
                l10n.preQuestExercisesSection,
                style: textTheme.titleSmall?.copyWith(
                  color: colorScheme.onSurfaceVariant,
                ),
              ),
              const SizedBox(height: 8),

              ...preview.exercises.asMap().entries.map(
                    (entry) => PreQuestExerciseCard(
                      exercise: entry.value,
                      index: entry.key,
                    ),
                  ),
            ],
          ),
        ),

        // Bottom CTAs
        SafeArea(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                if (preview.canChangeTrainingType)
                  OutlinedButton(
                    onPressed: () {
                      // US-051 will implement this action
                    },
                    style: OutlinedButton.styleFrom(
                      minimumSize: const Size.fromHeight(48),
                    ),
                    child: Text(l10n.preQuestChangeTypeButton),
                  ),
                const SizedBox(height: 8),
                FilledButton(
                  onPressed: () => context.pop(),
                  style: FilledButton.styleFrom(
                    minimumSize: const Size.fromHeight(48),
                  ),
                  child: Text(l10n.preQuestStartButton),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}

class _StatChip extends StatelessWidget {
  const _StatChip({required this.icon, required this.label});

  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(
        color: colorScheme.surfaceContainerHighest,
        borderRadius: BorderRadius.circular(20),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 16, color: colorScheme.primary),
          const SizedBox(width: 6),
          Text(label, style: Theme.of(context).textTheme.labelMedium),
        ],
      ),
    );
  }
}

class _PreQuestAccessBlocked extends StatelessWidget {
  const _PreQuestAccessBlocked({required this.questId});

  final String questId;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.lock_outline, size: 56,
                color: Theme.of(context).colorScheme.error),
            const SizedBox(height: 16),
            Text(
              l10n.preQuestAccessExpiredMessage,
              textAlign: TextAlign.center,
              style: Theme.of(context).textTheme.bodyLarge,
            ),
            const SizedBox(height: 24),
            FilledButton(
              onPressed: () => context.push(AppRoutes.subscription),
              child: Text(l10n.preQuestGoToSubscriptionButton),
            ),
          ],
        ),
      ),
    );
  }
}

class _PreQuestAlreadyStarted extends StatelessWidget {
  const _PreQuestAlreadyStarted({required this.questId});

  final String questId;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.play_circle_outline, size: 56,
                color: Theme.of(context).colorScheme.primary),
            const SizedBox(height: 16),
            Text(
              l10n.preQuestAlreadyStartedMessage,
              textAlign: TextAlign.center,
              style: Theme.of(context).textTheme.bodyLarge,
            ),
            const SizedBox(height: 24),
            OutlinedButton(
              onPressed: () => context.pop(),
              child: Text(l10n.dailyQuestBackTooltip),
            ),
          ],
        ),
      ),
    );
  }
}

class _PreQuestError extends StatelessWidget {
  const _PreQuestError({required this.questId, required this.ref});

  final String questId;
  final WidgetRef ref;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.error_outline, size: 56,
                color: Theme.of(context).colorScheme.error),
            const SizedBox(height: 16),
            Text(
              l10n.preQuestLoadErrorMessage,
              textAlign: TextAlign.center,
              style: Theme.of(context).textTheme.bodyLarge,
            ),
            const SizedBox(height: 24),
            FilledButton(
              onPressed: () => ref
                  .read(preQuestControllerProvider(questId).notifier)
                  .retry(questId),
              child: Text(l10n.preQuestRetryButton),
            ),
          ],
        ),
      ),
    );
  }
}
```

- [ ] **Step 2: Rodar análise completa do Flutter**

```bash
cd apps/mobile
flutter analyze
```

Expected: sem erros.

- [ ] **Step 3: Rodar todos os testes Flutter**

```bash
cd apps/mobile
flutter test -v
```

Expected: todos os testes passando.

- [ ] **Step 4: Rodar todos os testes backend**

```bash
cd backend
dotnet test -v minimal
```

Expected: todos os testes passando.

---

## Checklist de spec coverage (auto-review)

| Requisito da spec | Coberto por |
|---|---|
| Visualizar treino antes de iniciar (CA-001) | Task 11 — `PreQuestPage` + `_PreQuestContent` |
| Exibir tipo de treino atual (CA-002) | Task 11 — `_trainingTypeLabel` |
| Exercícios em modo somente leitura (CA-002, RN-005) | Task 10 — `PreQuestExerciseCard` sem ações |
| Exibir XP estimado e duração (fluxo principal) | Task 11 — `_StatChip` com XP e duração |
| CTA alterar tipo de treino (CA-002) | Task 11 — `OutlinedButton` reservado para US-051 |
| CTA confirmar/iniciar | Task 11 — `FilledButton` preQuestStartButton |
| Estado de carregamento | Task 9 — `PreQuestLoading` |
| Estado de acesso expirado (fluxo alternativo 9.1) | Task 9 — `PreQuestAccessBlocked` + `_PreQuestAccessBlocked` |
| Quest já iniciada não permite alteração (fluxo 9.2, RN-006) | Task 9 — `PreQuestAlreadyStarted` + `_PreQuestAlreadyStarted` |
| Estado de erro de carregamento | Task 9 — `PreQuestNetworkError` / `PreQuestUnexpectedError` |
| Analytics `quest_viewed` | Task 9 — `PreQuestController._load` |
| Analytics `access_blocked` | Task 9 — `PreQuestController._load` on `AccessBlockedError` |
| Endpoint `GET /api/quests/{questId}/preview` | Task 4 — controller |
| `canChangeTrainingType` = false quando quest iniciada | Task 2 — `QuestResponseMapper.ToPreviewResponse` |
| L10n PT-BR, EN, ES (+ FR exigido pelo ADR-021) | Task 6 — 4 ARBs |
| Testes unitários backend | Task 3 — `GetQuestPreviewQueryHandlerTests` |
| Testes integração backend | Task 5 — `QuestPreviewEndpointTests` |
| Testes unitários Flutter (data source) | Task 8 — `quest_preview_data_source_test.dart` |
| Testes unitários Flutter (controller/provider) | Task 9 — `pre_quest_controller_test.dart` |

> **Notas para US-051+**: O botão "Alterar tipo de treino" tem `onPressed` vazio (stub). O campo `TrainingType` no backend é derivado de `IsPersonalized`; US-051 adicionará `TrainingType` como campo próprio da entidade `Quest`. A fórmula `estimatedXp = durationMinutes * 4` é provisória e será refinada em US-053.
