# 04 — Catálogo de Eventos e Máquinas de Estado
## Ecossistema Dona Betinha

| | |
|---|---|
| **Projeto** | 004_DonaBetinha |
| **Documento** | Catálogo de Eventos e Máquinas de Estado |
| **Versão** | 1.0 |
| **Data** | 31/07/2026 |
| **Depende de** | `02-Arquitetura-Tecnica.md`, `03-Modelo-de-Dados.md` |

---

## 1. Por que este documento existe

O evento é a unidade fundamental do sistema. Dele derivam **quatro coisas ao mesmo tempo**:

```
                  ┌──► ESTADO       (a fila do KDS, o mapa de mesas)
   EVENTO ────────┼──► MÉTRICA      (tempo de produção, CMV, ticket)
                  ├──► ALERTA       (pedido atrasado, estoque mínimo)
                  └──► SINCRONIZAÇÃO (o que sobe para a nuvem)
```

Se um evento não for emitido, quatro coisas quebram silenciosamente. Por isso o catálogo é normativo: **nenhuma transição de estado pode ocorrer sem emitir seu evento correspondente**.

---

## 2. Anatomia de um evento

```json
{
  "id": "01919e2a-...",              // UUID v7 gerado na origem — chave de deduplicação
  "type": "order.item.fired",        // nome canônico
  "version": 1,                      // versão do schema do payload
  "tenantId": "...",
  "storeId": "...",
  "aggregateType": "OrderItem",
  "aggregateId": "...",
  "actorId": "...",                  // quem fez
  "deviceId": "...",                 // onde
  "origin": "EDGE",                  // EDGE | CLOUD
  "deviceSeq": 148223,               // sequência monotônica da instalação
  "occurredAt": "2026-07-31T20:47:12.334Z",   // horário do FATO
  "recordedAt": "2026-07-31T21:15:03.812Z",   // horário de chegada na nuvem
  "payload": { "...": "..." }
}
```

### 2.1 Regras invioláveis

| # | Regra |
|---|---|
| R1 | `id` é gerado na **origem**, nunca no destino — é o que torna o reenvio idempotente |
| R2 | `occurredAt` é o horário do fato; **toda métrica de tempo usa este campo** |
| R3 | Evento é **append-only** — nunca alterado, nunca apagado; correção se faz com evento compensatório |
| R4 | `payload` contém o **delta**, não o objeto inteiro (exceto em `*.created`) |
| R5 | Toda mudança de schema incrementa `version`; consumidores tratam versões antigas |
| R6 | Evento é emitido na **mesma transação** do estado (transactional outbox) |
| R7 | Nome no padrão `<agregado>.<entidade>.<ação no passado>` |

---

## 3. Catálogo de eventos

> **EVT-xxx** é o identificador de rastreabilidade. **Sync** indica se o evento trafega para a nuvem. **Fase** indica quando é implementado.

### 3.1 Pedido — o núcleo

| ID | Evento | Quando | Payload principal | Sync | Fase |
|---|---|---|---|:-:|:-:|
| EVT-001 | `order.created` | Rascunho aberto | channel, sessionId, tableId | ✓ | 1 |
| EVT-002 | `order.placed` | **T0** — pedido confirmado | items[], total, promisedAt | ✓ | 1 |
| EVT-003 | `order.item.added` | Item acrescentado a pedido aberto | variantId, qty, modifiers, fractions | ✓ | 1 |
| EVT-004 | `order.item.queued` | Item entra na fila da praça | stationId, position | ✓ | 1 |
| EVT-005 | `order.item.fired` | **T1** — produção iniciada | stationId, operatorId | ✓ | 1 |
| EVT-006 | `order.item.oven_in` | **T2** — entrou no gargalo | slotIndex | ✓ | 2 |
| EVT-007 | `order.item.oven_out` | **T3** — saiu do gargalo | cookSeconds | ✓ | 2 |
| EVT-008 | `order.item.ready` | **T4** — pronto | prepSeconds | ✓ | 1 |
| EVT-009 | `order.item.served` | **T5** — entregue à mesa | waiterId | ✓ | 1 |
| EVT-010 | `order.item.cancelled` | Item cancelado | reason, authorizedBy, wasStarted | ✓ | 1 |
| EVT-011 | `order.item.refired` | Item refeito | reason, originalItemId | ✓ | 2 |
| EVT-012 | `order.item.unavailable_flagged` | Cozinha marcou falta | variantId | ✓ | 1 |
| EVT-013 | `order.ready` | Todos os itens prontos | totalPrepSeconds | ✓ | 1 |
| EVT-014 | `order.dispatched` | Saiu para entrega | courierId, runId | ✓ | 4 |
| EVT-015 | `order.delivered` | Entregue ao cliente | deliverySeconds, outcome | ✓ | 4 |
| EVT-016 | `order.cancelled` | Pedido cancelado | reason, authorizedBy, stage | ✓ | 1 |
| EVT-017 | `order.promise_recalculated` | Prazo recalculado | oldPromise, newPromise, queueSize | ✓ | 2 |

