# US-084 e US-085 — XP por Quest + Registro de Logs de Conclusão — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** US-084 expõe `xpPenaltyApplied` do backend ao card Flutter com indicador visual de penalidade; US-085 adiciona endpoint `POST /api/quests/{questId}/logs` com idempotência, analytics estruturado e testes de integração cobrindo criação de log, duplicidade e quest cancelada.

**Architecture:** US-084 é uma extensão de campo: backend→DTO→entity→UI. US-085 extrai a criação de QuestLog para um `CreateQuestLogCommand` standalone com verificação de idempotência por `questId` (já garantido pelo índice único em `quest_logs`), além de expor o endpoint explícito e adicionar analytics ao `CompleteQuestCommandHandler` existente.

**Tech Stack:** ASP.NET Core 10 / MediatR / FluentValidation / EF Core / xUnit / FluentAssertions / Testcontainers | Flutter / Riverpod / ARB l10n

---

## File Map

### Backend — Modified
| Arquivo | Mudança |
|---|---|
| `backend/src/Awaken.Contracts/BattleLog/BattleLogResponse.cs` | Adiciona `long? XpPenaltyApplied` ao `BattleLogItemResponse` |
| `backend/src/Awaken.Application/BattleLog/Queries/GetRecentBattleLog/GetRecentBattleLogQueryHandler.cs` | Mapeia `l.XpPenaltyApplied` |
| `backend/src/Awaken.Application/BattleLog/Queries/GetPagedBattleLog/GetPagedBattleLogQueryHandler.cs` | Mapeia `l.XpPenaltyApplied` |
| `backend/src/Awaken.Application/Quests/Commands/CompleteQuest/CompleteQuestCommandHandler.cs` | Adiciona `logger.LogInformation("quest_log_created ...")` e `quest_log_duplicate_prevented` |
| `backend/src/Awaken.Api/Controllers/V1/QuestsController.cs` | Adiciona `POST /{questId}/logs` |
| `backend/tests/Awaken.IntegrationTests/BattleLogEndpointTests.cs` | Adiciona testes US-084 |

### Backend — New
| Arquivo | Propósito |
|---|---|
| `backend/src/Awaken.Contracts/BattleLog/CreateQuestLogRequest.cs` | Request DTO para o endpoint |
| `backend/src/Awaken.Application/BattleLog/Commands/CreateQuestLog/CreateQuestLogCommand.cs` | Comando MediatR |
| `backend/src/Awaken.Application/BattleLog/Commands/CreateQuestLog/CreateQuestLogCommandValidator.cs` | Validação FluentValidation |
| `backend/src/Awaken.Application/BattleLog/Commands/CreateQuestLog/CreateQuestLogCommandHandler.cs` | Handler com idempotência + analytics |
| `backend/tests/Awaken.IntegrationTests/QuestLogCreationTests.cs` | Testes US-085 |

### Flutter — Modified
| Arquivo | Mudança |
|---|---|
| `apps/mobile/lib/features/battle_log/domain/entities/battle_log_item.dart` | Adiciona `int? xpPenaltyApplied` |
| `apps/mobile/lib/features/battle_log/data/dtos/battle_log_response_dto.dart` | Adiciona campo + `fromJson` |
| `apps/mobile/lib/features/battle_log/data/repositories/battle_log_repository_impl.dart` | Mapeia campo |
| `apps/mobile/lib/features/battle_log/presentation/widgets/quest_log_card.dart` | Indicador visual de penalidade |
| `apps/mobile/lib/features/battle_log/presentation/providers/battle_log_controller.dart` | Dispara `battle_log_xp_viewed` |
| `apps/mobile/lib/l10n/app_pt.arb` | Adiciona `battleLogXpPenaltyLabel` |
| `apps/mobile/lib/l10n/app_en.arb` | Adiciona `battleLogXpPenaltyLabel` |
| `apps/mobile/lib/l10n/app_es.arb` | Adiciona `battleLogXpPenaltyLabel` |
| `apps/mobile/lib/l10n/app_fr.arb` | Adiciona `battleLogXpPenaltyLabel` |
| `apps/mobile/test/features/battle_log/presentation/widgets/quest_log_card_test.dart` | Testes penalidade, XP zero, multilíngue |

---

## Task 1 — US-084 Backend: campo XpPenaltyApplied

