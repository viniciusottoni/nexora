# Hunter Profile & Trial Visual Redesign

Date: 2026-06-22
Scope: Flutter UI only (`apps/mobile/`). No backend/API contract changes — both screens already receive all the data needed.

## Context

US-073/074/075/076 (Perfil Hunter: visualizar perfil, rank/level/XP/streak/atributos, classe inicial, avatar) and US-079/080 (card durante trial/premium) are implemented functionally, but `hunter_profile_page.dart` renders header, XP, streak and attributes as plain `Row`/`Column`/`Text` widgets with no card surface, no per-attribute color, and no use of the "épico" HUD aesthetic defined in `docs/design-system` (faceted containers, section labels, colored stat bars). Separately, `subscription_page.dart`'s `_TrialStatusContent` shows trial days remaining as a plain badge + text line, which reads flat compared to the rest of the app.

Two mockup rounds were reviewed visually by the user (browser companion) and decided:
- Hunter Profile → **Option A: HUD faceted card** (angled container, glow, section labels, colored attribute bars).
- Trial days → **Option A: circular progress ring** showing days remaining out of total trial length.

## 1. Hunter Profile — HUD card layout

Wrap the loaded-profile content in `AwakenAngledContainer` (already exists, used elsewhere for the angular-cut HUD look) instead of a bare `Column`. Structure, top to bottom:

1. **Header row**: `HunterAvatar` + `RankBadge` + name/level/class column (unchanged data, just placed inside the new container).
2. **Section label "XP"** — small dot + uppercase label + gradient line (new reusable pattern, see below), then the existing `XpBar`.
3. **Section label "Streak"** — same pattern, colored amber/orange (`AwakenColors.amber`), then the streak value (replacing the generic `_StatRow` for streak with a single emphasized line, e.g. icon + bold day count + label).
4. **Section label "Atributos"**, then one colored progress bar per attribute (replacing the plain `_StatRow` list) — see §2.

Add a small private `_SectionLabel` widget in `hunter_profile_page.dart` (dot + uppercase `AwakenTypography.labelSmall`-derived text + flexible gradient-ish divider line) so the three sections share one consistent visual treatment. This stays local to the page — it is not generic enough yet to promote to `design_system/components`.

The "no progress" and other non-loaded states keep their current `AwakenEmptyState`/`AwakenBlockedState` treatment (out of scope — those already follow the shared status-panel pattern).

## 2. Attribute bars — colors and component

Add the six fixed attribute colors to `AwakenColors` (`apps/mobile/lib/design_system/tokens/colors.dart`), matching `docs/design-system/tokens/colors.css`:

```dart
static const attrStrength = Color(0xFFFF5A3C);
static const attrAgility = Color(0xFF22D3A7);
static const attrEndurance = Color(0xFF2D6FF5);
static const attrVitality = Color(0xFFF5C518);
static const attrFocus = Color(0xFFA65CEE);
static const attrWisdom = Color(0xFF5FE8FF);
```

Create a new design-system component `AwakenStatBar` (`design_system/components/awaken_stat_bar.dart`), mirroring `docs/design-system/components/game/StatBar.jsx`: label (uppercase, `labelSmall`-ish) + numeric value on the right (colored, mono-ish), and a thin rounded track with a colored fill. Props: `label`, `value`, `color`, `max` (default `100`, clamped — the backend doesn't return a per-attribute max, so a fixed 0–100 scale is used client-side, same default as the JSX reference).

Replace the six `_StatRow` attribute entries in `hunter_profile_page.dart` with six `AwakenStatBar` instances, one per attribute, each using its fixed color from §2 and `value: profile.attributes?.X ?? 0`.

The generic `_StatRow` widget stays as-is for any non-attribute key/value rows if still needed elsewhere in the file (it is not being removed, just no longer used for attributes/streak).

## 3. Trial days — progress ring

In `subscription_page.dart`, replace the current `_TrialStatusBadge` + plain "days remaining" `Text` (inside `_TrialStatusContent`, shown when `isActive`) with a new `_TrialProgressRing` widget:

- A circular ring (`CustomPaint`, simple arc — no new dependency) showing **elapsed/total** trial days, with the **days remaining** number large in the center and "dias" label beneath it (mirrors the approved mockup).
- Total days = `trialEndsAt.difference(trialStartedAt).inDays` (both already present on `SubscriptionStatusLoaded`); elapsed = `total - daysRemaining`. If `trialStartedAt`/`trialEndsAt` are null (shouldn't happen for `trial_active`, but defensive), fall back to today's plain text line instead of the ring.
- Ring color: `AwakenColors.warning` when `isTrialEndingSoon`, otherwise `AwakenColors.success` (reuses the same semantics as today's badge color logic, just moved from a flat badge to the ring stroke).
- Below the ring: keep the existing "termina em breve" / "último dia" warning text (`trialStatusNearEnd` / `trialStatusLastDay`) unchanged — only the days-remaining presentation changes, not the messaging logic.
- The `_TrialStatusBadge` widget remains used as-is for `trial_expired` and `subscription_active` states (only the active-trial-with-days-remaining branch gets the ring).

No new l10n keys are needed — existing `trialStatusDaysRemaining`, `trialStatusNearEnd`, `trialStatusLastDay` strings are reused (the ring's center number can reuse `daysRemaining` directly without needing the full sentence).

## 4. Testing

- Update/extend existing widget tests in `test/features/hunter_profile/.../hunter_profile_page_test.dart` (or equivalent) to assert the six attribute bars render with expected values, and that section labels are present.
- Update `test/features/subscriptions/presentation/pages/subscription_page_test.dart` to assert the ring renders with the correct center value for `trial_active`, and that the badge path is unchanged for `trial_expired`/`subscription_active`.
- No backend tests needed (no backend changes).

## Out of scope

- Backend/API changes (attribute max values, trial length as an explicit field) — not needed, computed client-side from existing fields.
- Hunter card (`hunter_card_page.dart`) visuals — US-077/078/079/080, not mentioned by the user, not touched here.
- Localization — no new strings introduced.
