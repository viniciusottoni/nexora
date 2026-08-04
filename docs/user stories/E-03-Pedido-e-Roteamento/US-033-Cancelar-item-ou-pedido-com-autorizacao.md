# US-033 · Cancelar item ou pedido com autorizacao

|  |  |
|---|---|
| **Épico** | [E-03 · Pedido e Roteamento](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 2 |
| **Requisitos funcionais** | RF-PED-04, RF-PED-05 |
| **Regras de negócio** | RN-008, RN-011 |
| **ADRs** | ADR-023, ADR-021 |
| **Eventos** | EVT-010, EVT-016, EVT-071 |
| **Aplicações** | web-pos, web-kds, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** garçom (P2), caixa (P4) e gestor (P8),
> **quero** cancelar um item ou um pedido inteiro, com motivo e autorização quando necessário,
> **para** que o erro seja corrigido sem virar buraco no controle nem prejuízo invisível.

## 2. Contexto e motivação

Cancelamento é o ponto em que operação, autorização, auditoria e estoque se encontram. É também um dos indicadores de gestão mais reveladores: cancelamentos acima do padrão apontam problema de processo, de treinamento ou de desvio.

A regra estruturante é a RN-008: **item cancelado após o início da produção não estorna insumo — gera registro de perda**. Estornar insumo de uma pizza que já foi para o forno inventaria estoque que não existe, e o CMV da Fase 2 nasceria errado.

A máquina de estados do documento 04 lista `IN_PRODUCTION → CANCELLED` como transição que exige autorização de perfil superior.

## 3. Escopo

### 3.1 Dentro desta história

- Cancelamento de item individual e de pedido inteiro
- Motivo obrigatório, escolhido de lista configurável
- Autorização de perfil superior quando o item já foi iniciado (`X-Authorization-Token`)
- Registro de perda quando houver produção iniciada (preparado para a Fase 2)
- Registro completo em `audit_log`: quem executou, quem autorizou, valores antes e depois
- Remoção do item da fila do KDS e do consumo da mesa
- Alerta a garçom, caixa e cozinha

### 3.2 Fora desta história

- Estorno de pagamento (RF-CXA-13, Fase 2)
- Baixa e registro de perda em estoque propriamente ditos (US-105, Fase 2)
- Refazimento de item — `re-fire` (RF-KDS-11, Fase 2)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Cancelamento com autorização

  Cenário: Cancelamento antes do início da produção
    Dado um item em estado QUEUED
    Quando o garçom solicitar o cancelamento com motivo
    Então o item deve ser cancelado sem exigir autorização superior
    E deve sumir da fila do KDS
    E não deve compor o total da mesa

  Cenário: Cancelamento após início de produção
    Dado um item em estado FIRED
    Quando o garçom solicitar o cancelamento
    Então deve ser exigida autorização de perfil superior
    E, autorizado, o item deve ser cancelado com motivo obrigatório
    E o insumo consumido deve gerar registro de perda

  Cenário: Autorização negada
    Dado um pedido de cancelamento de item já iniciado
    Quando o PIN informado não tiver a permissão necessária
    Então o cancelamento deve ser recusado com 403
    E a tentativa deve ser registrada em audit_log

  Cenário: Cancelamento de pedido inteiro
    Dado um pedido com três itens, sendo um já iniciado
    Quando o cancelamento do pedido for solicitado
    Então deve ser exigida autorização pelo item iniciado
    E todos os itens devem ser cancelados na mesma operação
    E o pedido deve ir para CANCELLED

  Cenário: Pedido fechado não cancela
    Dado um pedido em estado CLOSED
    Quando alguém tentar cancelá-lo
    Então deve receber 409
    E a orientação deve apontar o fluxo de estorno

  Cenário: Registro completo na auditoria
    Dado um cancelamento autorizado
    Quando a ação for concluída
    Então o log deve conter executor, autorizador, horário, dispositivo,
         motivo e o valor do item cancelado

  Cenário: Cancelamento offline
    Dado que a loja está sem internet
    Quando um cancelamento com autorização for executado
    Então o PIN do autorizador deve ser validado localmente
    E a operação deve concluir normalmente
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-008 | Item cancelado após início da produção não estorna insumo; gera registro de perda | **[HIPÓTESE]** — `wasStarted` no payload determina o tratamento de estoque |
| RN-011 | Ação sensível exige autorização de perfil superior | Autorização por `X-Authorization-Token` |
| RN-004 | Toda ação registra autor, horário e dispositivo | Executor e autorizador registrados separadamente |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-010 | `order.item.cancelled` | Item cancelado | reason, authorizedBy, wasStarted | ↑ |
| EVT-016 | `order.cancelled` | Pedido cancelado | reason, authorizedBy, stage | ↑ |
| EVT-071 | `authorization.granted` | Autorização concedida | action, authorizedBy, context | ↑ |

> Reação normativa (doc. 04, seção 5): `order.item.cancelled` → item em CANCELLED, notifica garçom e caixa, +1 cancelamento na métrica, perda de estoque se já iniciado.

## 7. Contrato de API

```http
PATCH /v1/orders/{id}/items/{itemId}/cancel
Idempotency-Key: <uuid>
X-Authorization-Token: <obrigatório se o item já foi iniciado>
{ "reason": "CUSTOMER_REQUEST", "notes": "cliente desistiu" }
→ 200 { "item": { "status": "CANCELLED", "cancelledAt": "...",
                  "wasStarted": true, "authorizedBy": {...} } }
→ 403 { "code": "AUTHORIZATION_REQUIRED",
        "detail": "Item já iniciado. É necessária autorização de perfil superior.",
        "meta": { "action": "CANCEL_STARTED_ITEM", "itemStatus": "FIRED" } }
→ 409 { "code": "INVALID_STATE_TRANSITION" }

POST /v1/orders/{id}/cancel     { "reason": "..." }

# Obtenção da autorização (doc. 05, 2.2):
POST /v1/auth/authorize
{ "action": "CANCEL_STARTED_ITEM", "pin": "9911",
  "context": { "orderItemId": "..." } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `order_item` | Estado e motivo | `status=CANCELLED`, `cancelled_at`, `cancel_reason`, `cancelled_by`, `authorized_by`, `was_started` |
| `order` | Estado do pedido | `status=CANCELLED`, `cancel_stage` |
| `audit_log` | Trilha imutável | `action`, `actor_id`, `authorized_by`, `before`, `after`, `device_id` |
| `stock_movement` | Perda registrada (Fase 2) | `type=WASTE`, `waste_reason=CANCELLED_AFTER_START` |

## 9. Comportamento offline

Integralmente local, incluindo a autorização: o PIN do gerente é validado contra a réplica local de `app_user`. Se dependesse da nuvem, um cancelamento durante uma queda de internet ficaria bloqueado — e a operação pararia por uma razão administrativa, o que viola o requisito estruturante.

A permissão vigente offline é a da última sincronização de configuração (US-063).

## 10. Interface e experiência

- Motivo escolhido de lista curta, com opção de observação — texto livre obrigatório gera preenchimento aleatório
- Modal de autorização sobre o contexto: o gerente digita o PIN no dispositivo do operador, sem trocar de sessão
- Confirmação explícita quando o item já foi iniciado, com aviso de que gera perda
- No KDS, item cancelado some da fila com animação breve, evitando que a cozinha continue preparando

## 11. Métricas, alertas e observabilidade

- Contagem e valor de cancelamentos por motivo, operador, produto e faixa horária
- Percentual de cancelamentos após início da produção — mede o desperdício real
- Alerta ao gestor quando cancelamentos ultrapassam o padrão configurado (RF-ALT-01)
- Cancelamentos por autorizador — insumo de auditoria de gestão

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Máquina de estados: quais transições exigem autorização |
| Integração | Cancelamento sem token de autorização é recusado quando o item já foi iniciado |
| Integração | Token de autorização expirado é recusado |
| Integração | Item cancelado sai da fila do KDS e do total da mesa |
| Integração | `audit_log` contém executor, autorizador e valores |
| Caos offline | Cancelamento com autorização funciona com internet caída |
| E2E | Fluxo completo: garçom solicita, gerente autoriza no mesmo dispositivo |

## 13. Dependências

**Depende de:** US-004, US-030  
**Habilita:** US-090, US-105

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

- **RN-008 é hipótese não validada.** É preciso confirmar com o cliente o que acontece com o insumo de um item cancelado depois de iniciado (pendência registrada em Visão Geral 10.3).
- Quem pode autorizar cancelamento é regra de negócio pendente — precisa ser definida antes do piloto.
- Cancelamento excessivo pode indicar desvio; o alerta de padrão anômalo é o controle, não a proibição.

---

*US-033 · Épico E-03 · Pacote 004_DonaBetinha · Replay Studio.*