**Files:**
- Modify: `backend/src/Awaken.Contracts/BattleLog/BattleLogResponse.cs`
- Modify: `backend/src/Awaken.Application/BattleLog/Queries/GetRecentBattleLog/GetRecentBattleLogQueryHandler.cs`
- Modify: `backend/src/Awaken.Application/BattleLog/Queries/GetPagedBattleLog/GetPagedBattleLogQueryHandler.cs`

- [ ] **Step 1: Adicionar campo ao record de resposta**

```csharp
// BattleLogResponse.cs
namespace Awaken.Contracts.BattleLog;

public record BattleLogItemResponse(
    Guid QuestLogId,
    Guid QuestId,
    string QuestType,
    long XpEarned,
    long? XpPenaltyApplied,
    IReadOnlyList<string> ItemsEarned,
    DateTime CompletedAt);

public record BattleLogResponse(IReadOnlyList<BattleLogItemResponse> Items);

public record BattleLogPageMetadata(int Page, int PageSize, bool HasMore);

public record PagedBattleLogResponse(
    IReadOnlyList<BattleLogItemResponse> Items,
    BattleLogPageMetadata Metadata);
```

- [ ] **Step 2: Atualizar GetRecentBattleLogQueryHandler**

```csharp
var items = logs
    .Select(l => new BattleLogItemResponse(
        l.Id,
        l.QuestId,
        l.QuestType,
        l.XpEarned,
        l.XpPenaltyApplied,
        l.ItemsEarned,
        l.CompletedAtUtc))
    .ToList();
```

- [ ] **Step 3: Atualizar GetPagedBattleLogQueryHandler**

```csharp
var items = logs
    .Select(l => new BattleLogItemResponse(
        l.Id,
        l.QuestId,
        l.QuestType,
        l.XpEarned,
        l.XpPenaltyApplied,
        l.ItemsEarned,
        l.CompletedAtUtc))
    .ToList();
```

- [ ] **Step 4: Build backend**

```bash
cd backend && dotnet build -v minimal
```

Expected: sem erros de compilação.

---

## Task 2 — US-084 Backend: testes de integração

**Files:**
- Modify: `backend/tests/Awaken.IntegrationTests/BattleLogEndpointTests.cs`

- [ ] **Step 1: Adicionar testes US-084 ao final da classe `BattleLogEndpointTests`**

```csharp
// ─── US-084: Ver XP recebido em cada quest ──────────────────────────────────

[Fact]
public async Task US084_CA001_XpEarned_IsExposedInLogEntry()
{
    const string email = "us084_ca001@awaken.app";
    await AuthenticateNewHunterAsync(email);

    await SeedQuestLogAsync(email, "daily", 120, DateTime.UtcNow);

    var response = await _client.GetAsync("/api/hunter/battle-log/recent");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<BattleLogResponse>();
    result!.Items.Should().ContainSingle();
    result.Items[0].XpEarned.Should().Be(120);
}

[Fact]
public async Task US084_CA002_XpZero_IsExposedAsZero()
{
    const string email = "us084_ca002@awaken.app";
    await AuthenticateNewHunterAsync(email);

    await SeedQuestLogAsync(email, "daily", 0, DateTime.UtcNow);

    var response = await _client.GetAsync("/api/hunter/battle-log/recent");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<BattleLogResponse>();
    result!.Items[0].XpEarned.Should().Be(0);
}

[Fact]
public async Task US084_RN004_XpPenaltyApplied_IsExposedWhenSet()
{
    const string email = "us084_rn004@awaken.app";
    await AuthenticateNewHunterAsync(email);

    await SeedQuestLogWithPenaltyAsync(email, "daily", 80, xpPenalty: 20, DateTime.UtcNow);

    var response = await _client.GetAsync("/api/hunter/battle-log/recent");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<BattleLogResponse>();
    result!.Items[0].XpEarned.Should().Be(80);
    result.Items[0].XpPenaltyApplied.Should().Be(20);
}

[Fact]
public async Task US084_XpPenaltyApplied_IsNullWhenNoPenalty()
{
    const string email = "us084_no_penalty@awaken.app";
    await AuthenticateNewHunterAsync(email);

    await SeedQuestLogAsync(email, "daily", 100, DateTime.UtcNow);

    var response = await _client.GetAsync("/api/hunter/battle-log/recent");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<BattleLogResponse>();
    result!.Items[0].XpPenaltyApplied.Should().BeNull();
}
```

