# US-001 — Splash Screen com Identidade AWAKEN

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implementar splash screen com identidade AWAKEN — marca visível, fundo dark, animação leve via `flutter_animate`, fallback estático, eventos de analytics (`app_opened`, `splash_viewed`), e roteamento pós-inicialização para visitantes.

**Architecture:** `SplashController` (`StateNotifier`) gerencia lógica de inicialização e roteamento. `AnalyticsService` abstrato permite injeção em testes. `SplashPage` (`HookConsumerWidget`) dispara init via `useEffect` e navega ao concluir. Provider `splashAnimationsEnabledProvider` torna fallback testável.

**Tech Stack:** Flutter / Dart, flutter_animate, hooks_riverpod, flutter_hooks, go_router, mocktail (testes), integration_test.

---

## Mapa de arquivos

| Ação | Arquivo |
|---|---|
| CREATE | `apps/mobile/lib/core/analytics/analytics_service.dart` |
| CREATE | `apps/mobile/lib/core/analytics/no_op_analytics_service.dart` |
| CREATE | `apps/mobile/lib/core/analytics/analytics_provider.dart` |
| CREATE | `apps/mobile/lib/features/splash/presentation/controllers/splash_controller.dart` |
| MODIFY | `apps/mobile/lib/features/splash/presentation/pages/splash_page.dart` |
| CREATE | `apps/mobile/test/features/splash/presentation/controllers/splash_controller_test.dart` |
| CREATE | `apps/mobile/test/features/splash/presentation/pages/splash_page_test.dart` |
| CREATE | `apps/mobile/integration_test/splash_flow_test.dart` |

---

## Task 1: AnalyticsService — abstração e provider

**Files:**
- Create: `apps/mobile/lib/core/analytics/analytics_service.dart`
- Create: `apps/mobile/lib/core/analytics/no_op_analytics_service.dart`
- Create: `apps/mobile/lib/core/analytics/analytics_provider.dart`

- [ ] **Step 1.1: Criar interface abstrata**

```dart
// apps/mobile/lib/core/analytics/analytics_service.dart
abstract class AnalyticsService {
  Future<void> logEvent(String name, {Map<String, Object>? params});
}
```

- [ ] **Step 1.2: Criar implementação NoOp (dev/test)**

```dart
// apps/mobile/lib/core/analytics/no_op_analytics_service.dart
import 'analytics_service.dart';

class NoOpAnalyticsService implements AnalyticsService {
  const NoOpAnalyticsService();

  @override
  Future<void> logEvent(String name, {Map<String, Object>? params}) async {}
}
```

- [ ] **Step 1.3: Criar provider Riverpod**

```dart
// apps/mobile/lib/core/analytics/analytics_provider.dart
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'analytics_service.dart';
import 'no_op_analytics_service.dart';

final analyticsServiceProvider = Provider<AnalyticsService>((ref) {
  return const NoOpAnalyticsService();
});
```

> Nota: A implementação Firebase (`FirebaseAnalyticsService`) será adicionada na EPIC-014. O provider será sobrescrito no `main.dart` quando Firebase estiver configurado.

- [ ] **Step 1.4: Verificar que arquivos foram criados corretamente**

Confirmar que os três arquivos existem e que `no_op_analytics_service.dart` implementa `AnalyticsService` sem erros de lint.

---

## Task 2: SplashController

**Files:**
- Create: `apps/mobile/lib/features/splash/presentation/controllers/splash_controller.dart`

- [ ] **Step 2.1: Criar SplashController**

```dart
// apps/mobile/lib/features/splash/presentation/controllers/splash_controller.dart
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/analytics/analytics_provider.dart';
import '../../../../core/analytics/analytics_service.dart';
import '../../../../app/app_router.dart';

enum SplashStatus { initializing, navigating }

class SplashController extends StateNotifier<SplashStatus> {
  SplashController(
    this._analytics, {
    Duration delay = const Duration(milliseconds: 1500),
  })  : _delay = delay,
        super(SplashStatus.initializing);

  final AnalyticsService _analytics;
  final Duration _delay;

  Future<String> initialize() async {
    await _analytics.logEvent('app_opened');
    await Future.delayed(_delay);
    await _analytics.logEvent('splash_viewed');
    state = SplashStatus.navigating;
    return AppRoutes.pricingIntro;
  }
}

final splashControllerProvider =
    StateNotifierProvider<SplashController, SplashStatus>((ref) {
  return SplashController(ref.read(analyticsServiceProvider));
});
```

> `delay` injetável permite testes sem espera real. `pricingIntro` é a rota padrão para visitantes — EPIC-002 (auth) estenderá com verificação de sessão.

