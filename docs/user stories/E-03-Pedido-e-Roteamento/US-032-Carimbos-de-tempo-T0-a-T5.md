# US-032 · Carimbos de tempo T0 a T5

|  |  |
|---|---|
| **Épico** | [E-03 · Pedido e Roteamento](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 1 |
| **Requisitos funcionais** | RF-PED-02, RF-PED-03 |
| **Regras de negócio** | RN-004, RN-020 |
| **ADRs** | ADR-006, ADR-018, ADR-034 |
| **Eventos** | EVT-002, EVT-005, EVT-008, EVT-009 |
| **Aplicações** | api-edge, packages/domain, packages/db |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** que cada etapa do pedido tenha carimbo de tempo, autor e dispositivo,
> **para** que eu saiba, de verdade, quantos minutos leva cada parte do processo.

## 2. Contexto e motivação

Responde à declaração mais direta da descoberta: *"eu vou saber quantos minutos a minha pizza tá sendo feita"* e *"não sei quais etapas hoje são mais rápidas e mais lentas"*.

A decisão de modelagem (ERD, decisão 5) é gravar **seis carimbos em `order_item`**, não um tempo total. O motivo: cada intervalo é um diagnóstico diferente, e a média única esconde o gargalo. Um pedido que leva 25 minutos porque ficou 18 na fila é um problema completamente distinto de um que leva 25 porque a cocção demorou.

Uma constraint de banco (`ck_item_sequence`) impede que os carimbos fiquem fora de ordem — duração negativa corromperia todo indicador derivado.

## 3. Escopo

### 3.1 Dentro desta história

- Seis colunas de carimbo em `order_item`: `placed_at`, `fired_at`, `oven_in_at`, `oven_out_at`, `ready_at`, `served_at`
- Autor e dispositivo por transição
- `occurred_at` distinto de `recorded_at` em todo evento
- Constraint garantindo ordem cronológica dos carimbos
- `business_day` materializado, conforme ADR-018
- Funções de derivação das métricas MET-001 a MET-007
- Tratamento de relógio dessincronizado no dispositivo (ADR-034)

### 3.2 Fora desta história

- Cálculo de agregados (E-07)
- Exibição no painel (US-071)
- Cronômetro visual do KDS (US-040)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Carimbos de tempo do ciclo do pedido

  Cenário: Registro completo do ciclo
    Dado um item que percorreu todo o fluxo
    Quando consultar o item
    Então devem existir placedAt, firedAt, readyAt e servedAt
    E cada carimbo deve ter autor e dispositivo registrados
    E o horário gravado deve ser o de ocorrência, não o de sincronização

  Cenário: Item que passa pelo gargalo
    Dado um item roteado para a praça marcada como gargalo
    Quando percorrer o fluxo completo
    Então devem existir também ovenInAt e ovenOutAt
    E o tempo de cocção deve ser calculável

  Cenário: Item que não passa pelo gargalo
    Dado um refrigerante na praça Bebidas
    Quando for concluído
    Então ovenInAt e ovenOutAt devem permanecer nulos
    E o tempo total deve ser calculado normalmente

  Cenário: Ordem cronológica garantida
    Dado uma tentativa de gravar readyAt anterior a firedAt
    Quando a operação for executada
    Então o banco deve recusar pela constraint ck_item_sequence

  Cenário: Relógio do dispositivo adiantado
    Dado um dispositivo com relógio 4 minutos à frente do servidor
    Quando enviar X-Occurred-At
    Então o servidor deve corrigir pela diferença conhecida do dispositivo
    E deve registrar o desvio para diagnóstico

  Cenário: Métrica com sincronização atrasada
    Dado um pedido feito às 20h03 offline e sincronizado às 21h15
    Quando o relatório por faixa horária for gerado
    Então o pedido deve ser contabilizado às 20h

  Cenário: Dia operacional que vira depois da meia-noite
    Dado o dia operacional configurado para virar às 5h
    E um pedido feito às 00h40
    Quando o fechamento do dia for apurado
    Então o pedido deve pertencer ao dia operacional anterior
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-004 | Toda ação registra autor, horário e dispositivo | Cada carimbo tem autor e dispositivo associados |
| RN-020 | Métrica de horário usa sempre `ocorrido_em`, nunca o horário de sincronização | `occurred_at` é a única fonte de toda métrica temporal |
| RN-002 | A cozinha registra obrigatoriamente início e conclusão de cada item | T1 e T4 são obrigatórios no ciclo |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-002 | `order.placed` | **T0** — pedido confirmado | items[], total, promisedAt | ↑ |
| EVT-005 | `order.item.fired` | **T1** — produção iniciada | stationId, operatorId | ↑ |
| EVT-006 | `order.item.oven_in` | **T2** — entrou no gargalo | slotIndex | ↑ |
| EVT-007 | `order.item.oven_out` | **T3** — saiu do gargalo | cookSeconds | ↑ |
| EVT-008 | `order.item.ready` | **T4** — pronto | prepSeconds | ↑ |
| EVT-009 | `order.item.served` | **T5** — entregue à mesa | waiterId | ↑ |

> Regra inviolável R2 do documento 04: `occurredAt` é o horário do fato e toda métrica de tempo usa este campo.

## 7. Contrato de API

```http
GET /v1/orders/{id}/items/{itemId}/timeline
→ { "orderItemId": "...",
    "timestamps": {
      "placedAt":  { "at": "...", "actor": {...}, "device": {...} },
      "firedAt":   { "at": "...", "actor": {...}, "device": {...} },
      "ovenInAt":  { "at": "...", "actor": {...} },
      "ovenOutAt": { "at": "...", "actor": {...} },
      "readyAt":   { "at": "...", "actor": {...} },
      "servedAt":  { "at": "...", "actor": {...} }
    },
    "durations": {
      "queueSeconds": 214, "assemblySeconds": 96, "cookSeconds": 420,
      "finishSeconds": 30, "serveSeconds": 88,
      "prepSeconds": 546, "totalSeconds": 848
    } }
```

> Este é o endpoint que sustenta o drill-down do painel (RF-BI-11): do número ao pedido individual em no máximo três toques.

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `order_item` | Os seis carimbos e o dia operacional | `placed_at`, `fired_at`, `oven_in_at`, `oven_out_at`, `ready_at`, `served_at`, `business_day` |
| `order_item` | Autoria por transição | `fired_by`, `ready_by`, `served_by`, `fired_device_id`, … |
| `domain_event` | Histórico completo, particionado por `occurred_at` | `occurred_at`, `recorded_at`, `actor_id`, `device_id`, `device_seq` |
| `order` | Carimbos agregados do pedido | `placed_at`, `ready_at`, `served_at`, `promised_at` |

> Derivação direta (doc. 04, 4.2): T1−T0 fila (MET-001), T2−T1 montagem (MET-002), T3−T2 cocção (MET-003), T4−T3 finalização (MET-004), T5−T4 expedição (MET-005), T5−T0 total (MET-006), T4−T1 produção (MET-007).

## 9. Comportamento offline

**É a história que garante que a métrica sobreviva ao offline.** Sem a separação entre `occurred_at` e `recorded_at`, todo pedido feito durante uma queda de internet apareceria no relatório no horário em que sincronizou — e o mapa de calor do pico ficaria completamente errado.

O `X-Occurred-At` vem do dispositivo. Como relógio de dispositivo desvia, o ADR-034 define a correção: o edge mantém o desvio conhecido de cada dispositivo (medido no handshake) e ajusta o horário recebido, registrando o desvio aplicado para diagnóstico.

O `business_day` é materializado na gravação, não calculado em consulta (ADR-018) — consulta por período não pode depender de função em tempo de execução.

## 10. Interface e experiência

- Sem interface própria — é infraestrutura de medição
- O efeito visível é a linha do tempo do pedido no drill-down do painel, mostrando cada etapa com autor

## 11. Métricas, alertas e observabilidade

- MET-001 a MET-007 — toda a família de métricas de tempo do produto
- Percentual de itens com ciclo completo de carimbos — abaixo de 100% indica transição não instrumentada
- Desvio médio de relógio por dispositivo
- Contagem de correções de horário aplicadas

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo dos sete intervalos, incluindo itens que não passam pelo gargalo |
| Unitário | Cálculo do `business_day` com virada configurável |
| Integração | Constraint `ck_item_sequence` recusa carimbos fora de ordem |
| Integração | `occurred_at` preservado após sincronização de 6 horas de atraso |
| Integração | Correção de relógio adiantado e atrasado |
| Propriedade | Para qualquer ciclo válido, nenhuma duração calculada é negativa |
| Regressão | Toda transição de estado emite seu evento — teste que falha se alguma faltar |

## 13. Dependências

**Depende de:** US-001  
**Habilita:** US-030, US-040, US-071, US-076

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
- [ ] Teste automatizado garantindo que nenhuma transição de estado ocorre sem emitir evento

## 15. Riscos, premissas e pendências

- Métrica sem qualidade de dado é pior que métrica nenhuma (risco 10 da Visão Geral). Um carimbo faltando invalida silenciosamente o indicador — daí o teste de regressão obrigatório.
- Dispositivo com relógio muito dessincronizado (horas, não minutos) pode produzir dado inconsistente; definir limiar de rejeição e alertar.

---

*US-032 · Épico E-03 · Pacote 004_DonaBetinha · Replay Studio.*