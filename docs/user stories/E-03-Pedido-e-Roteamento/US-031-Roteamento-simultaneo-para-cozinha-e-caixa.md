# US-031 · Roteamento simultaneo para cozinha e caixa

|  |  |
|---|---|
| **Épico** | [E-03 · Pedido e Roteamento](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 2 |
| **Requisitos funcionais** | RF-KDS-01, RF-CXA-01 |
| **Regras de negócio** | RN-001, RN-003 |
| **ADRs** | ADR-011, ADR-012 |
| **Eventos** | EVT-004 |
| **Aplicações** | api-edge, web-kds, web-pos |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** pizzaiolo (P3), caixa (P4) e garçom (P2),
> **quero** que o pedido confirmado apareça na minha tela imediatamente,
> **para** que nenhum pedido dependa de alguém levar um papel até mim.

## 2. Contexto e motivação

É a materialização da RN-001 e a resposta direta à dor central: *"o pedido é feito e não chega para cozinha"*.

O requisito de latência é duro — **2 segundos** (RF-KDS-01) — e só é atingível porque o trajeto é 100% local: o pedido é criado no edge, o WebSocket é do edge, o KDS está na mesma LAN. Nenhum round-trip à nuvem.

O roteamento é por **praça**: cada item vai para a fila de quem vai prepará-lo. O caixa recebe a atualização do consumo da mesa. O garçom recebe a confirmação. Todos ao mesmo tempo, pelo mesmo evento.

## 3. Escopo

### 3.1 Dentro desta história

- Emissão do evento para as salas de WebSocket corretas
- Roteamento de item por `station_id`
- Atualização do consumo da mesa no caixa
- Alerta a mesa, garçom, cozinha e caixa
- Fallback de polling a cada 5 s se o WebSocket cair
- Reenvio do que foi perdido na reconexão, via `lastEventId`

### 3.2 Fora desta história

- Renderização da fila do KDS (US-040)
- Prioridade dinâmica de fila (US-116, Fase 2)
- Fire time (US-115, Fase 2)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Roteamento do pedido

  Cenário: Chegada ao KDS
    Dado um pedido confirmado com itens de praças diferentes
    Quando o pedido for criado
    Então cada item deve aparecer na fila da sua praça em até 2 segundos
    E o caixa deve ver o consumo atualizado da mesa
    E mesa, garçom, cozinha e caixa devem receber alerta

  Cenário: Salas corretas do WebSocket
    Dado um pedido da mesa 12 com item da praça Forno
    Quando o evento for emitido
    Então deve alcançar as salas station:forno, role:cashier e table:12
    E não deve alcançar a sala da praça Bebidas

  Cenário: Queda do WebSocket no KDS
    Dado que a conexão em tempo real do KDS caiu
    Quando um novo pedido for confirmado
    Então o KDS deve exibi-lo em no máximo 5 segundos, via polling
    E deve indicar visualmente o modo degradado

  Cenário: Reconexão com recuperação
    Dado um KDS que ficou 40 segundos desconectado
    Quando reconectar informando o lastEventId
    Então o servidor deve reenviar os eventos perdidos no intervalo
    E nenhum pedido deve ficar ausente da fila

  Cenário: Pedido de delivery
    Dado um pedido do canal DELIVERY
    Quando for confirmado
    Então deve chegar ao KDS normalmente
    E deve ser visualmente distinguível dos pedidos de salão

  Cenário: Roteamento com internet caída
    Dado que a loja está sem internet
    Quando um pedido for confirmado
    Então o roteamento deve ocorrer normalmente pela rede local
    E a latência deve permanecer abaixo de 2 segundos
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-001 | Todo pedido confirmado é roteado simultaneamente para cozinha e caixa | É o objeto desta história |
| RN-003 | Cada transição de estado gera alerta aos perfis envolvidos | Alerta a quatro perfis pelo mesmo evento |
| RN-005 | A operação local não depende de internet | Trajeto integralmente na LAN |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-004 | `order.item.queued` | Item entra na fila da praça | stationId, position | ↑ |
| EVT-002 | `order.placed` | Consumido para disparar o roteamento | items[], total | ↑ |

> A tabela de reações do documento 04, seção 5, é normativa: `order.placed` → item em QUEUED, notificação a mesa, garçom, cozinha e caixa, métrica +1 pedido e T0.

## 7. Contrato de API

```http
# WebSocket — servidor para cliente:
{ "type": "order.placed",
  "data": { "orderId": "...", "code": "A47", "shortCode": "47",
            "table": "12", "channel": "DINE_IN",
            "items": [ { "orderItemId": "...", "productName": "...",
                         "stationId": "<forno>", "quantity": 1,
                         "modifiers": ["sem cebola"], "notes": "bem assada" } ] },
  "occurredAt": "..." }

# Salas inscritas automaticamente pelos claims do JWT:
#   tenant:{id} · store:{id} · station:{id} · table:{id} · role:{papel} · user:{id}

# Cliente para servidor:
{ "type": "subscribe", "data": { "rooms": ["station:oven"] } }
{ "type": "heartbeat" }

# Fallback (polling a cada 5 s):
GET /v1/kds/queue?stationId=...&since=<lastEventId>
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `order_item` | Praça e posição na fila | `station_id`, `status=QUEUED`, `placed_at` |
| `station` | Destino do roteamento | `id`, `code`, `capacity_slots` |
| `table_session` | Consumo atualizado no caixa | `total` |

## 9. Comportamento offline

É a história que **prova** o valor da arquitetura local-first. O trajeto pedido→KDS não toca a nuvem em nenhum ponto: PostgreSQL local, WebSocket local, dispositivos na mesma LAN.

Com a internet da loja caída, a latência é rigorosamente a mesma. O único efeito é que os eventos ficam acumulados no outbox para sincronização posterior — o que não afeta a operação em nada.

O fallback de polling (ADR-011) é requisito, não otimização: *a cozinha nunca pode depender de uma única via de comunicação*.

## 10. Interface e experiência

- Chegada de pedido no KDS com sinal sonoro configurável — a cozinha não fica olhando a tela
- Distinção visual clara entre canais (salão, delivery, balcão)
- Indicador de modo degradado discreto porém inequívoco quando o WebSocket cair
- No caixa, o valor da mesa atualiza sem recarregar e sem piscar a tela inteira

## 11. Métricas, alertas e observabilidade

- Latência pedido→exibição no KDS (p95) — meta abaixo de 2 s, medida ponta a ponta
- Contagem de quedas de WebSocket por dispositivo e por hora — diagnóstico de rede
- Tempo em modo degradado por dispositivo
- Eventos reenviados na reconexão

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Evento alcança exatamente as salas corretas e nenhuma outra |
| Integração | Latência abaixo de 2 s com 12 pedidos simultâneos |
| Integração | Fallback de polling entrega em no máximo 5 s |
| Integração | Reconexão com `lastEventId` recupera o intervalo perdido sem duplicar |
| Caos | Derrubar o WebSocket no meio do pico e verificar que nenhum pedido some |
| Caos offline | Roteamento com internet da loja derrubada mantém a latência |
| E2E | Do toque em confirmar até o cartão aparecer no KDS, cronometrado |

## 13. Dependências

**Depende de:** US-030, US-017  
**Habilita:** US-040, US-050, US-080

## 14. Definition of Ready e Definition of Done

**DoR — a história só entra em sprint quando:**

- [ ] Persona, ação e resultado estão claros
- [ ] Critérios de aceite escritos em Gherkin
- [ ] Requisito funcional (RF) e evento (EVT) referenciados
- [ ] Dependências identificadas e resolvidas
- [ ] Desenho de tela existe (quando há interface)
- [ ] Estimada pelo time
- [ ] Comportamento offline definido
- [ ] Impacto em métrica e alerta identificado

**DoD — a história só é concluída quando:**

- [ ] Código revisado e aprovado por outro desenvolvedor
- [ ] Testes unitários dos casos de negócio passando
- [ ] Teste de integração do fluxo principal passando
- [ ] Teste de isolamento multi-tenant (quando a história toca tabela com `tenant_id`)
- [ ] Eventos emitidos conforme o catálogo do documento 04
- [ ] Comportamento offline verificado (quando aplicável)
- [ ] Critérios de aceite validados em ambiente de teste pelo PO
- [ ] Sem violação do ADR-013 (proibição de código por cliente)
- [ ] Documentação atualizada (OpenAPI, catálogo de eventos, modelo de dados)
- [ ] Observabilidade instrumentada (log estruturado + traço OpenTelemetry)
- [ ] Aprovada pelo PO
- [ ] Latência medida e registrada em ambiente equivalente ao da loja

## 15. Riscos, premissas e pendências

- **Risco T3 (doc. 02)** — Wi-Fi instável na área operacional tem probabilidade alta. Mitigação de infraestrutura: rede cabeada para KDS e caixa, VLAN dedicada, AP separado para clientes.
- Perder um evento de roteamento é falha silenciosa — o pedido simplesmente não aparece. O mecanismo de `lastEventId` e o polling de fallback existem exatamente para isso, e precisam de teste de caos, não só de integração.

---

*US-031 · Épico E-03 · Pacote 004_DonaBetinha · Replay Studio.*