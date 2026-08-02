# Hunter Profile & Trial Visual Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Redesign the Hunter Profile screen (header, XP, streak, attributes) into a HUD-style faceted card with colored attribute bars, and replace the flat trial-days-remaining text with a circular progress ring — both purely visual changes in Flutter, no backend/API changes.

**Architecture:** Add six fixed attribute colors to the existing `AwakenColors` token file. Add a new reusable `AwakenStatBar` design-system component (mirrors the existing `XpBar` pattern: label + value + `LinearProgressIndicator`). Wrap `HunterProfilePage`'s loaded content in the existing `AwakenAngledContainer` and group it into XP/Streak/Attributes sections via a small private `_SectionLabel` widget. Add a private `_TrialProgressRing` (`CustomPaint` arc) to `subscription_page.dart`, computing total trial days from `trialStartedAt`/`trialEndsAt` (already on `SubscriptionStatusLoaded`) with a 7-day fallback when those are null.

**Tech Stack:** Flutter, Riverpod, existing `design_system/` token/component layer, `flutter_test` widget tests.

Full design rationale: `docs/superpowers/specs/2026-06-22-hunter-profile-trial-visual-design.md`.

---

### Task 1: Add the six attribute colors to `AwakenColors`

**Files:**
- Modify: `apps/mobile/lib/design_system/tokens/colors.dart`

- [ ] **Step 1: Add the attribute color constants**

In `apps/mobile/lib/design_system/tokens/colors.dart`, find this block:

```dart
  static const rankSSS = LinearGradient(
    colors: [Color(0xFF5FE8FF), Color(0xFF5FE8FF)],
  );

  // Borders & inputs (espelha docs/design-system/tokens/colors.css)
```

Replace it with (adds the new attribute block, keeps everything else unchanged):

```dart
  static const rankSSS = LinearGradient(
    colors: [Color(0xFF5FE8FF), Color(0xFF5FE8FF)],
  );

  // Character attributes (espelha docs/design-system/tokens/colors.css)
  static const attrStrength = Color(0xFFFF5A3C);
  static const attrAgility = Color(0xFF22D3A7);
  static const attrEndurance = Color(0xFF2D6FF5);
  static const attrVitality = Color(0xFFF5C518);
  static const attrFocus = Color(0xFFA65CEE);
  static const attrWisdom = Color(0xFF5FE8FF);

  // Borders & inputs (espelha docs/design-system/tokens/colors.css)
```

- [ ] **Step 2: Verify it compiles**

Run: `cd apps/mobile && flutter analyze lib/design_system/tokens/colors.dart`
Expected: `No issues found!`

- [ ] **Step 3: Commit**

```bash
git add apps/mobile/lib/design_system/tokens/colors.dart
git commit -m "feat(design-system): add fixed colors for the six hunter attributes"
```

---

### Task 2: Create the `AwakenStatBar` component

**Files:**
- Create: `apps/mobile/lib/design_system/components/awaken_stat_bar.dart`
- Test: `apps/mobile/test/design_system/components/awaken_stat_bar_test.dart`

- [ ] **Step 1: Write the failing test**

Create `apps/mobile/test/design_system/components/awaken_stat_bar_test.dart`:

```dart
import 'package:awaken/design_system/components/awaken_stat_bar.dart';
import 'package:awaken/design_system/tokens/colors.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('AwakenStatBar', () {
    testWidgets('exibe label e valor', (tester) async {
      await tester.pumpWidget(const MaterialApp(
        home: Scaffold(
          body: AwakenStatBar(
            label: 'Força',
            value: 62,
            color: AwakenColors.attrStrength,
          ),
        ),
      ));

      expect(find.text('Força'), findsOneWidget);
      expect(find.text('62'), findsOneWidget);
    });

    testWidgets('progresso reflete value/max', (tester) async {
      await tester.pumpWidget(const MaterialApp(
        home: Scaffold(
          body: AwakenStatBar(
            label: 'Foco',
            value: 50,
            color: AwakenColors.attrFocus,
            max: 100,
          ),
        ),
      ));

      final indicator = tester.widget<LinearProgressIndicator>(
        find.byType(LinearProgressIndicator),
      );
      expect(indicator.value, 0.5);
    });

    testWidgets('clampa valor acima do max em 100%', (tester) async {
      await tester.pumpWidget(const MaterialApp(
        home: Scaffold(
          body: AwakenStatBar(
            label: 'Vitalidade',
            value: 150,
            color: AwakenColors.attrVitality,
            max: 100,
          ),
        ),
      ));

      final indicator = tester.widget<LinearProgressIndicator>(
        find.byType(LinearProgressIndicator),
      );
      expect(indicator.value, 1.0);
    });
  });
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `cd apps/mobile && flutter test test/design_system/components/awaken_stat_bar_test.dart`
Expected: FAIL — `Error: Couldn't resolve the package 'awaken/design_system/components/awaken_stat_bar.dart'` (file doesn't exist yet).