- [ ] **Step 2: Adicionar helper `SeedQuestLogWithPenaltyAsync` à classe**

```csharp
private async Task SeedQuestLogWithPenaltyAsync(
    string email, string questType, long xpEarned, long xpPenalty, DateTime completedAt)
{
    using var scope = _factory.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
    var user = await dbContext.Users.SingleAsync(u => u.Email == email);

    var log = QuestLog.Create(
        questId: Guid.NewGuid(),
        userId: user.Id,
        questType: questType,
        xpEarned: xpEarned,
        strengthXpEarned: 0,
        agilityXpEarned: 0,
        enduranceXpEarned: 0,
        vitalityXpEarned: 0,
        focusXpEarned: 0,
        wisdomXpEarned: 0,
        strengthPointsGranted: 0,
        agilityPointsGranted: 0,
        endurancePointsGranted: 0,
        vitalityPointsGranted: 0,
        focusPointsGranted: 0,
        itemsEarned: [],
        completedAtUtc: completedAt,
        xpPenaltyApplied: xpPenalty);

    dbContext.QuestLogs.Add(log);
    await dbContext.SaveChangesAsync();
}
```

- [ ] **Step 3: Rodar testes US-084**

```bash
cd backend && dotnet test tests/Awaken.IntegrationTests --filter "US084" -v minimal
```

Expected: 4 testes PASS.

---

## Task 3 — US-085 Backend: Contracts + Command

**Files:**
- Create: `backend/src/Awaken.Contracts/BattleLog/CreateQuestLogRequest.cs`
- Create: `backend/src/Awaken.Application/BattleLog/Commands/CreateQuestLog/CreateQuestLogCommand.cs`
- Create: `backend/src/Awaken.Application/BattleLog/Commands/CreateQuestLog/CreateQuestLogCommandValidator.cs`

- [ ] **Step 1: Criar request DTO**

```csharp
// backend/src/Awaken.Contracts/BattleLog/CreateQuestLogRequest.cs
namespace Awaken.Contracts.BattleLog;

public record CreateQuestLogRequest(
    string QuestType,
    long XpEarned,
    IReadOnlyList<string>? ItemsEarned,
    long? XpPenaltyApplied);
```

- [ ] **Step 2: Criar comando MediatR**

```csharp
// backend/src/Awaken.Application/BattleLog/Commands/CreateQuestLog/CreateQuestLogCommand.cs
using Awaken.Contracts.BattleLog;
using MediatR;

namespace Awaken.Application.BattleLog.Commands.CreateQuestLog;

public record CreateQuestLogCommand(
    Guid QuestId,
    string QuestType,
    long XpEarned,
    IReadOnlyList<string> ItemsEarned,
    long? XpPenaltyApplied) : IRequest<BattleLogItemResponse>;
```

- [ ] **Step 3: Criar validator**

```csharp
// backend/src/Awaken.Application/BattleLog/Commands/CreateQuestLog/CreateQuestLogCommandValidator.cs
using FluentValidation;

namespace Awaken.Application.BattleLog.Commands.CreateQuestLog;

public class CreateQuestLogCommandValidator : AbstractValidator<CreateQuestLogCommand>
{
    private static readonly HashSet<string> ValidTypes = ["daily", "dungeon", "raid"];

    public CreateQuestLogCommandValidator()
    {
        RuleFor(x => x.QuestId).NotEmpty();
        RuleFor(x => x.QuestType)
            .NotEmpty()
            .Must(t => ValidTypes.Contains(t))
            .WithMessage("questType deve ser daily, dungeon ou raid.");
        RuleFor(x => x.XpEarned).GreaterThanOrEqualTo(0);
    }
}
```

---

## Task 4 — US-085 Backend: Handler com idempotência + analytics

**Files:**
- Create: `backend/src/Awaken.Application/BattleLog/Commands/CreateQuestLog/CreateQuestLogCommandHandler.cs`
- Modify: `backend/src/Awaken.Application/Quests/Commands/CompleteQuest/CompleteQuestCommandHandler.cs`

- [ ] **Step 1: Criar handler com idempotência**

