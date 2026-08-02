# Awaken — App UI kit

Interactive, high-fidelity recreation of the **Awaken** mobile app (Flutter, Android-first),
built on the Awaken design-system components. Open `index.html` and walk the full flow.

## Flow
**Splash** → **Plans** (shown up-front — honest freemium, no surprise paywall) →
**Onboarding** (objetivo, nível, local/equipamento, dias — multi-step, respects the user) →
**Home** (daily quests, rank + XP hero, nutrition rings) ↔ **Profile** (shareable hunter card,
attributes, achievements). The center **Treinar** FAB / tapping a quest opens the live
**Workout** screen; finishing it fires the **Level-up** overlay.

## Files
- `index.html` — entry; loads React, the DS bundle, then the JSX below in order.
- `icons.jsx` — inline Lucide line-icon set (`window.AwakenIcon`).
- `chrome.jsx` — `PhoneFrame`, `StatusBar`, `AppHeader`, `IconBtn`, `TabBar`, `ScreenScroll`.
- `screens-onboarding.jsx` — `Splash`, `Plans`, `Onboarding`.
- `screens-main.jsx` — `Home`, `Profile`, `Workout`, `LevelUp` (+ `HUNTER` sample data).
- `app.jsx` — root navigation state, mounts to `#root`.

## Built from
The `uploads/projeto-conceito.md` brief (pillars, freemium rules, PT-BR voice, anti-dark-pattern
stance) and the Awaken logo. Components come from `window.AwakenDesignSystem_956798`
(Button, Card, Badge, Avatar, RankBadge, XPBar, StatBar, QuestCard, ProgressRing).

> No production Flutter code or Figma was provided — screens realize the documented concept.
> When the real app exists, reconcile copy, spacing and the workout/nutrition data models.
