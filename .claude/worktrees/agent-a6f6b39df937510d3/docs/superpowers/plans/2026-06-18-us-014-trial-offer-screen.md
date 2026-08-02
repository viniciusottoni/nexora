# US-014 — Trial Offer Screen Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the trial offer screen that appears before onboarding, explaining the 7-day free trial and the monthly/annual plan options, with a CTA to start the free trial.

**Architecture:** Backend exposes a public `GET /api/app-config/plans` endpoint returning static config (trial days + plan names). Flutter consumes this via a data source + repository, manages state with a Riverpod Notifier, and shows the screen with loading/loaded/fallback states. Route resolver updated to send visitors to `pricingIntro` instead of `login`.

**Tech Stack:** Flutter (Riverpod, go_router, Dio), ASP.NET Core 10 (MediatR), xUnit + FluentAssertions + WebApplicationFactory (backend tests), flutter_test + mocktail (Flutter tests).

---

## File Map

### Backend — CREATE
| File | Purpose |
|---|---|
| `backend/src/Awaken.Contracts/AppConfig/PlansConfigResponse.cs` | DTO returned by the endpoint |
| `backend/src/Awaken.Application/AppConfig/Queries/GetPlansConfig/GetPlansConfigQuery.cs` | MediatR query record |
| `backend/src/Awaken.Application/AppConfig/Queries/GetPlansConfig/GetPlansConfigQueryHandler.cs` | Returns hardcoded config |
| `backend/src/Awaken.Api/Controllers/V1/AppConfigController.cs` | Public controller, no auth |
| `backend/tests/Awaken.UnitTests/AppConfig/GetPlansConfigQueryHandlerTests.cs` | Unit test for handler |
| `backend/tests/Awaken.IntegrationTests/AppConfigEndpointTests.cs` | Integration test for endpoint |

### Flutter — CREATE
| File | Purpose |
|---|---|
| `apps/mobile/lib/features/pricing/data/dtos/plans_config_dto.dart` | JSON DTO |
| `apps/mobile/lib/features/pricing/data/datasources/app_config_remote_data_source.dart` | HTTP call to backend |
| `apps/mobile/lib/features/pricing/domain/repositories/app_config_repository.dart` | Abstract repository |
| `apps/mobile/lib/features/pricing/data/repositories/app_config_repository_impl.dart` | Impl wrapping data source |
| `apps/mobile/lib/features/pricing/presentation/providers/trial_offer_state.dart` | Sealed state classes |
| `apps/mobile/lib/features/pricing/presentation/providers/trial_offer_controller.dart` | Riverpod Notifier |
| `apps/mobile/lib/features/pricing/presentation/providers/pricing_providers.dart` | Provider wiring |
| `apps/mobile/test/features/pricing/data/datasources/app_config_remote_data_source_test.dart` | Data source test |
| `apps/mobile/test/features/pricing/presentation/providers/trial_offer_controller_test.dart` | Controller test |

### Flutter — MODIFY
| File | Change |
|---|---|
| `apps/mobile/lib/l10n/app_pt.arb` | Remove old freemium keys; add trial offer keys |
| `apps/mobile/lib/l10n/app_en.arb` | Same |
| `apps/mobile/lib/l10n/app_es.arb` | Same |
| `apps/mobile/lib/features/pricing/presentation/pages/pricing_page.dart` | Full rewrite |
| `apps/mobile/lib/app/navigation/route_resolver.dart` | Visitor → `pricingIntro` |
| `apps/mobile/test/app/navigation/route_resolver_test.dart` | Update visitor initial route assertion |
| `apps/mobile/test/features/pricing/presentation/pages/pricing_page_test.dart` | Full rewrite |

---

## Task 1: Backend — Contract + Unit-Tested Query Handler

**Files:**
- Create: `backend/src/Awaken.Contracts/AppConfig/PlansConfigResponse.cs`
- Create: `backend/src/Awaken.Application/AppConfig/Queries/GetPlansConfig/GetPlansConfigQuery.cs`
- Create: `backend/src/Awaken.Application/AppConfig/Queries/GetPlansConfig/GetPlansConfigQueryHandler.cs`
- Create: `backend/tests/Awaken.UnitTests/AppConfig/GetPlansConfigQueryHandlerTests.cs`

- [ ] **Step 1: Write the failing unit test**

```csharp
// backend/tests/Awaken.UnitTests/AppConfig/GetPlansConfigQueryHandlerTests.cs
using Awaken.Application.AppConfig.Queries.GetPlansConfig;
using FluentAssertions;

namespace Awaken.UnitTests.AppConfig;

public class GetPlansConfigQueryHandlerTests
{
    [Fact]
    public async Task HandleReturnsPlansConfigWithTrialDays7()
    {
        var handler = new GetPlansConfigQueryHandler();

        var result = await handler.Handle(new GetPlansConfigQuery(), CancellationToken.None);

        result.TrialDays.Should().Be(7);
        result.Plans.Should().BeEquivalentTo(new[] { "monthly", "annual" });
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
cd backend
dotnet test tests/Awaken.UnitTests --filter "GetPlansConfigQueryHandlerTests"
```

Expected: FAIL with compile error (types not found).

- [ ] **Step 3: Create the contract DTO**

```csharp
// backend/src/Awaken.Contracts/AppConfig/PlansConfigResponse.cs
namespace Awaken.Contracts.AppConfig;

public record PlansConfigResponse(int TrialDays, IReadOnlyList<string> Plans);
```

- [ ] **Step 4: Create the query record**

```csharp
// backend/src/Awaken.Application/AppConfig/Queries/GetPlansConfig/GetPlansConfigQuery.cs
using Awaken.Contracts.AppConfig;
using MediatR;

namespace Awaken.Application.AppConfig.Queries.GetPlansConfig;

public record GetPlansConfigQuery : IRequest<PlansConfigResponse>;
```