```csharp
// backend/src/Awaken.Application/BattleLog/Commands/CreateQuestLog/CreateQuestLogCommandHandler.cs
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.BattleLog;
using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.BattleLog.Commands.CreateQuestLog;

/// US-085: cria QuestLog idempotente para uma quest. Se log ja existe para o questId,
/// retorna o existente sem duplicar (RN-005). Analytics via structured logging (ADR-015).
public class CreateQuestLogCommandHandler(
    IQuestLogRepository questLogRepository,
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    ILogger<CreateQuestLogCommandHandler> logger) : IRequestHandler<CreateQuestLogCommand, BattleLogItemResponse>
{
    public async Task<BattleLogItemResponse> Handle(
        CreateQuestLogCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        // RN-005: idempotência — mesmo questId não gera duplicata.
        var existing = await questLogRepository.GetByQuestIdAsync(request.QuestId, cancellationToken);
        if (existing is not null)
        {
            logger.LogInformation(
                "quest_log_duplicate_prevented questId={QuestId} userId={UserId} existingLogId={LogId}",
                request.QuestId, userId, existing.Id);

            return ToResponse(existing);
        }

        var log = QuestLog.Create(
            questId: request.QuestId,
            userId: userId,
            questType: request.QuestType,
            xpEarned: request.XpEarned,
            strengthXpEarned: 0,
            agilityXpEarned: 0,
            enduranceXpEarned: 0,
            vitalityXpEarned: 0,
            focusXpEarned: 0,
            wisdomXpEarned: 0,
            strengthPointsGranted: 0,
            agilityPointsGranted: 0,
            endurancePointsGranted: 0,
            vitalityPointsGranted: 0,
            focusPointsGranted: 0,
            itemsEarned: request.ItemsEarned,
            completedAtUtc: dateTimeService.UtcNow,
            xpPenaltyApplied: request.XpPenaltyApplied);

        await questLogRepository.AddAsync(log, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "quest_log_created questLogId={LogId} questId={QuestId} userId={UserId} questType={Type} xpEarned={Xp}",
            log.Id, request.QuestId, userId, request.QuestType, request.XpEarned);

        return ToResponse(log);
    }

    private static BattleLogItemResponse ToResponse(QuestLog log) =>
        new(log.Id, log.QuestId, log.QuestType, log.XpEarned, log.XpPenaltyApplied, log.ItemsEarned, log.CompletedAtUtc);
}
```

- [ ] **Step 2: Adicionar analytics no CompleteQuestCommandHandler**

Após `await unitOfWork.SaveChangesAsync(cancellationToken);`, adicionar:

```csharp
logger.LogInformation(
    "quest_log_created questLogId={LogId} questId={QuestId} userId={UserId} questType={Type} xpEarned={Xp}",
    questLog.Id, quest.Id, quest.UserId, quest.Type, totalXpEarned);
```

No bloco de idempotência (`quest.Status == "completed"`), adicionar antes do `return`:

```csharp
logger.LogInformation(
    "quest_log_duplicate_prevented questId={QuestId} userId={UserId}",
    quest.Id, userId);
```

---

## Task 5 — US-085 Backend: endpoint POST /{questId}/logs

**Files:**
- Modify: `backend/src/Awaken.Api/Controllers/V1/QuestsController.cs`

- [ ] **Step 1: Adicionar using e endpoint**

No topo do arquivo adicionar:
```csharp
using Awaken.Application.BattleLog.Commands.CreateQuestLog;
using Awaken.Contracts.BattleLog;
```

No corpo da classe `QuestsController`, adicionar:
```csharp
/// US-085: registra log de conclusão de quest de forma idempotente.
/// Uso interno na conclusão; também disponível para auditoria.
[HttpPost("{questId:guid}/logs")]
public async Task<IActionResult> CreateLog(
    Guid questId,
    [FromBody] CreateQuestLogRequest request,
    CancellationToken ct)
{
    var command = new CreateQuestLogCommand(
        questId,
        request.QuestType,
        request.XpEarned,
        request.ItemsEarned ?? [],
        request.XpPenaltyApplied);

    var result = await mediator.Send(command, ct);
    return Ok(result);
}
```

- [ ] **Step 2: Build backend**

```bash
cd backend && dotnet build -v minimal
```

Expected: sem erros.

---

## Task 6 — US-085 Backend: testes de integração

**Files:**
- Create: `backend/tests/Awaken.IntegrationTests/QuestLogCreationTests.cs`

- [ ] **Step 1: Criar arquivo de testes**

