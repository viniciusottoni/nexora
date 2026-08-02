# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**AWAKEN** is a gamified fitness mobile app — "the Duolingo of training, with the soul of an anime." Flutter frontend + ASP.NET Core backend. MVP phase: architecture is finalized, implementation has not started.

---

## Commands

### Flutter (`apps/mobile/`)

```bash
flutter gen-l10n          # generate l10n from ARB files (run after any ARB change)
flutter analyze           # lint
flutter test              # unit + widget tests
flutter test integration_test/app_test.dart  # integration tests
flutter build apk --debug
flutter build apk --release
flutter run
```

### Backend (`backend/src/`)

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project Awaken.Api/Awaken.Api.csproj   # Swagger at /swagger/ui

# EF migrations
dotnet ef migrations add <Name> -p Awaken.Infrastructure -s Awaken.Api
dotnet ef database update -p Awaken.Infrastructure -s Awaken.Api
```

### Local environment

```bash
docker-compose up -d      # starts PostgreSQL + Redis
```

---

## Architecture

### Stack

```
Flutter + Dart  ──HTTPS REST──▶  ASP.NET Core Web API (C#, .NET 10)
                                  ├── PostgreSQL (primary data)
                                  ├── Redis (cache, rate-limit, locks)
                                  ├── Object storage (S3-compatible, Railway bucket — ADR-024)
                                  ├── Firebase (FCM, Analytics, Crashlytics)
                                  ├── RevenueCat (subscriptions)
                                  └── OpenAI / Azure OpenAI (workout generation)
```

### Flutter — Feature-first + Clean Architecture (lite)

```
lib/
├── main.dart
├── app/            # app_router.dart (go_router), app_providers.dart (Riverpod root)
├── core/           # network (Dio+Retrofit), auth (JWT), storage (Drift, secure_storage), errors
├── design_system/  # tokens, components (AwakenButton, RankBadge, XPBar, QuestCard…), theme
├── features/       # one folder per vertical slice
│   ├── auth/
│   ├── onboarding/
│   ├── quests/
│   ├── progression/
│   ├── nutrition/
│   ├── hunter_profile/
│   └── settings/
└── l10n/
    ├── app_pt.arb   # default (pt-BR)
    ├── app_en.arb
    ├── app_es.arb
    └── app_fr.arb
```

Key Flutter conventions:
- State via **Riverpod** + `AsyncValue`; hooks via `flutter_hooks`
- Local cache: **Drift**; tokens: **flutter_secure_storage** (never `shared_preferences`)
- No hardcoded strings — all user-visible text in ARB files
- App never decides XP, rank, streak, or premium status; backend is authority (ADR-009)

### Backend — Modular Monolith + Clean Architecture + CQRS (MediatR)

```
backend/src/
├── Awaken.Api/           # thin controllers (routing + auth only), middlewares, Program.cs
├── Awaken.Application/   # commands, queries, handlers, validators per domain
├── Awaken.Domain/        # entities, value objects, domain events, repository interfaces
├── Awaken.Infrastructure/# EF Core, Redis, Firebase Admin, RevenueCat, OpenAI, R2
├── Awaken.Contracts/     # request/response DTOs shared between layers
└── Awaken.Shared/        # helpers, extensions, constants
```

MediatR pipeline order: `ValidationBehavior → AuthorizationBehavior → IdempotencyBehavior → LoggingBehavior → TransactionBehavior`

Key backend conventions:
- Commands → `*Command`, Queries → `*Query`, Handlers → `*Handler`, Validators → `*Validator`
- All `DateTime` through `IDateTimeService.UtcNow` — no `DateTime.Now` directly
- All responses include `correlationId`
- Logs must never contain passwords, tokens, or full payloads (ADR-015)
- Quest completion uses idempotency keys (ADR-010)
- Test infrastructure: **xUnit** + **FluentAssertions** + **Testcontainers** + **NetArchTest**

---

## Multi-language (ADR-021)

Four languages are required **from MVP**: `pt-BR` (default), `en`, `es`, `fr`.

- Flutter: ARB keys use semantic names (`dailyQuestTitle`, `onboardingGoalGainMuscle`); fallback to `pt-BR`
- Backend: `User.PreferredLanguage` drives push templates, email templates, and localized error messages
- Analytics events stay in English (technical names)
- QA must validate main flows in all four languages; no screen may mix languages

---

## Key architectural decisions (ADRs)

Before touching any of these areas, read the corresponding ADR in `docs/adrs/`:

| Area | ADR |
|---|---|
| Backend authority (XP, rank, streak) | ADR-009 |
| Quest idempotency | ADR-010 |
| Offline strategy | ADR-011 |
| Hybrid workout generation (AI + templates) | ADR-012 |
| Fallback workouts | ADR-013 |
| LGPD / personal data | ADR-014 |
| Sensitive data in logs | ADR-015 |
| Multi-language strategy | ADR-021 |
| Inventory / shop (minimal scope) | ADR-022 |

Full list at `docs/adrs/index.md`. The authoritative architecture reference is `ARCHITECTURE.md` (42 sections).

---

## Definition of done

A story is done only when:
- Backend logic (handler + validator) implemented
- Flutter UI implemented with loading, error, and empty states
- All user-visible text localized in pt-BR, EN, and ES
- Unit + integration tests written
- Analytics event fired
- Backend logs sanitized
- Main flows validated in all four languages
