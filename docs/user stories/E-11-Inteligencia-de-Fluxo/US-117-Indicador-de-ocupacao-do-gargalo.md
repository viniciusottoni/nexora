# US-117 · Indicador de ocupacao do gargalo

|  |  |
|---|---|
| **Épico** | [E-11 · Inteligencia de Fluxo](./README.md) |
| **Fase** | 2 — Custo e controle |
| **Prioridade** | S — Should have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Fase 2 |
| **Requisitos funcionais** | RF-KDS-08 |
| **Regras de negócio** | — |
| **ADRs** | ADR-012 |
| **Eventos** | EVT-006, EVT-007 |
| **Aplicações** | web-kds, web-admin, api-edge, api-cloud |
| **Autoridade do dado** | Local (medição) → consolidada na nuvem |

---

## 1. História

> **Como** pizzaiolo (P3) e gestor (P8),
> **quero** ver quantas posições do forno estão ocupadas e quantas estão livres,
> **para** que eu não deixe o forno vazio enquanto tem fila esperando.

## 2. Contexto e motivação

O forno é o recurso-gargalo da pizzaria: é ele que determina a capacidade real de produção. Duas informações importam.

Para a **cozinha**, em tempo real: quantos slots estão ocupados agora. Forno com posição livre e fila esperando é capacidade jogada fora.

Para o **gestor**, em histórico: quantos minutos por dia o gargalo ficou ocioso **enquanto havia fila**. Esse número (MET-030, especificado no doc. 04, seção 6.2) é um dos mais reveladores do produto — mostra capacidade desperdiçada por problema de fluxo, não de demanda.

## 3. Escopo

### 3.1 Dentro desta história

- Indicador de slots ocupados e livres no KDS
- Alerta visual quando há slot livre com fila esperando
- Métrica de ociosidade com fila, por minuto
- Ocupação média do gargalo por faixa horária
- Capacidade configurável por praça
- Estimativa de capacidade máxima do turno

### 3.2 Fora desta história

- Sugestão automática do que colocar no forno
- Prioridade dinâmica (US-116)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Ocupação do gargalo

  Cenário: Slots ocupados em tempo real
    Dado o forno com capacidade de 5 slots
    E 3 itens em estado IN_OVEN
    Quando o KDS for exibido
    Então deve indicar 3 de 5 ocupados

  Cenário: Slot livre com fila
    Dado o forno com 2 slots livres
    E 8 itens aguardando na fila
    Quando o KDS for exibido
    Então deve haver alerta visual de capacidade ociosa

  Cenário: Ociosidade com fila medida
    Dado um turno com períodos de forno parcialmente vazio e fila presente
    Quando a métrica for apurada
    Então deve informar quantos minutos houve slot livre com fila esperando

  Cenário: Ocupação por faixa horária
    Dado o histórico do turno
    Quando a ocupação por hora for exibida
    Então deve mostrar o percentual médio de ocupação em cada faixa

  Cenário: Forno cheio
    Dado todos os slots ocupados
    Quando um novo item chegar
    Então deve indicar que o gargalo está cheio
    E deve estimar quando o próximo slot ficará livre

  Cenário: Praça sem gargalo
    Dado uma praça não marcada como gargalo
    Quando o KDS dessa praça for exibido
    Então nenhum indicador de slots deve aparecer
```

## 5. Regras de negócio aplicáveis

_Não se aplica a esta história._

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-006 | `order.item.oven_in` | **T2** — entrou no gargalo | slotIndex | ↑ |
| EVT-007 | `order.item.oven_out` | **T3** — saiu do gargalo | cookSeconds | ↑ |

> Reação normativa: `order.item.oven_in` → item em IN_OVEN, métrica de ocupação do gargalo.

## 7. Contrato de API

```http
GET /v1/kds/queue?stationId=<forno>
→ { "items": [...],
    "bottleneck": { "slotsTotal": 5, "slotsUsed": 3, "slotsFree": 2,
                    "queueSize": 8, "idleWithQueue": true,
                    "nextSlotFreeInSeconds": 180 } }

GET /v1/metrics/bottleneck?from=...&to=...&groupBy=hour
→ { "series": [ { "hour": "20:00", "avgOccupancy": 0.82,
                  "idleWithQueueMinutes": 7,
                  "maxThroughput": 24, "actualThroughput": 19 } ] }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `station` | Capacidade do gargalo | `capacity_slots`, `is_bottleneck` |
| `order_item` | Ocupação atual e histórico | `status=IN_OVEN`, `oven_in_at`, `oven_out_at`, `oven_slot` |
| `metric_hourly` | Ociosidade agregada | `oven_idle_with_queue_seconds`, `avg_oven_occupancy` |

> MET-030 (doc. 04, 6.2): conta os minutos em que a quantidade de itens `IN_OVEN` é menor que os slots configurados **e** existe fila aguardando.

## 9. Comportamento offline

Medição integralmente local; agregação consolidada na nuvem.

## 10. Interface e experiência

- Slots como blocos visuais no cabeçalho do KDS — legíveis de relance, sem leitura de número
- Alerta de capacidade ociosa discreto porém visível, sem som
- Para o gestor, a ociosidade com fila em minutos por dia, com valor estimado da capacidade perdida
- Estimativa de próximo slot livre em segundos, para o operador se organizar

## 11. Métricas, alertas e observabilidade

- MET-030 — ociosidade do gargalo com fila
- Ocupação média por faixa horária
- Vazão real contra vazão máxima teórica
- Capacidade perdida estimada em unidades e em faturamento

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo de ociosidade com fila, por minuto |
| Integração | Contagem de slots reflete os itens em IN_OVEN |
| Integração | Praça sem gargalo não exibe indicador |
| Validação | Ociosidade medida conferida com observação presencial da cozinha |

## 13. Dependências

**Depende de:** US-017, US-041  
**Habilita:** US-116, US-118

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

## 15. Riscos, premissas e pendências

- Exige que a cozinha marque `oven_in` e `oven_out` de forma disciplinada. Se a marcação for pulada, a métrica não existe — o KDS precisa tornar essas transições tão fáceis quanto as demais.

---

*US-117 · Épico E-11 · Pacote 004_DonaBetinha · Replay Studio.*