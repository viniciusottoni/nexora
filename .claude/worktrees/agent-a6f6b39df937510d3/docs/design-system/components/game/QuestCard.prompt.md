The daily-loop unit. Composes `Badge` for the attribute tag. Three states: `todo`, `active` (energy glow), `done` (dimmed + struck through).

```jsx
<QuestCard title="Flexões — 4x12" subtitle="Sem equipamento" xp={120} attr="strength" status="active" icon={<Dumbbell size={20} />} onToggle={complete} />
<QuestCard title="Corrida leve 15min" xp={90} attr="endurance" status="done" onToggle={undo} />
```

`onToggle` fires from the completion circle (stops propagation); `onClick` opens the quest detail. The XP reward always renders gold.

Each workout grants XP **and** points in one or two attributes. Use `rewards` for amounts; `attr` is the legacy single-tag shorthand (no amount):

```jsx
<QuestCard title="Agachamento 5x10" xp={140}
  rewards={[{ attr: 'strength', amount: 3 }, { attr: 'endurance', amount: 1 }]} status="active" />
```
