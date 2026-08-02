# US-019 — Reconhecer Acesso de Assinante — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Exibir na `SubscriptionPage` o plano exato do assinante (mensal vs anual) e garantir cobertura de testes para todos os critérios de aceite da US-019 (CA-001, CA-002, CA-003, RN-005, RN-006), sem alterar nenhuma lógica de negócio já implementada na US-018.

**Architecture:** A infraestrutura de dados, estado e roteamento foi construída na US-018. US-019 é thin: adiciona chaves l10n para `subscriptionMonthlyActiveStatus` / `subscriptionAnnualActiveStatus`, passa `plan` para o widget `_TrialStatusContent`, e cobre os CAs com testes de widget, de route guard e de integração do backend.

**Tech Stack:** Flutter + Riverpod + go_router (front); ASP.NET Core + Testcontainers (backend integração).

---

## Contexto — o que já existe (US-018)

| Camada | Já implementado |
|---|---|
| Backend domain | `Subscription.CreateFromPaidPlan`, `ActivatePaidPlan`, `MarkExpired` |
| Backend application | `GetSubscriptionStatusQueryHandler` retorna `plan` + `expiresAt` |
| Backend API | `GET /api/subscriptions/status`, `POST /api/subscriptions/sync` |
| Backend infra | Migration com campos RC, `SubscriptionConfiguration` mapeada |
| Backend testes | `SubscriptionTests`, `SyncEntitlementCommandHandlerTests`, `GetSubscriptionStatusQueryHandlerTests`, `SubscriptionsSyncEndpointTests`, `SubscriptionsStatusEndpointTests` |
| Flutter data | DTOs, `SubscriptionRemoteDataSource`, `SubscriptionRepositoryImpl`, `RevenueCatServiceImpl` |
| Flutter state | `SubscriptionStatusState` (com `isSubscriptionActive`, `isSubscriptionExpired`, `plan`, `expiresAt`), `SubscriptionStatusController` |
| Flutter sync | `SyncEntitlementState`, `SyncEntitlementController` |
| Flutter route | `resolveRedirect` e `resolveInitialRoute` tratam `subscriptionActive` corretamente |
| Flutter testes | `subscription_status_controller_test`, `sync_entitlement_controller_test`, `subscription_page_test` (badge genérico), `route_resolver_test` (loop-free, acesso ativo) |

## O que falta para US-019

| # | Item | Motivo |
|---|---|---|
| 1 | Chaves l10n `subscriptionMonthlyActiveStatus` / `subscriptionAnnualActiveStatus` | UI precisa distinguir mensal vs anual |
| 2 | `SubscriptionPage` passa `plan` ao `_TrialStatusContent` e usa label específico | Req. seção 10 do spec |
| 3 | Testes de widget: CA-001 (mensal), CA-002 (anual), CA-003 (sem paywall) | Critérios de aceite não cobertos |
| 4 | Testes de route guard: RN-005 (assinante sem onboarding → onboarding) | Não há teste explícito com `subscriptionActive` + `onboardingCompleted=false` |
| 5 | Teste de integração backend: `GET /status` retorna `subscription_active` + `plan` após sync | Fluxo completo não coberto em `SubscriptionsStatusEndpointTests` |

---

## File Map

### Flutter — Modified
| File | Change |
|---|---|
| `apps/mobile/lib/l10n/app_pt.arb` | Add `subscriptionMonthlyActiveStatus`, `subscriptionAnnualActiveStatus` |
| `apps/mobile/lib/l10n/app_en.arb` | Same keys in English |
| `apps/mobile/lib/l10n/app_es.arb` | Same keys in Spanish |
| `apps/mobile/lib/features/subscriptions/presentation/pages/subscription_page.dart` | Pass `plan` to `_TrialStatusContent`; update `_resolveStatusLabel` |

### Flutter — Tests (Modified)
| File | Change |
|---|---|
| `apps/mobile/test/features/subscriptions/presentation/pages/subscription_page_test.dart` | Add CA-001 (monthly), CA-002 (annual), CA-003 (no paywall for subscriber) |
| `apps/mobile/test/app/navigation/route_resolver_test.dart` | Add RN-005 (subscriptionActive + onboarding=false → onboarding) |

