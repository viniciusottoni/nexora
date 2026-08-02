# AwakenParticlesLayer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create an ambient particle effect widget (luminous dust in blue/purple tones falling from the top of the screen) and integrate it into all app pages except SplashPage.

**Architecture:** A single `AwakenParticlesLayer` StatefulWidget using a raw `Ticker` (not `AnimationController`) to track elapsed time. A `CustomPainter` draws 25 circular particles per frame on a `RepaintBoundary`-wrapped `CustomPaint`, computing each particle's position and opacity independently from its own random `durationMs`. Each page inserts the widget as an early child of a `Stack` — after background layers, before all UI content.

**Tech Stack:** Flutter, `dart:math`, `SingleTickerProviderStateMixin`, `CustomPainter`, `RepaintBoundary`

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| **Create** | `apps/mobile/lib/design_system/components/awaken_particles_layer.dart` | `_Particle` data class, `_AwakenParticlesPainter`, `AwakenParticlesLayer` widget |
| **Create** | `apps/mobile/test/design_system/components/awaken_particles_layer_test.dart` | Widget tests |
| **Modify** | `apps/mobile/lib/features/auth/presentation/pages/login_page.dart` | Insert particle layer inside existing Stack (Pattern A) |
| **Modify** | `apps/mobile/lib/features/auth/presentation/pages/register_page.dart` | Insert particle layer inside existing Stack (Pattern A) |
| **Modify** | `apps/mobile/lib/features/auth/presentation/pages/forgot_password_page.dart` | Wrap body in Stack (Pattern B) |
| **Modify** | `apps/mobile/lib/features/auth/presentation/pages/delete_account_page.dart` | Wrap body in Stack (Pattern B) |
| **Modify** | `apps/mobile/lib/features/settings/presentation/pages/settings_page.dart` | Wrap body in Stack (Pattern B) |
| **Modify** | `apps/mobile/lib/features/subscriptions/presentation/pages/subscription_page.dart` | Wrap body in Stack (Pattern B) |
| **Modify** | `apps/mobile/lib/features/pricing/presentation/pages/pricing_page.dart` | Wrap body in Stack (Pattern B) |
| **Modify** | `apps/mobile/lib/features/home/presentation/pages/home_page.dart` | Wrap body in Stack (Pattern B) |
| **Modify** | `apps/mobile/lib/features/settings/presentation/pages/language_settings_page.dart` | Wrap body in Stack (Pattern B) |
| **Modify** | `apps/mobile/lib/features/onboarding/presentation/pages/onboarding_page.dart` | Wrap body in Stack (Pattern B) |
| **Modify** | `apps/mobile/lib/features/hunter_profile/presentation/pages/hunter_card_page.dart` | Wrap body in Stack (Pattern B) |
| **Modify** | `apps/mobile/lib/features/quests/presentation/pages/daily_quest_page.dart` | Wrap body in Stack (Pattern B) |
| **Modify** | `apps/mobile/lib/features/nutrition/presentation/pages/nutrition_page.dart` | Wrap body in Stack (Pattern B) |
| **Modify** | `apps/mobile/lib/features/progression/presentation/pages/progression_page.dart` | Wrap body in Stack (Pattern B) |

---

## Task 1: Widget tests for AwakenParticlesLayer

**Files:**
- Create: `apps/mobile/test/design_system/components/awaken_particles_layer_test.dart`

- [ ] **Step 1: Write the failing test**

```dart
// apps/mobile/test/design_system/components/awaken_particles_layer_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:awaken/design_system/components/awaken_particles_layer.dart';

void main() {
  group('AwakenParticlesLayer', () {
    testWidgets('renders without errors inside a MaterialApp', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: Stack(
              children: [AwakenParticlesLayer()],
            ),
          ),
        ),
      );

      expect(find.byType(AwakenParticlesLayer), findsOneWidget);
      expect(tester.takeException(), isNull);
    });

    testWidgets('does not throw when screen size is zero', (tester) async {
      await tester.binding.setSurfaceSize(Size.zero);
      addTearDown(() => tester.binding.setSurfaceSize(null));

      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: Stack(
              children: [AwakenParticlesLayer()],
            ),
          ),
        ),
      );

      expect(tester.takeException(), isNull);
    });

    testWidgets('disposes ticker without leak', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: Stack(
              children: [AwakenParticlesLayer()],
            ),
          ),
        ),
      );

      // Replace widget tree to trigger dispose
      await tester.pumpWidget(const MaterialApp(home: Scaffold()));
      await tester.pumpAndSettle();

      expect(tester.takeException(), isNull);
    });
  });
}
```

