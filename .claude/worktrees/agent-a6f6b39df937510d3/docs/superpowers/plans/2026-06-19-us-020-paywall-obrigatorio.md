# US-020 — Paywall Obrigatório para Acesso Expirado

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Exibir paywall obrigatório (com cards mensal/anual e analytics) quando trial ou assinatura expirar, e bloquear endpoints protegidos no backend com 403.

**Architecture:** Flutter side: SubscriptionPage já redireciona expirados, mas precisa de plan cards e evento `paywall_after_trial_viewed`. Backend: novo `ActiveAccessMiddleware` bloqueia paths protegidos retornando 403 para usuários com acesso expirado; paths de auth/subscriptions/app-config são allowlisted.

**Tech Stack:** Flutter/Riverpod/go_router, ASP.NET Core middleware pipeline, xUnit + Testcontainers (integração), flutter_test + flutter_riverpod

---

## File Map

| Arquivo | Ação |
|---|---|
| `apps/mobile/lib/l10n/app_pt.arb` | ADD chave `paywallProgressPreservedMessage` |
| `apps/mobile/lib/l10n/app_en.arb` | ADD chave `paywallProgressPreservedMessage` |
| `apps/mobile/lib/l10n/app_es.arb` | ADD chave `paywallProgressPreservedMessage` |
| `apps/mobile/lib/features/subscriptions/presentation/pages/subscription_page.dart` | MODIFY — substituir `AwakenBlockedState` por `_PaywallContent` com plan cards + analytics |
| `apps/mobile/test/features/subscriptions/presentation/pages/subscription_page_test.dart` | ADD — testes US-020: cards, analytics, subscription_expired |
| `backend/src/Awaken.Api/Middlewares/ActiveAccessMiddleware.cs` | CREATE |
| `backend/src/Awaken.Api/Program.cs` | MODIFY — registrar middleware |
| `backend/tests/Awaken.IntegrationTests/AccessBlockedEndpointTests.cs` | CREATE |

---

## Task 1: l10n — chave paywallProgressPreservedMessage

**Files:**
- Modify: `apps/mobile/lib/l10n/app_pt.arb`
- Modify: `apps/mobile/lib/l10n/app_en.arb`
- Modify: `apps/mobile/lib/l10n/app_es.arb`

- [ ] Adicionar ao final de cada ARB (antes do `}` final), após a última chave de syncEntitlement:

**app_pt.arb:**
```json
  "paywallProgressPreservedMessage": "Seu progresso está salvo. Continue de onde parou.",
  "@paywallProgressPreservedMessage": { "description": "Mensagem no paywall indicando que o progresso do usuário foi preservado" }
```

**app_en.arb:**
```json
  "paywallProgressPreservedMessage": "Your progress is saved. Pick up where you left off.",
  "@paywallProgressPreservedMessage": { "description": "Paywall message indicating user progress is preserved" }
```

**app_es.arb:**
```json
  "paywallProgressPreservedMessage": "Tu progreso está guardado. Continúa donde lo dejaste.",
  "@paywallProgressPreservedMessage": { "description": "Mensaje en el paywall indicando que el progreso del usuario fue preservado" }
```

- [ ] Rodar `flutter gen-l10n` em `apps/mobile/`:
```
cd apps/mobile && flutter gen-l10n
```

---

## Task 2: SubscriptionPage — plan cards + analytics

**Files:**
- Modify: `apps/mobile/lib/features/subscriptions/presentation/pages/subscription_page.dart`

Substituir o branch `if (session != null && session.isAccessExpired)` por um widget `_PaywallContent` que:
1. Exibe ícone de cadeado + título + descrição (trial vs subscription)
2. Exibe mensagem `paywallProgressPreservedMessage`
3. Exibe dois cards: Mensal e Anual (dados de `PlansConfigDto.fallback`)
4. Exibe botão "Assinar agora" que chama `_presentPaywall()`
5. Exibe botão "Restaurar compras" secundário
6. Dispara analytics `paywall_after_trial_viewed` na primeira exibição (trial_expired only)

- [ ] Adicionar ao `_SubscriptionPageState`:
```dart
bool _paywallViewedLogged = false;
```

- [ ] Em `initState`, após os callbacks existentes, adicionar lógica para logar evento quando sessão está expirada:
```dart
WidgetsBinding.instance.addPostFrameCallback((_) async {
  final session = ref.read(currentSessionStateProvider);
  if (session != null && session.isAccessExpired && !_paywallViewedLogged) {
    _paywallViewedLogged = true;
    final analytics = ref.read(analyticsServiceProvider);
    if (session.accessStatus == AccessStatus.trialExpired) {
      await analytics.logEvent('paywall_after_trial_viewed');
    }
    await analytics.logEvent('access_blocked');
  }
  // ... resto do initState existente
});
```