- [ ] **Step 5: Create the query handler**

```csharp
// backend/src/Awaken.Application/AppConfig/Queries/GetPlansConfig/GetPlansConfigQueryHandler.cs
using Awaken.Contracts.AppConfig;
using MediatR;

namespace Awaken.Application.AppConfig.Queries.GetPlansConfig;

public class GetPlansConfigQueryHandler : IRequestHandler<GetPlansConfigQuery, PlansConfigResponse>
{
    private static readonly IReadOnlyList<string> _plans = ["monthly", "annual"];

    public Task<PlansConfigResponse> Handle(GetPlansConfigQuery request, CancellationToken cancellationToken)
        => Task.FromResult(new PlansConfigResponse(TrialDays: 7, Plans: _plans));
}
```

- [ ] **Step 6: Run test to verify it passes**

```bash
cd backend
dotnet test tests/Awaken.UnitTests --filter "GetPlansConfigQueryHandlerTests"
```

Expected: PASS (1 test).

---

## Task 2: Backend — Controller + Integration Test

**Files:**
- Create: `backend/src/Awaken.Api/Controllers/V1/AppConfigController.cs`
- Create: `backend/tests/Awaken.IntegrationTests/AppConfigEndpointTests.cs`

- [ ] **Step 1: Write the integration test**

```csharp
// backend/tests/Awaken.IntegrationTests/AppConfigEndpointTests.cs
using System.Net;
using System.Net.Http.Json;
using Awaken.Contracts.AppConfig;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Awaken.IntegrationTests;

public class AppConfigEndpointTests
{
    private readonly HttpClient _client;

    public AppConfigEndpointTests()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseEnvironment("Development"));
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
    }

    [Fact]
    public async Task GetPlansReturns200WithTrialDays7AndTwoPlans()
    {
        var response = await _client.GetAsync("/api/app-config/plans");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PlansConfigResponse>();
        body!.TrialDays.Should().Be(7);
        body.Plans.Should().BeEquivalentTo(new[] { "monthly", "annual" });
    }

    [Fact]
    public async Task GetPlansDoesNotRequireAuthentication()
    {
        // No Authorization header — must still return 200.
        var response = await _client.GetAsync("/api/app-config/plans?platform=android&locale=pt-BR");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
cd backend
dotnet test tests/Awaken.IntegrationTests --filter "AppConfigEndpointTests"
```

Expected: FAIL with 404 (route not registered).

- [ ] **Step 3: Create the controller**

```csharp
// backend/src/Awaken.Api/Controllers/V1/AppConfigController.cs
using Awaken.Application.AppConfig.Queries.GetPlansConfig;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Awaken.Api.Controllers.V1;

[ApiController]
[Route("api/app-config")]
public class AppConfigController(IMediator mediator) : ControllerBase
{
    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans(CancellationToken ct)
    {
        var result = await mediator.Send(new GetPlansConfigQuery(), ct);
        return Ok(result);
    }
}
```

- [ ] **Step 4: Run tests to verify both pass**

```bash
cd backend
dotnet test tests/Awaken.IntegrationTests --filter "AppConfigEndpointTests"
```

Expected: PASS (2 tests).

- [ ] **Step 5: Run full backend test suite to confirm no regressions**

```bash
cd backend
dotnet test
```

Expected: All tests pass.

---

## Task 3: Flutter — L10n Keys Update

**Files:**
- Modify: `apps/mobile/lib/l10n/app_pt.arb`
- Modify: `apps/mobile/lib/l10n/app_en.arb`
- Modify: `apps/mobile/lib/l10n/app_es.arb`

> **Important:** The dart l10n files (`app_localizations*.dart`) are auto-generated. Only edit the ARB files, then run `flutter gen-l10n`. Do NOT manually edit the generated dart files.

- [ ] **Step 1: Rewrite `app_pt.arb` — remove old freemium pricing keys, add trial offer keys**

Replace the `"pricingTitle"` through `"pricingStartPremium"` block with the following trial offer keys (keep everything else untouched):

```json
  "trialOfferTitle": "Teste grátis por 7 dias",
  "@trialOfferTitle": { "description": "Título principal da tela de proposta do trial" },

  "trialOfferSubtitle": "Acesse tudo do S-Rank sem precisar de cartão de crédito. Após o teste, você escolhe continuar.",

  "trialNoCreditCard": "Sem cartão de crédito",

  "planMonthlyTitle": "Mensal",
  "planMonthlyPrice": "R$ 14,90/mês",

  "planAnnualTitle": "Anual",
  "planAnnualPrice": "R$ 99,90/ano",
  "planAnnualEquivalent": "equivale a R$ 8,32/mês",
  "planAnnualDiscount": "45% de desconto",
  "planAnnualBadge": "Melhor valor",

  "trialStartButton": "Começar teste gratuito",
  "trialPlansNote": "Após os 7 dias, escolha o plano ou cancele.",
  "trialOfferFallbackNote": "Não foi possível carregar os detalhes dos planos.",
```

The keys to REMOVE from `app_pt.arb`:
- `pricingTitle`, `pricingFreeTitle`, `pricingPremiumTitle`
- `pricingFreeFeature1`, `pricingFreeFeature2`, `pricingFreeFeature3`, `pricingFreeFeature4`
- `pricingPremiumFeature1`, `pricingPremiumFeature2`, `pricingPremiumFeature3`, `pricingPremiumFeature4`, `pricingPremiumFeature5`
- `pricingContinueFree`, `pricingStartPremium`

- [ ] **Step 2: Update `app_en.arb` with equivalent English trial offer keys (same keys, English values)**

Replace same removed keys with:

