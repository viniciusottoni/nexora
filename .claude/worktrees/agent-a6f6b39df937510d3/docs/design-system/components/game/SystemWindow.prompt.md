The "System" HUD window that announces a quest — Awaken's most cinematic surface. A faceted, glowing frame themed by the quest taxonomy.

**Quest taxonomy (drives the color + label):**
- `daily` — blue. **Quest Diária**: a grouped set of workout goals. Renders a red penalty **AVISO** (missing it resets the streak) — Solo-Leveling-style, daily quests only.
- `dungeon` — purple. A one-off side quest (treino pontual).
- `raid` — red/gold. Group-only; pass `participants` to show squad assembly.

Each workout grants XP **and** points in one or two attributes — pass both via `xp` and `rewards`.

```jsx
<SystemWindow
  kind="daily"
  title="Treino do Dia — Tronco"
  rank="C"
  goals={[
    { label: 'Flexões', current: 40, target: 100, unit: 'reps' },
    { label: 'Prancha', current: 90, target: 120, unit: 's' },
    { label: 'Corrida', done: true },
  ]}
  xp={180}
  rewards={[{ attr: 'strength', amount: 3 }, { attr: 'endurance', amount: 1 }]}
  cta="Aceitar Quest"
/>

<SystemWindow kind="dungeon" title="Mobilidade de Quadril" xp={90}
  goals={[{ label: 'Alongamento guiado', done: true }]}
  rewards={[{ attr: 'agility', amount: 2 }]} />

<SystemWindow kind="raid" title="Desafio do Esquadrão" rank="S"
  participants={{ current: 3, max: 5 }} xp={500}
  rewards={[{ attr: 'vitality', amount: 4 }, { attr: 'focus', amount: 2 }]} />
```

Theme colors are fixed per kind so users learn them — don't recolor. `warning` defaults to streak-reset copy; pass a string to override or `false` to hide. Pass `false` only when intentionally dropping the anti-streak contract.