- [ ] Substituir o branch expired no `build()` por `_PaywallContent`:
```dart
if (session != null && session.isAccessExpired) {
  return Scaffold(
    backgroundColor: AwakenColors.backgroundPrimary,
    body: Stack(
      children: [
        const AwakenParticlesLayer(),
        SafeArea(
          child: _PaywallContent(
            key: const Key('paywall-content'),
            accessStatus: session.accessStatus!,
            isActionLoading: _isRevenueCatActionRunning,
            onSubscribe: _presentPaywall,
            onRestore: _restorePurchases,
          ),
        ),
      ],
    ),
  );
}
```

- [ ] Adicionar widget `_PaywallContent` no arquivo:
```dart
class _PaywallContent extends StatelessWidget {
  const _PaywallContent({
    super.key,
    required this.accessStatus,
    required this.isActionLoading,
    required this.onSubscribe,
    required this.onRestore,
  });

  final AccessStatus accessStatus;
  final bool isActionLoading;
  final VoidCallback onSubscribe;
  final VoidCallback onRestore;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final plans = PlansConfigDto.fallback.plans;
    final monthly = plans.firstWhere((p) => p.id == 'monthly');
    final annual = plans.firstWhere((p) => p.id == 'annual');
    final description = accessStatus == AccessStatus.trialExpired
        ? l10n.accessBlockedTrialDescription
        : l10n.accessBlockedSubscriptionDescription;

    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(AwakenSpacing.lg),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              Icons.lock_clock_outlined,
              color: AwakenColors.amber,
              size: 48,
            ),
            const SizedBox(height: AwakenSpacing.md),
            Text(
              l10n.accessBlockedTitle,
              key: const Key('paywall-title'),
              style: AwakenTypography.displayMedium,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: AwakenSpacing.sm),
            Text(
              description,
              key: const Key('paywall-description'),
              style: AwakenTypography.bodyMedium.copyWith(
                color: AwakenColors.textSecondary,
              ),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: AwakenSpacing.sm),
            Text(
              l10n.paywallProgressPreservedMessage,
              key: const Key('paywall-progress-preserved'),
              style: AwakenTypography.bodySmall.copyWith(
                color: AwakenColors.success,
              ),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: AwakenSpacing.lg),
            _PlanCard(
              key: const Key('paywall-plan-monthly'),
              plan: monthly,
              onTap: onSubscribe,
            ),
            const SizedBox(height: AwakenSpacing.sm),
            _PlanCard(
              key: const Key('paywall-plan-annual'),
              plan: annual,
              onTap: onSubscribe,
            ),
            const SizedBox(height: AwakenSpacing.lg),
            AwakenButton(
              key: const Key('paywall-subscribe-button'),
              label: l10n.subscribeButton,
              onPressed: onSubscribe,
              isLoading: isActionLoading,
            ),
            const SizedBox(height: AwakenSpacing.sm),
            AwakenButton(
              key: const Key('paywall-restore-button'),
              label: l10n.restorePurchases,
              onPressed: onRestore,
              isLoading: isActionLoading,
              variant: AwakenButtonVariant.secondary,
            ),
          ],
        ),
      ),
    );
  }
}

class _PlanCard extends StatelessWidget {
  const _PlanCard({
    super.key,
    required this.plan,
    required this.onTap,
  });

  final PlanConfigDto plan;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: double.infinity,
        padding: const EdgeInsets.all(AwakenSpacing.md),
        decoration: BoxDecoration(
          color: plan.highlighted
              ? AwakenColors.xp.withValues(alpha: 0.10)
              : AwakenColors.surface,
          borderRadius: BorderRadius.circular(AwakenSpacing.cardRadius),
          border: Border.all(
            color: plan.highlighted
                ? AwakenColors.xp.withValues(alpha: 0.50)
                : AwakenColors.border,
          ),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(plan.label,
                    style: AwakenTypography.labelLarge.copyWith(
                      color: plan.highlighted
                          ? AwakenColors.xp
                          : AwakenColors.textPrimary,
                    )),
                Text(
                  '${plan.priceLabel}${plan.periodLabel}',
                  style: AwakenTypography.bodyMedium.copyWith(
                    color: plan.highlighted
                        ? AwakenColors.xp
                        : AwakenColors.textSecondary,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
            if (plan.savingLabel != null) ...[
              const SizedBox(height: AwakenSpacing.xs),
              Text(
                plan.savingLabel!,
                style: AwakenTypography.bodySmall.copyWith(
                  color: AwakenColors.success,
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
```

