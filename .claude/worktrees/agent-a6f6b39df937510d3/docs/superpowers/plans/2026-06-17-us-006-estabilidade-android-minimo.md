# US-006 — Estabilidade Android Mínimo — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Garantir minSdk Android explícito, captura global de crashes (Crashlytics-ready), tratamento de erro controlado em falha de boot, e smoke test e2e em tela mínima — sem quebrar nada do que US-001..005 já entregaram.

**Architecture:** Segue o padrão já existente de `AnalyticsService` (interface + NoOp + provider Riverpod) para criar `CrashReportingService`. Handlers globais (`FlutterError.onError`, `PlatformDispatcher.instance.onError`, `runZonedGuarded`) são extraídos para uma função pura testável em `core/bootstrap/`, evitando lógica não testável dentro de `main()`. Backend já tem `ExceptionHandlingMiddleware` com correlationId (ADR conforme CLAUDE.md) — falta apenas cobertura de teste.

**Tech Stack:** Flutter 3.32 / Dart >=3.4, Riverpod, firebase_crashlytics (já em pubspec, hoje não inicializado), go_router, integration_test, mocktail. Backend: .NET 10, xUnit, FluentAssertions.

---

## Task 1: minSdk explícito no Android

**Files:**
- Modify: `apps/mobile/android/app/build.gradle.kts`

- [ ] **Step 1:** No bloco `defaultConfig`, trocar `minSdk = flutter.minSdkVersion` por `minSdk = 23` (Android 6.0 — cobre Crashlytics e ~98% do parque ativo; valor documentado via RN-001).

```kotlin
defaultConfig {
    applicationId = "com.example.awaken"
    minSdk = 23
    targetSdk = flutter.targetSdkVersion
    versionCode = flutter.versionCode
    versionName = flutter.versionName
}
```

- [ ] **Step 2:** Validar build: `flutter build apk --debug` (working dir `apps/mobile`). Esperado: build verde, sem mudar comportamento de runtime.

---

## Task 2: `CrashReportingService` (interface + NoOp)

**Files:**
- Create: `apps/mobile/lib/core/crash_reporting/crash_reporting_service.dart`
- Create: `apps/mobile/lib/core/crash_reporting/no_op_crash_reporting_service.dart`
- Create: `apps/mobile/lib/core/crash_reporting/crash_reporting_provider.dart`
- Test: `apps/mobile/test/core/crash_reporting/no_op_crash_reporting_service_test.dart`

- [ ] **Step 1: interface**

```dart
// crash_reporting_service.dart
abstract class CrashReportingService {
  Future<void> recordError(
    Object error,
    StackTrace? stackTrace, {
    bool fatal = false,
  });
}
```

- [ ] **Step 2: NoOp**

```dart
// no_op_crash_reporting_service.dart
import 'crash_reporting_service.dart';

class NoOpCrashReportingService implements CrashReportingService {
  const NoOpCrashReportingService();

  @override
  Future<void> recordError(
    Object error,
    StackTrace? stackTrace, {
    bool fatal = false,
  }) async {}
}
```

- [ ] **Step 3: provider (mesmo padrão de `analytics_provider.dart`)**

```dart
// crash_reporting_provider.dart
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'crash_reporting_service.dart';
import 'no_op_crash_reporting_service.dart';

final crashReportingServiceProvider = Provider<CrashReportingService>((ref) {
  return const NoOpCrashReportingService();
});
```

- [ ] **Step 4: teste**

```dart
// no_op_crash_reporting_service_test.dart
import 'package:flutter_test/flutter_test.dart';
import 'package:awaken/core/crash_reporting/no_op_crash_reporting_service.dart';

void main() {
  test('recordError não lança e completa normalmente', () async {
    const service = NoOpCrashReportingService();
    await expectLater(
      service.recordError(Exception('boom'), StackTrace.current, fatal: true),
      completes,
    );
  });
}
```

- [ ] **Step 5:** `flutter test test/core/crash_reporting/no_op_crash_reporting_service_test.dart` → PASS.

---

## Task 3: `FirebaseCrashReportingService` (injetável/testável)

**Files:**
- Create: `apps/mobile/lib/core/crash_reporting/firebase_crash_reporting_service.dart`
- Test: `apps/mobile/test/core/crash_reporting/firebase_crash_reporting_service_test.dart`