### 3.2 Mesa e comanda

| ID | Evento | Quando | Payload | Sync | Fase |
|---|---|---|---|:-:|:-:|
| EVT-020 | `table.session.opened` | Mesa aberta | tableId, guestCount, waiterId, source (QR/WAITER) | ✓ | 1 |
| EVT-021 | `table.waiter_called` | Cliente chamou o garçom | tableId | ✓ | 1 |
| EVT-022 | `table.bill_requested` | Conta solicitada | tableId, splitMode | ✓ | 1 |
| EVT-023 | `table.session.closed` | Comanda encerrada | total, serviceFee, durationSeconds | ✓ | 1 |
| EVT-024 | `table.session.rated` | Avaliação registrada | rating, comment | ✓ | 2 |
| EVT-025 | `table.items_transferred` | Itens movidos entre mesas | fromTableId, toTableId, itemIds | ✓ | 2 |
| EVT-026 | `table.released` | Mesa liberada para o próximo | turnaroundSeconds | ✓ | 1 |

### 3.3 Caixa e pagamento

| ID | Evento | Quando | Payload | Sync | Fase |
|---|---|---|---|:-:|:-:|
| EVT-030 | `cash.session.opened` | Caixa aberto | operatorId, openingAmount | ✓ | 1 |
| EVT-031 | `cash.movement.registered` | Sangria/suprimento | type, amount, reason, authorizedBy | ✓ | 1 |
| EVT-032 | `payment.registered` | Pagamento recebido | method, amount, provider, providerRef | ✓ | 1 |
| EVT-033 | `payment.refunded` | Estorno | amount, reason, authorizedBy | ✓ | 2 |
| EVT-034 | `discount.applied` | Desconto aplicado | amount, percent, reason, authorizedBy | ✓ | 1 |
| EVT-035 | `service_fee.waived` | Taxa de serviço retirada | amount, reason | ✓ | 1 |
| EVT-036 | `cash.session.closed` | Caixa fechado | expected, counted, divergence | ✓ | 1 |

### 3.4 Estoque

| ID | Evento | Quando | Payload | Sync | Fase |
|---|---|---|---|:-:|:-:|
| EVT-040 | `stock.deducted` | Baixa por produção | ingredientId, qty, orderItemId, cost | ✓ | 2 |
| EVT-041 | `stock.received` | Entrada de compra | purchaseId, items[], totalCost | ✓ | 2 |
| EVT-042 | `stock.wasted` | Perda registrada | ingredientId, qty, reason, cost | ✓ | 2 |
| EVT-043 | `stock.adjusted` | Ajuste manual | ingredientId, qty, reason, authorizedBy | ✓ | 2 |
| EVT-044 | `stock.counted` | Contagem cíclica | countId, items[], totalDivergenceCost | ✓ | 2 |
| EVT-045 | `stock.below_minimum` | Cruzou o mínimo | ingredientId, current, minimum | ✓ | 2 |
| EVT-046 | `stock.expiring_soon` | Validade próxima | ingredientId, lotId, expiresAt | ✓ | 2 |

### 3.5 Catálogo e configuração (origem: nuvem)

| ID | Evento | Quando | Sync | Fase |
|---|---|---|:-:|:-:|
| EVT-050 | `product.created` / `product.updated` | Cadastro alterado | ↓ | 1 |
| EVT-051 | `product.availability_changed` | Disponibilidade alterada | ↕ | 1 |
| EVT-052 | `price.changed` | Preço alterado | ↓ | 1 |
| EVT-053 | `recipe.updated` | Ficha técnica alterada | ↓ | 2 |
| EVT-054 | `tenant.config_updated` | Configuração/limiar alterado | ↓ | 1 |
| EVT-055 | `tenant.branding_updated` | Identidade visual alterada | ↓ | 1 |

> `↓` = nuvem → loja · `↑` = loja → nuvem · `↕` = bidirecional

### 3.6 Delivery