```json
  "trialOfferTitle": "7-day free trial",
  "@trialOfferTitle": { "description": "Main heading of the trial offer screen" },

  "trialOfferSubtitle": "Access everything in S-Rank with no credit card required. After the trial, you choose whether to continue.",

  "trialNoCreditCard": "No credit card required",

  "planMonthlyTitle": "Monthly",
  "planMonthlyPrice": "R$ 14.90/month",

  "planAnnualTitle": "Annual",
  "planAnnualPrice": "R$ 99.90/year",
  "planAnnualEquivalent": "equals R$ 8.32/month",
  "planAnnualDiscount": "45% discount",
  "planAnnualBadge": "Best value",

  "trialStartButton": "Start free trial",
  "trialPlansNote": "After 7 days, pick a plan or cancel.",
  "trialOfferFallbackNote": "Could not load plan details.",
```

- [ ] **Step 3: Update `app_es.arb` with equivalent Spanish trial offer keys**

```json
  "trialOfferTitle": "Prueba gratis por 7 días",
  "@trialOfferTitle": { "description": "Título principal de la pantalla de oferta del período de prueba" },

  "trialOfferSubtitle": "Accede a todo el S-Rank sin necesidad de tarjeta de crédito. Después del período de prueba, decides si quieres continuar.",

  "trialNoCreditCard": "Sin tarjeta de crédito",

  "planMonthlyTitle": "Mensual",
  "planMonthlyPrice": "R$ 14,90/mes",

  "planAnnualTitle": "Anual",
  "planAnnualPrice": "R$ 99,90/año",
  "planAnnualEquivalent": "equivale a R$ 8,32/mes",
  "planAnnualDiscount": "45% de descuento",
  "planAnnualBadge": "Mejor valor",

  "trialStartButton": "Comenzar prueba gratuita",
  "trialPlansNote": "Después de 7 días, elige un plan o cancela.",
  "trialOfferFallbackNote": "No se pudieron cargar los detalles de los planes.",
```

- [ ] **Step 4: Run gen-l10n to regenerate dart files**

```bash
cd apps/mobile
flutter gen-l10n
```

Expected: No errors. The generated `app_localizations*.dart` files now contain the new getters.

- [ ] **Step 5: Run the ARB parity test**

```bash
cd apps/mobile
flutter test test/l10n/arb_parity_test.dart
```

Expected: PASS (3 tests — all 3 ARBs have identical keys with non-empty values).

---

## Task 4: Flutter — Data Layer

**Files:**
- Create: `apps/mobile/lib/features/pricing/data/dtos/plans_config_dto.dart`
- Create: `apps/mobile/lib/features/pricing/data/datasources/app_config_remote_data_source.dart`
- Create: `apps/mobile/lib/features/pricing/domain/repositories/app_config_repository.dart`
- Create: `apps/mobile/lib/features/pricing/data/repositories/app_config_repository_impl.dart`
- Create: `apps/mobile/test/features/pricing/data/datasources/app_config_remote_data_source_test.dart`

- [ ] **Step 1: Write the data source test**

```dart
// apps/mobile/test/features/pricing/data/datasources/app_config_remote_data_source_test.dart
import 'dart:convert';
import 'dart:typed_data';

import 'package:awaken/features/pricing/data/datasources/app_config_remote_data_source.dart';
import 'package:awaken/features/pricing/data/dtos/plans_config_dto.dart';
import 'package:awaken/core/errors/app_error.dart';
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
  @override
  Future<ResponseBody> fetch(RequestOptions options, Stream<Uint8List>? req, Future<void>? cancel) async {
    throw DioException(
      requestOptions: options,
      type: DioExceptionType.connectionError,
    );
  }

  @override
  void close({bool force = false}) {}
}

void main() {
  Dio _buildDio(HttpClientAdapter adapter) {
    final dio = Dio(BaseOptions(baseUrl: 'https://api.test'));
    dio.httpClientAdapter = adapter;
    return dio;
  }

  group('AppConfigRemoteDataSource.getPlansConfig', () {
    test('sends GET to /api/app-config/plans', () async {
      RequestOptions? captured;
      final adapter = _FakeAdapter(
        statusCode: 200,
        body: {'trialDays': 7, 'plans': ['monthly', 'annual']},
      );
      // Intercept to capture options.
      final dio = Dio(BaseOptions(baseUrl: 'https://api.test'));
      dio.httpClientAdapter = _CapturingAdapter(
        next: adapter,
        onCapture: (o) => captured = o,
      );
      final dataSource = AppConfigRemoteDataSource(dio);

      await dataSource.getPlansConfig();

      expect(captured?.method, 'GET');
      expect(captured?.path, '/api/app-config/plans');
    });

    test('parses 200 response into PlansConfigDto', () async {
      final adapter = _FakeAdapter(
        statusCode: 200,
        body: {'trialDays': 7, 'plans': ['monthly', 'annual']},
      );
      final dataSource = AppConfigRemoteDataSource(_buildDio(adapter));

      final result = await dataSource.getPlansConfig();

      expect(result.trialDays, 7);
      expect(result.plans, ['monthly', 'annual']);
    });

    test('throws NetworkError on connection failure', () async {
      final dataSource = AppConfigRemoteDataSource(_buildDio(_ErrorAdapter()));

      expect(dataSource.getPlansConfig(), throwsA(isA<NetworkError>()));
    });
  });
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
```

- [ ] **Step 2: Run test to verify it fails**

```bash
cd apps/mobile
flutter test test/features/pricing/data/datasources/app_config_remote_data_source_test.dart
```

Expected: FAIL (types not found).

- [ ] **Step 3: Create the DTO**

