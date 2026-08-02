# US-021 — Paywall Honesto e Previsível

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tornar o paywall do AWAKEN honesto e previsível — adicionando evento `paywall_viewed`, link de termos de uso e acesso às configurações diretamente do paywall (RN-004), sem dark patterns.

**Architecture:** Flutter side: `_PaywallContent` recebe novo callback `onSettings` e renderiza links de Termos de Uso e Configurações no rodapé. O evento `paywall_viewed` é disparado via analytics em `initState` para todos os estados expirados (trial ou assinatura). Backend e rotas já estão configurados corretamente pela US-020 — `_expiredAccessAllowedRoutes` já inclui `/settings`.

**Tech Stack:** Flutter/Riverpod/go_router, flutter_test, ARB/l10n, integration_test

---

## File Map

| Arquivo | Ação |
|---|---|
| `apps/mobile/lib/l10n/app_pt.arb` | ADD `paywallTermsLink`, `paywallSettingsLink` |
| `apps/mobile/lib/l10n/app_en.arb` | ADD `paywallTermsLink`, `paywallSettingsLink` |
| `apps/mobile/lib/l10n/app_es.arb` | ADD `paywallTermsLink`, `paywallSettingsLink` |
| `apps/mobile/lib/features/subscriptions/presentation/pages/subscription_page.dart` | MODIFY — evento `paywall_viewed`, `onSettings` em `_PaywallContent`, links termos+configurações |
| `apps/mobile/test/features/subscriptions/presentation/pages/subscription_page_test.dart` | ADD — grupo US-021 |
| `apps/mobile/integration_test/navigation_guard_flow_test.dart` | ADD — teste de navegação paywall → settings |

---

## Task 1: l10n — chaves paywallTermsLink e paywallSettingsLink

**Files:**
- Modify: `apps/mobile/lib/l10n/app_pt.arb`
- Modify: `apps/mobile/lib/l10n/app_en.arb`
- Modify: `apps/mobile/lib/l10n/app_es.arb`

- [ ] Adicionar ao final de `app_pt.arb` (antes do `}` final, após `paywallProgressPreservedMessage`):

```json
  "paywallTermsLink": "Termos de uso",
  "@paywallTermsLink": { "description": "Link para termos de uso no rodapé do paywall" },
  "paywallSettingsLink": "Configurações",
  "@paywallSettingsLink": { "description": "Link para configurações no rodapé do paywall" }
```

- [ ] Adicionar ao final de `app_en.arb` (antes do `}` final, após `paywallProgressPreservedMessage`):

```json
  "paywallTermsLink": "Terms of use",
  "@paywallTermsLink": { "description": "Link to terms of use in paywall footer" },
  "paywallSettingsLink": "Settings",
  "@paywallSettingsLink": { "description": "Link to settings in paywall footer" }
```

- [ ] Adicionar ao final de `app_es.arb` (antes do `}` final, após `paywallProgressPreservedMessage`):

```json
  "paywallTermsLink": "Términos de uso",
  "@paywallTermsLink": { "description": "Enlace a los términos de uso en el pie del paywall" },
  "paywallSettingsLink": "Configuración",
  "@paywallSettingsLink": { "description": "Enlace a la configuración en el pie del paywall" }
```

- [ ] Rodar `flutter gen-l10n` em `apps/mobile/`:

```
cd apps/mobile && flutter gen-l10n
```

Esperado: sem erros, arquivos `app_localizations_*.dart` regenerados.

---

## Task 2: SubscriptionPage — paywall_viewed + onSettings + links de rodapé

**Files:**
- Modify: `apps/mobile/lib/features/subscriptions/presentation/pages/subscription_page.dart`

### 2.1 — Evento `paywall_viewed` em `initState`

Localizar o bloco de analytics em `initState` (linhas 40–47 do arquivo atual):

```dart
if (session != null && session.isAccessExpired && !_paywallViewedLogged) {
  _paywallViewedLogged = true;
  final analytics = ref.read(analyticsServiceProvider);
  if (session.accessStatus == AccessStatus.trialExpired) {
    await analytics.logEvent('paywall_after_trial_viewed');
  }
  await analytics.logEvent('access_blocked');
}
```