- [ ] **Step 2.2: Verificar imports**

Confirmar que `AppRoutes.pricingIntro` existe em `app_router.dart` (já existe: `static const pricingIntro = '/pricing-intro'`).

---

## Task 3: Testes unitários do SplashController

**Files:**
- Create: `apps/mobile/test/features/splash/presentation/controllers/splash_controller_test.dart`

- [ ] **Step 3.1: Escrever testes (TDD — escrever antes de rodar)**

```dart
// apps/mobile/test/features/splash/presentation/controllers/splash_controller_test.dart
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:awaken/app/app_router.dart';
import 'package:awaken/core/analytics/analytics_service.dart';
import 'package:awaken/features/splash/presentation/controllers/splash_controller.dart';

class MockAnalyticsService extends Mock implements AnalyticsService {}

void main() {
  late MockAnalyticsService mockAnalytics;

  setUp(() {
    mockAnalytics = MockAnalyticsService();
    when(
      () => mockAnalytics.logEvent(any(), params: any(named: 'params')),
    ).thenAnswer((_) async {});
  });

  SplashController buildController() =>
      SplashController(mockAnalytics, delay: Duration.zero);

  group('SplashController', () {
    test('estado inicial é initializing', () {
      expect(buildController().state, SplashStatus.initializing);
    });

    test('initialize dispara app_opened', () async {
      await buildController().initialize();
      verify(() => mockAnalytics.logEvent('app_opened')).called(1);
    });

    test('initialize dispara splash_viewed após app_opened', () async {
      final calls = <String>[];
      when(
        () => mockAnalytics.logEvent(any(), params: any(named: 'params')),
      ).thenAnswer((inv) async {
        calls.add(inv.positionalArguments.first as String);
      });

      await buildController().initialize();

      expect(calls, equals(['app_opened', 'splash_viewed']));
    });

    test('initialize retorna rota pricingIntro', () async {
      final route = await buildController().initialize();
      expect(route, equals(AppRoutes.pricingIntro));
    });

    test('estado vira navigating após initialize', () async {
      final controller = buildController();
      await controller.initialize();
      expect(controller.state, SplashStatus.navigating);
    });
  });
}
```

- [ ] **Step 3.2: Rodar testes unitários**

```bash
cd apps/mobile
flutter test test/features/splash/presentation/controllers/splash_controller_test.dart --reporter=expanded
```

Esperado: 5 testes PASS.

- [ ] **Step 3.3: Confirmar cobertura dos CAs**

Mapear testes para critérios da spec:
- `app_opened` + `splash_viewed` → CA-018 (analytics)
- `pricingIntro` retornado → CA-002 (redirecionamento visitante)
- estado `initializing` → CA-001 (fluxo inicia corretamente)

---

## Task 4: SplashPage — UI completo

**Files:**
- Modify: `apps/mobile/lib/features/splash/presentation/pages/splash_page.dart`

- [ ] **Step 4.1: Substituir implementação da SplashPage**

```dart
// apps/mobile/lib/features/splash/presentation/pages/splash_page.dart
import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:flutter_hooks/flutter_hooks.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:hooks_riverpod/hooks_riverpod.dart';
// ignore: depend_on_referenced_packages
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

import '../../../../design_system/tokens/colors.dart';
import '../../../../design_system/tokens/typography.dart';
import '../controllers/splash_controller.dart';

final splashAnimationsEnabledProvider = Provider<bool>((_) => true);

class SplashPage extends HookConsumerWidget {
  const SplashPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final animationsEnabled = ref.watch(splashAnimationsEnabledProvider);

    useEffect(() {
      bool cancelled = false;
      ref.read(splashControllerProvider.notifier).initialize().then((route) {
        if (!cancelled && context.mounted) context.go(route);
      });
      return () => cancelled = true;
    }, const []);

    return Scaffold(
      backgroundColor: AwakenColors.backgroundPrimary,
      body: Center(
        child: animationsEnabled
            ? const _AnimatedSplash()
            : const _StaticSplash(),
      ),
    );
  }
}

class _AnimatedSplash extends StatelessWidget {
  const _AnimatedSplash();

  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        const _LogoText()
            .animate()
            .fadeIn(duration: 600.ms, curve: Curves.easeOut)
            .scale(
              begin: const Offset(0.85, 0.85),
              end: const Offset(1.0, 1.0),
              duration: 600.ms,
              curve: Curves.easeOut,
            ),
        const SizedBox(height: 16),
        const _TaglineText()
            .animate()
            .fadeIn(delay: 400.ms, duration: 500.ms, curve: Curves.easeOut),
      ],
    );
  }
}

class _StaticSplash extends StatelessWidget {
  const _StaticSplash();

  @override
  Widget build(BuildContext context) {
    return const Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        _LogoText(),
        SizedBox(height: 16),
        _TaglineText(),
      ],
    );
  }
}

class _LogoText extends StatelessWidget {
  const _LogoText();

  @override
  Widget build(BuildContext context) {
    return const Text(
      'AWAKEN',
      style: TextStyle(
        fontSize: 52,
        fontWeight: FontWeight.w900,
        color: AwakenColors.primary,
        letterSpacing: 10,
      ),
    );
  }
}

class _TaglineText extends StatelessWidget {
  const _TaglineText();

  @override
  Widget build(BuildContext context) {
    final tagline = AppLocalizations.of(context)?.splashTagline ?? '';
    if (tagline.isEmpty) return const SizedBox.shrink();
    return Text(
      tagline,
      style: AwakenTypography.bodyMedium.copyWith(
        color: AwakenColors.textSecondary,
        letterSpacing: 1,
      ),
    );
  }
}
```