```dart
// apps/mobile/lib/features/pricing/data/dtos/plans_config_dto.dart
class PlansConfigDto {
  const PlansConfigDto({required this.trialDays, required this.plans});

  final int trialDays;
  final List<String> plans;

  factory PlansConfigDto.fromJson(Map<String, dynamic> json) => PlansConfigDto(
        trialDays: json['trialDays'] as int,
        plans: List<String>.from(json['plans'] as List),
      );

  static PlansConfigDto get fallback =>
      const PlansConfigDto(trialDays: 7, plans: ['monthly', 'annual']);
}
```

- [ ] **Step 4: Create the data source**

```dart
// apps/mobile/lib/features/pricing/data/datasources/app_config_remote_data_source.dart
import 'package:dio/dio.dart';
import '../../../../core/errors/app_error.dart';
import '../dtos/plans_config_dto.dart';

class AppConfigRemoteDataSource {
  const AppConfigRemoteDataSource(this._dio);
  final Dio _dio;

  Future<PlansConfigDto> getPlansConfig() async {
    try {
      final response = await _dio.get('/api/app-config/plans');
      return PlansConfigDto.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      if (e.type == DioExceptionType.connectionTimeout ||
          e.type == DioExceptionType.sendTimeout ||
          e.type == DioExceptionType.receiveTimeout ||
          e.type == DioExceptionType.connectionError) {
        throw const NetworkError();
      }
      throw const UnexpectedError();
    }
  }
}
```

- [ ] **Step 5: Create the abstract repository**

```dart
// apps/mobile/lib/features/pricing/domain/repositories/app_config_repository.dart
import '../../data/dtos/plans_config_dto.dart';

abstract class AppConfigRepository {
  Future<PlansConfigDto> getPlansConfig();
}
```

- [ ] **Step 6: Create the repository implementation**

```dart
// apps/mobile/lib/features/pricing/data/repositories/app_config_repository_impl.dart
import '../../domain/repositories/app_config_repository.dart';
import '../datasources/app_config_remote_data_source.dart';
import '../dtos/plans_config_dto.dart';

class AppConfigRepositoryImpl implements AppConfigRepository {
  const AppConfigRepositoryImpl(this._dataSource);
  final AppConfigRemoteDataSource _dataSource;

  @override
  Future<PlansConfigDto> getPlansConfig() => _dataSource.getPlansConfig();
}
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
cd apps/mobile
flutter test test/features/pricing/data/datasources/app_config_remote_data_source_test.dart
```

Expected: PASS (3 tests).

---

## Task 5: Flutter — State, Controller + Providers

**Files:**
- Create: `apps/mobile/lib/features/pricing/presentation/providers/trial_offer_state.dart`
- Create: `apps/mobile/lib/features/pricing/presentation/providers/trial_offer_controller.dart`
- Create: `apps/mobile/lib/features/pricing/presentation/providers/pricing_providers.dart`
- Create: `apps/mobile/test/features/pricing/presentation/providers/trial_offer_controller_test.dart`

- [ ] **Step 1: Write the failing controller test**

```dart
// apps/mobile/test/features/pricing/presentation/providers/trial_offer_controller_test.dart
import 'package:awaken/core/analytics/analytics_service.dart';
import 'package:awaken/core/analytics/analytics_provider.dart';
import 'package:awaken/core/errors/app_error.dart';
import 'package:awaken/features/pricing/data/dtos/plans_config_dto.dart';
import 'package:awaken/features/pricing/domain/repositories/app_config_repository.dart';
import 'package:awaken/features/pricing/presentation/providers/trial_offer_controller.dart';
import 'package:awaken/features/pricing/presentation/providers/trial_offer_state.dart';
import 'package:awaken/features/pricing/presentation/providers/pricing_providers.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

class _FakeRepository implements AppConfigRepository {
  _FakeRepository({required this.result});
  final Future<PlansConfigDto> result;

  @override
  Future<PlansConfigDto> getPlansConfig() => result;
}

class _FakeAnalytics implements AnalyticsService {
  final List<String> events = [];

  @override
  Future<void> logEvent(String name, {Map<String, Object>? params}) async {
    events.add(name);
  }
}

ProviderContainer _buildContainer({
  required AppConfigRepository repository,
  required _FakeAnalytics analytics,
}) {
  return ProviderContainer(overrides: [
    appConfigRepositoryProvider.overrideWithValue(repository),
    analyticsServiceProvider.overrideWithValue(analytics),
  ]);
}

void main() {
  group('TrialOfferController', () {
    test('initial state is TrialOfferLoading', () {
      final analytics = _FakeAnalytics();
      final container = _buildContainer(
        repository: _FakeRepository(
          result: Future.delayed(const Duration(seconds: 10), PlansConfigDto.fallback),
        ),
        analytics: analytics,
      );
      addTearDown(container.dispose);

      expect(container.read(trialOfferControllerProvider), isA<TrialOfferLoading>());
    });

    test('state transitions to TrialOfferLoaded on success', () async {
      final analytics = _FakeAnalytics();
      const dto = PlansConfigDto(trialDays: 7, plans: ['monthly', 'annual']);
      final container = _buildContainer(
        repository: _FakeRepository(result: Future.value(dto)),
        analytics: analytics,
      );
      addTearDown(container.dispose);

      // Trigger load by reading the controller.
      container.read(trialOfferControllerProvider.notifier).load();
      await Future.microtask(() {});
      await Future.microtask(() {});

      final state = container.read(trialOfferControllerProvider);
      expect(state, isA<TrialOfferLoaded>());
      final loaded = state as TrialOfferLoaded;
      expect(loaded.trialDays, 7);
      expect(loaded.plans, ['monthly', 'annual']);
    });

    test('state transitions to TrialOfferFallback on network error', () async {
      final analytics = _FakeAnalytics();
      final container = _buildContainer(
        repository: _FakeRepository(result: Future.error(const NetworkError())),
        analytics: analytics,
      );
      addTearDown(container.dispose);

      container.read(trialOfferControllerProvider.notifier).load();
      await Future.microtask(() {});
      await Future.microtask(() {});

      expect(container.read(trialOfferControllerProvider), isA<TrialOfferFallback>());
    });

    test('logs trial_offer_viewed and plans_viewed on successful load', () async {
      final analytics = _FakeAnalytics();
      const dto = PlansConfigDto(trialDays: 7, plans: ['monthly', 'annual']);
      final container = _buildContainer(
        repository: _FakeRepository(result: Future.value(dto)),
        analytics: analytics,
      );
      addTearDown(container.dispose);

      container.read(trialOfferControllerProvider.notifier).load();
      await Future.microtask(() {});
      await Future.microtask(() {});

      expect(analytics.events, containsAll(['trial_offer_viewed', 'plans_viewed']));
    });

    test('logs trial_offer_viewed even on fallback', () async {
      final analytics = _FakeAnalytics();
      final container = _buildContainer(
        repository: _FakeRepository(result: Future.error(const NetworkError())),
        analytics: analytics,
      );
      addTearDown(container.dispose);

      container.read(trialOfferControllerProvider.notifier).load();
      await Future.microtask(() {});
      await Future.microtask(() {});

      expect(analytics.events, contains('trial_offer_viewed'));
    });
  });
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
cd apps/mobile
flutter test test/features/pricing/presentation/providers/trial_offer_controller_test.dart
```

