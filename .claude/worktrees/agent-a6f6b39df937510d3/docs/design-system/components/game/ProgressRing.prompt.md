Circular goal meter for the nutrition / daily-goal tiles (água, proteína, quests). Stroke animates on value change.

```jsx
<ProgressRing value={6} max={8} color="energy" label="6" sublabel="copos" />
<ProgressRing value={1800} max={2200} color="var(--attr-vitality)" sublabel="kcal" />
```

`color="energy"` uses the brand gradient; otherwise pass any CSS color (e.g. an attribute token). Supply `children` to fully customize the center.