### Backend — Tests (Modified)
| File | Change |
|---|---|
| `backend/tests/Awaken.IntegrationTests/SubscriptionsStatusEndpointTests.cs` | Add test: status retorna subscription_active com plan após sync |

---

## Task 1: l10n — chaves de plano específico

**Files:**
- Modify: `apps/mobile/lib/l10n/app_pt.arb`
- Modify: `apps/mobile/lib/l10n/app_en.arb`
- Modify: `apps/mobile/lib/l10n/app_es.arb`

- [ ] **Step 1.1: Adicionar chaves em `app_pt.arb`**

Inserir antes da chave `"syncEntitlementConnectionError"` (ou antes do `}`):

```json
  "subscriptionMonthlyActiveStatus": "Assinatura mensal ativa",
  "@subscriptionMonthlyActiveStatus": { "description": "Status quando assinatura mensal está ativa" },
  "subscriptionAnnualActiveStatus": "Assinatura anual ativa",
  "@subscriptionAnnualActiveStatus": { "description": "Status quando assinatura anual está ativa" },
```

- [ ] **Step 1.2: Adicionar chaves em `app_en.arb`**

Inserir antes da chave `"syncEntitlementConnectionError"` (ou antes do `}`):

```json
  "subscriptionMonthlyActiveStatus": "Monthly subscription active",
  "@subscriptionMonthlyActiveStatus": { "description": "Status when monthly subscription is active" },
  "subscriptionAnnualActiveStatus": "Annual subscription active",
  "@subscriptionAnnualActiveStatus": { "description": "Status when annual subscription is active" },
```

- [ ] **Step 1.3: Adicionar chaves em `app_es.arb`**

Inserir antes da chave `"syncEntitlementConnectionError"` (ou antes do `}`):

```json
  "subscriptionMonthlyActiveStatus": "Suscripción mensual activa",
  "@subscriptionMonthlyActiveStatus": { "description": "Estado cuando la suscripción mensual está activa" },
  "subscriptionAnnualActiveStatus": "Suscripción anual activa",
  "@subscriptionAnnualActiveStatus": { "description": "Estado cuando la suscripción anual está activa" },
```

- [ ] **Step 1.4: Regenerar l10n**

```bash
cd apps/mobile && flutter gen-l10n
```

Expected: sem erros, `app_localizations.dart` atualizado com as novas chaves.

---

## Task 2: UI — SubscriptionPage passa `plan` e usa label específico

**Files:**
- Modify: `apps/mobile/lib/features/subscriptions/presentation/pages/subscription_page.dart`

- [ ] **Step 2.1: Atualizar switch no `build()` para extrair `plan` e passar ao widget**

Localizar o bloco `switch (statusState)` no `build()`:

```dart
              SubscriptionStatusLoaded(
                :final accessStatus,
                :final daysRemaining
              ) =>
                _TrialStatusContent(
                  l10n: l10n,
                  accessStatus: accessStatus,
                  daysRemaining: daysRemaining,
                ),
```

Substituir por:

```dart
              SubscriptionStatusLoaded(
                :final accessStatus,
                :final daysRemaining,
                :final plan,
              ) =>
                _TrialStatusContent(
                  l10n: l10n,
                  accessStatus: accessStatus,
                  daysRemaining: daysRemaining,
                  plan: plan,
                ),
```

- [ ] **Step 2.2: Atualizar `_TrialStatusContent` para receber e usar `plan`**

Localizar a classe `_TrialStatusContent` e seu construtor:

```dart
class _TrialStatusContent extends StatelessWidget {
  const _TrialStatusContent({
    required this.l10n,
    required this.accessStatus,
    required this.daysRemaining,
  });

  final AppLocalizations l10n;
  final String accessStatus;
  final int? daysRemaining;
```

Substituir por:

```dart
class _TrialStatusContent extends StatelessWidget {
  const _TrialStatusContent({
    required this.l10n,
    required this.accessStatus,
    required this.daysRemaining,
    this.plan,
  });

  final AppLocalizations l10n;
  final String accessStatus;
  final int? daysRemaining;
  final String? plan;
```