| ID | Evento | Quando | Sync | Fase |
|---|---|---|:-:|:-:|
| EVT-060 | `delivery.assigned` | Entrega atribuída | ✓ | 4 |
| EVT-061 | `delivery.run.started` | Rota iniciada | ✓ | 4 |
| EVT-062 | `delivery.stop.completed` | Parada concluída | ✓ | 4 |
| EVT-063 | `delivery.failed` | Ocorrência de entrega | ✓ | 4 |
| EVT-064 | `courier.arrived` | Entregador chegou à loja | ✓ | 4 |

### 3.7 Identidade e auditoria

| ID | Evento | Quando | Sync | Fase |
|---|---|---|:-:|:-:|
| EVT-070 | `user.authenticated` | Login realizado | ✓ | 1 |
| EVT-071 | `authorization.granted` | Ação sensível autorizada | ✓ | 1 |
| EVT-072 | `permission.changed` | Permissão alterada | ✓ | 1 |
| EVT-073 | `device.registered` | Terminal autorizado | ✓ | 1 |
| EVT-074 | `support.access.granted` | Replay acessou dados do tenant | ✓ | 1 |

### 3.8 Sistema e sincronização

| ID | Evento | Quando | Sync | Fase |
|---|---|---|:-:|:-:|
| EVT-080 | `sync.batch.sent` | Lote enviado | — | 1 |
| EVT-081 | `sync.delayed` | Atraso acima do limiar | ✓ | 1 |
| EVT-082 | `sync.conflict.detected` | Conflito registrado | ✓ | 1 |
| EVT-083 | `edge.offline_detected` | Perda de conexão | ✓ | 1 |
| EVT-084 | `edge.reconnected` | Conexão restabelecida | ✓ | 1 |

---

## 4. Máquinas de estado

### 4.1 Pedido (`order`)

```
        ┌────────┐
        │ DRAFT  │  criado, ainda editável (carrinho)
        └───┬────┘
            │ order.placed  [T0]
            ▼
        ┌────────┐  ─── order.cancelled ──► ┌───────────┐
        │ PLACED │                          │ CANCELLED │
        └───┬────┘  ◄─── (só antes de FIRED)└───────────┘
            │ primeiro item fired
            ▼
     ┌───────────────┐
     │ IN_PRODUCTION │
     └───────┬───────┘
             │ todos os itens ready
             ▼
        ┌────────┐
        │ READY  │
        └───┬────┘
            │
     ┌──────┴──────┐
     │ salão       │ delivery
     ▼             ▼
┌──────────┐  ┌────────────┐
│ DELIVERED│  │ DISPATCHED │
│ (mesa)   │  └─────┬──────┘
└────┬─────┘        │ delivered
     │              ▼
     │        ┌───────────┐
     │        │ DELIVERED │
     │        └─────┬─────┘
     └──────┬───────┘
            │ pagamento confirmado
            ▼
        ┌────────┐
        │ CLOSED │  terminal
        └────────┘
```

**Transições proibidas:**

| De | Para | Motivo |
|---|---|---|
| `IN_PRODUCTION` | `CANCELLED` | Só com autorização de perfil superior (RN-005) e gera perda de insumo (RN-008) |
| `READY` | `PLACED` | Não há retrocesso; correção usa `order.item.refired` |
| `CLOSED` | qualquer | Terminal; correção usa estorno |

### 4.2 Item do pedido (`order_item`) — onde nasce a métrica

```
   ┌────────┐
   │ QUEUED │ ── na fila da praça ──────────────── T0
   └───┬────┘
       │ order.item.fired                          T1
       ▼
   ┌────────┐
   │ FIRED  │ ── em montagem/preparo
   └───┬────┘
       │
   ┌───┴──────────────────────┐
   │ item de estação gargalo? │
   ├──── sim ────┐   └── não ─┼──────────────┐
   ▼             │            │              │
┌─────────┐      │            │              │
│ IN_OVEN │  T2  │            │              │
└────┬────┘      │            │              │
     │ oven_out  │ T3         │              │
     ▼           │            │              │
┌──────────────┐ │            │              │
│ OUT_OF_OVEN  │ │            │              │
└──────┬───────┘ │            │              │
       └─────────┴────────────┘              │
                 │ order.item.ready       T4 │
                 ▼                           │
            ┌────────┐                       │
            │ READY  │ ◄─────────────────────┘
            └───┬────┘
                │ order.item.served         T5
                ▼
           ┌─────────┐
           │ SERVED  │  terminal
           └─────────┘

  Qualquer estado ──► CANCELLED (com regra de autorização)
  READY/SERVED    ──► REFIRED → gera novo item em QUEUED
```

**Derivação direta das métricas:**