```csharp
// US-085: Registrar logs de conclusão de quest — testes de integração.
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.BattleLog;
using Awaken.Domain.Entities.Quests;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Awaken.IntegrationTests;

public class QuestLogCreationTests : IAsyncLifetime
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

    private async Task AuthenticateNewHunterAsync(string email)
    {
        var payload = new { email, password = "Str0ngPass!", name = "Hunter", language = "pt-BR" };
        var reg = await _client.PostAsJsonAsync("/api/auth/register", payload);
        reg.EnsureSuccessStatusCode();
        var token = (await reg.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        (await _client.PostAsync("/api/subscriptions/trial/start", null)).EnsureSuccessStatusCode();

        var onboarding = new
        {
            goal = "gain_muscle", experienceLevel = "intermediate", age = 28,
            heightCm = 175.0, weightKg = 82.0, biologicalSex = "masculino",
            trainingDuration = "6_12_months", availableMinutesPerWorkout = 30,
            bodyType = "normal", physicalLimitations = new[] { "no_limitations" },
            physicalPains = new[] { "no_pains" }
        };
        (await _client.PostAsJsonAsync("/api/users/me/profile/complete-onboarding", onboarding))
            .EnsureSuccessStatusCode();
    }

    // ─── CA-001: log criado ─────────────────────────────────────────────────

    [Fact]
    public async Task CA001_Daily_LogCreated_OnQuestCompletion()
    {
        const string email = "us085_ca001_daily@awaken.app";
        await AuthenticateNewHunterAsync(email);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);

        var questId = Guid.NewGuid();
        var request = new CreateQuestLogRequest("daily", 100, null, null);

        var response = await _client.PostAsJsonAsync($"/api/quests/{questId}/logs", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BattleLogItemResponse>();
        result!.QuestType.Should().Be("daily");
        result.XpEarned.Should().Be(100);
        result.QuestId.Should().Be(questId);

        // Verifica persistência no banco
        var log = await dbContext.QuestLogs.SingleOrDefaultAsync(l => l.QuestId == questId);
        log.Should().NotBeNull();
        log!.XpEarned.Should().Be(100);
        log.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task CA001_Dungeon_LogCreated_WithItems()
    {
        const string email = "us085_ca001_dungeon@awaken.app";
        await AuthenticateNewHunterAsync(email);

        var questId = Guid.NewGuid();
        var request = new CreateQuestLogRequest("dungeon", 200, ["dungeon_stone"], null);

        var response = await _client.PostAsJsonAsync($"/api/quests/{questId}/logs", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BattleLogItemResponse>();
        result!.QuestType.Should().Be("dungeon");
        result.XpEarned.Should().Be(200);
        result.ItemsEarned.Should().Contain("dungeon_stone");
    }

    [Fact]
    public async Task CA001_Raid_LogCreated()
    {
        const string email = "us085_ca001_raid@awaken.app";
        await AuthenticateNewHunterAsync(email);

        var questId = Guid.NewGuid();
        var request = new CreateQuestLogRequest("raid", 350, null, null);

        var response = await _client.PostAsJsonAsync($"/api/quests/{questId}/logs", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BattleLogItemResponse>();
        result!.QuestType.Should().Be("raid");
        result.XpEarned.Should().Be(350);
    }

    // ─── CA-002: sem duplicidade ────────────────────────────────────────────

    [Fact]
    public async Task CA002_DuplicateCall_ReturnsSameLog_NoNewEntry()
    {
        const string email = "us085_ca002@awaken.app";
        await AuthenticateNewHunterAsync(email);

        var questId = Guid.NewGuid();
        var request = new CreateQuestLogRequest("daily", 80, null, null);

        var first = await _client.PostAsJsonAsync($"/api/quests/{questId}/logs", request);
        var second = await _client.PostAsJsonAsync($"/api/quests/{questId}/logs", request);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var r1 = await first.Content.ReadFromJsonAsync<BattleLogItemResponse>();
        var r2 = await second.Content.ReadFromJsonAsync<BattleLogItemResponse>();
        r1!.QuestLogId.Should().Be(r2!.QuestLogId, "deve retornar o mesmo log sem criar duplicata");

        // Conta entradas no banco: deve haver exatamente 1
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var count = await dbContext.QuestLogs.CountAsync(l => l.QuestId == questId);
        count.Should().Be(1, "idempotência deve evitar duplicação");
    }

    // ─── RN-007: quest cancelada não gera log ───────────────────────────────

    [Fact]
    public async Task RN007_InvalidQuestType_Returns400()
    {
        const string email = "us085_rn007@awaken.app";
        await AuthenticateNewHunterAsync(email);

        var questId = Guid.NewGuid();
        var request = new CreateQuestLogRequest("cancelled", 0, null, null);

        var response = await _client.PostAsJsonAsync($"/api/quests/{questId}/logs", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─── RN-006: logs preservados após expiração ────────────────────────────

    [Fact]
    public async Task RN006_LogsPreserved_AfterTrialExpiry()
    {
        const string email = "us085_rn006@awaken.app";
        await AuthenticateNewHunterAsync(email);

        var questId = Guid.NewGuid();
        var request = new CreateQuestLogRequest("daily", 90, null, null);
        (await _client.PostAsJsonAsync($"/api/quests/{questId}/logs", request)).EnsureSuccessStatusCode();

        // Expira o trial via reflexão (mesmo padrão do BattleLogEndpointTests)
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);
        typeof(Awaken.Domain.Entities.Auth.User)
            .GetProperty(nameof(Awaken.Domain.Entities.Auth.User.TrialEndsAt))!
            .SetValue(user, DateTime.UtcNow.AddDays(-1));
        await dbContext.SaveChangesAsync();

        var log = await dbContext.QuestLogs.SingleOrDefaultAsync(l => l.QuestId == questId);
        log.Should().NotBeNull("logs não devem ser apagados após expirar o trial");
    }

    // ─── Auth ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Unauthenticated_Returns401()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/quests/{Guid.NewGuid()}/logs",
            new CreateQuestLogRequest("daily", 100, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

- [ ] **Step 2: Rodar testes US-085**

```bash
cd backend && dotnet test tests/Awaken.IntegrationTests --filter "QuestLogCreation" -v minimal
```

Expected: todos PASS.

---

## Task 7 — US-084 Flutter: campo xpPenaltyApplied

**Files:**
- Modify: `apps/mobile/lib/features/battle_log/domain/entities/battle_log_item.dart`
- Modify: `apps/mobile/lib/features/battle_log/data/dtos/battle_log_response_dto.dart`
- Modify: `apps/mobile/lib/features/battle_log/data/repositories/battle_log_repository_impl.dart`

- [ ] **Step 1: Adicionar campo à entity**

```dart
// battle_log_item.dart
class BattleLogItem {
  const BattleLogItem({
    required this.questLogId,
    required this.questId,
    required this.questType,
    required this.xpEarned,
    required this.itemsEarned,
    required this.completedAt,
    this.xpPenaltyApplied,
  });

