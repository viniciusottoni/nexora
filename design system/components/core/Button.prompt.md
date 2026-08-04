Botão de ação da Nexora — `primary` herda a cor do tenant, `accent` (verde) confirma operação, `danger` cancela/estorna.

```jsx
<Button variant="primary" iconLeft="send" size="touch">Enviar pedido</Button>
<Button variant="secondary" size="sm">Cancelar</Button>
```

Em telas de operação (mesa, garçom, KDS) use `size="lg"` ou `"touch"` — alvo mínimo de 48px.