| Intervalo | Métrica | Código |
|---|---|---|
| T1 − T0 | Tempo de fila | MET-001 |
| T2 − T1 | Tempo de montagem | MET-002 |
| T3 − T2 | Tempo de cocção | MET-003 |
| T4 − T3 | Tempo de finalização | MET-004 |
| T5 − T4 | Tempo de expedição | MET-005 |
| T5 − T0 | **Tempo total** | MET-006 |
| T4 − T1 | Tempo de produção | MET-007 |

### 4.3 Sessão de mesa (`table_session`)

```
┌──────┐  bill_requested  ┌────────────────┐  payment ok  ┌──────┐  released  ┌────────┐
│ OPEN │ ───────────────► │ BILL_REQUESTED │ ───────────► │ PAID │ ─────────► │ CLOSED │
└──────┘                  └────────────────┘              └──────┘            └────────┘
    │                             │
    └──── novo pedido permitido ──┘  (volta a OPEN se cliente pedir mais)
```

**Regra:** `BILL_REQUESTED` → `PAID` é bloqueado se houver item pendente de entrega, salvo autorização registrada (RN-017).

### 4.4 Caixa (`cash_session`)

```
┌──────┐  fechamento iniciado  ┌─────────┐  conferência ok  ┌────────┐
│ OPEN │ ────────────────────► │ CLOSING │ ───────────────► │ CLOSED │
└──────┘                       └────┬────┘                  └────────┘
                                    │ divergência > limiar
                                    ▼
                          exige autorização + motivo
```

**Regra:** caixa não fecha com mesa aberta, salvo autorização registrada (RN-018).

### 4.5 Entrega (`delivery_stop`)

```
┌─────────┐ assigned ┌──────────┐ run started ┌────────────┐ delivered ┌───────────┐
│ PENDING │ ───────► │ ASSIGNED │ ──────────► │ IN_TRANSIT │ ────────► │ DELIVERED │
└─────────┘          └──────────┘             └─────┬──────┘           └───────────┘
                                                    │ ocorrência
                                                    ▼
                                              ┌──────────┐
                                              │  FAILED  │ → reagenda ou cancela
                                              └──────────┘
```

---

## 5. Reações a eventos (efeitos colaterais)

Tabela normativa: cada evento dispara efeitos determinados. **Isto é contrato, não sugestão.**

| Evento | Estado | Notificação | Métrica | Estoque | Financeiro |
|---|---|---|---|---|---|
| `order.placed` | order→PLACED, itens→QUEUED | mesa, garçom, cozinha, caixa | +1 pedido, T0 | — | — |
| `order.item.fired` | item→FIRED | mesa, garçom | tempo de fila | — | — |
| `order.item.oven_in` | item→IN_OVEN | — | ocupação do gargalo | — | — |
| `order.item.ready` | item→READY | garçom, mesa | tempo de produção | **baixa por ficha técnica** | — |
| `order.item.served` | item→SERVED | mesa | tempo de expedição | — | — |
| `order.item.cancelled` | item→CANCELLED | garçom, caixa | +1 cancelamento | **perda se já iniciado** | — |
| `order.item.unavailable_flagged` | produto→indisponível | **todos os canais** | +1 ruptura | — | — |
| `payment.registered` | payment criado | caixa, mesa | receita, ticket | — | **lançamento de receita** |
| `table.session.closed` | session→CLOSED | caixa | giro de mesa, permanência | — | — |
| `cash.session.closed` | session→CLOSED | gestor se divergente | divergência de caixa | — | conciliação |
| `stock.received` | saldo recalculado | — | custo médio atualizado | **entrada** | **despesa** |
| `stock.counted` | saldo ajustado | gestor se divergente | **CMV real, divergência** | ajuste | — |
| `sync.delayed` | — | gestor + plataforma | atraso de sync | — | — |

---

## 6. Derivação de métricas a partir de eventos

### 6.1 Pipeline

```
domain_event ──► Metric Worker ──► metric_hourly ──► metric_daily ──► Painel
     │                                                                  │
     └────────────── drill-down (RF-BI-11) ◄───────────────────────────┘
```

O worker consome eventos em ordem de `occurred_at` e atualiza agregados incrementalmente. Um job noturno **recalcula o dia anterior por completo** — isso corrige agregados afetados por eventos que chegaram atrasados pela sincronização.

### 6.2 Exemplos de derivação