- [ ] **Step 2: Run test — expect FAIL (class not found)**

```bash
cd apps/mobile && flutter test test/design_system/components/awaken_particles_layer_test.dart
```

Expected: compile error — `Target of URI doesn't exist: 'package:awaken/design_system/components/awaken_particles_layer.dart'`

---

## Task 2: Implement AwakenParticlesLayer

**Files:**
- Create: `apps/mobile/lib/design_system/components/awaken_particles_layer.dart`

- [ ] **Step 1: Create the widget**

```dart
// apps/mobile/lib/design_system/components/awaken_particles_layer.dart
import 'dart:math';
import 'package:flutter/material.dart';
import 'package:flutter/scheduler.dart';
import '../tokens/colors.dart';

class AwakenParticlesLayer extends StatefulWidget {
  const AwakenParticlesLayer({super.key});

  @override
  State<AwakenParticlesLayer> createState() => _AwakenParticlesLayerState();
}

class _AwakenParticlesLayerState extends State<AwakenParticlesLayer>
    with SingleTickerProviderStateMixin {
  late final Ticker _ticker;
  Duration _elapsed = Duration.zero;
  late final List<_Particle> _particles;

  @override
  void initState() {
    super.initState();
    _particles = List.generate(25, (_) => _Particle.random(Random()));
    _ticker = createTicker((elapsed) {
      setState(() => _elapsed = elapsed);
    })..start();
  }

  @override
  void dispose() {
    _ticker.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return RepaintBoundary(
      child: CustomPaint(
        painter: _AwakenParticlesPainter(_elapsed, _particles),
        size: Size.infinite,
        isComplex: true,
      ),
    );
  }
}

class _Particle {
  const _Particle({
    required this.xFraction,
    required this.durationMs,
    required this.phaseOffset,
    required this.driftAmplitude,
    required this.driftFrequency,
    required this.radius,
    required this.color,
  });

  final double xFraction;
  final int durationMs;
  final double phaseOffset;
  final double driftAmplitude;
  final double driftFrequency;
  final double radius;
  final Color color;

  static _Particle random(Random rng) {
    // 40% primary (blue), 40% primaryAlt (purple), 20% borderFocus (light blue)
    const colors = [
      AwakenColors.primary,
      AwakenColors.primary,
      AwakenColors.primaryAlt,
      AwakenColors.primaryAlt,
      AwakenColors.borderFocus,
    ];
    return _Particle(
      xFraction: rng.nextDouble(),
      durationMs: 1000 + rng.nextInt(2001), // 1000–3000 ms
      phaseOffset: rng.nextDouble(),
      driftAmplitude: 2.0 + rng.nextDouble() * 6.0, // 2.0–8.0 px
      driftFrequency: 0.8 + rng.nextDouble() * 1.2, // 0.8–2.0
      radius: 0.8 + rng.nextDouble() * 1.4, // 0.8–2.2 px
      color: colors[rng.nextInt(colors.length)],
    );
  }
}

class _AwakenParticlesPainter extends CustomPainter {
  const _AwakenParticlesPainter(this.elapsed, this.particles);

  final Duration elapsed;
  final List<_Particle> particles;

  @override
  void paint(Canvas canvas, Size size) {
    if (size.isEmpty) return;

    for (final p in particles) {
      final progress =
          ((elapsed.inMilliseconds / p.durationMs) + p.phaseOffset) % 1.0;

      final double opacity;
      if (progress < 0.12) {
        opacity = (progress / 0.12) * 0.45;
      } else if (progress < 0.72) {
        opacity = 0.45;
      } else {
        opacity = ((1.0 - progress) / 0.28) * 0.45;
      }

      final x = (p.xFraction * size.width) +
          sin(progress * p.driftFrequency * 2 * pi) * p.driftAmplitude;
      final y = progress * size.height;

      canvas.drawCircle(
        Offset(x, y),
        p.radius,
        Paint()..color = p.color.withValues(alpha: opacity),
      );
    }
  }

  @override
  bool shouldRepaint(_AwakenParticlesPainter oldDelegate) => true;
}
```

- [ ] **Step 2: Run test — expect PASS**

```bash
cd apps/mobile && flutter test test/design_system/components/awaken_particles_layer_test.dart
```

Expected: `All tests passed!`

- [ ] **Step 3: Commit**

