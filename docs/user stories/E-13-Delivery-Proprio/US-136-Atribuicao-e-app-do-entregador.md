# US-136 · Atribuicao e app do entregador

|  |  |
|---|---|
| **Épico** | [E-13 · Delivery Proprio](./README.md) |
| **Fase** | 4 — Delivery próprio |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Fase 4 |
| **Requisitos funcionais** | RF-DEL-06, RF-DEL-07, RF-DEL-10 |
| **Regras de negócio** | RN-004 |
| **ADRs** | ADR-009 |
| **Eventos** | EVT-060, EVT-061, EVT-062, EVT-063, EVT-064 |
| **Aplicações** | web-pos, api-edge, api-cloud |
| **Autoridade do dado** | Local (despacho) → sincronizado |

---

## 1. História

> **Como** entregador (P5),
> **quero** ver minhas entregas e registrar saída e conclusão pelo celular,
> **para** que eu não precise voltar à loja para avisar que entreguei.

## 2. Contexto e motivação

A dor da persona P5 é registrada como *espera na loja sem saber quanto falta*. O app do entregador resolve os dois lados: dá visibilidade ao entregador e captura os carimbos de tempo que faltam para medir a meta de 25 minutos ponta a ponta.

O desenho depende de uma pendência importante: **entregadores próprios ou terceirizados** ainda não foi definido (Visão Geral 6.2).

## 3. Escopo

### 3.1 Dentro desta história

- Cadastro de entregadores
- Atribuição de entrega, manual ou por sugestão
- PWA do entregador com lista de entregas
- Registro de saída e de conclusão
- Registro de ocorrência com motivo
- Registro de chegada do entregador à loja
- Endereço com link para aplicativo de mapa

### 3.2 Fora desta história

- Roteirização otimizada
- Rastreamento por GPS em tempo real
- Agrupamento de entregas (US-138)
- Pagamento de entregador (fora do escopo)

## 4. Critérios de aceite

```gherkin
Funcionalidade: App do entregador

  Cenário: Atribuição de entrega
    Dado um pedido pronto para despacho
    Quando for atribuído a um entregador
    Então ele deve ver a entrega no celular
    E o cliente deve passar a ver o nome dele no acompanhamento

  Cenário: Registro de saída
    Dado uma entrega atribuída
    Quando o entregador registrar a saída
    Então o pedido deve ir para DISPATCHED
    E o carimbo de despacho deve ser gravado

  Cenário: Registro de conclusão
    Dado uma entrega em rota
    Quando o entregador registrar a conclusão
    Então o pedido deve ir para DELIVERED
    E o tempo total deve ser calculado

  Cenário: Ocorrência de entrega
    Dado um cliente ausente
    Quando o entregador registrar a ocorrência com motivo
    Então a entrega deve ir para FAILED
    E a loja deve ser alertada imediatamente

  Cenário: Chegada à loja
    Dado um entregador retornando
    Quando registrar a chegada
    Então a loja deve saber que ele está disponível

  Cenário: Endereço com mapa
    Dado uma entrega atribuída
    Quando o entregador tocar no endereço
    Então deve abrir o aplicativo de mapa do celular

  Cenário: Registro com sinal fraco
    Dado o entregador em área sem sinal
    Quando registrar a conclusão
    Então a ação deve ficar em fila local
    E deve ser enviada ao recuperar o sinal, com o horário real
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-004 | Toda ação registra autor, horário e dispositivo | Entregador identificado em cada registro |
| RN-020 | Métrica usa `ocorrido_em` | Registro feito sem sinal preserva o horário real |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-060 | `delivery.assigned` | Entrega atribuída | courierId, orderId | ↑ |
| EVT-061 | `delivery.run.started` | Rota iniciada | courierId, stops[] | ↑ |
| EVT-062 | `delivery.stop.completed` | Parada concluída | deliverySeconds | ↑ |
| EVT-063 | `delivery.failed` | Ocorrência de entrega | reason | ↑ |
| EVT-064 | `courier.arrived` | Entregador chegou à loja | courierId | ↑ |
| EVT-014 | `order.dispatched` | Saiu para entrega | courierId, runId | ↑ |
| EVT-015 | `order.delivered` | Entregue ao cliente | deliverySeconds, outcome | ↑ |

## 7. Contrato de API

```http
POST /v1/couriers            { "name": "...", "phone": "...", "type": "OWN" }
POST /v1/delivery/assign     { "orderId": "...", "courierId": "..." }

GET  /v1/courier/me/stops
→ { "stops": [ { "orderId": "...", "code": "A47",
                 "address": {...}, "customer": { "name","phone" },
                 "paymentMethod": "ON_DELIVERY", "amountToCollect": 5800,
                 "status": "ASSIGNED" } ] }

POST /v1/courier/runs/start
POST /v1/courier/stops/{id}/complete   { "outcome": "DELIVERED" }
POST /v1/courier/stops/{id}/fail       { "reason": "CUSTOMER_ABSENT" }
POST /v1/courier/arrived
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `courier` | Entregador | `name`, `phone`, `type`, `is_active` |
| `delivery_run` | Rota | `courier_id`, `started_at`, `ended_at` |
| `delivery_stop` | Parada | `order_id`, `status`, `assigned_at`, `completed_at`, `fail_reason` |
| `order` | Carimbos de entrega | `dispatched_at`, `delivered_at` |

## 9. Comportamento offline

O PWA do entregador precisa funcionar com sinal instável — é a condição normal de quem está na rua.

Ações são enfileiradas localmente (IndexedDB) e enviadas ao recuperar o sinal, com o horário real preservado pelo `X-Occurred-At`. Sem isso, o tempo de entrega ficaria errado sempre que houvesse área de sombra.

## 10. Interface e experiência

- Lista de entregas em ordem sugerida, com endereço e valor a receber em destaque
- Botões grandes: o entregador está de capacete, com pressa, muitas vezes de moto parada
- Endereço tocável, abrindo o mapa do celular
- Registro de conclusão em um toque, com confirmação
- Indicação discreta de ação pendente de envio quando sem sinal

## 11. Métricas, alertas e observabilidade

- Tempo de despacho (pronto até saída) — gargalo frequente e invisível
- Tempo de rota por zona e por entregador
- **Tempo total de delivery (p90) contra a meta de 25 minutos**
- Entregas por entregador e taxa de ocorrência

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Ciclo completo: atribuição, saída, conclusão |
| Integração | Ocorrência alerta a loja imediatamente |
| Integração | Registro sem sinal enfileirado e enviado com horário real |
| Usabilidade | Operação com uma das mãos, em teste de campo |

## 13. Dependências

**Depende de:** US-131, US-133  
**Habilita:** US-137, US-138

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

- **Pendência da Visão Geral 6.2** — não foi definido se os entregadores serão próprios ou terceirizados. A resposta muda o cadastro, o vínculo e o modelo de pagamento.
- Entregador terceirizado usando o próprio celular exige cuidado com dados pessoais do cliente (LGPD).

---

*US-136 · Épico E-13 · Pacote 004_DonaBetinha · Replay Studio.*