- [ ] **Step 1:** implementação que recebe `FirebaseCrashlytics` por injeção (testável com mocktail, sem precisar inicializar plugin real).

```dart
import 'package:firebase_crashlytics/firebase_crashlytics.dart';
import 'crash_reporting_service.dart';

class FirebaseCrashReportingService implements CrashReportingService {
  const FirebaseCrashReportingService(this._crashlytics);

  final FirebaseCrashlytics _crashlytics;

  @override
  Future<void> recordError(
    Object error,
    StackTrace? stackTrace, {
    bool fatal = false,
  }) {
    return _crashlytics.recordError(error, stackTrace, fatal: fatal);
  }
}
```

- [ ] **Step 2:** teste com mock.

```dart
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:firebase_crashlytics/firebase_crashlytics.dart';
import 'package:awaken/core/crash_reporting/firebase_crash_reporting_service.dart';

class _MockCrashlytics extends Mock implements FirebaseCrashlytics {}

void main() {
  test('delega para FirebaseCrashlytics.recordError com fatal correto', () async {
    final crashlytics = _MockCrashlytics();
    when(() => crashlytics.recordError(any(), any(), fatal: any(named: 'fatal')))
        .thenAnswer((_) async {});

    final service = FirebaseCrashReportingService(crashlytics);
    final error = Exception('boom');
    final stack = StackTrace.current;

    await service.recordError(error, stack, fatal: true);

    verify(() => crashlytics.recordError(error, stack, fatal: true)).called(1);
  });
}
```

- [ ] **Step 3:** `flutter test test/core/crash_reporting/firebase_crash_reporting_service_test.dart` → PASS.

---

## Task 4: handlers globais testáveis (`core/bootstrap`)

**Files:**
- Create: `apps/mobile/lib/core/bootstrap/global_error_handlers.dart`
- Test: `apps/mobile/test/core/bootstrap/global_error_handlers_test.dart`

- [ ] **Step 1:** função pura que registra `FlutterError.onError` e retorna o callback de `runZonedGuarded`, delegando para `CrashReportingService` e disparando analytics `crash_detected`. Não chama `runApp` — só prepara os handlers, pra ser testável sem boot real do app.

```dart
import 'package:flutter/foundation.dart';
import '../analytics/analytics_service.dart';
import '../crash_reporting/crash_reporting_service.dart';

void registerGlobalErrorHandlers({
  required CrashReportingService crashReportingService,
  required AnalyticsService analyticsService,
}) {
  FlutterError.onError = (details) {
    crashReportingService.recordError(
      details.exception,
      details.stack,
      fatal: true,
    );
    analyticsService.logEvent('crash_detected', params: {
      'source': 'flutter_error',
    });
  };
}

Future<void> reportZoneError(
  Object error,
  StackTrace stack, {
  required CrashReportingService crashReportingService,
  required AnalyticsService analyticsService,
}) async {
  await crashReportingService.recordError(error, stack, fatal: true);
  await analyticsService.logEvent('crash_detected', params: {
    'source': 'zone',
  });
}
```

- [ ] **Step 2:** testes (mock de `CrashReportingService`/`AnalyticsService` via mocktail; disparar `FlutterError.onError` manualmente e checar chamada; chamar `reportZoneError` direto).

```dart
import 'package:flutter/foundation.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:awaken/core/analytics/analytics_service.dart';
import 'package:awaken/core/crash_reporting/crash_reporting_service.dart';
import 'package:awaken/core/bootstrap/global_error_handlers.dart';

class _MockCrash extends Mock implements CrashReportingService {}
class _MockAnalytics extends Mock implements AnalyticsService {}

void main() {
  late _MockCrash crash;
  late _MockAnalytics analytics;

  setUp(() {
    crash = _MockCrash();
    analytics = _MockAnalytics();
    when(() => crash.recordError(any(), any(), fatal: any(named: 'fatal')))
        .thenAnswer((_) async {});
    when(() => analytics.logEvent(any(), params: any(named: 'params')))
        .thenAnswer((_) async {});
  });

  test('FlutterError.onError encaminha para crash reporting e analytics', () {
    registerGlobalErrorHandlers(
      crashReportingService: crash,
      analyticsService: analytics,
    );

    final details = FlutterErrorDetails(
      exception: Exception('widget boom'),
      stack: StackTrace.current,
    );
    FlutterError.onError!(details);

    verify(() => crash.recordError(details.exception, details.stack, fatal: true))
        .called(1);
    verify(() => analytics.logEvent('crash_detected',
        params: {'source': 'flutter_error'})).called(1);
  });

  test('reportZoneError encaminha erro de zona não capturado', () async {
    final error = Exception('zone boom');
    final stack = StackTrace.current;

    await reportZoneError(error, stack,
        crashReportingService: crash, analyticsService: analytics);

    verify(() => crash.recordError(error, stack, fatal: true)).called(1);
    verify(() => analytics.logEvent('crash_detected',
        params: {'source': 'zone'})).called(1);
  });
}
```