```bash
cd apps/mobile && git add lib/design_system/components/awaken_particles_layer.dart test/design_system/components/awaken_particles_layer_test.dart
git commit -m "feat: add AwakenParticlesLayer to design system"
```

---

## Task 3: Integrate into login and register pages (Pattern A)

Login and register already use `Stack(fit: StackFit.expand)` with a background image + gradient overlay. Insert `AwakenParticlesLayer` as the third child (after the gradient `Container`, before `SafeArea`).

**Files:**
- Modify: `apps/mobile/lib/features/auth/presentation/pages/login_page.dart`
- Modify: `apps/mobile/lib/features/auth/presentation/pages/register_page.dart`

- [ ] **Step 1: Update login_page.dart**

Add import after the existing design system imports:
```dart
import '../../../../design_system/components/awaken_particles_layer.dart';
```

In the `build` method, find the `Stack` children and insert the particle layer between the gradient `Container` and `SafeArea`. The Stack currently reads:

```dart
Stack(
  fit: StackFit.expand,
  children: [
    Opacity(
      opacity: 0.5,
      child: Image.asset(
        'assets/images/background-login.jpg',
        key: const Key('login-background'),
        fit: BoxFit.cover,
      ),
    ),
    Container(
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
          colors: [
            AwakenColors.backgroundPrimary.withValues(alpha: 0.16),
            AwakenColors.backgroundPrimary.withValues(alpha: 0.72),
          ],
        ),
      ),
    ),
    SafeArea(
```

Change it to:

```dart
Stack(
  fit: StackFit.expand,
  children: [
    Opacity(
      opacity: 0.5,
      child: Image.asset(
        'assets/images/background-login.jpg',
        key: const Key('login-background'),
        fit: BoxFit.cover,
      ),
    ),
    Container(
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
          colors: [
            AwakenColors.backgroundPrimary.withValues(alpha: 0.16),
            AwakenColors.backgroundPrimary.withValues(alpha: 0.72),
          ],
        ),
      ),
    ),
    const AwakenParticlesLayer(),
    SafeArea(
```

- [ ] **Step 2: Update register_page.dart**

Add import after the existing design system imports:
```dart
import '../../../../design_system/components/awaken_particles_layer.dart';
```

Find the `Stack` in the `build` method. It currently reads:

```dart
Stack(
  fit: StackFit.expand,
  children: [
    Opacity(
      opacity: 0.5,
      child: Image.asset(
        'assets/images/background-register.jpg',
        fit: BoxFit.cover,
      ),
    ),
    Container(
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
          colors: [
            Colors.black.withValues(alpha: 0.35),
            Colors.black.withValues(alpha: 0.62),
          ],
        ),
      ),
    ),
    SafeArea(
```

Change it to:

```dart
Stack(
  fit: StackFit.expand,
  children: [
    Opacity(
      opacity: 0.5,
      child: Image.asset(
        'assets/images/background-register.jpg',
        fit: BoxFit.cover,
      ),
    ),
    Container(
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
          colors: [
            Colors.black.withValues(alpha: 0.35),
            Colors.black.withValues(alpha: 0.62),
          ],
        ),
      ),
    ),
    const AwakenParticlesLayer(),
    SafeArea(
```

- [ ] **Step 3: Run existing auth page tests**

```bash
cd apps/mobile && flutter test test/features/auth/presentation/pages/login_page_test.dart test/features/auth/presentation/pages/register_page_test.dart
```

Expected: all pass.

- [ ] **Step 4: Commit**

```bash
git add apps/mobile/lib/features/auth/presentation/pages/login_page.dart apps/mobile/lib/features/auth/presentation/pages/register_page.dart
git commit -m "feat: add particle layer to login and register pages"
```

---

## Task 4: Integrate into pages with appBar + SafeArea body (Pattern B — group 1)

These pages have `Scaffold(appBar: ..., body: SafeArea(...))` or `Scaffold(appBar: ..., body: ListView(...))`. The body becomes a `Stack` with `AwakenParticlesLayer` as the first child.

**Files:**
- Modify: `apps/mobile/lib/features/auth/presentation/pages/forgot_password_page.dart`
- Modify: `apps/mobile/lib/features/auth/presentation/pages/delete_account_page.dart`
- Modify: `apps/mobile/lib/features/settings/presentation/pages/settings_page.dart`

- [ ] **Step 1: Update forgot_password_page.dart**

Add import:
```dart
import '../../../../design_system/components/awaken_particles_layer.dart';
```