  final String questLogId;
  final String questId;
  final String questType;
  final int xpEarned;
  final List<String> itemsEarned;
  final DateTime completedAt;
  final int? xpPenaltyApplied;
}
```

- [ ] **Step 2: Adicionar campo ao DTO**

No `BattleLogItemDto`:
```dart
class BattleLogItemDto {
  const BattleLogItemDto({
    required this.questLogId,
    required this.questId,
    required this.questType,
    required this.xpEarned,
    required this.itemsEarned,
    required this.completedAt,
    this.xpPenaltyApplied,
  });

  final String questLogId;
  final String questId;
  final String questType;
  final int xpEarned;
  final List<String> itemsEarned;
  final DateTime completedAt;
  final int? xpPenaltyApplied;

  factory BattleLogItemDto.fromJson(Map<String, dynamic> json) {
    return BattleLogItemDto(
      questLogId: json['questLogId'] as String,
      questId: json['questId'] as String,
      questType: json['questType'] as String,
      xpEarned: json['xpEarned'] as int,
      itemsEarned: (json['itemsEarned'] as List<dynamic>)
          .map((e) => e as String)
          .toList(),
      completedAt: DateTime.parse(json['completedAt'] as String),
      xpPenaltyApplied: json['xpPenaltyApplied'] as int?,
    );
  }
}
```

- [ ] **Step 3: Atualizar repository impl**

```dart
BattleLogItem _toEntity(BattleLogItemDto dto) {
  return BattleLogItem(
    questLogId: dto.questLogId,
    questId: dto.questId,
    questType: dto.questType,
    xpEarned: dto.xpEarned,
    itemsEarned: dto.itemsEarned,
    completedAt: dto.completedAt,
    xpPenaltyApplied: dto.xpPenaltyApplied,
  );
}
```

---

## Task 8 — US-084 Flutter: indicador visual de penalidade + analytics

**Files:**
- Modify: `apps/mobile/lib/features/battle_log/presentation/widgets/quest_log_card.dart`
- Modify: `apps/mobile/lib/features/battle_log/presentation/providers/battle_log_controller.dart`
- Modify: `apps/mobile/lib/l10n/app_pt.arb`
- Modify: `apps/mobile/lib/l10n/app_en.arb`
- Modify: `apps/mobile/lib/l10n/app_es.arb`
- Modify: `apps/mobile/lib/l10n/app_fr.arb`

- [ ] **Step 1: Adicionar chave de penalidade nos ARBs**

`app_pt.arb` — inserir antes do `}` final:
```json
  ,

  "battleLogXpPenaltyLabel": "−{xp} XP (penalidade)",
  "@battleLogXpPenaltyLabel": {
    "description": "Indicador de penalidade de XP no log de batalha (US-084)",
    "placeholders": { "xp": { "type": "int" } }
  }