- [ ] Substituir por (adiciona `paywall_viewed` como primeiro evento, disparado para qualquer expirado):

```dart
if (session != null && session.isAccessExpired && !_paywallViewedLogged) {
  _paywallViewedLogged = true;
  final analytics = ref.read(analyticsServiceProvider);
  await analytics.logEvent('paywall_viewed');
  if (session.accessStatus == AccessStatus.trialExpired) {
    await analytics.logEvent('paywall_after_trial_viewed');
  }
  await analytics.logEvent('access_blocked');
}
```

### 2.2 — Passar `onSettings` ao `_PaywallContent`

Localizar o bloco `if (session != null && session.isAccessExpired)` no método `build()`:

```dart
child: _PaywallContent(
  key: const Key('paywall-content'),
  accessStatus: session.accessStatus!,
  isActionLoading: _isRevenueCatActionRunning,
  onSubscribe: _presentPaywall,
  onRestore: _restorePurchases,
),
```

- [ ] Substituir por (adiciona `onSettings`):

```dart
child: _PaywallContent(
  key: const Key('paywall-content'),
  accessStatus: session.accessStatus!,
  isActionLoading: _isRevenueCatActionRunning,
  onSubscribe: _presentPaywall,
  onRestore: _restorePurchases,
  onSettings: () => context.go(AppRoutes.settings),
),
```

- [ ] Adicionar import de `go_router` no topo do arquivo (se ainda não existir):

```dart
import 'package:go_router/go_router.dart';
import '../../../../app/app_router.dart';
```

### 2.3 — Atualizar `_PaywallContent` com `onSettings` e links de rodapé

Localizar a classe `_PaywallContent` (começa em `class _PaywallContent extends StatelessWidget`):

- [ ] Adicionar parâmetro `onSettings` ao construtor e campo:

```dart
class _PaywallContent extends StatelessWidget {
  const _PaywallContent({
    super.key,
    required this.accessStatus,
    required this.isActionLoading,
    required this.onSubscribe,
    required this.onRestore,
    required this.onSettings,
  });

  final AccessStatus accessStatus;
  final bool isActionLoading;
  final VoidCallback onSubscribe;
  final VoidCallback onRestore;
  final VoidCallback onSettings;
```

- [ ] Adicionar links de rodapé ao final do `Column` em `build()`, após o botão de restaurar (antes do fechamento de `children: [`):

```dart
          const SizedBox(height: AwakenSpacing.lg),
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              TextButton(
                key: const Key('paywall-settings-link'),
                onPressed: onSettings,
                style: TextButton.styleFrom(
                  foregroundColor: AwakenColors.textMuted,
                  padding: EdgeInsets.zero,
                  minimumSize: Size.zero,
                  tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                ),
                child: Text(
                  l10n.paywallSettingsLink,
                  style: AwakenTypography.bodySmall.copyWith(
                    color: AwakenColors.textMuted,
                  ),
                ),
              ),
              Padding(
                padding: const EdgeInsets.symmetric(
                    horizontal: AwakenSpacing.sm),
                child: Text(
                  '·',
                  style: AwakenTypography.bodySmall.copyWith(
                    color: AwakenColors.textMuted,
                  ),
                ),
              ),
              TextButton(
                key: const Key('paywall-terms-link'),
                onPressed: onSettings,
                style: TextButton.styleFrom(
                  foregroundColor: AwakenColors.textMuted,
                  padding: EdgeInsets.zero,
                  minimumSize: Size.zero,
                  tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                ),
                child: Text(
                  l10n.paywallTermsLink,
                  style: AwakenTypography.bodySmall.copyWith(
                    color: AwakenColors.textMuted,
                  ),
                ),
              ),
            ],
          ),
```

> **Nota:** `paywallTermsLink` navega para settings porque os termos de uso estão acessíveis na tela de Configurações (RN-004 — "rota permitida"). Ambos os links usam `onSettings`.

---

## Task 3: Testes Flutter — US-021

**Files:**
- Modify: `apps/mobile/test/features/subscriptions/presentation/pages/subscription_page_test.dart`

- [ ] Adicionar `import 'package:go_router/go_router.dart';` no bloco de imports se necessário (não é necessário se apenas verificarmos keys).

