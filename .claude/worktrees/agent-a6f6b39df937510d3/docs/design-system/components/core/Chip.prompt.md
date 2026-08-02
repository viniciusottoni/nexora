Selectable pill used heavily in onboarding ("Qual seu objetivo?") and as filters. Manage `selected` in parent state.

```jsx
<Chip selected={goal === 'massa'} onClick={() => setGoal('massa')}>Ganhar massa</Chip>
<Chip icon={<Dumbbell size={16} />}>Em casa</Chip>
```

Single- or multi-select is your call — the component is presentational. Always ≥44px tall for touch.