Expected: FAIL (types not found).

- [ ] **Step 3: Create the state sealed classes**

```dart
// apps/mobile/lib/features/pricing/presentation/providers/trial_offer_state.dart
sealed class TrialOfferState {
  const TrialOfferState();
}

final class TrialOfferLoading extends TrialOfferState {
  const TrialOfferLoading();
}

final class TrialOfferLoaded extends TrialOfferState {
  const TrialOfferLoaded({required this.trialDays, required this.plans});
  final int trialDays;
  final List<String> plans;
}

final class TrialOfferFallback extends TrialOfferState {
  const TrialOfferFallback();
}
```

- [ ] **Step 4: Create the controller**

```dart
// apps/mobile/lib/features/pricing/presentation/providers/trial_offer_controller.dart
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/analytics/analytics_provider.dart';
import '../../data/dtos/plans_config_dto.dart';
import '../../domain/repositories/app_config_repository.dart';
import 'pricing_providers.dart';
import 'trial_offer_state.dart';

class TrialOfferController extends Notifier<TrialOfferState> {
  @override
  TrialOfferState build() => const TrialOfferLoading();

  Future<void> load() async {
    final analytics = ref.read(analyticsServiceProvider);
    final repository = ref.read(appConfigRepositoryProvider);

    try {
      final dto = await repository.getPlansConfig();
      await analytics.logEvent('trial_offer_viewed');
      await analytics.logEvent('plans_viewed');
      state = TrialOfferLoaded(trialDays: dto.trialDays, plans: dto.plans);
    } catch (_) {
      await analytics.logEvent('trial_offer_viewed');
      state = const TrialOfferFallback();
    }
  }
}

final trialOfferControllerProvider =
    NotifierProvider<TrialOfferController, TrialOfferState>(
        TrialOfferController.new);
```

- [ ] **Step 5: Create the providers wiring file**

```dart
// apps/mobile/lib/features/pricing/presentation/providers/pricing_providers.dart
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/network/dio_client.dart';
import '../../data/datasources/app_config_remote_data_source.dart';
import '../../data/repositories/app_config_repository_impl.dart';
import '../../domain/repositories/app_config_repository.dart';

final appConfigRemoteDataSourceProvider =
    Provider<AppConfigRemoteDataSource>((ref) {
  return AppConfigRemoteDataSource(ref.watch(unauthenticatedDioProvider));
});

final appConfigRepositoryProvider = Provider<AppConfigRepository>((ref) {
  return AppConfigRepositoryImpl(ref.watch(appConfigRemoteDataSourceProvider));
});
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
cd apps/mobile
flutter test test/features/pricing/presentation/providers/trial_offer_controller_test.dart
```

Expected: PASS (5 tests).

---

## Task 6: Flutter — PricingPage Rewrite + Widget Tests

**Files:**
- Modify: `apps/mobile/lib/features/pricing/presentation/pages/pricing_page.dart`
- Modify: `apps/mobile/test/features/pricing/presentation/pages/pricing_page_test.dart`

- [ ] **Step 1: Rewrite `pricing_page.dart`**

