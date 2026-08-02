# US-154 + US-155 — RankScore Diminishing Returns & Abuse Protection

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Apply rank-based diminishing returns and monthly cap to RankScore (US-154), and block/reduce artificial gains (US-155).

**Architecture:** RankScore becomes an incremental accumulated value (not sum of attrs); each gain is multiplied by rank multiplier before being added. At E/D/C ranks multiplier=1.0 so existing behavior is unchanged. Abuse protection (strong pain, low completion) is evaluated before calling domain methods and passes an externalMultiplier.

**Tech Stack:** C#/.NET 10, EF Core, xUnit, FluentAssertions

---

## File Map

### New files
- `backend/src/Awaken.Domain/Entities/Progression/RankScoreLog.cs`
- `backend/src/Awaken.Domain/Repositories/IRankScoreLogRepository.cs`
- `backend/src/Awaken.Application/Progression/Services/RankScoreAbuseProtectionService.cs`
- `backend/src/Awaken.Infrastructure/Persistence/Configurations/RankScoreLogConfiguration.cs`
- `backend/src/Awaken.Infrastructure/Persistence/Repositories/RankScoreLogRepository.cs`
- `backend/tests/Awaken.UnitTests/Progression/RankScoreDiminishingReturnsTests.cs`
- `backend/tests/Awaken.UnitTests/Progression/RankScoreAbuseProtectionServiceTests.cs`

### Modified files
- `backend/src/Awaken.Domain/Entities/Progression/HunterProgression.cs` — add DR logic, monthly limit, new return types
- `backend/src/Awaken.Infrastructure/Persistence/AwakenDbContext.cs` — add RankScoreLogs DbSet
- `backend/src/Awaken.Infrastructure/Persistence/Configurations/HunterProgressionConfiguration.cs` — new columns
- `backend/src/Awaken.Infrastructure/DependencyInjection.cs` — register IRankScoreLogRepository
- `backend/src/Awaken.Application/Quests/Commands/CompleteExercise/CompleteExerciseCommandHandler.cs` — abuse eval + DR log
- `backend/src/Awaken.Application/Quests/Commands/CompleteQuest/CompleteQuestCommandHandler.cs` — streak bonus with DR
- `backend/tests/Awaken.UnitTests/Domain/HunterProgressionTests.cs` — update + add DR tests

## Multipliers

| Rank | Multiplier |
|------|-----------|
| E/D/C | 1.00 |
| B | 0.90 |
| A | 0.80 |
| S | 0.70 |
| SS/SSS | 0.60 |

## Monthly limit: 24 RankScore/month (hard cap, excess = 0)

## Abuse rules
- Strong pain reported → externalMultiplier = 0.0
- SetsCompleted < 50% of totalSets → externalMultiplier = 0.5
- Otherwise → externalMultiplier = 1.0