- [ ] **Step 4.2: Gerar l10n**

```bash
cd apps/mobile
flutter gen-l10n
```

Esperado: sem erros. `splashTagline` já existe nos 3 ARBs.

- [ ] **Step 4.3: Verificar análise estática**

```bash
cd apps/mobile
flutter analyze lib/features/splash/ lib/core/analytics/
```

Esperado: sem warnings ou erros.

---

## Task 5: Widget tests da SplashPage

**Files:**
- Create: `apps/mobile/test/features/splash/presentation/pages/splash_page_test.dart`

- [ ] **Step 5.1: Escrever widget tests**

```dart
// apps/mobile/test/features/splash/presentation/pages/splash_page_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mocktail/mocktail.dart';
// ignore: depend_on_referenced_packages
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

import 'package:awaken/core/analytics/analytics_service.dart';
import 'package:awaken/core/analytics/analytics_provider.dart';
import 'package:awaken/features/splash/presentation/controllers/splash_controller.dart';
import 'package:awaken/features/splash/presentation/pages/splash_page.dart';

class MockAnalyticsService extends Mock implements AnalyticsService {}

GoRouter _buildRouter() => GoRouter(
      initialLocation: '/splash',
      routes: [
        GoRoute(path: '/splash', builder: (_, __) => const SplashPage()),
        GoRoute(
          path: '/pricing-intro',
          builder: (_, __) => const Scaffold(body: Text('PricingPage')),
        ),
      ],
    );

Widget _buildTestApp({
  required List<Override> overrides,
  GoRouter? router,
}) {
  return ProviderScope(
    overrides: overrides,
    child: MaterialApp.router(
      routerConfig: router ?? _buildRouter(),
      localizationsDelegates: const [
        AppLocalizations.delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      supportedLocales: const [Locale('pt', 'BR')],
      locale: const Locale('pt', 'BR'),
    ),
  );
}

void main() {
  late MockAnalyticsService mockAnalytics;

  setUp(() {
    mockAnalytics = MockAnalyticsService();
    when(
      () => mockAnalytics.logEvent(any(), params: any(named: 'params')),
    ).thenAnswer((_) async {});
  });

  List<Override> defaultOverrides({Duration delay = Duration.zero}) => [
        analyticsServiceProvider.overrideWithValue(mockAnalytics),
        splashControllerProvider.overrideWith(
          (ref) => SplashController(
            ref.read(analyticsServiceProvider),
            delay: delay,
          ),
        ),
      ];

  group('SplashPage', () {
    testWidgets('CA-001: exibe marca AWAKEN', (tester) async {
      await tester.pumpWidget(_buildTestApp(overrides: defaultOverrides()));
      await tester.pump();

      expect(find.text('AWAKEN'), findsOneWidget);
    });

    testWidgets('CA-001: fundo dark aplicado (backgroundPrimary)', (tester) async {
      await tester.pumpWidget(_buildTestApp(overrides: defaultOverrides()));
      await tester.pump();

      final scaffold = tester.widget<Scaffold>(
        find.byType(Scaffold).first,
      );
      expect(scaffold.backgroundColor, const Color(0xFF0A0A0F));
    });

    testWidgets('CA-001: tagline localizada visível', (tester) async {
      await tester.pumpWidget(_buildTestApp(overrides: defaultOverrides()));
      await tester.pump();

      expect(find.text('Desperte o hunter em você'), findsOneWidget);
    });

    testWidgets('CA-002: navega para pricingIntro após inicialização', (tester) async {
      await tester.pumpWidget(_buildTestApp(overrides: defaultOverrides()));
      await tester.pump();
      expect(find.text('AWAKEN'), findsOneWidget);

      await tester.pumpAndSettle();
      expect(find.text('PricingPage'), findsOneWidget);
    });

    testWidgets('CA-003: fallback estático exibido quando animações desativadas', (tester) async {
      await tester.pumpWidget(
        _buildTestApp(overrides: [
          ...defaultOverrides(),
          splashAnimationsEnabledProvider.overrideWithValue(false),
        ]),
      );
      await tester.pump();

      expect(find.text('AWAKEN'), findsOneWidget);
      expect(find.byType(SplashPage), findsOneWidget);
    });

    testWidgets('CA-004: sem conteúdo de paywall na splash', (tester) async {
      await tester.pumpWidget(_buildTestApp(overrides: defaultOverrides()));
      await tester.pump();

      expect(find.textContaining('plano'), findsNothing);
      expect(find.textContaining('assinar'), findsNothing);
      expect(find.textContaining('R\$'), findsNothing);
      expect(find.textContaining('trial'), findsNothing);
    });

    testWidgets('dispara evento app_opened', (tester) async {
      await tester.pumpWidget(_buildTestApp(overrides: defaultOverrides()));
      await tester.pumpAndSettle();

      verify(() => mockAnalytics.logEvent('app_opened')).called(1);
    });

    testWidgets('dispara evento splash_viewed', (tester) async {
      await tester.pumpWidget(_buildTestApp(overrides: defaultOverrides()));
      await tester.pumpAndSettle();

      verify(() => mockAnalytics.logEvent('splash_viewed')).called(1);
    });
  });
}
```