- [ ] **Step 3: Implement `AwakenStatBar`**

Create `apps/mobile/lib/design_system/components/awaken_stat_bar.dart`:

```dart
import 'package:flutter/material.dart';
import '../tokens/colors.dart';
import '../tokens/typography.dart';

/// A single hunter attribute (Força, Agilidade, ...) rendered as a
/// colored RPG-style status bar. Mirrors the web design system's
/// StatBar component (docs/design-system/components/game/StatBar.jsx).
class AwakenStatBar extends StatelessWidget {
  const AwakenStatBar({
    super.key,
    required this.label,
    required this.value,
    required this.color,
    this.max = 100,
  });

  final String label;
  final int value;
  final Color color;
  final int max;

  @override
  Widget build(BuildContext context) {
    final progress = max > 0 ? (value / max).clamp(0.0, 1.0) : 0.0;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(
              label,
              style: AwakenTypography.labelSmall.copyWith(
                color: AwakenColors.textSecondary,
              ),
            ),
            Text(
              '$value',
              style: AwakenTypography.stat.copyWith(
                color: color,
                fontWeight: FontWeight.w700,
              ),
            ),
          ],
        ),
        const SizedBox(height: 4),
        ClipRRect(
          borderRadius: BorderRadius.circular(4),
          child: LinearProgressIndicator(
            value: progress,
            backgroundColor:
                AwakenColors.cardSurface(AwakenColors.cardElevated),
            valueColor: AlwaysStoppedAnimation(color),
            minHeight: 6,
          ),
        ),
      ],
    );
  }
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `cd apps/mobile && flutter test test/design_system/components/awaken_stat_bar_test.dart`
Expected: `00:0X +3: All tests passed!`

- [ ] **Step 5: Commit**

```bash
git add apps/mobile/lib/design_system/components/awaken_stat_bar.dart apps/mobile/test/design_system/components/awaken_stat_bar_test.dart
git commit -m "feat(design-system): add AwakenStatBar for colored attribute bars"
```

---

### Task 3: Redesign `HunterProfilePage` into a HUD card

**Files:**
- Modify: `apps/mobile/lib/features/hunter_profile/presentation/pages/hunter_profile_page.dart`
- Test: `apps/mobile/test/features/hunter_profile/presentation/pages/hunter_profile_page_test.dart`

This task wraps the loaded-profile content in `AwakenAngledContainer`, groups it into XP / Streak / Atributos sections with a shared `_SectionLabel`, and replaces the six attribute `_StatRow`s with `AwakenStatBar`. The existing widget keys (`hunter-profile-display-name`, `hunter-profile-xp-bar`, `hunter-profile-streak`, `hunter-profile-attributes`, etc.) and all existing localized strings are preserved exactly, so none of the current assertions in `hunter_profile_page_test.dart` need to change — this step only adds one new test to lock in the new structure.

- [ ] **Step 1: Write the failing test**

In `apps/mobile/test/features/hunter_profile/presentation/pages/hunter_profile_page_test.dart`, add this new test inside the `group('HunterProfilePage', () { ... })` block (e.g. right after the `'exibe todos os 6 atributos com seus levels'` test):

```dart
    testWidgets(
        'atributos sao exibidos como AwakenStatBar dentro de um card HUD',
        (tester) async {
      await tester.pumpWidget(_wrap(_FakeRepository(
        result: const HunterProfileDto(
          accessStatus: 'trial_active',
          hasProgress: true,
          displayName: 'Vinícius',
          rank: 'E',
          level: 3,
          xp: 240,
          xpToNextLevel: 500,
          streakDays: 4,
          attributes: _defaultAttributes,
        ),
      )));
      await tester.pumpAndSettle();

      expect(find.byType(AwakenAngledContainer), findsOneWidget);
      expect(find.byType(AwakenStatBar), findsNWidgets(6));
    });