- [ ] **Step 2.3: Atualizar `_resolveStatusLabel` para retornar label específico de plano**

Localizar o método:

```dart
  String _resolveStatusLabel(
      AppLocalizations l10n, String accessStatus, int? daysRemaining) {
    return switch (accessStatus) {
      'trial_active' => l10n.trialStatusActive,
      'trial_expired' => l10n.trialStatusExpired,
      'subscription_active' => l10n.subscriptionActiveStatus,
      _ => l10n.trialStatusNoTrial,
    };
  }
```

Substituir por:

```dart
  String _resolveStatusLabel(
      AppLocalizations l10n, String accessStatus, int? daysRemaining) {
    if (accessStatus == 'subscription_active') {
      return plan == 'annual'
          ? l10n.subscriptionAnnualActiveStatus
          : l10n.subscriptionMonthlyActiveStatus;
    }
    return switch (accessStatus) {
      'trial_active' => l10n.trialStatusActive,
      'trial_expired' => l10n.trialStatusExpired,
      _ => l10n.trialStatusNoTrial,
    };
  }
```

- [ ] **Step 2.4: Verificar que `_resolveStatusLabel` é chamado com os parâmetros corretos**

No método `build()` de `_TrialStatusContent`, confirmar que a chamada é:

```dart
_resolveStatusLabel(l10n, accessStatus, daysRemaining)
```

(Sem alteração necessária — já existe assim.)

- [ ] **Step 2.5: Rodar `flutter analyze` para confirmar zero erros**

```bash
cd apps/mobile && flutter analyze
```

Expected: No issues found.

---

## Task 3: Flutter Tests — CA-001, CA-002, CA-003, RN-005

**Files:**
- Modify: `apps/mobile/test/features/subscriptions/presentation/pages/subscription_page_test.dart`
- Modify: `apps/mobile/test/app/navigation/route_resolver_test.dart`

- [ ] **Step 3.1: Adicionar testes de widget US-019 em `subscription_page_test.dart`**

Após o último `testWidgets` existente (antes do `}` final do `main()`), adicionar:

```dart
  group('SubscriptionPage — US-019 reconhecer acesso de assinante', () {
    testWidgets('CA-001: assinatura mensal ativa exibe badge com label mensal',
        (tester) async {
      await tester.pumpWidget(_buildTestApp(
        session: const SessionState(
          hasSession: true,
          accessStatus: AccessStatus.subscriptionActive,
          onboardingCompleted: true,
        ),
        statusState: const SubscriptionStatusLoaded(
          accessStatus: 'subscription_active',
          plan: 'monthly',
        ),
      ));
      await tester.pump();

      expect(find.byKey(const Key('trial-status-badge')), findsOneWidget);
      expect(find.text('Assinatura mensal ativa'), findsOneWidget);
      expect(find.byType(AwakenBlockedState), findsNothing);
    });

    testWidgets('CA-002: assinatura anual ativa exibe badge com label anual',
        (tester) async {
      await tester.pumpWidget(_buildTestApp(
        session: const SessionState(
          hasSession: true,
          accessStatus: AccessStatus.subscriptionActive,
          onboardingCompleted: true,
        ),
        statusState: const SubscriptionStatusLoaded(
          accessStatus: 'subscription_active',
          plan: 'annual',
        ),
      ));
      await tester.pump();

      expect(find.byKey(const Key('trial-status-badge')), findsOneWidget);
      expect(find.text('Assinatura anual ativa'), findsOneWidget);
      expect(find.byType(AwakenBlockedState), findsNothing);
    });

    testWidgets('CA-003: assinante ativo não vê estado bloqueado na subscription page',
        (tester) async {
      await tester.pumpWidget(_buildTestApp(
        session: const SessionState(
          hasSession: true,
          accessStatus: AccessStatus.subscriptionActive,
          onboardingCompleted: true,
        ),
        statusState: const SubscriptionStatusLoaded(
          accessStatus: 'subscription_active',
          plan: 'monthly',
        ),
      ));
      await tester.pump();

      expect(find.byType(AwakenBlockedState), findsNothing);
      expect(find.byKey(const Key('subscription-page')), findsOneWidget);
    });

    testWidgets(
        'subscription_active sem plan exibe label genérico como fallback',
        (tester) async {
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

      expect(find.byKey(const Key('trial-status-badge')), findsOneWidget);
      expect(find.text('Assinatura mensal ativa'), findsOneWidget);
    });
  });
```