- [ ] Adicionar o grupo de testes US-021 **ao final** de `void main()`, antes do `}` de fechamento:

```dart
  group('SubscriptionPage — US-021 paywall honesto e previsível', () {
    testWidgets('analytics: paywall_viewed disparado para trial_expired',
        (tester) async {
      final analytics = _CapturingAnalyticsService();
      await tester.pumpWidget(_buildTestApp(
        session: const SessionState(
          hasSession: true,
          accessStatus: AccessStatus.trialExpired,
          onboardingCompleted: true,
        ),
        analytics: analytics,
      ));
      await tester.pump();

      expect(analytics.events, contains('paywall_viewed'));
    });

    testWidgets('analytics: paywall_viewed disparado para subscription_expired',
        (tester) async {
      final analytics = _CapturingAnalyticsService();
      await tester.pumpWidget(_buildTestApp(
        session: const SessionState(
          hasSession: true,
          accessStatus: AccessStatus.subscriptionExpired,
          onboardingCompleted: true,
        ),
        analytics: analytics,
      ));
      await tester.pump();

      expect(analytics.events, contains('paywall_viewed'));
    });

    testWidgets(
        'analytics: paywall_viewed precede paywall_after_trial_viewed para trial_expired',
        (tester) async {
      final analytics = _CapturingAnalyticsService();
      await tester.pumpWidget(_buildTestApp(
        session: const SessionState(
          hasSession: true,
          accessStatus: AccessStatus.trialExpired,
          onboardingCompleted: true,
        ),
        analytics: analytics,
      ));
      await tester.pump();

      final viewedIdx = analytics.events.indexOf('paywall_viewed');
      final afterTrialIdx =
          analytics.events.indexOf('paywall_after_trial_viewed');
      expect(viewedIdx, lessThan(afterTrialIdx));
    });

    testWidgets('RN-004: link de configurações visível no paywall',
        (tester) async {
      await tester.pumpWidget(_buildTestApp(
        session: const SessionState(
          hasSession: true,
          accessStatus: AccessStatus.trialExpired,
          onboardingCompleted: true,
        ),
      ));
      await tester.pump();

      expect(find.byKey(const Key('paywall-settings-link')), findsOneWidget);
    });

    testWidgets('RN-004: link de termos de uso visível no paywall',
        (tester) async {
      await tester.pumpWidget(_buildTestApp(
        session: const SessionState(
          hasSession: true,
          accessStatus: AccessStatus.trialExpired,
          onboardingCompleted: true,
        ),
      ));
      await tester.pump();

      expect(find.byKey(const Key('paywall-terms-link')), findsOneWidget);
    });

    testWidgets('RN-004: links de termos e configurações visíveis para subscription_expired',
        (tester) async {
      await tester.pumpWidget(_buildTestApp(
        session: const SessionState(
          hasSession: true,
          accessStatus: AccessStatus.subscriptionExpired,
          onboardingCompleted: true,
        ),
      ));
      await tester.pump();

      expect(find.byKey(const Key('paywall-settings-link')), findsOneWidget);
      expect(find.byKey(const Key('paywall-terms-link')), findsOneWidget);
    });

    testWidgets('RN-005: paywall não exibe urgência falsa — sem contagem regressiva',
        (tester) async {
      await tester.pumpWidget(_buildTestApp(
        session: const SessionState(
          hasSession: true,
          accessStatus: AccessStatus.trialExpired,
          onboardingCompleted: true,
        ),
      ));
      await tester.pump();

      // Não deve haver textos de urgência falsa
      expect(find.textContaining('Oferta expira em'), findsNothing);
      expect(find.textContaining('Últimas vagas'), findsNothing);
      expect(find.textContaining('Promoção por tempo limitado'), findsNothing);
    });

    testWidgets('RN-006: usuário ativo não vê links de paywall', (tester) async {
      await tester.pumpWidget(_buildTestApp(
        session: const SessionState(
          hasSession: true,
          accessStatus: AccessStatus.subscriptionActive,
          onboardingCompleted: true,
        ),
        statusState: const SubscriptionStatusLoaded(
          accessStatus: 'subscription_active',
        ),
      ));
      await tester.pump();

      expect(find.byKey(const Key('paywall-settings-link')), findsNothing);
      expect(find.byKey(const Key('paywall-terms-link')), findsNothing);
    });
  });
```