```sql
-- MET-006: tempo total, p90, por hora e canal
SELECT
  date_trunc('hour', o.placed_at)                                       AS hour,
  o.channel,
  count(*)                                                              AS orders,
  percentile_cont(0.9) WITHIN GROUP (
    ORDER BY EXTRACT(EPOCH FROM (o.served_at - o.placed_at))
  )                                                                     AS p90_seconds,
  avg(EXTRACT(EPOCH FROM (o.served_at - o.placed_at)))                  AS avg_seconds
FROM "order" o
WHERE o.tenant_id = $1
  AND o.placed_at >= $2 AND o.placed_at < $3
  AND o.served_at IS NOT NULL
GROUP BY 1, 2;

-- MET-020: aderência ao prazo (OTD)
SELECT
  count(*) FILTER (WHERE served_at <= promised_at)::numeric / count(*) AS otd
FROM "order"
WHERE tenant_id = $1 AND placed_at >= $2 AND served_at IS NOT NULL;

-- MET-030: ociosidade do gargalo com fila (por minuto)
WITH minutes AS (
  SELECT generate_series($2::timestamptz, $3::timestamptz, '1 minute') AS m
)
SELECT count(*) AS idle_with_queue_minutes
FROM minutes
WHERE (SELECT count(*) FROM order_item i
        WHERE i.tenant_id = $1 AND i.status = 'IN_OVEN'
          AND i.oven_in_at <= m AND (i.oven_out_at IS NULL OR i.oven_out_at > m)
      ) < $4  -- slots configurados
  AND (SELECT count(*) FROM order_item i
        WHERE i.tenant_id = $1 AND i.status IN ('QUEUED','FIRED')
          AND i.placed_at <= m
      ) > 0;
```

### 6.3 Regra de ouro da métrica

> **Nenhuma métrica é digitada. Toda métrica é derivada.** Se um indicador exigir entrada manual de alguém, ele está mal desenhado — reveja qual evento operacional deveria tê-lo produzido.

---

## 7. Motor de regras de fluxo (Fase 2)

### 7.1 Fire time — sequenciamento reverso

```ts
function calculateFireTimes(items: OrderItem[]): Map<string, Date> {
  // o item mais longo define a saída sincronizada
  const longest = Math.max(...items.map(i => i.prepMinutes));
  const now = new Date();
  return new Map(
    items.map(i => [
      i.id,
      addMinutes(now, longest - i.prepMinutes)   // itens curtos começam depois
    ])
  );
}
```

O KDS exibe o item apenas quando `now >= fire_at`, ou o destaca como "aguardando momento de iniciar". Isso resolve a perda P3 (itens dessincronizados).

### 7.2 Prioridade dinâmica da fila

```
score = w1 · urgência        (prazo restante, invertido)
      + w2 · espera          (tempo desde T0)
      + w3 · sincronização   (outro item da mesma mesa já pronto)
      + w4 · canal           (delivery esfria em rota)
      − w5 · fire_time       (ainda não é hora de iniciar)
```

Pesos configuráveis por tenant. A ordem é **exibida com o motivo** e pode ser sobreposta pelo operador (RF-KDS-12) — sistema que reordena sem explicar perde a confiança da cozinha na primeira semana.

### 7.3 Prazo dinâmico

```
promessa = prep(item mais longo)
         + fila_projetada(itens à frente ÷ capacidade_atual)
         + expedição_média
         + rota(zona)              [delivery]
         + margem_segurança
```

Recalculado a cada `order.placed`; mudança relevante emite `order.promise_recalculated` e notifica o cliente.

---

## 8. Versionamento de eventos

| Situação | Como tratar |
|---|---|
| Campo novo opcional | Mantém `version`; consumidores ignoram o desconhecido |
| Campo obrigatório novo | `version + 1`; consumidor trata as duas versões por ao menos 2 releases |
| Semântica alterada | **Novo tipo de evento**, nunca reutilizar o nome |
| Evento descontinuado | Marcar como deprecated; parar de emitir; manter o consumidor até o arquivamento |

**Nunca** reprocessar o histórico alterando eventos gravados. Correção se faz com evento compensatório (`stock.adjusted`, `payment.refunded`).

---

## 9. Checklist de implementação de um evento novo

- [ ] Nome segue `<agregado>.<entidade>.<ação passada>`
- [ ] Schema definido em `packages/events` com Zod e versionado
- [ ] Emitido na mesma transação do estado (outbox)
- [ ] Decidido se sincroniza e em qual direção
- [ ] Reações mapeadas na tabela da seção 5
- [ ] Métricas afetadas identificadas
- [ ] Alertas disparados definidos
- [ ] Teste que garante emissão em toda transição
- [ ] Documentado neste catálogo com ID EVT-xxx

---

*Documento 04 do pacote 004_DonaBetinha. Replay Studio.*