In `build`, the `Scaffold` body currently is:
```dart
body: SafeArea(
  child: Center(
    child: SingleChildScrollView(
      padding: const EdgeInsets.all(AwakenSpacing.lg),
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 420),
        child: isSuccess
            ? _SuccessContent(l10n: l10n, onBack: () => context.pop())
            : _FormContent(
                l10n: l10n,
                emailController: _emailController,
                emailError: _emailError(l10n, state),
                isSubmitting: isSubmitting,
                onSubmit: isSubmitting ? null : _submit,
              ),
      ),
    ),
  ),
),
```

Change it to:
```dart
body: Stack(
  children: [
    const AwakenParticlesLayer(),
    SafeArea(
      child: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(AwakenSpacing.lg),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 420),
            child: isSuccess
                ? _SuccessContent(l10n: l10n, onBack: () => context.pop())
                : _FormContent(
                    l10n: l10n,
                    emailController: _emailController,
                    emailError: _emailError(l10n, state),
                    isSubmitting: isSubmitting,
                    onSubmit: isSubmitting ? null : _submit,
                  ),
          ),
        ),
      ),
    ),
  ],
),
```

- [ ] **Step 2: Update delete_account_page.dart**

Add import:
```dart
import '../../../../design_system/components/awaken_particles_layer.dart';
```

In `build`, the `Scaffold` body currently is:
```dart
body: SafeArea(
  child: Center(
    child: SingleChildScrollView(
      padding: const EdgeInsets.all(AwakenSpacing.lg),
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 420),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Icon(
              Icons.delete_forever_outlined,
              size: 64,
              color: AwakenColors.error,
            ),
            // ... rest of column children
          ],
        ),
      ),
    ),
  ),
),
```

Change it to:
```dart
body: Stack(
  children: [
    const AwakenParticlesLayer(),
    SafeArea(
      child: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(AwakenSpacing.lg),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 420),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const Icon(
                  Icons.delete_forever_outlined,
                  size: 64,
                  color: AwakenColors.error,
                ),
                const SizedBox(height: AwakenSpacing.lg),
                Text(
                  l10n.deleteAccountTitle,
                  textAlign: TextAlign.center,
                  style: AwakenTypography.displayMedium,
                ),
                const SizedBox(height: AwakenSpacing.sm),
                Text(
                  l10n.deleteAccountWarning,
                  textAlign: TextAlign.center,
                  style: AwakenTypography.bodyMedium,
                ),
                if (hasActiveSubscription) ...[
                  const SizedBox(height: AwakenSpacing.md),
                  Container(
                    padding: const EdgeInsets.all(AwakenSpacing.md),
                    decoration: BoxDecoration(
                      color: AwakenColors.warning.withValues(alpha: 0.12),
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(
                          color: AwakenColors.warning.withValues(alpha: 0.4)),
                    ),
                    child: Text(
                      l10n.deleteAccountSubscriptionWarning,
                      textAlign: TextAlign.center,
                      style: AwakenTypography.bodySmall,
                    ),
                  ),
                ],
                const SizedBox(height: AwakenSpacing.xl),
                AwakenButton(
                  key: const Key('delete-account-button'),
                  label: l10n.deleteAccountButton,
                  isLoading: isLoading,
                  variant: AwakenButtonVariant.secondary,
                  onPressed:
                      isLoading ? null : () => _confirmDelete(context, ref, l10n),
                ),
              ],
            ),
          ),
        ),
      ),
    ),
  ],
),
```

- [ ] **Step 3: Update settings_page.dart**

Add import:
```dart
import '../../../../design_system/components/awaken_particles_layer.dart';
```

In `build`, the `Scaffold` body currently is:
```dart
body: ListView(
  children: [
    _SettingsTile(...),
    const Divider(height: 1, indent: AwakenSpacing.lg),
    _SettingsTile(...),
    const Divider(height: 1, indent: AwakenSpacing.lg),
    _SettingsTile(...),
  ],
),
```

Change it to:
```dart
body: Stack(
  children: [
    const AwakenParticlesLayer(),
    ListView(
      children: [
        _SettingsTile(...),
        const Divider(height: 1, indent: AwakenSpacing.lg),
        _SettingsTile(...),
        const Divider(height: 1, indent: AwakenSpacing.lg),
        _SettingsTile(...),
      ],
    ),
  ],
),
```

- [ ] **Step 4: Run tests for these pages**