```

Add the two new imports at the top of the test file, alongside the existing `package:awaken/...` imports:

```dart
import 'package:awaken/design_system/components/awaken_angled_container.dart';
import 'package:awaken/design_system/components/awaken_stat_bar.dart';
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `cd apps/mobile && flutter test test/features/hunter_profile/presentation/pages/hunter_profile_page_test.dart --plain-name "AwakenStatBar dentro de um card HUD"`
Expected: FAIL — `AwakenAngledContainer` and `AwakenStatBar` are not found in the widget tree (page doesn't use them yet).

- [ ] **Step 3: Update imports in `hunter_profile_page.dart`**

In `apps/mobile/lib/features/hunter_profile/presentation/pages/hunter_profile_page.dart`, find:

```dart
import '../../../../design_system/components/awaken_blocked_state.dart';
import '../../../../design_system/components/awaken_empty_state.dart';
import '../../../../design_system/components/awaken_error_state.dart';
import '../../../../design_system/components/awaken_loading_page.dart';
import '../../../../design_system/components/hunter_avatar.dart';
import '../../../../design_system/components/rank_badge.dart';
import '../../../../design_system/components/xp_bar.dart';
```

Replace with:

```dart
import '../../../../design_system/components/awaken_angled_container.dart';
import '../../../../design_system/components/awaken_blocked_state.dart';
import '../../../../design_system/components/awaken_empty_state.dart';
import '../../../../design_system/components/awaken_error_state.dart';
import '../../../../design_system/components/awaken_loading_page.dart';
import '../../../../design_system/components/awaken_stat_bar.dart';
import '../../../../design_system/components/hunter_avatar.dart';
import '../../../../design_system/components/rank_badge.dart';
import '../../../../design_system/components/xp_bar.dart';
```

- [ ] **Step 4: Replace `_HunterProfileContent.build()`**

In the same file, find the entire `_HunterProfileContent` class:

```dart
class _HunterProfileContent extends StatelessWidget {
  const _HunterProfileContent({required this.l10n, required this.profile});

  final AppLocalizations l10n;
  final HunterProfileDto profile;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(AwakenSpacing.lg),
      child: ConstrainedBox(
        constraints:
            const BoxConstraints(maxWidth: AwakenSpacing.contentMaxWidth),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                HunterAvatar(
                  key: const Key('hunter-profile-avatar'),
                  avatarUrl: profile.avatarUrl,
                  displayName: profile.displayName,
                  size: 56,
                  semanticLabel: l10n.hunterProfileAvatarLabel,
                ),
                const SizedBox(width: AwakenSpacing.md),
                RankBadge(
                  key: const Key('hunter-profile-rank-badge'),
                  rank: profile.rank!,
                  size: 56,
                ),
                const SizedBox(width: AwakenSpacing.md),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        profile.displayName ?? '',
                        key: const Key('hunter-profile-display-name'),
                        style: AwakenTypography.titleMedium,
                      ),
                      Text(
                        l10n.hunterProfileLevelLabel(profile.level ?? 1),
                        key: const Key('hunter-profile-level'),
                        style: AwakenTypography.bodyMedium.copyWith(
                          color: AwakenColors.textSecondary,
                        ),
                      ),
                      _HunterClassBadge(
                        l10n: l10n,
                        hunterClass: profile.hunterClass,
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: AwakenSpacing.lg),
            XpBar(
              key: const Key('hunter-profile-xp-bar'),
              currentXp: profile.xp ?? 0,
              targetXp: profile.xpToNextLevel ?? 0,
            ),
            const SizedBox(height: AwakenSpacing.lg),
            _StatRow(
              key: const Key('hunter-profile-streak'),
              label: l10n.hunterProfileStreakLabel,
              value: l10n.hunterProfileStreakValue(profile.streakDays ?? 0),
            ),
            const SizedBox(height: AwakenSpacing.lg),
            Text(
              l10n.hunterProfileAttributesTitle,
              style: AwakenTypography.labelLarge,
            ),
            const SizedBox(height: AwakenSpacing.sm),
            Column(
              key: const Key('hunter-profile-attributes'),
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                _StatRow(
                  label: l10n.hunterProfileAttributeStrength,
                  value: '${profile.attributes?.strength ?? 0}',
                ),
                _StatRow(
                  label: l10n.hunterProfileAttributeEndurance,
                  value: '${profile.attributes?.endurance ?? 0}',
                ),
                _StatRow(
                  label: l10n.hunterProfileAttributeAgility,
                  value: '${profile.attributes?.agility ?? 0}',
                ),
                _StatRow(
                  label: l10n.hunterProfileAttributeVitality,
                  value: '${profile.attributes?.vitality ?? 0}',
                ),
                _StatRow(
                  label: l10n.hunterProfileAttributeFocus,
                  value: '${profile.attributes?.focus ?? 0}',
                ),
                _StatRow(
                  label: l10n.hunterProfileAttributeWisdom,
                  value: '${profile.attributes?.wisdom ?? 0}',
                ),
              ],
            ),
            const SizedBox(height: AwakenSpacing.lg),
            OutlinedButton(
              key: const Key('hunter-profile-generate-card-action'),
              onPressed: () => context.go(AppRoutes.hunterCard),
              child: Text(l10n.hunterProfileGenerateCardAction),
            ),
          ],
        ),
      ),
    );
  }
}
```

Replace it with:

```dart
class _HunterProfileContent extends StatelessWidget {
  const _HunterProfileContent({required this.l10n, required this.profile});

  final AppLocalizations l10n;
  final HunterProfileDto profile;

  @override
  Widget build(BuildContext context) {
    final attributes = profile.attributes;

    return SingleChildScrollView(
      padding: const EdgeInsets.all(AwakenSpacing.lg),
      child: ConstrainedBox(
        constraints:
            const BoxConstraints(maxWidth: AwakenSpacing.contentMaxWidth),
        child: AwakenAngledContainer(
          color: AwakenColors.cardElevated,
          padding: const EdgeInsets.all(AwakenSpacing.lg),
          boxShadow: const [
            BoxShadow(
              color: AwakenColors.glowBlue,
              blurRadius: 24,
              spreadRadius: -4,
            ),
          ],
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  HunterAvatar(
                    key: const Key('hunter-profile-avatar'),
                    avatarUrl: profile.avatarUrl,
                    displayName: profile.displayName,
                    size: 56,
                    semanticLabel: l10n.hunterProfileAvatarLabel,
                  ),
                  const SizedBox(width: AwakenSpacing.md),
                  RankBadge(
                    key: const Key('hunter-profile-rank-badge'),
                    rank: profile.rank!,
                    size: 56,
                  ),
                  const SizedBox(width: AwakenSpacing.md),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          profile.displayName ?? '',
                          key: const Key('hunter-profile-display-name'),
                          style: AwakenTypography.titleMedium,
                        ),
                        Text(
                          l10n.hunterProfileLevelLabel(profile.level ?? 1),
                          key: const Key('hunter-profile-level'),
                          style: AwakenTypography.bodyMedium.copyWith(
                            color: AwakenColors.textSecondary,
                          ),
                        ),
                        _HunterClassBadge(
                          l10n: l10n,
                          hunterClass: profile.hunterClass,
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AwakenSpacing.sectionGap),
              const _SectionLabel(label: 'XP', color: AwakenColors.xp),
              const SizedBox(height: AwakenSpacing.sm),
              XpBar(
                key: const Key('hunter-profile-xp-bar'),
                currentXp: profile.xp ?? 0,
                targetXp: profile.xpToNextLevel ?? 0,
              ),
              const SizedBox(height: AwakenSpacing.sectionGap),
              _SectionLabel(
                label: l10n.hunterProfileStreakLabel,
                color: AwakenColors.amber,
              ),
              const SizedBox(height: AwakenSpacing.sm),
              Row(
                key: const Key('hunter-profile-streak'),
                children: [
                  const Icon(
                    Icons.local_fire_department,
                    color: AwakenColors.amber,
                    size: 20,
                  ),
                  const SizedBox(width: AwakenSpacing.xs),
                  Text(
                    l10n.hunterProfileStreakValue(profile.streakDays ?? 0),
                    style: AwakenTypography.bodyLarge.copyWith(
                      color: AwakenColors.amber,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AwakenSpacing.sectionGap),
              _SectionLabel(
                label: l10n.hunterProfileAttributesTitle,
                color: AwakenColors.primary,
              ),
              const SizedBox(height: AwakenSpacing.sm),
              Column(
                key: const Key('hunter-profile-attributes'),
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  AwakenStatBar(
                    label: l10n.hunterProfileAttributeStrength,
                    value: attributes?.strength ?? 0,
                    color: AwakenColors.attrStrength,
                  ),
                  const SizedBox(height: AwakenSpacing.sm),
                  AwakenStatBar(
                    label: l10n.hunterProfileAttributeEndurance,
                    value: attributes?.endurance ?? 0,
                    color: AwakenColors.attrEndurance,
                  ),
                  const SizedBox(height: AwakenSpacing.sm),
                  AwakenStatBar(
                    label: l10n.hunterProfileAttributeAgility,
                    value: attributes?.agility ?? 0,
                    color: AwakenColors.attrAgility,
                  ),
                  const SizedBox(height: AwakenSpacing.sm),
                  AwakenStatBar(
                    label: l10n.hunterProfileAttributeVitality,
                    value: attributes?.vitality ?? 0,
                    color: AwakenColors.attrVitality,
                  ),
                  const SizedBox(height: AwakenSpacing.sm),
                  AwakenStatBar(
                    label: l10n.hunterProfileAttributeFocus,
                    value: attributes?.focus ?? 0,
                    color: AwakenColors.attrFocus,
                  ),
                  const SizedBox(height: AwakenSpacing.sm),
                  AwakenStatBar(
                    label: l10n.hunterProfileAttributeWisdom,
                    value: attributes?.wisdom ?? 0,
                    color: AwakenColors.attrWisdom,
                  ),
                ],
              ),
              const SizedBox(height: AwakenSpacing.lg),
              OutlinedButton(
                key: const Key('hunter-profile-generate-card-action'),
                onPressed: () => context.go(AppRoutes.hunterCard),
                child: Text(l10n.hunterProfileGenerateCardAction),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _SectionLabel extends StatelessWidget {
  const _SectionLabel({required this.label, required this.color});

  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Transform.rotate(
          angle: 0.785398,
          child: Container(width: 6, height: 6, color: color),
        ),
        const SizedBox(width: AwakenSpacing.sm),
        Text(
          label,
          style: AwakenTypography.labelSmall.copyWith(
            color: color,
            letterSpacing: 1.6,
          ),
        ),
        const SizedBox(width: AwakenSpacing.sm),
        Expanded(
          child: Container(height: 1, color: color.withValues(alpha: 0.3)),
        ),
      ],
    );
  }
}
```

This both replaces the layout and removes the two `_StatRow` use sites (streak and the six attributes) — `_StatRow` now has no remaining callers.

- [ ] **Step 5: Delete the now-unused `_StatRow` class**

In the same file, find and delete entirely:

```dart
class _StatRow extends StatelessWidget {
  const _StatRow({super.key, required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          label,
          style: AwakenTypography.bodyMedium.copyWith(
            color: AwakenColors.textSecondary,
          ),
        ),
        Text(
          value,
          style:
              AwakenTypography.bodyMedium.copyWith(color: AwakenColors.textPrimary),
        ),
      ],
    );
  }
}
```

(Delete the whole class, including the closing brace — leave nothing in its place.)

- [ ] **Step 6: Run the full hunter profile test file**

Run: `cd apps/mobile && flutter test test/features/hunter_profile/presentation/pages/hunter_profile_page_test.dart`
Expected: `All tests passed!` — every pre-existing test keeps passing because all keys, labels and value strings are unchanged, and the new test from Step 1 now passes too.

- [ ] **Step 7: Run `flutter analyze` on the changed files**

Run: `cd apps/mobile && flutter analyze lib/features/hunter_profile/presentation/pages/hunter_profile_page.dart test/features/hunter_profile/presentation/pages/hunter_profile_page_test.dart`
Expected: `No issues found!` (confirms `_StatRow` removal left no dangling references).

- [ ] **Step 8: Commit**

```bash
git add apps/mobile/lib/features/hunter_profile/presentation/pages/hunter_profile_page.dart apps/mobile/test/features/hunter_profile/presentation/pages/hunter_profile_page_test.dart
git commit -m "feat(hunter-profile): redesign profile into HUD card with colored attribute bars"
```

---

### Task 4: Add the trial progress ring to `SubscriptionPage`

**Files:**
- Modify: `apps/mobile/lib/features/subscriptions/presentation/pages/subscription_page.dart`
- Test: `apps/mobile/test/features/subscriptions/presentation/pages/subscription_page_test.dart`

The ring is additive: it sits above the existing "X dias restantes" text, which keeps its key (`trial-days-remaining`) and exact localized string, so none of the current trial-related assertions change. This step only adds new tests for the ring itself.

- [ ] **Step 1: Write the failing tests**

In `apps/mobile/test/features/subscriptions/presentation/pages/subscription_page_test.dart`, add these two tests inside `group('SubscriptionPage', () { ... })`, right after the `'trial com 7 dias restantes mostra contador sem alerta'` test:

```dart
    testWidgets('trial ativo exibe anel de progresso com dias restantes',
        (tester) async {
      await tester.pumpWidget(_buildTestApp(
        session: const SessionState(
          hasSession: true,
          accessStatus: AccessStatus.trialActive,
          onboardingCompleted: true,
        ),
        statusState: SubscriptionStatusLoaded(
          accessStatus: 'trial_active',
          daysRemaining: 4,
          trialStartedAt: DateTime(2026, 1, 1),
          trialEndsAt: DateTime(2026, 1, 8),
        ),
        revenueCat: _FakeRevenueCatService(catalog: _catalog()),
        catalog: _catalog(),
      ));
      await tester.pumpAndSettle();

      final l10n = AppLocalizations.of(
        tester.element(find.byType(SubscriptionPage)),
      );

      expect(find.byKey(const Key('trial-progress-ring')), findsOneWidget);
      expect(find.text(l10n.streakDays(4)), findsOneWidget);
      expect(find.byKey(const Key('trial-days-remaining')), findsOneWidget);
      expect(find.text(l10n.trialStatusDaysRemaining(4)), findsOneWidget);
    });

    testWidgets(
        'trial ativo sem datas de inicio/fim ainda exibe o anel (fallback de 7 dias)',
        (tester) async {
      await tester.pumpWidget(_buildTestApp(
        session: const SessionState(
          hasSession: true,
          accessStatus: AccessStatus.trialActive,
          onboardingCompleted: true,
        ),
        statusState: const SubscriptionStatusLoaded(
          accessStatus: 'trial_active',
          daysRemaining: 2,
        ),
        revenueCat: _FakeRevenueCatService(catalog: _catalog()),
        catalog: _catalog(),
      ));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('trial-progress-ring')), findsOneWidget);
    });

    testWidgets('trial expirado nao exibe o anel de progresso', (tester) async {
      await tester.pumpWidget(_buildTestApp(
        session: const SessionState(
          hasSession: true,
          accessStatus: AccessStatus.trialExpired,
          onboardingCompleted: true,
        ),
        statusState:
            const SubscriptionStatusLoaded(accessStatus: 'trial_expired'),
        revenueCat: _FakeRevenueCatService(catalog: _catalog()),
        catalog: _catalog(),
      ));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('trial-progress-ring')), findsNothing);
    });
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `cd apps/mobile && flutter test test/features/subscriptions/presentation/pages/subscription_page_test.dart --plain-name "anel de progresso"`
Expected: FAIL — `trial-progress-ring` key not found (widget doesn't exist yet). The "trial expirado nao exibe" test passes trivially (findsNothing on a key that doesn't exist anywhere yet) — that's fine, it'll stay meaningful once the ring exists.

- [ ] **Step 3: Add the `dart:math` import**

In `apps/mobile/lib/features/subscriptions/presentation/pages/subscription_page.dart`, find:

```dart
import 'package:awaken/l10n/app_localizations.dart';
import 'package:flutter/material.dart';
```

Replace with:

```dart
import 'dart:math' as math;

import 'package:awaken/l10n/app_localizations.dart';
import 'package:flutter/material.dart';
```

- [ ] **Step 4: Thread `trialStartedAt`/`trialEndsAt` into `_TrialStatusContent`**

Find this block in `_SubscriptionPageState.build()`:

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
              isActionLoading: _isRevenueCatActionRunning,
              onSubscribe: _purchasePlan,
              onRestore: _restorePurchases,
              onManageSubscription: _presentCustomerCenter,
            ),
```

Replace with:

```dart
          SubscriptionStatusLoaded(
            :final accessStatus,
            :final daysRemaining,
            :final plan,
            :final trialStartedAt,
            :final trialEndsAt,
          ) =>
            _TrialStatusContent(
              l10n: l10n,
              accessStatus: accessStatus,
              daysRemaining: daysRemaining,
              plan: plan,
              trialStartedAt: trialStartedAt,
              trialEndsAt: trialEndsAt,
              isActionLoading: _isRevenueCatActionRunning,
              onSubscribe: _purchasePlan,
              onRestore: _restorePurchases,
              onManageSubscription: _presentCustomerCenter,
            ),
```

- [ ] **Step 5: Add the new fields to `_TrialStatusContent` and insert the ring**

Find:

```dart
class _TrialStatusContent extends StatelessWidget {
  const _TrialStatusContent({
    required this.l10n,
    required this.accessStatus,
    required this.daysRemaining,
    required this.isActionLoading,
    required this.onSubscribe,
    required this.onRestore,
    required this.onManageSubscription,
    this.plan,
  });

  final AppLocalizations l10n;
  final String accessStatus;
  final int? daysRemaining;
  final bool isActionLoading;
  final Future<void> Function(String planId) onSubscribe;
  final Future<void> Function() onRestore;
  final Future<void> Function() onManageSubscription;
  final String? plan;
```

Replace with:

```dart
class _TrialStatusContent extends StatelessWidget {
  const _TrialStatusContent({
    required this.l10n,
    required this.accessStatus,
    required this.daysRemaining,
    required this.isActionLoading,
    required this.onSubscribe,
    required this.onRestore,
    required this.onManageSubscription,
    this.plan,
    this.trialStartedAt,
    this.trialEndsAt,
  });

  final AppLocalizations l10n;
  final String accessStatus;
  final int? daysRemaining;
  final bool isActionLoading;
  final Future<void> Function(String planId) onSubscribe;
  final Future<void> Function() onRestore;
  final Future<void> Function() onManageSubscription;
  final String? plan;
  final DateTime? trialStartedAt;
  final DateTime? trialEndsAt;
```

Now find:

```dart
            if (isActive && daysRemaining != null) ...[
              const SizedBox(height: AwakenSpacing.sm),
              Text(
                key: const Key('trial-days-remaining'),
                l10n.trialStatusDaysRemaining(daysRemaining!),
                style: AwakenTypography.bodyMedium.copyWith(
                  color: isTrialEndingSoon
                      ? AwakenColors.warning
                      : AwakenColors.textSecondary,
                ),
                textAlign: TextAlign.center,
              ),
```

Replace with:

```dart
            if (isActive && daysRemaining != null) ...[
              const SizedBox(height: AwakenSpacing.md),
              _TrialProgressRing(
                daysRemaining: daysRemaining!,
                totalDays: trialStartedAt != null && trialEndsAt != null
                    ? trialEndsAt!.difference(trialStartedAt!).inDays
                    : _TrialProgressRing.defaultTotalDays,
                color: isTrialEndingSoon
                    ? AwakenColors.warning
                    : AwakenColors.success,
                centerLabel: l10n.streakDays(daysRemaining!),
              ),
              const SizedBox(height: AwakenSpacing.sm),
              Text(
                key: const Key('trial-days-remaining'),
                l10n.trialStatusDaysRemaining(daysRemaining!),
                style: AwakenTypography.bodyMedium.copyWith(
                  color: isTrialEndingSoon
                      ? AwakenColors.warning
                      : AwakenColors.textSecondary,
                ),
                textAlign: TextAlign.center,
              ),
```

(Everything below this block — the `isTrialEndingSoon` warning text, the `isExpired`/`hasNoTrial` branches, the action buttons — stays exactly as-is.)

- [ ] **Step 6: Add the `_TrialProgressRing` widget and its painter**

In the same file, find the closing brace of `_TrialStatusBadge` (the class right after `_TrialStatusContent`):

```dart
class _TrialStatusBadge extends StatelessWidget {
  const _TrialStatusBadge({
    super.key,
    required this.label,
    required this.color,
  });

  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AwakenSpacing.md,
        vertical: AwakenSpacing.sm,
      ),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.14),
        borderRadius: BorderRadius.circular(AwakenSpacing.badgeRadius + 4),
        border: Border.all(color: color.withValues(alpha: 0.35)),
      ),
      child: Text(
        label,
        style: AwakenTypography.labelLarge.copyWith(
          color: color,
          letterSpacing: 0.5,
        ),
      ),
    );
  }
}
```

Immediately after it (before `class _PaywallContent extends StatefulWidget {`), insert:

```dart
class _TrialProgressRing extends StatelessWidget {
  const _TrialProgressRing({
    required this.daysRemaining,
    required this.totalDays,
    required this.color,
    required this.centerLabel,
  });

  static const defaultTotalDays = 7;

  final int daysRemaining;
  final int totalDays;
  final Color color;
  final String centerLabel;

  @override
  Widget build(BuildContext context) {
    final elapsed = (totalDays - daysRemaining).clamp(0, totalDays);
    final progress = totalDays > 0 ? elapsed / totalDays : 0.0;

    return SizedBox(
      key: const Key('trial-progress-ring'),
      width: 120,
      height: 120,
      child: CustomPaint(
        painter: _TrialRingPainter(progress: progress, color: color),
        child: Center(
          child: Text(
            centerLabel,
            textAlign: TextAlign.center,
            style: AwakenTypography.titleLarge.copyWith(color: color),
          ),
        ),
      ),
    );
  }
}

class _TrialRingPainter extends CustomPainter {
  const _TrialRingPainter({required this.progress, required this.color});

  final double progress;
  final Color color;

  @override
  void paint(Canvas canvas, Size size) {
    final center = size.center(Offset.zero);
    final radius = size.shortestSide / 2 - 6;

    final trackPaint = Paint()
      ..color = AwakenColors.cardElevated
      ..style = PaintingStyle.stroke
      ..strokeWidth = 8
      ..strokeCap = StrokeCap.round;

    final progressPaint = Paint()
      ..color = color
      ..style = PaintingStyle.stroke
      ..strokeWidth = 8
      ..strokeCap = StrokeCap.round;

    canvas.drawCircle(center, radius, trackPaint);

    final sweep = 2 * math.pi * progress;
    canvas.drawArc(
      Rect.fromCircle(center: center, radius: radius),
      -math.pi / 2,
      sweep,
      false,
      progressPaint,
    );
  }

  @override
  bool shouldRepaint(covariant _TrialRingPainter oldDelegate) =>
      oldDelegate.progress != progress || oldDelegate.color != color;
}
```

- [ ] **Step 7: Run the new tests to verify they pass**

Run: `cd apps/mobile && flutter test test/features/subscriptions/presentation/pages/subscription_page_test.dart --plain-name "anel de progresso"`
Expected: `All tests passed!`

- [ ] **Step 8: Run the full subscription page test file**

Run: `cd apps/mobile && flutter test test/features/subscriptions/presentation/pages/subscription_page_test.dart`
Expected: `All tests passed!` — confirms every pre-existing trial/paywall test (badges, analytics, localization, error states) is unaffected.

- [ ] **Step 9: Run `flutter analyze` on the changed files**

Run: `cd apps/mobile && flutter analyze lib/features/subscriptions/presentation/pages/subscription_page.dart test/features/subscriptions/presentation/pages/subscription_page_test.dart`
Expected: `No issues found!`

- [ ] **Step 10: Commit**

```bash
git add apps/mobile/lib/features/subscriptions/presentation/pages/subscription_page.dart apps/mobile/test/features/subscriptions/presentation/pages/subscription_page_test.dart
git commit -m "feat(subscription): show trial days remaining as a progress ring"
```

---

### Task 5: Full verification

**Files:** none (verification only)

- [ ] **Step 1: Run the full mobile test suite**

Run: `cd apps/mobile && flutter test`
Expected: all tests pass (no regressions outside the two files touched).

- [ ] **Step 2: Run full analyze**

Run: `cd apps/mobile && flutter analyze`
Expected: `No issues found!`

- [ ] **Step 3: Manually verify on a running app**

Run: `cd apps/mobile && flutter run`, navigate to the Hunter Profile screen and to the Subscription/trial screen (trial-active account), and visually confirm:
- Hunter Profile renders as an angled HUD card with XP / Streak / Atributos section labels and six colored attribute bars.
- Trial screen shows the circular ring with days remaining, above the existing "X dias restantes" text and the existing near-end/last-day warning.

No further commit needed for this task (verification only, no code changes).