```dart
// apps/mobile/lib/features/pricing/presentation/pages/pricing_page.dart
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:awaken/l10n/app_localizations.dart';
import '../../../../app/app_router.dart';
import '../../../../design_system/components/awaken_button.dart';
import '../../../../design_system/components/awaken_error_state.dart';
import '../../../../design_system/components/awaken_loading_state.dart';
import '../../../../design_system/components/awaken_particles_layer.dart';
import '../../../../design_system/components/language_selector_overlay.dart';
import '../../../../design_system/tokens/colors.dart';
import '../../../../design_system/tokens/spacing.dart';
import '../../../../design_system/tokens/typography.dart';
import '../providers/trial_offer_controller.dart';
import '../providers/trial_offer_state.dart';

class PricingPage extends ConsumerStatefulWidget {
  const PricingPage({super.key});

  @override
  ConsumerState<PricingPage> createState() => _PricingPageState();
}

class _PricingPageState extends ConsumerState<PricingPage> {
  @override
  void initState() {
    super.initState();
    // Kick off the config fetch after the first frame.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(trialOfferControllerProvider.notifier).load();
    });
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final state = ref.watch(trialOfferControllerProvider);

    return Scaffold(
      key: const Key('pricing-page'),
      backgroundColor: AwakenColors.backgroundPrimary,
      body: Stack(
        children: [
          const AwakenParticlesLayer(),
          LanguageSelectorOverlay(
            body: SafeArea(
              child: switch (state) {
                TrialOfferLoading() => const AwakenLoadingState(),
                TrialOfferLoaded() => _TrialOfferContent(l10n: l10n, showFallbackNote: false),
                TrialOfferFallback() => _TrialOfferContent(l10n: l10n, showFallbackNote: true),
              },
            ),
          ),
        ],
      ),
    );
  }
}

class _TrialOfferContent extends StatelessWidget {
  const _TrialOfferContent({required this.l10n, required this.showFallbackNote});

  final AppLocalizations l10n;
  final bool showFallbackNote;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.symmetric(
        horizontal: AwakenSpacing.lg,
        vertical: AwakenSpacing.xl,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const SizedBox(height: AwakenSpacing.xl),
          // No credit card badge
          Center(
            child: Container(
              key: const Key('trial-no-credit-card-badge'),
              padding: const EdgeInsets.symmetric(
                horizontal: AwakenSpacing.md,
                vertical: AwakenSpacing.xs,
              ),
              decoration: BoxDecoration(
                color: AwakenColors.primary.withValues(alpha: 0.15),
                borderRadius: BorderRadius.circular(20),
                border: Border.all(color: AwakenColors.primary, width: 1),
              ),
              child: Text(
                l10n.trialNoCreditCard,
                style: AwakenTypography.labelSmall.copyWith(
                  color: AwakenColors.primary,
                ),
              ),
            ),
          ),
          const SizedBox(height: AwakenSpacing.lg),
          // Main title
          Text(
            l10n.trialOfferTitle,
            key: const Key('trial-offer-title'),
            textAlign: TextAlign.center,
            style: AwakenTypography.displayMedium,
          ),
          const SizedBox(height: AwakenSpacing.md),
          // Subtitle
          Text(
            l10n.trialOfferSubtitle,
            textAlign: TextAlign.center,
            style: AwakenTypography.bodyMedium.copyWith(
              color: AwakenColors.textSecondary,
            ),
          ),
          const SizedBox(height: AwakenSpacing.xl),
          // Plan cards
          _PlanCard(
            key: const Key('plan-card-monthly'),
            title: l10n.planMonthlyTitle,
            price: l10n.planMonthlyPrice,
            subtitle: null,
            badge: null,
            isHighlighted: false,
          ),
          const SizedBox(height: AwakenSpacing.md),
          _PlanCard(
            key: const Key('plan-card-annual'),
            title: l10n.planAnnualTitle,
            price: l10n.planAnnualPrice,
            subtitle: l10n.planAnnualEquivalent,
            badge: l10n.planAnnualBadge,
            discount: l10n.planAnnualDiscount,
            isHighlighted: true,
          ),
          const SizedBox(height: AwakenSpacing.lg),
          if (showFallbackNote)
            Padding(
              padding: const EdgeInsets.only(bottom: AwakenSpacing.md),
              child: Text(
                l10n.trialOfferFallbackNote,
                key: const Key('trial-fallback-note'),
                textAlign: TextAlign.center,
                style: AwakenTypography.bodySmall.copyWith(
                  color: AwakenColors.textSecondary,
                ),
              ),
            ),
          // CTA
          AwakenButton(
            key: const Key('trial-start-button'),
            label: l10n.trialStartButton,
            onPressed: () => context.go(AppRoutes.register),
          ),
          const SizedBox(height: AwakenSpacing.md),
          Text(
            l10n.trialPlansNote,
            textAlign: TextAlign.center,
            style: AwakenTypography.bodySmall.copyWith(
              color: AwakenColors.textSecondary,
            ),
          ),
        ],
      ),
    );
  }
}

class _PlanCard extends StatelessWidget {
  const _PlanCard({
    super.key,
    required this.title,
    required this.price,
    this.subtitle,
    this.badge,
    this.discount,
    required this.isHighlighted,
  });

  final String title;
  final String price;
  final String? subtitle;
  final String? badge;
  final String? discount;
  final bool isHighlighted;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(AwakenSpacing.md),
      decoration: BoxDecoration(
        color: isHighlighted
            ? AwakenColors.primary.withValues(alpha: 0.08)
            : AwakenColors.surfaceCard,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: isHighlighted ? AwakenColors.primary : AwakenColors.borderSubtle,
          width: isHighlighted ? 1.5 : 1,
        ),
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(title, style: AwakenTypography.titleSmall),
                if (subtitle != null) ...[
                  const SizedBox(height: 2),
                  Text(
                    subtitle!,
                    style: AwakenTypography.bodySmall.copyWith(
                      color: AwakenColors.textSecondary,
                    ),
                  ),
                ],
              ],
            ),
          ),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              if (badge != null)
                Container(
                  padding: const EdgeInsets.symmetric(
                      horizontal: AwakenSpacing.sm, vertical: 2),
                  decoration: BoxDecoration(
                    color: AwakenColors.primary,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(
                    badge!,
                    style: AwakenTypography.labelSmall.copyWith(
                      color: AwakenColors.backgroundPrimary,
                    ),
                  ),
                ),
              if (badge != null) const SizedBox(height: 4),
              Text(price, style: AwakenTypography.titleMedium),
              if (discount != null)
                Text(
                  discount!,
                  style: AwakenTypography.labelSmall.copyWith(
                    color: AwakenColors.primary,
                  ),
                ),
            ],
          ),
        ],
      ),
    );
  }
}
```

- [ ] **Step 2: Rewrite `pricing_page_test.dart`**