```bash
cd apps/mobile && flutter test test/features/auth/presentation/pages/forgot_password_page_test.dart test/features/auth/presentation/pages/delete_account_page_test.dart test/features/settings/presentation/pages/settings_page_test.dart
```

Expected: all pass.

- [ ] **Step 5: Commit**

```bash
git add apps/mobile/lib/features/auth/presentation/pages/forgot_password_page.dart apps/mobile/lib/features/auth/presentation/pages/delete_account_page.dart apps/mobile/lib/features/settings/presentation/pages/settings_page.dart
git commit -m "feat: add particle layer to forgot-password, delete-account, settings pages"
```

---

## Task 5: Integrate into remaining pages (Pattern B — group 2)

These are either stub pages or pages with a non-appBar scaffold body. All follow the same pattern: wrap the existing `body` value in a `Stack` with `AwakenParticlesLayer` as the first child.

**Files:**
- Modify: `apps/mobile/lib/features/subscriptions/presentation/pages/subscription_page.dart`
- Modify: `apps/mobile/lib/features/pricing/presentation/pages/pricing_page.dart`
- Modify: `apps/mobile/lib/features/home/presentation/pages/home_page.dart`
- Modify: `apps/mobile/lib/features/settings/presentation/pages/language_settings_page.dart`
- Modify: `apps/mobile/lib/features/onboarding/presentation/pages/onboarding_page.dart`
- Modify: `apps/mobile/lib/features/hunter_profile/presentation/pages/hunter_card_page.dart`
- Modify: `apps/mobile/lib/features/quests/presentation/pages/daily_quest_page.dart`
- Modify: `apps/mobile/lib/features/nutrition/presentation/pages/nutrition_page.dart`
- Modify: `apps/mobile/lib/features/progression/presentation/pages/progression_page.dart`

- [ ] **Step 1: Update subscription_page.dart**

Add import:
```dart
import '../../../../design_system/components/awaken_particles_layer.dart';
```

Change `body:` from:
```dart
body: SafeArea(
  child: Center(
    child: session != null && session.isAccessExpired
        ? AwakenBlockedState(
            accessStatus: session.accessStatus!,
            onSubscribe: () {},
          )
        : Text(
            l10n.subscriptionTitle,
            style: AwakenTypography.displayMedium,
          ),
  ),
),
```

To:
```dart
body: Stack(
  children: [
    const AwakenParticlesLayer(),
    SafeArea(
      child: Center(
        child: session != null && session.isAccessExpired
            ? AwakenBlockedState(
                accessStatus: session.accessStatus!,
                onSubscribe: () {},
              )
            : Text(
                l10n.subscriptionTitle,
                style: AwakenTypography.displayMedium,
              ),
      ),
    ),
  ],
),
```

- [ ] **Step 2: Update pricing_page.dart**

Add import:
```dart
import '../../../../design_system/components/awaken_particles_layer.dart';
```

Change `body:` from:
```dart
body: LanguageSelectorOverlay(
  body: SafeArea(
    child: Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            l10n.pricingTitle,
            textAlign: TextAlign.center,
            style: AwakenTypography.displayMedium,
          ),
          const SizedBox(height: 24),
          TextButton(
            onPressed: () => context.go(AppRoutes.register),
            child: Text(l10n.pricingContinueFree),
          ),
        ],
      ),
    ),
  ),
),
```

To:
```dart
body: Stack(
  children: [
    const AwakenParticlesLayer(),
    LanguageSelectorOverlay(
      body: SafeArea(
        child: Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                l10n.pricingTitle,
                textAlign: TextAlign.center,
                style: AwakenTypography.displayMedium,
              ),
              const SizedBox(height: 24),
              TextButton(
                onPressed: () => context.go(AppRoutes.register),
                child: Text(l10n.pricingContinueFree),
              ),
            ],
          ),
        ),
      ),
    ),
  ],
),
```

- [ ] **Step 3: Update home_page.dart**

Add import:
```dart
import '../../../../design_system/components/awaken_particles_layer.dart';
```

Replace entire `build`:
```dart
@override
Widget build(BuildContext context) => Scaffold(
  backgroundColor: AwakenColors.backgroundPrimary,
  body: const Stack(
    children: [
      AwakenParticlesLayer(),
      Center(child: Text('Home (TODO)', style: TextStyle(color: AwakenColors.textPrimary))),
    ],
  ),
);
```

- [ ] **Step 4: Update language_settings_page.dart**

Add import:
```dart
import '../../../../design_system/components/awaken_particles_layer.dart';
```

