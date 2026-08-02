# AwakenParticlesLayer — Design Spec

**Date:** 2026-06-18  
**Status:** Approved  
**Scope:** Design system component + integration in all screens except SplashPage

---

## 1. Overview

Add an ambient particle effect to all app screens (except SplashPage). Particles are tiny luminous dust dots in purple/blue palette colors that fall from the top of the screen, pass through the middle, and fade out before reaching the bottom. The effect is soft and delicate — a texture layer, not a focal element.

Z-order: above the page background (color or image), below all UI content.

---

## 2. Component Architecture

**File:** `apps/mobile/lib/design_system/components/awaken_particles_layer.dart`

### Widget tree

```
AwakenParticlesLayer (StatefulWidget + SingleTickerProviderStateMixin)
└── RepaintBoundary
    └── CustomPaint(
          painter: AwakenParticlesPainter(elapsed, particles),
          size: Size.infinite,
          isComplex: true,
        )
```

### Classes

**`_Particle`** — immutable data class, instantiated once in `initState`, never mutated:

| Field | Type | Range | Description |
|---|---|---|---|
| `xFraction` | double | 0.0–1.0 | Horizontal start position as fraction of screen width |
| `durationMs` | int | 1000–3000 | Individual fall duration in milliseconds |
| `phaseOffset` | double | 0.0–1.0 | Cycle offset so particles are distributed across the timeline |
| `driftAmplitude` | double | 2.0–8.0 | Horizontal sinusoidal drift amplitude in logical pixels |
| `driftFrequency` | double | 0.8–2.0 | Oscillation frequency multiplier |
| `radius` | double | 0.8–2.2 | Dot radius in logical pixels |
| `color` | Color | (see palette) | Assigned color from the three-color pool |

**`AwakenParticlesPainter`** — extends `CustomPainter`:
- Receives `elapsed` (Duration) and `particles` (List<_Particle>)
- For each particle: computes `progress`, position, opacity, then `canvas.drawCircle`
- `shouldRepaint`: always `true` (driven by ticker, not value comparison)

**`AwakenParticlesLayer`** — `StatefulWidget` with `SingleTickerProviderStateMixin`:
- Creates a `Ticker` via `createTicker`, updating `_elapsed` (Duration) on each tick
- Each tick calls `setState(() {})` to trigger a repaint via the `CustomPainter`
- `dispose()` stops and disposes the ticker

> **Why Ticker and not AnimationController:** Each particle has its own `durationMs` — there is no single shared cycle length, so `AnimationController`'s built-in duration/value model would not be used. A raw `Ticker` is simpler and more honest here.

---

## 3. Visual Parameters

### Particle count
**25 particles** — sparse enough to be ambient, visible enough to be felt.

### Color distribution (assigned randomly at init, fixed per particle lifetime)
| Color | Token | Hex | Share |
|---|---|---|---|
| Blue | `AwakenColors.primary` | `#2D6FF5` | 40% |
| Purple | `AwakenColors.primaryAlt` | `#8B3FD8` | 40% |
| Light blue | `AwakenColors.borderFocus` | `#4D8BFF` | 20% |

### Fall timing
- `durationMs` per particle: random int between **1000ms and 3000ms**
- Progress for particle `i` at time `elapsed`:
  ```dart
  double progress = ((elapsed.inMilliseconds / particle.durationMs) + particle.phaseOffset) % 1.0;
  ```
- Each particle loops independently — no global cycle

### Position
- `y = progress * screenHeight`
- `x = (particle.xFraction * screenWidth) + sin(progress * particle.driftFrequency * 2π) * particle.driftAmplitude`

### Opacity lifecycle
| Phase | Progress range | Opacity |
|---|---|---|
| Fade in | 0.00 → 0.12 | `0.0 → 0.45` (linear) |
| Visible | 0.12 → 0.72 | `0.45` (constant) |
| Fade out | 0.72 → 1.00 | `0.45 → 0.0` (linear) |

Maximum opacity: **0.45** — visible against `#01020A` dark background without competing with UI elements.

### Dot shape
Circular (`canvas.drawCircle`). No blur or shadow — keeping it crisp and minimal.

---

## 4. Integration Pattern

There is no shared BaseScaffold in the app. Each page independently builds its `Scaffold`. Two patterns exist:

### Pattern A — Pages with existing Stack (background image)
Auth pages (login, register, forgot_password, delete_account) already use a `Stack` with a background image and gradient overlay. Insert `AwakenParticlesLayer()` as the child immediately after the last overlay layer, before `SafeArea`:

```dart
Stack(children: [
  Image.asset(...),             // background image
  Container(gradient: ...),     // gradient overlay
  const AwakenParticlesLayer(), // ← insert here
  SafeArea(child: ...),         // UI content
])
```

### Pattern B — Pages with simple Scaffold body
All other pages wrap the existing body in a `Stack`:

```dart
// Before:
Scaffold(body: SafeArea(child: content))

// After:
Scaffold(
  body: Stack(
    children: [
      const AwakenParticlesLayer(),
      SafeArea(child: content),
    ],
  ),
)
```

### Pages to update (13 total)

| Page | File | Pattern |
|---|---|---|
| LoginPage | `features/auth/presentation/pages/login_page.dart` | A |
| RegisterPage | `features/auth/presentation/pages/register_page.dart` | A |
| ForgotPasswordPage | `features/auth/presentation/pages/forgot_password_page.dart` | A |
| DeleteAccountPage | `features/auth/presentation/pages/delete_account_page.dart` | A or B (verify) |
| HomePage | `features/home/presentation/pages/home_page.dart` | B |
| DailyQuestPage | `features/quests/presentation/pages/daily_quest_page.dart` | B |
| ProgressionPage | `features/progression/presentation/pages/progression_page.dart` | B |
| HunterCardPage | `features/hunter_profile/presentation/pages/hunter_card_page.dart` | B |
| NutritionPage | `features/nutrition/presentation/pages/nutrition_page.dart` | B |
| OnboardingPage | `features/onboarding/presentation/pages/onboarding_page.dart` | B |
| SubscriptionPage | `features/subscriptions/presentation/pages/subscription_page.dart` | B |
| SettingsPage | `features/settings/presentation/pages/settings_page.dart` | B |
| PricingPage | `features/pricing/presentation/pages/pricing_page.dart` | B |

**Excluded:** `SplashPage` (`features/splash/presentation/pages/splash_page.dart`)

---

## 5. Design System Registration

Add export to `lib/design_system/design_system.dart` (or equivalent barrel file):
```dart
export 'components/awaken_particles_layer.dart';
```

---

## 6. Testing

- **Widget test** (`test/design_system/components/awaken_particles_layer_test.dart`):
  - Renders without errors inside a `MaterialApp`
  - Does not throw when screen size is zero
  - Disposes ticker without leak (verify via `tester.pumpAndSettle`)
- No golden tests required — the animation is time-driven and non-deterministic

---

## 7. Non-Goals

- No blur/glow effect on particles (keeps it crisp, avoids `ImageFilter` overhead)
- No interactivity (particles do not react to touch)
- No configuration props exposed (count, colors, speed hardcoded — not a generic particle system)
- No dark/light theme branching (app is dark-only)