```dart
// apps/mobile/test/features/pricing/presentation/pages/pricing_page_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:awaken/app/app_router.dart';
import 'package:awaken/core/analytics/analytics_provider.dart';
import 'package:awaken/core/analytics/no_op_analytics_service.dart';
import 'package:awaken/design_system/components/awaken_loading_state.dart';
import 'package:awaken/design_system/components/language_selector.dart';
import 'package:awaken/features/auth/presentation/pages/register_page.dart';
import 'package:awaken/features/pricing/presentation/pages/pricing_page.dart';
import 'package:awaken/features/pricing/presentation/providers/pricing_providers.dart';
import 'package:awaken/features/pricing/presentation/providers/trial_offer_controller.dart';
import 'package:awaken/features/pricing/presentation/providers/trial_offer_state.dart';
import 'package:awaken/l10n/app_localizations.dart';

GoRouter _buildRouter() => GoRouter(
      initialLocation: AppRoutes.pricingIntro,
      routes: [
        GoRoute(
          path: AppRoutes.pricingIntro,
          builder: (_, __) => const PricingPage(),
        ),
        GoRoute(
          path: AppRoutes.register,
          builder: (_, __) => const RegisterPage(),
        ),
      ],
    );

Widget _buildApp({List<Override> overrides = const []}) {
  return TickerMode(
    enabled: false,
    child: ProviderScope(
      overrides: [
        analyticsServiceProvider.overrideWithValue(NoOpAnalyticsService()),
        ...overrides,
      ],
      child: MaterialApp.router(
        routerConfig: _buildRouter(),
        theme: ThemeData.dark(useMaterial3: false),
        darkTheme: ThemeData.dark(useMaterial3: false),
        themeMode: ThemeMode.dark,
        localizationsDelegates: const [
          AppLocalizations.delegate,
          GlobalMaterialLocalizations.delegate,
          GlobalWidgetsLocalizations.delegate,
          GlobalCupertinoLocalizations.delegate,
        ],
        supportedLocales: const [Locale('pt', 'BR')],
        locale: const Locale('pt', 'BR'),
      ),
    ),
  );
}

Override _stateOverride(TrialOfferState state) =>
    trialOfferControllerProvider.overrideWith(() {
      final ctrl = TrialOfferController();
      // Use a fixed state; disable auto-load by overriding build only.
      return ctrl;
    });

void main() {
  group('PricingPage — estado loading', () {
    testWidgets('exibe AwakenLoadingState enquanto carrega', (tester) async {
      // The controller starts in TrialOfferLoading; do not call .load() so it stays loading.
      await tester.pumpWidget(_buildApp(overrides: [
        appConfigRepositoryProvider.overrideWith((ref) => throw UnimplementedError()),
      ]));
      await tester.pump(); // Process initState scheduling.

      // Do NOT pumpAndSettle — stay in loading state.
      expect(find.byType(AwakenLoadingState), findsOneWidget);
    });
  });

  group('PricingPage — estado carregado', () {
    setUp(() {});

    testWidgets('CA-001: exibe título e subtítulo do trial', (tester) async {
      await tester.pumpWidget(_buildApp(overrides: [
        trialOfferControllerProvider.overrideWith(
          () => _FixedController(const TrialOfferLoaded(trialDays: 7, plans: ['monthly', 'annual'])),
        ),
      ]));
      await tester.pump();

      expect(find.byKey(const Key('trial-offer-title')), findsOneWidget);
      expect(find.text('Teste grátis por 7 dias'), findsOneWidget);
    });

    testWidgets('CA-002: sem menção a plano gratuito permanente — badge exibe sem cartão', (tester) async {
      await tester.pumpWidget(_buildApp(overrides: [
        trialOfferControllerProvider.overrideWith(
          () => _FixedController(const TrialOfferLoaded(trialDays: 7, plans: ['monthly', 'annual'])),
        ),
      ]));
      await tester.pump();

      expect(find.byKey(const Key('trial-no-credit-card-badge')), findsOneWidget);
      expect(find.text('Sem cartão de crédito'), findsOneWidget);
      // Confirms no "Free Hunter" or "Continuar grátis" style text.
      expect(find.text('Free Hunter'), findsNothing);
      expect(find.text('Continuar grátis'), findsNothing);
    });

    testWidgets('exibe card de plano mensal e anual', (tester) async {
      await tester.pumpWidget(_buildApp(overrides: [
        trialOfferControllerProvider.overrideWith(
          () => _FixedController(const TrialOfferLoaded(trialDays: 7, plans: ['monthly', 'annual'])),
        ),
      ]));
      await tester.pump();

      expect(find.byKey(const Key('plan-card-monthly')), findsOneWidget);
      expect(find.byKey(const Key('plan-card-annual')), findsOneWidget);
      expect(find.text('R\$ 14,90/mês'), findsOneWidget);
      expect(find.text('R\$ 99,90/ano'), findsOneWidget);
      expect(find.text('45% de desconto'), findsOneWidget);
      expect(find.text('Melhor valor'), findsOneWidget);
    });

    testWidgets('não exibe nota de fallback quando carregado', (tester) async {
      await tester.pumpWidget(_buildApp(overrides: [
        trialOfferControllerProvider.overrideWith(
          () => _FixedController(const TrialOfferLoaded(trialDays: 7, plans: ['monthly', 'annual'])),
        ),
      ]));
      await tester.pump();

      expect(find.byKey(const Key('trial-fallback-note')), findsNothing);
    });

    testWidgets('CA-003: CTA navega para cadastro ao tocar', (tester) async {
      await tester.pumpWidget(_buildApp(overrides: [
        trialOfferControllerProvider.overrideWith(
          () => _FixedController(const TrialOfferLoaded(trialDays: 7, plans: ['monthly', 'annual'])),
        ),
      ]));
      await tester.pump();

      await tester.tap(find.byKey(const Key('trial-start-button')));
      await tester.pumpAndSettle();

      // RegisterPage has a specific key on its first field.
      expect(find.byKey(const Key('register-nationality')), findsOneWidget);
    });
  });

  group('PricingPage — estado fallback', () {
    testWidgets('exibe conteúdo do trial mesmo com fallback', (tester) async {
      await tester.pumpWidget(_buildApp(overrides: [
        trialOfferControllerProvider.overrideWith(
          () => _FixedController(const TrialOfferFallback()),
        ),
      ]));
      await tester.pump();

      expect(find.byKey(const Key('trial-offer-title')), findsOneWidget);
      expect(find.byKey(const Key('trial-start-button')), findsOneWidget);
    });

    testWidgets('exibe nota de fallback quando config falhou', (tester) async {
      await tester.pumpWidget(_buildApp(overrides: [
        trialOfferControllerProvider.overrideWith(
          () => _FixedController(const TrialOfferFallback()),
        ),
      ]));
      await tester.pump();

      expect(find.byKey(const Key('trial-fallback-note')), findsOneWidget);
      expect(
        find.text('Não foi possível carregar os detalhes dos planos.'),
        findsOneWidget,
      );
    });
  });

  group('PricingPage — i18n', () {
    testWidgets('exibe seletor de idioma', (tester) async {
      await tester.pumpWidget(_buildApp(overrides: [
        trialOfferControllerProvider.overrideWith(
          () => _FixedController(const TrialOfferLoaded(trialDays: 7, plans: ['monthly', 'annual'])),
        ),
      ]));
      await tester.pump();

      expect(find.byType(LanguageSelector), findsOneWidget);
    });
  });
}

/// Helper: a TrialOfferController that always emits a fixed state without loading.
class _FixedController extends TrialOfferController {
  _FixedController(this._fixed);
  final TrialOfferState _fixed;

  @override
  TrialOfferState build() => _fixed;

  @override
  Future<void> load() async {}
}
```