Replace entire `build`:
```dart
@override
Widget build(BuildContext context) => Scaffold(
  backgroundColor: AwakenColors.backgroundPrimary,
  body: const Stack(
    children: [
      AwakenParticlesLayer(),
      Center(child: Text('Language Settings (TODO)', style: TextStyle(color: AwakenColors.textPrimary))),
    ],
  ),
);
```

- [ ] **Step 5: Update onboarding_page.dart**

Add import:
```dart
import '../../../../design_system/components/awaken_particles_layer.dart';
```

Replace entire `build`:
```dart
@override
Widget build(BuildContext context) => Scaffold(
  backgroundColor: AwakenColors.backgroundPrimary,
  body: Stack(
    children: [
      const AwakenParticlesLayer(),
      Center(child: ElevatedButton(
        onPressed: () => context.go(AppRoutes.home),
        child: const Text('Complete Onboarding (TODO)'),
      )),
    ],
  ),
);
```

- [ ] **Step 6: Update hunter_card_page.dart**

Add import:
```dart
import '../../../../design_system/components/awaken_particles_layer.dart';
```

Replace entire `build`:
```dart
@override
Widget build(BuildContext context) => Scaffold(
  backgroundColor: AwakenColors.backgroundPrimary,
  body: const Stack(
    children: [
      AwakenParticlesLayer(),
      Center(child: Text('Hunter Card (TODO)', style: TextStyle(color: AwakenColors.textPrimary))),
    ],
  ),
);
```

- [ ] **Step 7: Update daily_quest_page.dart**

Add import:
```dart
import '../../../../design_system/components/awaken_particles_layer.dart';
```

Replace entire `build`:
```dart
@override
Widget build(BuildContext context) => Scaffold(
  backgroundColor: AwakenColors.backgroundPrimary,
  body: const Stack(
    children: [
      AwakenParticlesLayer(),
      Center(child: Text('Daily Quest (TODO)', style: TextStyle(color: AwakenColors.textPrimary))),
    ],
  ),
);
```

- [ ] **Step 8: Update nutrition_page.dart**

Add import:
```dart
import '../../../../design_system/components/awaken_particles_layer.dart';
```

Replace entire `build`:
```dart
@override
Widget build(BuildContext context) => Scaffold(
  backgroundColor: AwakenColors.backgroundPrimary,
  body: const Stack(
    children: [
      AwakenParticlesLayer(),
      Center(child: Text('Nutrition (TODO)', style: TextStyle(color: AwakenColors.textPrimary))),
    ],
  ),
);
```

- [ ] **Step 9: Update progression_page.dart**

Add import:
```dart
import '../../../../design_system/components/awaken_particles_layer.dart';
```

Replace entire `build`:
```dart
@override
Widget build(BuildContext context) => Scaffold(
  backgroundColor: AwakenColors.backgroundPrimary,
  body: const Stack(
    children: [
      AwakenParticlesLayer(),
      Center(child: Text('Progression (TODO)', style: TextStyle(color: AwakenColors.textPrimary))),
    ],
  ),
);
```

- [ ] **Step 10: Run all affected tests**

```bash
cd apps/mobile && flutter test test/features/pricing/presentation/pages/pricing_page_test.dart test/features/subscriptions/
```

Expected: all pass.

- [ ] **Step 11: Commit**

```bash
git add \
  apps/mobile/lib/features/subscriptions/presentation/pages/subscription_page.dart \
  apps/mobile/lib/features/pricing/presentation/pages/pricing_page.dart \
  apps/mobile/lib/features/home/presentation/pages/home_page.dart \
  apps/mobile/lib/features/settings/presentation/pages/language_settings_page.dart \
  apps/mobile/lib/features/onboarding/presentation/pages/onboarding_page.dart \
  apps/mobile/lib/features/hunter_profile/presentation/pages/hunter_card_page.dart \
  apps/mobile/lib/features/quests/presentation/pages/daily_quest_page.dart \
  apps/mobile/lib/features/nutrition/presentation/pages/nutrition_page.dart \
  apps/mobile/lib/features/progression/presentation/pages/progression_page.dart
git commit -m "feat: add particle layer to remaining pages"
```

---

## Task 6: Full test suite

- [ ] **Step 1: Run all Flutter tests**

```bash
cd apps/mobile && flutter test
```

Expected: all tests pass. If any fail, investigate the failure — the particle widget integration must not break existing widget tests.

- [ ] **Step 2: Run Flutter analyze**

```bash
cd apps/mobile && flutter analyze
```

Expected: no issues.
