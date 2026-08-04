# ADR-011 · WebSocket local com fallback de polling

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead |
| **Relacionados** | ADR-001, ADR-027 |
| **Requisitos afetados** | RF-KDS-01, RNF-PER-01, RNF-DIS-07 |

---

## Contexto

A promessa central do produto é que o pedido chega à cozinha. O requisito é menos de 2 segundos (RNF-PER-01).

Mas há um risco pior que a lentidão: **falha silenciosa**. Se o WebSocket cair sem que ninguém perceba, o KDS continua exibindo a tela normalmente, apenas sem pedidos novos. A cozinha acha que está tudo calmo enquanto o salão enche. Esse é exatamente o cenário que o produto existe para eliminar — e seria constrangedor reproduzi-lo por decisão técnica.

## Decisão

**SignalR (ASP.NET Core) sobre WebSocket no servidor local, com fallback automático para polling a cada 5 segundos**, sinalização visual do modo degradado e recuperação de mensagens perdidas na reconexão.

## Detalhamento

### Salas

```
tenant:{id}      todos os usuários do estabelecimento
store:{id}       usuários da loja
station:{id}     terminais de uma praça de produção
table:{id}       sessão de uma mesa específica
role:{papel}     todos os garçons, todos os caixas
user:{id}        alertas pessoais
```

A inscrição é derivada dos claims do token, não solicitada pelo cliente — o cliente não escolhe o que pode ouvir.

### Reconexão com recuperação

```ts
connection.onreconnected(async () => {
  await connection.invoke('Resume', { lastEventId: store.getLastEventId() });
});
// hub do SignalR reenvia os eventos posteriores ao lastEventId
```

No servidor (`Nexora.Infrastructure`, hub registrado em `Api.Edge`):

```csharp
public sealed class KdsHub : Hub
{
    public async Task Resume(string lastEventId)
    {
        var pending = await _queue.GetEventsAfterAsync(lastEventId);
        foreach (var evt in pending)
            await Clients.Caller.SendAsync("kds.event", evt);
    }
}
```

### Fallback

```ts
type Mode = 'ws' | 'polling';
let mode: Mode = 'ws';

connection.onclose(() => {
  startPolling(5000);                 // GET /v1/kds/queue?since=<lastEventId>
  ui.showDegradedBadge();             // sinalização visível, discreta
});

connection.onreconnected(() => {
  stopPolling();
  ui.hideDegradedBadge();
});
```

### Deduplicação no cliente

Como as duas vias podem entregar o mesmo evento, o cliente descarta `eventId` já processado.

### Parâmetros

| Parâmetro | Valor |
|---|---|
| Heartbeat | 20 s |
| Timeout sem resposta | 60 s → reconecta |
| Backoff de reconexão | 1s, 2s, 4s, 8s… teto 30 s |
| Intervalo de polling | 5 s |
| Janela de recuperação | Últimos 500 eventos ou 30 min |

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Apenas polling | Simples; robusto | Latência de 5 s e carga desnecessária | Não atende ao requisito de 2 s |
| Apenas WebSocket | Latência mínima | Falha silenciosa possível | Risco inaceitável no gargalo do produto |
| Server-Sent Events | Simples; reconexão nativa | Unidirecional; precisamos de ack do cliente | Faltaria confirmação de entrega e ack de alerta |
| WebSocket puro sem SignalR | Menos peso | Reconexão, grupos e fallback teriam de ser escritos | Reimplementar o que o framework já resolve |
| Long polling | Compatível | Pior que as opções acima em tudo | — |

## Consequências

**Positivas**

- Latência abaixo de 2 s no caminho normal
- Degradação previsível e **visível** — a cozinha sabe quando está em modo alternativo
- Recuperação de mensagens perdidas na reconexão
- Salas por papel e praça evitam enviar tudo para todos

**Negativas**

- Duas vias de entrega para manter e testar
- Necessidade de deduplicação no cliente
- Cliente `@microsoft/signalr` adiciona peso ao bundle (~30 KB)

**Mitigações**

- Teste E2E específico com WebSocket derrubado (cenário C-08)
- Deduplicação centralizada em um único hook no frontend
- Bundle do KDS não é crítico (rede local, terminal fixo)

## Como validar

- Cenário C-08: derrubar WebSocket e confirmar entrega em até 5 s via polling, com indicação visual
- RNF-PER-01 medido em produção: p95 abaixo de 2 s
- Teste de reconexão após 10 min desconectado: nenhum pedido perdido

## Revisitar quando

- O número de dispositivos por loja crescer a ponto de exigir outra estratégia de fan-out
- Surgir necessidade de tempo real entre lojas (rede multi-unidade)