- [ ] **Step 3: Run the widget tests**

```bash
cd apps/mobile
flutter test test/features/pricing/presentation/pages/pricing_page_test.dart
```

Expected: PASS (all tests).

> If tests fail because of missing token constants like `AwakenColors.surfaceCard`, `AwakenColors.borderSubtle`, `AwakenColors.textSecondary`, `AwakenColors.primary`, or `AwakenSpacing.*` constants — check `lib/design_system/tokens/colors.dart` and `lib/design_system/tokens/spacing.dart`, then update the page widget to use the correct existing token names before re-running.

- [ ] **Step 4: Run full Flutter test suite**

```bash
cd apps/mobile
flutter test
```

Expected: All tests pass.

---

## Task 7: Flutter — Route Resolver Update

**Files:**
- Modify: `apps/mobile/lib/app/navigation/route_resolver.dart`
- Modify: `apps/mobile/test/app/navigation/route_resolver_test.dart`

- [ ] **Step 1: Update `route_resolver.dart` — visitor initial route → `pricingIntro`**

In `resolveInitialRoute`, change the visitor branch:

```dart
// Before:
if (!session.hasSession) return AppRoutes.login;

// After:
if (!session.hasSession) return AppRoutes.pricingIntro;
```

The rest of the function and `resolveRedirect` are unchanged — visitors trying to access protected routes via deep links still get redirected to `AppRoutes.login`.

- [ ] **Step 2: Update `route_resolver_test.dart` — fix the visitor initial route assertion**

Change the first test in `resolveInitialRoute` group:

```dart
// Before:
test('RN-001/CA-001: visitante sem sessão vai para rota inicial pública', () {
  expect(
    resolveInitialRoute(const SessionState.visitor()),
    AppRoutes.login,
  );
});

// After:
test('CA-001/US-014: visitante sem sessão vai para tela de proposta do trial', () {
  expect(
    resolveInitialRoute(const SessionState.visitor()),
    AppRoutes.pricingIntro,
  );
});
```

- [ ] **Step 3: Run route resolver tests**

```bash
cd apps/mobile
flutter test test/app/navigation/route_resolver_test.dart
```

Expected: PASS (all tests, including the no-loop anti-regression test).

- [ ] **Step 4: Run full Flutter test suite for final confirmation**

```bash
cd apps/mobile
flutter test
```

Expected: All tests pass, no regressions.

---

## Self-Review: Spec Coverage Checklist

| Requirement | Task |
|---|---|
| RN-001: Trial comunicado antes do onboarding | Task 7 (route resolver) |
| RN-002: Tela informa 7 dias de trial | Task 6 (UI + tests CA-001) |
| RN-003: Tela informa assinatura obrigatória após trial | Task 6 (subtitle + trialPlansNote) |
| RN-004: Plano mensal e anual apresentados | Task 6 (plan cards) |
| RN-005: Sem sugestão de plano gratuito permanente | Task 6 (CA-002 test) |
| RN-006: CTA leva ao fluxo de início de trial | Task 6 (CA-003 test → register) |
| Estado de carregamento | Task 6 (loading state test) |
| Estado de conteúdo carregado | Task 6 (loaded state tests) |
| Estado de fallback (planos indisponíveis) | Task 6 (fallback state tests) |
| Localização PT-BR, EN, ES | Task 3 (ARB parity test) |
| Analytics `trial_offer_viewed` | Task 5 (controller test) |
| Analytics `plans_viewed` | Task 5 (controller test) |
| Backend endpoint público | Task 2 (integration test: no auth) |
| Backend retorna trialDays=7 e plans | Task 1 + 2 |
| Fallback seguro sem endpoint | Task 5 (NetworkError → Fallback state) |
| Visitante pode acessar tela sem sessão | Task 7 (route guard) |
| 45% de desconto no plano anual visível | Task 6 (plan card annual + test) |

All requirements covered.
