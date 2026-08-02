# Awaken — Design System

> **Desperte o seu potencial.**
> The design system for **Awaken**, a gamified fitness app that turns your daily
> workout into an epic anime-style journey of self-evolution. Dark, immersive, epic —
> the dark of a shonen anime: intense, motivating, aspirational. Built mobile-first
> (Flutter), Android → iOS.

---

## 1. Product context

Awaken is **"the Duolingo of training, with the soul of an anime."** You are not just
working out — you level up, complete quests, collect achievements and watch your hunter
character grow as your body changes. The gym is the **dungeon**, you are the **hunter**,
every workout is a **quest**, every week is a **season**.

It is a direct, honest answer to the app **Arise** (Solo Leveling–themed fitness app),
whose users complain about exactly three things: it's fully paid, English-only, and buggy.
Awaken's wedge: **native PT-BR, honest freemium, real personalization, zero dark patterns,
stable.**

**Core pillars**
1. **Progression system** — Ranks **E → D → C → B → A → S → SS → SSS**, levels within each
   rank, XP from workouts/streaks/quests, and six character **attributes**: Força,
   Agilidade, Resistência, Vitalidade, Foco, Sabedoria.
2. **Quest system** — three quest kinds, each a `SystemWindow` HUD themed by color:
   **Quest Diária** (blue — a grouped set of the day's workouts, with a streak-penalty warning),
   **Dungeon** (purple — a one-off side quest / treino pontual) and **Raid** (red/gold —
   group-only, activates when a squad assembles). Every workout grants XP **and** points in
   one or two attributes. Plus weekly Master Quests and a persistent "battle log".
3. **Hunter profile / card** — Rank, Level, Streak, Class, Attributes; a shareable animated
   card for organic virality.
4. **Smart workouts** — detailed onboarding; the plan respects the user's equipment and
   limits 100%; easy/hard variants; edit before starting; preferences persist.
5. **Nutrition** — daily water/protein/calorie goals shown as RPG status bars.
6. **Progress & charts** — evolution per exercise; quest history; physical timeline.

**Audience.** Primary: 16–30, anime fans (isekai/shonen) who find traditional fitness apps
boring. Secondary: 30–50 who like gamification. Central pain: *"I want to train but I lose
motivation after two weeks."*

**Business model.** Honest freemium — plans shown **before** onboarding. Free Hunter
(1 daily quest, full XP/rank, shareable card, 7-day history) vs **S-Rank** premium
(unlimited personalized quests, full nutrition, Master Quests, animated card, no ads).
R$14,90/mo · R$99,90/yr · 7-day trial, no card.

### Sources provided
- `uploads/projeto-conceito.md` — full product concept, positioning, MVP scope, and the
  original visual-identity brief (paleta, tipografia, do's & don'ts). **Primary brand source.**
- `uploads/stack-tecnologica.md` — tech stack. **Decision: Flutter** (Dart) + ASP.NET Core
  backend. Flutter chosen for heavy animations (XP particles, level-up, glowing rank cards).
- `uploads/logo.png` — the master logo (glowing eyes + blue flame + AWAKEN wordmark).
  Cropped into `assets/logo-mark.png`, `assets/logo-wordmark.png`, `assets/logo-full.png`.

No codebase or Figma was attached — this system is built from the concept brief + logo.
When real product screens or a Flutter repo exist, fold them in and reconcile.

---

## 2. Content fundamentals — voice & tone

**Language: PT-BR, native.** This is non-negotiable and the #1 competitive wedge. Never
ship English UI. (This design system documents copy in PT-BR; English appears only in dev notes.)

**Voice: the System talking to a hunter.** Second person, imperative, motivating. The app
addresses the user as **você** and speaks like the "System" of a shonen anime — confident,
a little mythic, never corny or aggressive.

- **Casing.** Headings and labels in the display face are frequently **UPPERCASE** for the
  epic, HUD-like feel ("QUEST DIÁRIA", "NÍVEL 37", "DESBLOQUEAR S-RANK"). Body copy is
  sentence case.
- **Person.** "você" / imperative ("Desperte.", "Complete a quest.", "Suba de rank."). The
  app speaks *to* the hunter; the hunter is always the protagonist.
- **Tone words:** épico, intenso, claro, honesto. **Not** medieval-fantasy, **not** horror,
  **not** corporate-gym.
- **Numbers matter.** XP, levels, streaks, attributes are shown precisely and proudly, in
  the mono face (`640 / 900 XP`, `Streak 12`, `+72 Força`).
- **Emoji:** sparingly and purposefully — the streak flame 🔥 is the main sanctioned one
  (it maps to a real product concept). Avoid decorative emoji soup. Prefer real iconography.
- **Honesty in copy.** Because the brand is defined against dark patterns: prices and limits
  are stated plainly and early. No "grátis*" asterisks, no fake urgency, no guilt.

**Examples (PT-BR)**
- Splash / hero: **"Desperte o seu potencial."**
- CTA primary: **"Iniciar Quest"**, **"Continuar treino"**, **"Desbloquear S-Rank"**
- Empty state: *"Sua primeira quest te espera. Bora?"*
- Level up: **"VOCÊ SUBIU PARA O NÍVEL 38"** / *"Atributo +1: Força"*
- Paywall (shown up front): *"Veja os planos antes de começar. Sem surpresa."*
- Positioning line: *"A academia é o dungeon. Você é o hunter."*

---

## 3. Visual foundations

The look is **dark, immersive, epic — with clarity**. Not heavy like a horror game; the dark
of a shonen anime. Reference atmospheres (do **not** copy): Solo Leveling, Jujutsu Kaisen,
Blue Lock; gamification clarity of Duolingo; functional cleanliness of Nike Training Club.

**Color.** Deep near-black backgrounds with a faint blue cast (`--bg-void` #07080D →
`--bg-base` #0A0B12). **Electric blue** (`--primary` #2D6FF5) is the energy/primary; **blue-flame
purple** (`--secondary` #8B3FD8) is the companion (straight from the logo flame); **gold**
(`--accent` #F5C518) is *always* XP / achievement / premium. A cyan **spark** (#5FE8FF) is the
glint from the logo's eyes, used for highlights. The **rank ladder** has its own fixed colors
(E gray → SSS cyan-gradient) and the six **attributes** each own a fixed color — these are
learned by users, so never recolor them.

**Gradients.** The signature is **`--grad-energy`** — a 135° blue→purple wash, the visual
DNA of the brand (it's the logo flame as a gradient). Used on primary buttons, selected
states, hero washes. XP uses gold→amber (`--grad-xp`). Backgrounds use a subtle radial
`--grad-void` (lighter at top, like a dawn over the dungeon). Gradients are used with
restraint — energy is the spice, not the whole plate. **Avoid generic bluish-purple SaaS
gradients** as flat fills; the energy gradient earns its place on interactive/hero elements.

**Type.** Display = **Chakra Petch** — an angular, cut-corner sci-fi face, the closest open
match to the bespoke AWAKEN wordmark (sharp shonen terminals); set tight, often uppercase,
for headings/ranks/CTAs. Body/UI = **Sora** — clean geometric sans, excellent in dark mode.
Stats/numbers = **JetBrains Mono**, tabular, for XP/levels/timers/attributes. (See font
substitution note in §5.)

**Spacing & layout.** 4px base scale. Mobile gutter `--screen-pad` 20px; content column caps
at `--content-max` 440px (phone). Min touch target 44px. Bottom tab bar + top status header
are the fixed elements; content scrolls between them.

**Corners & cards.** Moderate radii — default card `--radius-lg` 18px. A sharper
`--radius-blade` (4px) is available for the most "HUD/sci-fi" elements (rank chips, hunter
card frames). Cards are dark surfaces (`--bg-surface`) with a 1px hairline border
(`--border-default`), a soft black depth shadow (`--shadow-md`) and a faint top sheen
(`--inset-sheen`). Hero/hunter cards swap the shadow for a **rank-colored glow** border.

**Elevation — two languages.** (1) **Black depth shadows** (`--shadow-sm/md/lg`) for ordinary
layering. (2) **Colored energy glows** (`--glow-blue/purple/gold/cyan`) for "alive" elements —
active quests, CTAs, level-up. Glow is reserved for emphasis; most surfaces use depth shadow only.

**Borders.** Hairlines are translucent white (`--border-subtle` 6% → `--border-default` 10%);
`--border-strong` is a bluish 28% for focus/active framing. Dividers are 7% white.

**Transparency & blur.** Glass surfaces (the bottom nav, bottom sheets, sticky headers) use a
dark translucent fill + `backdrop-filter: blur(--blur-md)`. Used for chrome that floats over
scrolling content — not decoratively.

**Imagery vibe.** Cool, electric, high-contrast on black; blue/purple energy with cyan glints.
**Do not** use generic AI anime art (the Arise mistake) or stock gym photography. Prefer the
logo's eye-and-flame language, energy particles, silhouettes, and the rank/attribute HUD.
Character art, when added, should be original/artistic silhouettes — never generic AI.

**Motion.** Fast and cinematic, never sluggish. Standard transitions `--dur-base` 220ms on
`--ease-out`. Big moments (level-up, rank-up, XP fill) use `--dur-epic` 600ms and the springy
`--ease-spring` for a satisfying pop; XP/stat bars animate their width. Energy particles on
XP/level gains. No infinite decorative loops on content. Respect `prefers-reduced-motion`.

**Interaction states.**
- **Hover** (desktop/preview): slight `brightness(1.08)` and/or a 2px lift; border brightens
  to `--border-strong`.
- **Press:** quick `scale(0.96)` — the tactile "tap." Selected chips/cards gain the energy
  glow.
- **Focus:** blue ring (`--ring-focus`) on inputs; `--border-focus` border.
- **Disabled:** 40% opacity, no glow, `not-allowed`.

---

## 4. Iconography

**Approach: line icons, uniform stroke** — matching the brief ("ícones de exercício: estilo
linha com espessura uniforme"). Clean, geometric, ~2px stroke, rounded joins. They read as a
sleek HUD, not a cartoon.

- **Recommended set:** **[Lucide](https://lucide.dev)** — uniform 2px stroke, rounded, huge
  coverage (dumbbell, flame, zap, trophy, target, droplet, timer, user, chevrons, etc.).
  It is the closest open match to the brief's icon direction. **This is a substitution** —
  no production icon set was supplied; swap to the app's real Flutter icon set when it exists.
  Load from CDN: `https://unpkg.com/lucide@latest` (or `lucide-react` in React surfaces).
- **Rank emblems** are **not** icons — they are the hexagonal `RankBadge` component
  (faceted shield, glows by rank). The hexagon/shield is the brand's signature game glyph.
- **Attribute glyphs:** pair each attribute color with a Lucide glyph (Força→`dumbbell`,
  Agilidade→`wind`/`zap`, Resistência→`heart-pulse`/`activity`, Vitalidade→`shield`,
  Foco→`target`, Sabedoria→`eye`). Keep consistent app-wide.
- **Emoji:** essentially one sanctioned glyph — the streak flame 🔥 — and even that can be
  replaced by a Lucide `flame`. No emoji as primary UI icons.
- **Unicode/arrows:** use Lucide chevrons, not unicode arrows, for consistency.

Icons inherit `currentColor`; tint them with text or accent tokens. On dark surfaces use
`--text-secondary` for default, `--blue-300`/accent for active.

---

## 5. Fonts & substitutions ⚠️

The AWAKEN **wordmark is a bespoke typeface** (sharp, faceted, custom). For product UI we
substitute open fonts that capture the spirit:

| Role | Token | Font (substitute) | Why |
|---|---|---|---|
| Display / headings | `--font-display` | **Chakra Petch** | Angular cut-corner sci-fi; closest open match to the wordmark |
| Body / UI | `--font-body` | **Sora** | Clean geometric sans, great on dark |
| Stats / numbers | `--font-mono` | **JetBrains Mono** | Tabular figures for XP/levels/timers |

Loaded via Google Fonts `@import` in `tokens/fonts.css` (no self-hosted binaries shipped).
**Please confirm or provide the licensed display face** if Awaken has a custom UI font, and
self-host it if offline support is required. The logo PNGs already use the real wordmark for
brand marks.

---

## 6. Index / manifest

**Root**
- `styles.css` — the single entry point consumers link (an `@import` manifest only).
- `tokens/` — `fonts.css`, `colors.css`, `typography.css`, `spacing.css`, `effects.css`,
  `base.css` (resets).
- `assets/` — `logo-full.png`, `logo-mark.png`, `logo-wordmark.png`.
- `SKILL.md` — Agent-Skill front-matter for use in Claude Code.

**Components** (`window.AwakenDesignSystem_956798.*`)
- `components/core/` — **Button, Card, Badge, Chip, Input, Switch, Avatar**
- `components/game/` — **RankBadge, XPBar, StatBar, QuestCard, ProgressRing, SystemWindow**
  Each has `.jsx` + `.d.ts` + `.prompt.md`; each directory has a `@dsCard` showcase HTML.

**Foundations** (Design System tab cards — `guidelines/`)
- Colors: `color-brand`, `color-surfaces`, `color-text-status`, `color-ranks`, `color-attributes`
- Type: `type-display`, `type-body`, `type-mono`, `type-scale`
- Spacing: `spacing-scale`, `spacing-radii`, `spacing-elevation`
- Brand: `brand-logo`, `brand-gradients`

**UI kit** (`ui_kits/app/`) — interactive recreation of the Awaken mobile app: onboarding/auth,
the up-front paywall, the daily-quest home, the hunter profile, and a live workout. See
`ui_kits/app/README.md`.

---

*Built June 2026 from the Awaken concept brief + master logo. Stack: Flutter (Dart).*
