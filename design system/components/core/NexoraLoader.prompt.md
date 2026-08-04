Carregamento da plataforma. O símbolo da Nexora fica dentro de uma moeda que quica
suavemente e, na mesma cadência, gira 360° no eixo Y — moeda vista de frente. Embaixo,
uma linha dizendo **o que** está acontecendo, nunca só "aguarde".

`NexoraLoader` é a espera genérica. `NexoraSplash` é o uso obrigatório antes de cartão de
**login** e de **primeiro acesso**: quica duas vezes e some enquanto o cartão abre do
centro para os lados (do meio para a esquerda e do meio para a direita). Depois que o
cartão termina de abrir, o `NexoraLogo` dentro dele — se renderizado com `shine` —
brilha uma vez, da esquerda para a direita.

```jsx
<NexoraSplash label="Preparando o primeiro acesso">
  <form className="cartao">
    <NexoraLogo variant="lockup" height={42} shine />
    …
  </form>
</NexoraSplash>

<NexoraLoader label="Sincronizando pedidos" />              {/* espera indefinida */}
<NexoraLoader inverse label="Reconectando" size={64} />     {/* sobre navy */}
```

O gatilho do brilho é a classe `.is-open`, aplicada pelo próprio `NexoraSplash` no
conteúdo assim que a abertura termina — `NexoraLogo` com `shine` fora de um
`NexoraSplash` fica parado, invisível (sem `.is-open` ancestral não há o que tocar).

Regras: o rótulo descreve a ação em português, sem emoji. Sobre navy use `inverse`.
Toda duração vem de `tokens/motion.css` (`--dur-*`, `--ease-*`) — com
`prefers-reduced-motion` os tokens zeram, a quicada não acontece e a tela cai direto no
conteúdo, que é o comportamento correto. Nunca troque a quicada por um spinner genérico
em fluxo de entrada: é o único momento em que a marca aparece em movimento.