- [ ] **Step 5.2: Rodar widget tests**

```bash
cd apps/mobile
flutter test test/features/splash/presentation/pages/splash_page_test.dart --reporter=expanded
```

Esperado: 7 testes PASS.

---

## Task 6: Integration test — fluxo completo

**Files:**
- Create: `apps/mobile/integration_test/splash_flow_test.dart`

- [ ] **Step 6.1: Escrever integration test**

```dart
// apps/mobile/integration_test/splash_flow_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';

import 'package:awaken/app/awaken_app.dart';
import 'package:awaken/core/analytics/analytics_provider.dart';
import 'package:awaken/core/analytics/no_op_analytics_service.dart';
import 'package:awaken/features/splash/presentation/controllers/splash_controller.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('US-001: splash exibe identidade e navega para próxima tela', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          analyticsServiceProvider.overrideWithValue(
            const NoOpAnalyticsService(),
          ),
          splashControllerProvider.overrideWith(
            (ref) => SplashController(
              ref.read(analyticsServiceProvider),
              delay: const Duration(milliseconds: 200),
            ),
          ),
        ],
        child: const AwakenApp(),
      ),
    );

    await tester.pump();
    expect(find.text('AWAKEN'), findsOneWidget);
    expect(find.textContaining('R\$'), findsNothing);

    await tester.pumpAndSettle(const Duration(seconds: 2));
    expect(find.text('AWAKEN'), findsNothing);
  });
}
```

- [ ] **Step 6.2: Rodar suite completa de testes unitários + widget**

```bash
cd apps/mobile
flutter test --reporter=expanded
```

Esperado: todos os testes PASS.

- [ ] **Step 6.3: Verificar análise estática geral**

```bash
cd apps/mobile
flutter analyze
```

Esperado: `No issues found!`

---

## Self-review contra spec

| Requisito spec | Task que cobre |
|---|---|
| RN-001 Splash ao abrir | Task 4 — SplashPage renderiza na rota inicial |
| RN-002 Não bloquear além do necessário | Task 2 — delay 1500ms, configurável |
| RN-003 Visitante → próxima tela | Task 2 — retorna `pricingIntro` |
| RN-004 Sessão válida → rota adequada | Stub retorna `pricingIntro`; EPIC-002 estende |
| RN-005 Fallback sem crash | Task 4 — `splashAnimationsEnabledProvider` |
| RN-006 Sem paywall na splash | CA-004 coberto por widget test |
| CA-001 Exibição splash | widget test "CA-001: exibe marca AWAKEN" |
| CA-002 Redirecionamento | widget test "CA-002: navega para pricingIntro" |
| CA-003 Fallback visual | widget test "CA-003: fallback estático" |
| CA-004 Sem paywall | widget test "CA-004: sem conteúdo de paywall" |
| `splash_viewed` event | unit test + widget test |
| `app_opened` event | unit test + widget test |
| PT-BR localizado | `splashTagline` em ARB — já existe |
| EN localizado | `splashTagline` em ARB — já existe |
| ES localizado | `splashTagline` em ARB — já existe |
