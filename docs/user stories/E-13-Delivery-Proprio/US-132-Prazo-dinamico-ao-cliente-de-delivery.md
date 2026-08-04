# US-132 · Prazo dinamico ao cliente de delivery

|  |  |
|---|---|
| **Épico** | [E-13 · Delivery Proprio](./README.md) |
| **Fase** | 4 — Delivery próprio |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Fase 4 |
| **Requisitos funcionais** | RF-DEL-03 |
| **Regras de negócio** | RN-013 |
| **ADRs** | ADR-012 |
| **Eventos** | EVT-017 |
| **Aplicações** | web-menu, api-cloud, api-edge |
| **Autoridade do dado** | Local (fila) + Nuvem (rota) |

---

## 1. História

> **Como** cliente de delivery (P6),
> **quero** saber quanto tempo vai demorar de verdade,
> **para** que eu decida se peço agora ou depois.

## 2. Contexto e motivação

A dor da persona P6 é registrada como *não sabe o prazo real*. Prazo fixo no delivery é ainda pior que no salão, porque soma a variabilidade da cozinha com a da rota.

Esta história estende o prazo dinâmico da US-118 com o componente de entrega: fila da cozinha, mais expedição, mais tempo de rota da zona, mais margem.

## 3. Escopo

### 3.1 Dentro desta história

- Prazo composto: produção, expedição, rota da zona e margem
- Exibição antes da confirmação
- Recalculo quando a fila muda de forma relevante
- Notificação ao cliente sobre mudança relevante
- Medição do prazo prometido contra o realizado
- Pausa automática do canal quando a fila excede o limite configurado

### 3.2 Fora desta história

- Rastreio em mapa
- Roteirização otimizada

## 4. Critérios de aceite

```gherkin
Funcionalidade: Prazo dinâmico no delivery

  Cenário: Prazo composto
    Dado fila de cozinha de 12 minutos e zona com 15 minutos de rota
    Quando o prazo for calculado
    Então deve somar produção, expedição, rota e margem

  Cenário: Prazo antes da confirmação
    Dado o cliente com o carrinho montado
    Quando estiver prestes a confirmar
    Então deve ver o prazo estimado da sua zona
    E o prazo deve refletir a fila daquele momento

  Cenário: Recalculo com mudança relevante
    Dado um pedido com prazo de 30 minutos
    Quando a fila crescer e o prazo passar a 45 minutos
    Então o cliente deve ser notificado da mudança

  Cenário: Pausa automática do canal
    Dado o limite de fila configurado
    Quando a fila ultrapassar o limite
    Então o canal deve pausar automaticamente
    E novos clientes devem ver aviso de indisponibilidade temporária

  Cenário: Medição contra a meta
    Dado a meta declarada de 25 minutos
    Quando os prazos realizados forem apurados
    Então o p90 deve ser comparado à meta

  Cenário: Prazo honesto no pico
    Dado o pico de sábado à noite
    Quando o prazo for calculado
    Então deve refletir a realidade, ainda que alto
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-013 | O prazo informado ao cliente é calculado pela fila atual, nunca fixo | **[HIPÓTESE]** — estendida com o componente de rota |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-017 | `order.promise_recalculated` | Prazo recalculado | oldPromise, newPromise, queueSize | ↑ |

## 7. Contrato de API

```http
GET /v1/public/delivery/estimate?zoneId=...&items=[...]
→ { "estimatedMinutes": 32,
    "breakdown": { "prepMinutes": 12, "queueMinutes": 6,
                   "dispatchMinutes": 3, "routeMinutes": 8,
                   "safetyMinutes": 3 },
    "queueSize": 8 }

PATCH /v1/tenant/config
{ "delivery": { "autoPauseQueueSize": 25,
                "promiseChangeNotifyMinutes": 8 } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `order` | Prazo prometido | `promised_at`, `original_promised_at` |
| `delivery_zone` | Tempo de rota | `additional_minutes` |
| `metric_hourly` | Tempos médios observados | `avg_dispatch_seconds`, `avg_delivery_seconds` |

## 9. Comportamento offline

O componente de fila vem do edge; o de rota, da configuração de zona. Com o edge offline, o canal pausa (US-130).

## 10. Interface e experiência

- Prazo em faixa ("30 a 40 minutos"), não em número exato — precisão falsa gera frustração
- Composição do prazo disponível ao gestor, não ao cliente
- Notificação de mudança apenas quando relevante
- Pausa do canal com mensagem que preserva a relação: "estamos com alta demanda, volte em instantes"

## 11. Métricas, alertas e observabilidade

- Tempo total de delivery (p90) contra a meta de 25 minutos
- Erro entre prometido e realizado
- Frequência e duração das pausas automáticas do canal
- Conversão em função do prazo exibido

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Composição do prazo com todos os componentes |
| Integração | Pausa automática no limite de fila |
| Integração | Notificação de mudança relevante |
| Validação | p90 comparado à meta de 25 minutos |

## 13. Dependências

**Depende de:** US-118, US-131  
**Habilita:** US-133

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

- **Pendência do PRD 7** — é preciso confirmar se a meta de 25 minutos é objetivo de negócio (o sistema mede e apoia) ou requisito do sistema (o sistema garante). São compromissos contratuais muito diferentes.

---

*US-132 · Épico E-13 · Pacote 004_DonaBetinha · Replay Studio.*