- [ ] Adicionar imports necessários no topo do arquivo:
```dart
import '../../data/dtos/plans_config_dto.dart'; // já via pricing feature
```
Nota: `PlansConfigDto` está em `apps/mobile/lib/features/pricing/data/dtos/plans_config_dto.dart`.

Import correto:
```dart
import '../../../pricing/data/dtos/plans_config_dto.dart';
```

- [ ] Verificar tokens existentes: `AwakenColors.surface`, `AwakenColors.border`, `AwakenColors.xs`, `AwakenColors.cardRadius`, `AwakenTypography.bodySmall`

---

## Task 3: Testes Flutter — US-020

**Files:**
- Modify: `apps/mobile/test/features/subscriptions/presentation/pages/subscription_page_test.dart`

- [ ] Adicionar stub de analytics que capture eventos:
```dart
class _CapturingAnalyticsService implements AnalyticsService {
  final events = <String>[];
  @override
  Future<void> logEvent(String name, {Map<String, dynamic>? params}) async =>
      events.add(name);
}
```

- [ ] Atualizar `_buildTestApp` para aceitar o analytics service como parâmetro:
```dart
Widget _buildTestApp({
  SessionState? session,
  SubscriptionStatusState? statusState,
  AnalyticsService? analytics,
})
```

- [ ] Adicionar grupo `US-020 — paywall obrigatório`:
```dart
group('SubscriptionPage — US-020 paywall obrigatório', () {
  testWidgets('CA-001: trial_expired exibe _PaywallContent com título',
      (tester) async {
    await tester.pumpWidget(_buildTestApp(
      session: const SessionState(
        hasSession: true,
        accessStatus: AccessStatus.trialExpired,
        onboardingCompleted: true,
      ),
    ));
    await tester.pump();

    expect(find.byKey(const Key('paywall-content')), findsOneWidget);
    expect(find.byKey(const Key('paywall-title')), findsOneWidget);
    expect(find.byKey(const Key('paywall-description')), findsOneWidget);
    expect(find.byKey(const Key('paywall-progress-preserved')), findsOneWidget);
  });

  testWidgets('CA-001: subscription_expired exibe paywall com descrição correta',
      (tester) async {
    await tester.pumpWidget(_buildTestApp(
      session: const SessionState(
        hasSession: true,
        accessStatus: AccessStatus.subscriptionExpired,
        onboardingCompleted: true,
      ),
    ));
    await tester.pump();

    expect(find.byKey(const Key('paywall-content')), findsOneWidget);
    expect(
      find.text('Sua assinatura expirou. Renove para continuar evoluindo.'),
      findsOneWidget,
    );
  });

  testWidgets('RN-003: cards de plano mensal e anual visíveis',
      (tester) async {
    await tester.pumpWidget(_buildTestApp(
      session: const SessionState(
        hasSession: true,
        accessStatus: AccessStatus.trialExpired,
        onboardingCompleted: true,
      ),
    ));
    await tester.pump();

    expect(find.byKey(const Key('paywall-plan-monthly')), findsOneWidget);
    expect(find.byKey(const Key('paywall-plan-annual')), findsOneWidget);
  });

  testWidgets('RN-003: CTA de assinatura e restauração visíveis',
      (tester) async {
    await tester.pumpWidget(_buildTestApp(
      session: const SessionState(
        hasSession: true,
        accessStatus: AccessStatus.trialExpired,
        onboardingCompleted: true,
      ),
    ));
    await tester.pump();

    expect(find.byKey(const Key('paywall-subscribe-button')), findsOneWidget);
    expect(find.byKey(const Key('paywall-restore-button')), findsOneWidget);
  });

  testWidgets('RN-004: mensagem de progresso preservado visível',
      (tester) async {
    await tester.pumpWidget(_buildTestApp(
      session: const SessionState(
        hasSession: true,
        accessStatus: AccessStatus.trialExpired,
        onboardingCompleted: true,
      ),
    ));
    await tester.pump();

    expect(find.byKey(const Key('paywall-progress-preserved')), findsOneWidget);
    expect(
      find.text('Seu progresso está salvo. Continue de onde parou.'),
      findsOneWidget,
    );
  });

  testWidgets('analytics: paywall_after_trial_viewed disparado ao exibir paywall de trial',
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

    expect(analytics.events, contains('paywall_after_trial_viewed'));
    expect(analytics.events, contains('access_blocked'));
  });

  testWidgets('analytics: paywall_after_trial_viewed NÃO disparado para subscription_expired',
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

    expect(analytics.events, isNot(contains('paywall_after_trial_viewed')));
    expect(analytics.events, contains('access_blocked'));
  });

  testWidgets('RN-001/RN-002: usuário ativo não vê paywall',
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

    expect(find.byKey(const Key('paywall-content')), findsNothing);
  });
});
```