- [ ] **Step 3:** `flutter test test/core/bootstrap/global_error_handlers_test.dart` → PASS.

---

## Task 5: ligar tudo em `main.dart`

**Files:**
- Modify: `apps/mobile/lib/main.dart`

- [ ] **Step 1:** inicializar Firebase de forma defensiva (sem `google-services.json` real ainda — projeto MVP, mantém TODO explícito igual ao já existente p/ RevenueCat) e escolher implementação de crash reporting/analytics conforme sucesso da inicialização.

```dart
import 'dart:async';
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_crashlytics/firebase_crashlytics.dart';
import 'package:flutter/material.dart';
import 'package:hooks_riverpod/hooks_riverpod.dart';
import 'app/awaken_app.dart';
import 'core/analytics/analytics_provider.dart';
import 'core/analytics/no_op_analytics_service.dart';
import 'core/bootstrap/global_error_handlers.dart';
import 'core/crash_reporting/crash_reporting_provider.dart';
import 'core/crash_reporting/crash_reporting_service.dart';
import 'core/crash_reporting/firebase_crash_reporting_service.dart';
import 'core/crash_reporting/no_op_crash_reporting_service.dart';
import 'core/localization/locale_controller.dart';

Future<CrashReportingService> _initCrashReporting() async {
  try {
    await Firebase.initializeApp();
    return FirebaseCrashReportingService(FirebaseCrashlytics.instance);
  } catch (_) {
    // TODO: remover fallback quando o projeto Firebase (google-services.json)
    // estiver configurado para todos os ambientes.
    return const NoOpCrashReportingService();
  }
}

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  final persistedLocale = await loadPersistedLocale();
  final crashReportingService = await _initCrashReporting();
  const analyticsService = NoOpAnalyticsService();

  registerGlobalErrorHandlers(
    crashReportingService: crashReportingService,
    analyticsService: analyticsService,
  );

  runZonedGuarded(
    () {
      runApp(
        ProviderScope(
          overrides: [
            localeControllerProvider.overrideWith(
              () => LocaleController(initial: persistedLocale),
            ),
            crashReportingServiceProvider.overrideWithValue(crashReportingService),
            analyticsServiceProvider.overrideWithValue(analyticsService),
          ],
          child: const AwakenApp(),
        ),
      );
    },
    (error, stack) => reportZoneError(
      error,
      stack,
      crashReportingService: crashReportingService,
      analyticsService: analyticsService,
    ),
  );
}
```

- [ ] **Step 2:** rodar `flutter analyze` (working dir `apps/mobile`) → sem erros novos.
- [ ] **Step 3:** rodar `flutter test` completo → todos os testes (incluindo os existentes de US-001..005) continuam verdes.

---

## Task 6: smoke test e2e — Android mínimo (CA-001 a CA-004)

**Files:**
- Create: `apps/mobile/integration_test/min_device_stability_flow_test.dart`

- [ ] **Step 1:** seguir o padrão de `navigation_guard_flow_test.dart`/`state_views_flow_test.dart` (stub `SessionRepository`, override de `analyticsServiceProvider`/`crashReportingServiceProvider`, `splashControllerProvider` com delay curto), fixando `tester.binding.setSurfaceSize` para resolução mínima (320x480, mesma já usada em US-005) e cobrindo: abertura sem crash (CA-001), navegação base sem travar (CA-002), nenhuma exceção solta durante transições com animação habilitada (CA-004).