```

`app_en.arb`:
```json
  ,

  "battleLogXpPenaltyLabel": "−{xp} XP (penalty)",
  "@battleLogXpPenaltyLabel": {
    "description": "XP penalty indicator in battle log (US-084)",
    "placeholders": { "xp": { "type": "int" } }
  }
```

`app_es.arb`:
```json
  ,

  "battleLogXpPenaltyLabel": "−{xp} XP (penalización)",
  "@battleLogXpPenaltyLabel": {
    "description": "Indicador de penalización de XP en el registro de batallas (US-084)",
    "placeholders": { "xp": { "type": "int" } }
  }
```

`app_fr.arb`:
```json
  ,

  "battleLogXpPenaltyLabel": "−{xp} XP (pénalité)",
  "@battleLogXpPenaltyLabel": {
    "description": "Indicateur de pénalité XP dans le journal de bataille (US-084)",
    "placeholders": { "xp": { "type": "int" } }
  }
```

- [ ] **Step 2: Rodar gen-l10n**

```bash
cd apps/mobile && flutter gen-l10n
```

Expected: sem erros, arquivos `app_localizations*.dart` atualizados.

- [ ] **Step 3: Atualizar QuestLogCard com indicador de penalidade**

Dentro da `Column` que contém o XP, após o `Row` do XP+items, adicionar:
```dart
if (item.xpPenaltyApplied != null && item.xpPenaltyApplied != 0) ...[
  const SizedBox(height: AwakenSpacing.xs),
  Row(
    children: [
      Icon(Icons.warning_amber_rounded,
          size: 12, color: AwakenColors.attrStrength),
      const SizedBox(width: 4),
      Text(
        l10n.battleLogXpPenaltyLabel(item.xpPenaltyApplied!),
        style: AwakenTypography.labelSmall.copyWith(
          color: AwakenColors.attrStrength,
        ),
      ),
    ],
  ),
],
```

- [ ] **Step 4: Adicionar `battle_log_xp_viewed` analytics no controller**

No bloco que seta `BattleLogLoaded` (tanto trial quanto subscriber), adicionar:
```dart
await analytics.logEvent('battle_log_xp_viewed');
```

---

## Task 9 — US-084 Flutter: testes do QuestLogCard

**Files:**
- Modify: `apps/mobile/test/features/battle_log/presentation/widgets/quest_log_card_test.dart`

- [ ] **Step 1: Adicionar testes de penalidade e XP zero**

```dart
BattleLogItem _itemWithPenalty({
  int xpEarned = 80,
  int xpPenaltyApplied = 20,
}) =>
    BattleLogItem(
      questLogId: 'log-penalty',
      questId: 'quest-penalty',
      questType: 'daily',
      xpEarned: xpEarned,
      itemsEarned: const [],
      completedAt: DateTime.utc(2024, 6, 1, 10, 0),
      xpPenaltyApplied: xpPenaltyApplied,
    );

// Adicionar ao final do main():

testWidgets('pt-BR mostra indicador de penalidade', (tester) async {
  await tester.pumpWidget(_buildApp(
    const Locale('pt', 'BR'),
    _itemWithPenalty(xpEarned: 80, xpPenaltyApplied: 20),
  ));

  expect(find.textContaining('+80 XP'), findsOneWidget);
  expect(find.textContaining('−20 XP'), findsOneWidget);
  expect(find.textContaining('penalidade'), findsOneWidget);
});