---

## Task 4: Backend — ActiveAccessMiddleware

**Files:**
- Create: `backend/src/Awaken.Api/Middlewares/ActiveAccessMiddleware.cs`

```csharp
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Repositories;

namespace Awaken.Api.Middlewares;

public class ActiveAccessMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> _allowedPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth",
        "/api/subscriptions",
        "/api/app-config",
        "/swagger",
        "/health",
    };

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUserService currentUserService,
        IUserRepository userRepository,
        ISubscriptionRepository subscriptionRepository,
        IDateTimeService dateTimeService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (_allowedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var userId = currentUserService.UserId;
        var user = await userRepository.GetByIdAsync(userId, context.RequestAborted);

        if (user is null)
        {
            await next(context);
            return;
        }

        var subscription = await subscriptionRepository.GetByUserIdAsync(userId, context.RequestAborted);
        var utcNow = dateTimeService.UtcNow;

        string accessStatus;
        if (subscription?.Plan is "monthly" or "annual")
        {
            accessStatus = subscription.ExpiresAt > utcNow ? "subscription_active" : "subscription_expired";
        }
        else
        {
            accessStatus = user.ComputeAccessStatus(utcNow);
        }

        if (accessStatus is "trial_expired" or "subscription_expired")
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                code = "ACCESS_BLOCKED",
                accessStatus,
                correlationId = context.Items.TryGetValue("CorrelationId", out var id) ? id : null,
            });
            return;
        }

        await next(context);
    }
}
```

---

## Task 5: Registrar middleware em Program.cs

**Files:**
- Modify: `backend/src/Awaken.Api/Program.cs`

Adicionar após `app.UseAuthorization()`:
```csharp
app.UseMiddleware<ActiveAccessMiddleware>();
```

---

## Task 6: Teste de integração backend — AccessBlockedEndpointTests

**Files:**
- Create: `backend/tests/Awaken.IntegrationTests/AccessBlockedEndpointTests.cs`

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Awaken.Contracts.Auth;
using Awaken.Contracts.Subscriptions;
using Awaken.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Awaken.IntegrationTests;

public class AccessBlockedEndpointTests : IAsyncLifetime
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

    [Fact]
    public async Task SubscriptionExpiredUserIsBlockedFromProtectedPath()
    {
        var token = await RegisterAndGetTokenAsync("blocked_sub@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Sync with expired subscription
        var expiresAt = DateTime.UtcNow.AddDays(-1);
        var syncPayload = new SyncEntitlementRequest("rc_blocked_test", "pro_access", "monthly", expiresAt);
        await _client.PostAsJsonAsync("/api/subscriptions/sync", syncPayload);

        // Try a protected path (não existe mas o middleware deve retornar 403 antes do roteamento)
        var response = await _client.GetAsync("/api/quests");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SubscriptionExpiredUserCanStillAccessSubscriptionEndpoints()
    {
        var token = await RegisterAndGetTokenAsync("blocked_sub_allow@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var expiresAt = DateTime.UtcNow.AddDays(-1);
        var syncPayload = new SyncEntitlementRequest("rc_blocked_allow_test", "pro_access", "monthly", expiresAt);
        await _client.PostAsJsonAsync("/api/subscriptions/sync", syncPayload);

        // /api/subscriptions/status deve ser acessível mesmo expirado
        var response = await _client.GetAsync("/api/subscriptions/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionStatusResponse>();
        body!.AccessStatus.Should().Be("subscription_expired");
    }

    [Fact]
    public async Task ActiveSubscriptionUserCanAccessProtectedPath()
    {
        var token = await RegisterAndGetTokenAsync("active_sub@awaken.app");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Sync with active subscription
        var expiresAt = DateTime.UtcNow.AddDays(30);
        var syncPayload = new SyncEntitlementRequest("rc_active_test", "pro_access", "monthly", expiresAt);
        await _client.PostAsJsonAsync("/api/subscriptions/sync", syncPayload);

        // Protected path → ativo passa pelo middleware, recebe 404 (rota não existe) não 403
        var response = await _client.GetAsync("/api/quests");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnauthenticatedRequestIsNotBlockedByMiddleware()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        // Sem token → middleware não bloqueia (JWT middleware cuida disso)
        var response = await _client.GetAsync("/api/quests");

        // JWT não configurado → 401, não 403
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```
