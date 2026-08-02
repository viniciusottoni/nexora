---
name: awaken-design
description: Use this skill to generate well-branded interfaces and assets for Awaken — a gamified, anime-style (Solo Leveling-esque) fitness app — either for production or throwaway prototypes/mocks. Contains essential design guidelines, colors, type, fonts, assets, and UI kit components for prototyping. Voice is PT-BR.
user-invocable: true
---

Read the `readme.md` file within this skill, and explore the other available files.

If creating visual artifacts (slides, mocks, throwaway prototypes, etc), copy assets out and
create static HTML files for the user to view. If working on production code, you can copy
assets and read the rules here to become an expert in designing with this brand.

If the user invokes this skill without any other guidance, ask them what they want to build or
design, ask some questions, and act as an expert designer who outputs HTML artifacts _or_
production code, depending on the need.

## Awaken essentials
- **Brand:** Awaken — "Desperte o seu potencial." Gamified fitness, anime/shonen energy. Dark,
  immersive, epic, but clean. The gym is the dungeon; the user is the hunter.
- **Voice:** PT-BR, native, second-person (você), imperative & motivating. Display labels often
  UPPERCASE. Numbers shown precisely in mono. Honest — no dark patterns. One sanctioned emoji: 🔥.
- **Color:** deep-void backgrounds (#07080D / #0A0B12); electric blue primary (#2D6FF5);
  blue-flame purple secondary (#8B3FD8); gold = XP/achievement (#F5C518). Signature gradient
  `--grad-energy` (blue→purple). Rank ladder E→SSS and 5 attributes each have fixed colors.
- **Type:** Chakra Petch (display), Sora (body), JetBrains Mono (stats). Substitutes for the
  bespoke wordmark — confirm/replace if a licensed face exists.
- **Icons:** Lucide line icons (uniform 2px stroke). Rank emblems are the hexagonal `RankBadge`.

## Where things are
- `styles.css` — link this; pulls all tokens + fonts. Tokens in `tokens/`.
- `components/` — React primitives (`window.AwakenDesignSystem_956798.*`): core (Button, Card,
  Badge, Chip, Input, Switch, Avatar) and game (RankBadge, XPBar, StatBar, QuestCard, ProgressRing).
- `ui_kits/app/` — interactive recreation of the full mobile app; copy patterns from here.
- `guidelines/` — foundation specimen cards.
- `assets/` — `logo-full.png`, `logo-mark.png`, `logo-wordmark.png`.
