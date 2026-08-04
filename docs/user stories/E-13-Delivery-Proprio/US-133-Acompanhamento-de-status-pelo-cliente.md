# US-133 · Acompanhamento de status pelo cliente

|  |  |
|---|---|
| **Épico** | [E-13 · Delivery Proprio](./README.md) |
| **Fase** | 4 — Delivery próprio |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Fase 4 |
| **Requisitos funcionais** | RF-DEL-04 |
| **Regras de negócio** | RN-003 |
| **ADRs** | ADR-011 |
| **Eventos** | EVT-014, EVT-015 |
| **Aplicações** | web-menu, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** cliente de delivery (P6),
> **quero** acompanhar em que etapa está meu pedido,
> **para** que eu não precise ligar para a loja perguntando.

## 2. Contexto e motivação

Rastreio reduz ansiedade do cliente e ligações à loja — que é tempo de operação consumido sem gerar receita.

As etapas precisam ser as que o cliente entende, não os estados internos da máquina: recebido, em preparo, saiu para entrega, entregue.

## 3. Escopo

### 3.1 Dentro desta história

- Página de acompanhamento acessível por link, sem login
- Etapas em linguagem do cliente
- Prazo estimado atualizado
- Notificação por push nas mudanças de etapa
- Identificação do entregador na etapa de rota
- Confirmação de entrega

### 3.2 Fora desta história

- Rastreio em mapa com posição em tempo real
- Chat com o entregador
- Avaliação do pedido

## 4. Critérios de aceite

```gherkin
Funcionalidade: Acompanhamento do pedido

  Cenário: Etapas em linguagem do cliente
    Dado um pedido em produção
    Quando o cliente abrir o acompanhamento
    Então deve ver "em preparo", não o nome do estado interno

  Cenário: Notificação de mudança
    Dado um pedido que saiu para entrega
    Quando a mudança ocorrer
    Então o cliente deve receber notificação
    E a página deve atualizar sem recarregar

  Cenário: Identificação do entregador
    Dado um pedido em rota
    Quando o cliente acompanhar
    Então deve ver o nome do entregador

  Cenário: Acesso sem login
    Dado o link enviado na confirmação
    Quando o cliente acessar
    Então deve ver o acompanhamento sem precisar autenticar
    E o link não deve dar acesso a outros pedidos

  Cenário: Prazo atualizado
    Dado uma mudança relevante no prazo
    Quando o cliente estiver acompanhando
    Então o novo prazo deve ser exibido

  Cenário: Pedido entregue
    Dado a entrega concluída
    Quando o cliente acessar
    Então deve ver a confirmação e o horário da entrega
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-003 | Cada transição de estado gera alerta aos perfis envolvidos | O cliente é um dos perfis notificados |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-014 | `order.dispatched` | Saiu para entrega | courierId, runId | ↑ |
| EVT-015 | `order.delivered` | Entregue ao cliente | deliverySeconds, outcome | ↑ |

## 7. Contrato de API

```http
GET /v1/public/orders/{id}/status?token=<token do pedido>
→ { "status": "IN_PRODUCTION",
    "statusLabel": "Em preparo",
    "steps": [ { "key": "RECEIVED",   "label": "Pedido recebido",  "at": "..." },
               { "key": "PREPARING",  "label": "Em preparo",       "at": "..." },
               { "key": "DISPATCHED", "label": "Saiu para entrega","at": null },
               { "key": "DELIVERED",  "label": "Entregue",         "at": null } ],
    "promisedAt": "...", "estimatedMinutes": 18,
    "courier": null }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `order` | Estado e carimbos | `status`, `placed_at`, `dispatched_at`, `delivered_at` |
| `delivery_stop` | Etapa de entrega | `status`, `courier_id` |
| `courier` | Entregador | `name` |

## 9. Comportamento offline

Página de nuvem. O status depende da sincronização do edge; a defasagem é sinalizada como em toda visão.

## 10. Interface e experiência

- Etapas como linha do tempo vertical, simples e sem jargão
- Prazo em destaque, sempre atualizado
- Notificação em cada mudança de etapa, não em cada evento interno
- Link do acompanhamento enviado na confirmação e acessível sem login

## 11. Métricas, alertas e observabilidade

- Frequência de acesso ao acompanhamento
- Correlação entre uso do acompanhamento e ligações à loja
- Tempo em cada etapa, visto pelo cliente

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Atualização em tempo real das etapas |
| Integração | Token do pedido não dá acesso a outro pedido |
| Integração | Notificação em cada mudança de etapa |

## 13. Dependências

**Depende de:** US-130, US-132  
**Habilita:** US-136

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

—

---

*US-133 · Épico E-13 · Pacote 004_DonaBetinha · Replay Studio.*