- [ ] Rodar os testes unitários:

```
cd apps/mobile && flutter test test/features/subscriptions/presentation/pages/subscription_page_test.dart
```

Esperado: todos os testes passam, incluindo os novos do grupo US-021.

---

## Task 4: Teste de integração — navegação paywall → settings

**Files:**
- Modify: `apps/mobile/integration_test/navigation_guard_flow_test.dart`

- [ ] Adicionar um novo grupo `US-021` ao final de `void main()`, antes do `}` de fechamento:

```dart
  group('US-021: paywall honesto — links de configurações e termos', () {
    testWidgets(
        'RN-004: link de configurações no paywall navega para tela de configurações',
        (tester) async {
      await _pumpAppWith(
        tester,
        const _StubSessionRepository(
          SessionState(
            hasSession: true,
            accessStatus: AccessStatus.trialExpired,
            onboardingCompleted: true,
          ),
        ),
      );

      expect(find.byKey(const Key('paywall-content')), findsOneWidget);
      expect(find.byKey(const Key('paywall-settings-link')), findsOneWidget);

      await tester.tap(find.byKey(const Key('paywall-settings-link')));
      await tester.pumpAndSettle();

      // Settings é uma rota permitida para expirados (_expiredAccessAllowedRoutes)
      expect(find.byKey(const Key('settings-page')), findsOneWidget);
    });

    testWidgets(
        'RN-004: link de termos no paywall também navega para configurações',
        (tester) async {
      await _pumpAppWith(
        tester,
        const _StubSessionRepository(
          SessionState(
            hasSession: true,
            accessStatus: AccessStatus.trialExpired,
            onboardingCompleted: true,
          ),
        ),
      );

      expect(find.byKey(const Key('paywall-terms-link')), findsOneWidget);

      await tester.tap(find.byKey(const Key('paywall-terms-link')));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('settings-page')), findsOneWidget);
    });
  });
```

> **Nota:** O teste de integração requer que `SettingsPage` tenha `key: const Key('settings-page')` no `Scaffold`. Verificar se já existe e adicionar se necessário.

- [ ] Verificar se `SettingsPage` tem key `settings-page`:

```
grep -n "settings-page" apps/mobile/lib/features/settings/presentation/pages/settings_page.dart
```

Se não encontrar, adicionar a key ao `Scaffold` da `SettingsPage`.

- [ ] Rodar o teste de integração (requer device/emulador conectado, ou testar em modo headless):

```
cd apps/mobile && flutter test integration_test/navigation_guard_flow_test.dart
```

---

## Self-Review: Cobertura da Spec

| Requisito | Cobertura |
|---|---|
| RN-001: Paywall não aparece antes de comunicar trial | Coberto pelo navigation guard (route_resolver.dart — já existente) |
| RN-002: Paywall explica motivo do bloqueio | Coberto pela US-020 (`accessBlockedTitle`, `accessBlockedTrialDescription`, `accessBlockedSubscriptionDescription`) |
| RN-003: Paywall oferece mensal e anual | Coberto pela US-020 (`_PlanCard` mensal e anual) |
| RN-004: Rotas permitidas visíveis (settings, termos) | **Task 2** — links `paywall-settings-link` e `paywall-terms-link` |
| RN-005: Sem dark patterns (urgência falsa) | **Task 3** — teste verifica ausência de textos de urgência |
| RN-006: Assinante ativo não vê paywall | Coberto pela US-020 + teste em Task 3 |
| Analytics: `paywall_viewed` | **Task 2** — disparado em `initState` para todos os expirados |
| Analytics: `paywall_after_trial_viewed` | Coberto pela US-020 |
| l10n PT-BR, EN, ES | **Task 1** — chaves em todos os 3 ARBs |
| Link para termos/política | **Task 2** — `paywall-terms-link` usando `onSettings` |
| Acesso a configurações | **Task 2** — `paywall-settings-link` com `context.go(AppRoutes.settings)` |
| Estado: usuário assinante detectado | Coberto pela US-020 (`RN-006` em session provider) |
| Estado: erro de conexão | Coberto pela US-020 (`AwakenErrorState` no branch não-expirado) |