```dart
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';

import 'package:awaken/app/awaken_app.dart';
import 'package:awaken/core/analytics/analytics_provider.dart';
import 'package:awaken/core/analytics/no_op_analytics_service.dart';
import 'package:awaken/core/auth/access_status.dart';
import 'package:awaken/core/auth/session_provider.dart';
import 'package:awaken/core/auth/session_repository.dart';
import 'package:awaken/core/auth/session_state.dart';
import 'package:awaken/core/crash_reporting/crash_reporting_provider.dart';
import 'package:awaken/core/crash_reporting/no_op_crash_reporting_service.dart';
import 'package:awaken/features/splash/presentation/controllers/splash_controller.dart';

class _StubSessionRepository implements SessionRepository {
  const _StubSessionRepository(this._session);
  final SessionState _session;
  @override
  Future<SessionState> getSessionState() async => _session;
}

class _FailingSessionRepository implements SessionRepository {
  const _FailingSessionRepository();
  @override
  Future<SessionState> getSessionState() => throw Exception('network down');
}

Future<void> _pumpAppWith(
  WidgetTester tester,
  SessionRepository sessionRepository,
) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        analyticsServiceProvider.overrideWithValue(const NoOpAnalyticsService()),
        crashReportingServiceProvider.overrideWithValue(
          const NoOpCrashReportingService(),
        ),
        sessionRepositoryProvider.overrideWithValue(sessionRepository),
        splashControllerProvider.overrideWith(
          () => SplashController(delay: const Duration(milliseconds: 50)),
        ),
      ],
      child: const AwakenApp(),
    ),
  );
  await tester.pumpAndSettle(const Duration(seconds: 1));
}

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  group('US-006: estabilidade em Android mínimo (320x480)', () {
    setUp(() {});

    testWidgets('CA-001: app abre sem crash em dispositivo mínimo',
        (tester) async {
      await tester.binding.setSurfaceSize(const Size(320, 480));
      addTearDown(() => tester.binding.setSurfaceSize(null));

      await _pumpAppWith(
        tester,
        const _StubSessionRepository(SessionState.visitor()),
      );

      expect(tester.takeException(), isNull);
    });

    testWidgets(
        'CA-002/CA-004: navegação base com animações não trava em tela mínima',
        (tester) async {
      await tester.binding.setSurfaceSize(const Size(320, 480));
      addTearDown(() => tester.binding.setSurfaceSize(null));

      await _pumpAppWith(
        tester,
        const _StubSessionRepository(SessionState.visitor()),
      );
      expect(tester.takeException(), isNull);

      await tester.tap(find.text('Continuar grátis'));
      await tester.pumpAndSettle();

      expect(tester.takeException(), isNull);
      expect(find.byType(TextField), findsNWidgets(2));
    });

    testWidgets(
        'RN-005: falha ao carregar dados essenciais exibe erro controlado, sem crash',
        (tester) async {
      await tester.binding.setSurfaceSize(const Size(320, 480));
      addTearDown(() => tester.binding.setSurfaceSize(null));

      await _pumpAppWith(tester, const _FailingSessionRepository());

      expect(tester.takeException(), isNull);
      expect(find.text('Tentar novamente'), findsOneWidget);
    });
  });
}
```

- [ ] **Step 2:** rodar `flutter test integration_test/min_device_stability_flow_test.dart` (ou via `flutter test integration_test/app_test.dart` se houver runner agregador — checar padrão do projeto antes) → PASS, sem exceptions.

---

## Task 7: l10n — nenhuma chave nova necessária

**Files:** nenhum.

- [ ] **Step 1:** confirmar que `errorNetwork`, `errorUnexpected`, `retryButton` (já existentes em `app_pt.arb`/`app_en.arb`/`app_es.arb`) cobrem RN-005/CA-003 sem precisar de chave nova. Não editar ARBs nesta US.

---

## Task 8: CI — validar minSdk no pipeline mobile

**Files:**
- Modify: `.github/workflows/mobile.yml`

- [ ] **Step 1:** adicionar um step após "Get dependencies" que falha o build se `minSdk` não estiver fixado em 23+ no gradle (grep simples, sem depender de build completo do Android SDK).