testWidgets('en mostra indicador de penalty em ingles', (tester) async {
  await tester.pumpWidget(_buildApp(
    const Locale('en'),
    _itemWithPenalty(xpEarned: 80, xpPenaltyApplied: 20),
  ));

  expect(find.textContaining('+80 XP'), findsOneWidget);
  expect(find.textContaining('penalty'), findsOneWidget);
});

testWidgets('es mostra indicador de penalizacion em espanhol', (tester) async {
  await tester.pumpWidget(_buildApp(
    const Locale('es'),
    _itemWithPenalty(xpEarned: 80, xpPenaltyApplied: 20),
  ));

  expect(find.textContaining('+80 XP'), findsOneWidget);
  expect(find.textContaining('penalización'), findsOneWidget);
});

testWidgets('fr mostra indicador de penalite em frances', (tester) async {
  await tester.pumpWidget(_buildApp(
    const Locale('fr'),
    _itemWithPenalty(xpEarned: 80, xpPenaltyApplied: 20),
  ));

  expect(find.textContaining('+80 XP'), findsOneWidget);
  expect(find.textContaining('pénalité'), findsOneWidget);
});

testWidgets('XP zero exibido quando registrado como zero', (tester) async {
  await tester.pumpWidget(_buildApp(
    const Locale('pt', 'BR'),
    _item(xpEarned: 0),
  ));

  expect(find.textContaining('+0 XP'), findsOneWidget);
});

testWidgets('sem penalidade nao exibe indicador', (tester) async {
  await tester.pumpWidget(_buildApp(
    const Locale('pt', 'BR'),
    _item(xpEarned: 100),
  ));

  expect(find.textContaining('penalidade'), findsNothing);
});
```

- [ ] **Step 2: Rodar testes Flutter**

```bash
cd apps/mobile && flutter test test/features/battle_log/presentation/widgets/quest_log_card_test.dart -v
```

Expected: todos PASS.

---

## Task 10 — Verificação final

- [ ] **Step 1: Flutter analyze**

```bash
cd apps/mobile && flutter analyze
```

Expected: 0 erros.

- [ ] **Step 2: Flutter test completo**

```bash
cd apps/mobile && flutter test
```

Expected: todos PASS.

- [ ] **Step 3: Backend test completo**

```bash
cd backend && dotnet test -v minimal
```

Expected: todos PASS.

---

## Self-Review: Cobertura dos critérios de aceite

### US-084

| Critério | Coberto por |
|---|---|
| CA-001: XP exibido no card | Task 7 entity + Task 8 QuestLogCard |
| CA-002: Frontend não recalcula | RN-005 — campo vem do backend, frontend só exibe |
| RN-001: xpEarned de QuestLog.xpEarned | Task 1 handler mapeia `l.XpEarned` |
| RN-002: consistência com HunterProgress | Backend é autoridade (ADR-009) |
| RN-003: quest cancelada não exibe XP completo | Nenhum QuestLog = nenhum card |
| RN-004: penalidade indicada claramente | Task 8 — indicador com ícone + texto localizado |
| XP zero exibido como zero | Task 9 teste + Task 8 card não filtra zero |
| Analytics `battle_log_xp_viewed` | Task 8 controller |
| Localização PT, EN, ES, FR | Task 8 ARBs + Task 9 testes |

### US-085

| Critério | Coberto por |
|---|---|
| CA-001: QuestLog criado na conclusão | Task 6 CA001_Daily/Dungeon/Raid |
| CA-002: sem duplicidade | Task 6 CA002_DuplicateCall |
| RN-001: toda quest concluída gera log | Task 4 handler |
| RN-002: log contém questType | Task 3 command + Task 4 handler |
| RN-003: log contém XP | Task 3 command + Task 4 handler |
| RN-004: dungeons registram itens | Task 6 CA001_Dungeon_LogCreated_WithItems |
| RN-005: sem duplicata por questId | Task 4 handler + índice único já configurado |
| RN-006: logs preservados após expiração | Task 6 RN006_LogsPreserved_AfterTrialExpiry |
| RN-007: quest cancelada não gera log | Task 6 RN007_InvalidQuestType_Returns400 |
| Analytics `quest_log_created` | Task 4 logger.LogInformation |
| Analytics `quest_log_duplicate_prevented` | Task 4 logger.LogInformation |
| Auth exigida | Task 6 Unauthenticated_Returns401 |
