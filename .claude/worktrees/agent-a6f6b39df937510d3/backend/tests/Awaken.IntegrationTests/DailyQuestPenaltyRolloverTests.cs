using Awaken.Application.Progression.Commands.ApplyDailyQuestPenalties;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Progression;
using Awaken.Domain.Entities.Quests;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

using Microsoft.Extensions.Configuration;
namespace Awaken.IntegrationTests;

/// US-129: cobre o job de virada de dia ponta a ponta contra um Postgres real
/// (deteccao da daily nao completada + acionamento da penalidade do EPIC-009/US-132).
public class DailyQuestPenaltyRolloverTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("awaken")
        .WithUsername("awaken")
        .WithPassword("awaken_test_password")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PostgreSQL"] = _postgres.GetConnectionString(),
                });
            });
        });

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private static readonly DateTime YesterdayUtc =
        DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(-1), DateTimeKind.Utc);

    private async Task<Guid> SeedUserWithDailyAsync(
        AwakenDbContext dbContext,
        bool trialActive,
        bool questCompleted,
        long initialXp = 1000,
        int previousMissedDays = 0)
    {
        var user = User.Create($"{Guid.NewGuid():N}@awaken.app", "hash", "Hunter");
        user.StartTrial(trialActive ? YesterdayUtc.AddDays(8) : YesterdayUtc.AddDays(-1));
        dbContext.Users.Add(user);

        var progression = HunterProgression.Create(user.Id);
        progression.AddXp(initialXp, DateTime.UtcNow);
        for (var i = 0; i < previousMissedDays; i++)
        {
            progression.ApplyDailyMissPenalty(YesterdayUtc.AddDays(-1 - i));
        }
        dbContext.HunterProgressions.Add(progression);

        var quest = Quest.Create(user.Id, YesterdayUtc, "pt-BR", $"{user.Id:N}_{YesterdayUtc:yyyyMMdd}_daily");
        if (questCompleted) quest.Complete(50, DateTime.UtcNow);
        dbContext.Quests.Add(quest);

        await dbContext.SaveChangesAsync();
        return user.Id;
    }

    // CA-001: daily nao completada + acesso ativo -> penalidade progressiva (RN-002 do US-132).
    [Fact]
    public async Task Handle_AppliesProgressivePenalty_WhenDailyMissedAndAccessActive()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var userId = await SeedUserWithDailyAsync(dbContext, trialActive: true, questCompleted: false);

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var summary = await mediator.Send(new ApplyDailyQuestPenaltiesCommand());

        summary.MissedDailiesChecked.Should().Be(1);
        summary.PenaltiesApplied.Should().Be(1);

        using var assertScope = _factory.Services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var progression = await assertContext.HunterProgressions.SingleAsync(p => p.UserId == userId);
        var quest = await assertContext.Quests.SingleAsync(q => q.UserId == userId);

        progression.TotalXp.Should().Be(89);
        progression.ConsecutiveMissedDailyDays.Should().Be(1);
        progression.RecentDailyPenaltyXp.Should().Be(10);
        quest.PenaltyCheckedAtUtc.Should().NotBeNull();
    }

    // CA-001 (US-132): a penalidade cresce com misses consecutivos.
    [Fact]
    public async Task Handle_AppliesProgressivePenalty_WhenDailyMissedConsecutively()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var userId = await SeedUserWithDailyAsync(
            dbContext,
            trialActive: true,
            questCompleted: false,
            previousMissedDays: 1);

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var summary = await mediator.Send(new ApplyDailyQuestPenaltiesCommand());

        summary.MissedDailiesChecked.Should().Be(1);
        summary.PenaltiesApplied.Should().Be(1);

        using var assertScope = _factory.Services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var progression = await assertContext.HunterProgressions.SingleAsync(p => p.UserId == userId);

        progression.TotalXp.Should().Be(69);
        progression.ConsecutiveMissedDailyDays.Should().Be(2);
        progression.RecentDailyPenaltyXp.Should().Be(20);
    }

    // CA-002/9.2 (US-129) e RN-004 (US-132): acesso expirado -> sem penalidade.
    [Fact]
    public async Task Handle_AppliesNoPenalty_WhenAccessExpired()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var userId = await SeedUserWithDailyAsync(dbContext, trialActive: false, questCompleted: false);

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var summary = await mediator.Send(new ApplyDailyQuestPenaltiesCommand());

        summary.PenaltiesApplied.Should().Be(0);

        using var assertScope = _factory.Services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var progression = await assertContext.HunterProgressions.SingleAsync(p => p.UserId == userId);

        progression.TotalXp.Should().Be(99);
        progression.ConsecutiveMissedDailyDays.Should().Be(0);
    }

    // Estado "daily completada" (US-129/secao 10): nao aciona penalidade.
    [Fact]
    public async Task Handle_AppliesNoPenalty_WhenDailyAlreadyCompleted()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var userId = await SeedUserWithDailyAsync(dbContext, trialActive: true, questCompleted: true);

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var summary = await mediator.Send(new ApplyDailyQuestPenaltiesCommand());

        summary.PenaltiesApplied.Should().Be(0);

        using var assertScope = _factory.Services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var progression = await assertContext.HunterProgressions.SingleAsync(p => p.UserId == userId);

        progression.TotalXp.Should().Be(99);
    }

    // RN-001/idempotencia (ADR-010): rodar o job duas vezes nao dobra a penalidade.
    [Fact]
    public async Task Handle_DoesNotReapplyPenalty_WhenRunTwiceForSameDay()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var userId = await SeedUserWithDailyAsync(dbContext, trialActive: true, questCompleted: false);

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new ApplyDailyQuestPenaltiesCommand());
        var secondRunSummary = await mediator.Send(new ApplyDailyQuestPenaltiesCommand());

        secondRunSummary.MissedDailiesChecked.Should().Be(0);

        using var assertScope = _factory.Services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<AwakenDbContext>();
        var progression = await assertContext.HunterProgressions.SingleAsync(p => p.UserId == userId);

        progression.TotalXp.Should().Be(89);
    }
}