```yaml
      - name: Validate Android minSdk (US-006 RN-001)
        run: |
          grep -E "minSdk = (2[3-9]|[3-9][0-9])" android/app/build.gradle.kts
        working-directory: apps/mobile
        shell: bash
```

- [ ] **Step 2:** validar localmente o regex contra o arquivo (`grep -E "minSdk = (2[3-9]|[3-9][0-9])" apps/mobile/android/app/build.gradle.kts`) → deve casar a linha do Task 1.

---

## Task 9: backend — cobertura de teste do `ExceptionHandlingMiddleware`

**Files:**
- Create: `backend/tests/Awaken.UnitTests/Middlewares/ExceptionHandlingMiddlewareTests.cs`

- [ ] **Step 1:** teste unitário usando `RequestDelegate` fake que lança cada tipo de exceção e validando status code + `correlationId` ecoado no corpo (sem stack trace exposto).

```csharp
using System.Text.Json;
using Awaken.Api.Middlewares;
using Awaken.Application.Common.Exceptions;
using Awaken.Contracts.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Awaken.UnitTests.Middlewares;

public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int StatusCode, ApiErrorResponse Body)> InvokeAsync(
        Exception thrown, string? correlationId = "corr-1")
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        if (correlationId is not null)
        {
            context.Items["CorrelationId"] = correlationId;
        }

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw thrown,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();
        var body = JsonSerializer.Deserialize<ApiErrorResponse>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        })!;

        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task NotFoundException_Returns404_WithCorrelationId()
    {
        var (statusCode, body) = await InvokeAsync(new NotFoundException("User", "123"));

        statusCode.Should().Be(StatusCodes.Status404NotFound);
        body.Code.Should().Be("NOT_FOUND");
        body.CorrelationId.Should().Be("corr-1");
    }

    [Fact]
    public async Task UnauthorizedException_Returns401()
    {
        var (statusCode, body) = await InvokeAsync(new UnauthorizedException());

        statusCode.Should().Be(StatusCodes.Status401Unauthorized);
        body.Code.Should().Be("UNAUTHORIZED");
    }

    [Fact]
    public async Task UnhandledException_Returns500_GenericMessage_NoStackTraceLeak()
    {
        var (statusCode, body) = await InvokeAsync(new InvalidOperationException("internal detail"));

        statusCode.Should().Be(StatusCodes.Status500InternalServerError);
        body.Code.Should().Be("INTERNAL_ERROR");
        body.Message.Should().NotContain("internal detail");
    }

    [Fact]
    public async Task MissingCorrelationId_GeneratesNewOne()
    {
        var (_, body) = await InvokeAsync(new UnauthorizedException(), correlationId: null);

        body.CorrelationId.Should().NotBeNullOrEmpty();
    }
}
```

- [ ] **Step 2:** `dotnet test backend/tests/Awaken.UnitTests --filter ExceptionHandlingMiddlewareTests` → PASS.

---

## Task 10: rodar suíte completa e revisar

- [ ] **Step 1:** `flutter analyze` (apps/mobile) → 0 erros.
- [ ] **Step 2:** `flutter test` (apps/mobile) → todos verdes (unit + widget, incluindo os novos).
- [ ] **Step 3:** `dotnet build` + `dotnet test` (backend) → verde.
- [ ] **Step 4:** revisar diff final contra CA-001..CA-004 e RN-001..RN-006 da US-006; confirmar nenhum regresso em US-001..005 (arquivos já modificados/não commitados continuam intactos).
- [ ] **Step 5:** não commitar — usuário fará revisão manual antes do commit.

---

## Self-Review

- **Cobertura da spec:** RN-001 (Task 1+8), RN-002/CA-002 (Task 6), RN-003/CA-003 (Tasks 2-5), RN-004/CA-004 (Task 6), RN-005 (Task 6 RN-005 case, reaproveitando `_SplashError` já existente), RN-006 (processo de QA — fora de código, documentado no PR). Analytics `app_opened` já existe (US-002); `crash_detected` novo (Task 4). Backend (Task 9). L10n (Task 7, sem gap).
- **Sem placeholders:** todos os steps têm código completo.
- **Consistência de tipos:** `CrashReportingService.recordError(Object, StackTrace?, {bool fatal})` usado de forma idêntica em NoOp, Firebase impl, handlers globais e testes.
