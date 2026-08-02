The default container for everything on a surface. Use `variant="glow"` with a `rank` to make hunter and achievement cards radiate their rank color.

```jsx
<Card>Conteúdo padrão</Card>
<Card variant="energy" padding={20}>Bloco em destaque</Card>
<Card variant="glow" rank="S" interactive>Card do Hunter</Card>
```

Variants: `default`, `energy` (faint brand wash), `glow` (rank-colored border halo). Pass `interactive` for hover lift on tappable cards.