- [ ] **Step 3.2: Rodar testes de widget para confirmar que passam**

```bash
cd apps/mobile && flutter test test/features/subscriptions/presentation/pages/subscription_page_test.dart
```

Expected: All tests pass.

- [ ] **Step 3.3: Adicionar testes de route guard US-019 em `route_resolver_test.dart`**

Dentro do `group('resolveInitialRoute', () {...})`, adicionar após o último `test` do grupo:

```dart
    test(
        'RN-005/US-019: assinante mensal sem onboarding vai para onboarding',
        () {
      const session = SessionState(
        hasSession: true,
        accessStatus: AccessStatus.subscriptionActive,
        onboardingCompleted: false,
      );
      expect(resolveInitialRoute(session), AppRoutes.onboarding);
    });

    test(
        'RN-006/US-019: assinante anual com onboarding completo vai para home',
        () {
      const session = SessionState(
        hasSession: true,
        accessStatus: AccessStatus.subscriptionActive,
        onboardingCompleted: true,
      );
      expect(resolveInitialRoute(session), AppRoutes.home);
    });
```

Dentro do `group('resolveRedirect', () {...})`, adicionar após o último `test` do grupo:

```dart
    test(
        'CA-003/US-019: assinante ativo é redirecionado para fora do paywall',
        () {
      const session = SessionState(
        hasSession: true,
        accessStatus: AccessStatus.subscriptionActive,
        onboardingCompleted: true,
      );
      expect(
        resolveRedirect(session: session, location: AppRoutes.pricingIntro),
        AppRoutes.home,
      );
    });

    test(
        'RN-005/US-019: assinante sem onboarding é redirecionado para onboarding',
        () {
      const session = SessionState(
        hasSession: true,
        accessStatus: AccessStatus.subscriptionActive,
        onboardingCompleted: false,
      );
      expect(
        resolveRedirect(session: session, location: AppRoutes.home),
        AppRoutes.onboarding,
      );
      expect(
        resolveRedirect(session: session, location: AppRoutes.onboarding),
        isNull,
      );
    });
```

- [ ] **Step 3.4: Rodar todos os testes Flutter**

```bash
cd apps/mobile && flutter test
```

Expected: All tests pass.

---

## Task 4: Backend Integration Test — GET /status após sync

**Files:**
- Modify: `backend/tests/Awaken.IntegrationTests/SubscriptionsStatusEndpointTests.cs`

- [ ] **Step 4.1: Adicionar teste de status subscription_active após sync**

No arquivo `SubscriptionsStatusEndpointTests.cs`, adicionar após o último `[Fact]` existente:

