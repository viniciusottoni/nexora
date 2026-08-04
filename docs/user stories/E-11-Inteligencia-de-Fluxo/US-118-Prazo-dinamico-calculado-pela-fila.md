# US-118 · Prazo dinamico calculado pela fila

|  |  |
|---|---|
| **Épico** | [E-11 · Inteligencia de Fluxo](./README.md) |
| **Fase** | 2 — Custo e controle |
| **Prioridade** | S — Should have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Fase 2 |
| **Requisitos funcionais** | RF-PED-07 |
| **Regras de negócio** | RN-013 |
| **ADRs** | ADR-012 |
| **Eventos** | EVT-017 |
| **Aplicações** | api-edge, web-menu, web-pos, packages/domain |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** cliente do salão (P1) e cliente de delivery (P6),
> **quero** receber um prazo que corresponda à realidade da cozinha naquele momento,
> **para** que eu não seja enganado por uma estimativa fixa.

## 2. Contexto e motivação

Prazo fixo é promessa que a operação não controla: quinze minutos às 18h é factível; às 21h de sábado, não. A RN-013 estabelece que *o prazo informado ao cliente é calculado pela fila atual, nunca fixo*.

A fórmula está no documento 04, seção 7.3: preparo do item mais longo, mais fila projetada dividida pela capacidade atual, mais expedição média, mais rota (no delivery), mais margem de segurança.

Quando o prazo muda de forma relevante, o cliente é notificado — silêncio sobre atraso é pior que o atraso.

## 3. Escopo

### 3.1 Dentro desta história

- Cálculo do prazo a partir da fila, da capacidade do gargalo e do tempo de expedição
- Margem de segurança configurável
- Recalculo a cada novo pedido confirmado
- Notificação ao cliente quando a mudança for relevante
- Exibição do prazo antes da confirmação do pedido
- Medição de aderência do prazo prometido contra o realizado

### 3.2 Fora desta história

- Rota e prazo de entrega do delivery (US-132, Fase 4)
- Pausa automática do canal por fila excessiva (RF-DEL-11, Fase 4)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Prazo dinâmico

  Cenário: Prazo pela fila atual
    Dado uma fila de 8 itens e capacidade de 5 slots
    Quando um novo pedido for confirmado
    Então o prazo deve considerar a fila projetada, não apenas o preparo do item

  Cenário: Prazo em momento tranquilo
    Dado a fila vazia
    Quando um pedido de pizza de 12 minutos for confirmado
    Então o prazo deve ser próximo de 12 minutos mais expedição e margem

  Cenário: Prazo no pico
    Dado a fila com 20 itens
    Quando o mesmo pedido for confirmado
    Então o prazo deve ser significativamente maior
    E o cliente deve ver o prazo antes de confirmar

  Cenário: Recalculo com mudança relevante
    Dado um pedido com prazo de 15 minutos
    Quando a fila crescer a ponto de o prazo passar a 25 minutos
    Então o evento order.promise_recalculated deve ser emitido
    E o cliente deve ser notificado da mudança

  Cenário: Mudança irrelevante não notifica
    Dado uma variação de prazo de 1 minuto
    Quando o recálculo ocorrer
    Então o cliente não deve ser notificado

  Cenário: Margem de segurança
    Dado a margem configurada em 15%
    Quando o prazo for calculado
    Então deve incluir a margem
    E o OTD deve refletir o prazo com margem

  Cenário: Prazo exibido antes de confirmar
    Dado o cliente montando o pedido
    Quando estiver prestes a confirmar
    Então deve ver o prazo estimado
    E o prazo deve refletir a fila daquele momento
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-013 | O prazo informado ao cliente é calculado pela fila atual, nunca fixo | **[HIPÓTESE]** — é o objeto desta história |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-017 | `order.promise_recalculated` | Prazo recalculado | oldPromise, newPromise, queueSize | ↑ |

## 7. Contrato de API

```http
# Retornado na criação do pedido:
POST /v1/orders
→ 201 { "order": {...}, "promisedAt": "...", "estimatedMinutes": 18,
        "promiseBreakdown": { "prepMinutes": 12, "queueMinutes": 4,
                              "serveMinutes": 1, "safetyMinutes": 1 } }

# Consulta antes de confirmar:
GET /v1/public/menu/estimate?items=[...]
→ { "estimatedMinutes": 18, "queueSize": 8 }

# WebSocket, quando muda de forma relevante:
{ "type": "order.promise_recalculated",
  "data": { "orderId": "...", "oldPromise": "...", "newPromise": "..." } }

PATCH /v1/tenant/config
{ "kitchen": { "promiseSafetyPercent": 15,
               "promiseChangeNotifyMinutes": 5 } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `order` | Prazo prometido e histórico | `promised_at`, `original_promised_at`, `promise_recalculated_count` |
| `station` | Capacidade para projeção da fila | `capacity_slots` |
| `metric_hourly` | Tempo de expedição médio | `avg_serve_seconds` |
| `tenant_config` | Margem e limiar de notificação | `kitchen.promiseSafetyPercent` |

> Fórmula (doc. 04, 7.3): promessa = preparo do item mais longo + fila projetada ÷ capacidade + expedição média + rota + margem.

## 9. Comportamento offline

Cálculo integralmente local, usando fila e capacidade do próprio edge.

## 10. Interface e experiência

- Prazo exibido antes da confirmação, nunca só depois — o cliente decide com a informação
- Composição do prazo disponível ao gestor, para entender por que ficou alto
- Notificação de mudança apenas quando relevante; notificar por um minuto vira ruído
- Linguagem de faixa ("cerca de 20 minutos"), não de precisão falsa ("18 minutos")

## 11. Métricas, alertas e observabilidade

- OTD com prazo dinâmico contra prazo fixo — a validação da hipótese
- Distribuição de prazos prometidos por faixa horária
- Frequência de recálculo relevante
- Erro médio entre prometido e realizado

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo do prazo com fila vazia, moderada e cheia |
| Unitário | Aplicação da margem de segurança |
| Integração | Recalculo notifica apenas em mudança relevante |
| Integração | Prazo exibido antes da confirmação reflete a fila do momento |
| Validação | OTD comparado antes e depois da ativação |

## 13. Dependências

**Depende de:** US-016, US-072, US-117  
**Habilita:** US-132

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

- **RN-013 é hipótese.** Prazo dinâmico alto no pico pode afastar o cliente — que é informação honesta, mas tem custo comercial. Decisão do gestor, não do sistema.
- Margem de segurança alta melhora o OTD artificialmente. Medir o erro entre prometido e realizado, não só o OTD.

---

*US-118 · Épico E-11 · Pacote 004_DonaBetinha · Replay Studio.*