```csharp
[Fact]
public async Task GetStatusReturnsSubscriptionActiveWithPlanAfterSync()
{
    var token = await RegisterAndGetTokenAsync("subactive_status@awaken.app");
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var expiresAt = DateTime.UtcNow.AddDays(30);
    var syncPayload = new SyncEntitlementRequest("rc_customer_status_test", "pro_access", "monthly", expiresAt);
    await _client.PostAsJsonAsync("/api/subscriptions/sync", syncPayload);

    var response = await _client.GetAsync("/api/subscriptions/status");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var body = await response.Content.ReadFromJsonAsync<SubscriptionStatusResponse>();
    body!.AccessStatus.Should().Be("subscription_active");
    body.Plan.Should().Be("monthly");
    body.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));
    body.DaysRemaining.Should().BeInRange(29, 30);
    body.TrialStartedAt.Should().BeNull();
    body.TrialEndsAt.Should().BeNull();
}

[Fact]
public async Task GetStatusReturnsSubscriptionActiveForAnnualPlanAfterSync()
{
    var token = await RegisterAndGetTokenAsync("subactive_annual@awaken.app");
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var expiresAt = DateTime.UtcNow.AddDays(365);
    var syncPayload = new SyncEntitlementRequest("rc_customer_annual_test", "pro_access", "annual", expiresAt);
    await _client.PostAsJsonAsync("/api/subscriptions/sync", syncPayload);

    var response = await _client.GetAsync("/api/subscriptions/status");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var body = await response.Content.ReadFromJsonAsync<SubscriptionStatusResponse>();
    body!.AccessStatus.Should().Be("subscription_active");
    body.Plan.Should().Be("annual");
    body.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));
}

[Fact]
public async Task GetStatusPrioritizesPaidPlanOverExpiredTrial()
{
    var token = await RegisterAndGetTokenAsync("subpriority@awaken.app");
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    await _client.PostAsync("/api/subscriptions/trial/start", null);

    var expiresAt = DateTime.UtcNow.AddDays(30);
    var syncPayload = new SyncEntitlementRequest("rc_priority_test", "pro_access", "monthly", expiresAt);
    await _client.PostAsJsonAsync("/api/subscriptions/sync", syncPayload);

    var response = await _client.GetAsync("/api/subscriptions/status");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var body = await response.Content.ReadFromJsonAsync<SubscriptionStatusResponse>();
    body!.AccessStatus.Should().Be("subscription_active");
    body.Plan.Should().Be("monthly");
}
```

Verificar que o `using` para `SyncEntitlementRequest` já está no arquivo. Se não, adicionar no topo:

```csharp
using Awaken.Contracts.Subscriptions;
```

- [ ] **Step 4.2: Rodar os novos testes de integração**

```bash
dotnet test backend/tests/Awaken.IntegrationTests --filter "FullyQualifiedName~SubscriptionsStatusEndpointTests"
```

Expected: All tests pass (incluindo os 3 novos).

---

## Task 5: Verificação final

- [ ] **Step 5.1: `flutter analyze`**

```bash
cd apps/mobile && flutter analyze
```

Expected: No issues found.

- [ ] **Step 5.2: `flutter test` completo**

```bash
cd apps/mobile && flutter test
```

Expected: All tests pass.

- [ ] **Step 5.3: `dotnet test` backend completo**

```bash
dotnet test backend/
```

Expected: All tests pass.

---

## Self-Review Checklist

### Spec Coverage

| Requisito | Task |
|---|---|
| RN-001: assinatura mensal ativa libera acesso | Task 3 (CA-001 widget test + route_resolver RN-005/RN-006) |
| RN-002: assinatura anual ativa libera acesso | Task 3 (CA-002 widget test) |
| RN-003: assinatura expirada bloqueia (já coberto) | — (existente em `subscription_page_test` e `route_resolver_test`) |
| RN-004: assinante não vê paywall obrigatório | Task 3 (CA-003 widget test + CA-003 route test) |
| RN-005: assinante sem onboarding → onboarding | Task 3 (`route_resolver_test` RN-005 tests) |
| RN-006: assinante com onboarding → Home | Task 3 (`route_resolver_test` RN-006 test) |
| Estado "verificando assinatura" (loading) | Já existente em `subscription_page_test` |
| Estado "assinatura mensal ativa" | Task 1 (l10n) + Task 2 (UI) + Task 3 (CA-001) |
| Estado "assinatura anual ativa" | Task 1 (l10n) + Task 2 (UI) + Task 3 (CA-002) |
| Estado "erro de sincronização" | Já existente em testes (NetworkError) |
| GET /status retorna plan após sync | Task 4 (integração) |
| PT-BR, EN, ES | Task 1 (todas 3 línguas) |
| Analytics: cobertos na US-018 (sync controller) | — |

### Placeholder Scan
- Sem TBDs, TODOs ou "fill in details". Todos os steps têm código completo.

### Type Consistency
- `plan` em `SubscriptionStatusLoaded` é `String?` — consistente com o uso `plan == 'annual'` com null-safety via fallback para 'monthly'.
- `_resolveStatusLabel` recebe `l10n`, `accessStatus`, `daysRemaining` — consistente com chamada existente.
- `SyncEntitlementRequest` no teste de integração — mesmo record usado em `SubscriptionsSyncEndpointTests.cs`.
