# US-044 · Marcar item indisponivel pelo KDS

|  |  |
|---|---|
| **Épico** | [E-04 · KDS Cozinha](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 4 |
| **Requisitos funcionais** | RF-KDS-10 |
| **Regras de negócio** | RN-012, RN-003 |
| **ADRs** | ADR-011 |
| **Eventos** | EVT-012, EVT-051 |
| **Aplicações** | web-kds, api-edge |
| **Autoridade do dado** | Local (bidirecional com a nuvem) |

---

## 1. História

> **Como** pizzaiolo (P3),
> **quero** sinalizar que acabou um item, direto do meu painel,
> **para** que ninguém continue pedindo o que eu não tenho como fazer.

## 2. Contexto e motivação

A cozinha é quem descobre primeiro que acabou a calabresa. Obrigar essa informação a passar pelo gestor significa que o cardápio continua vendendo por mais dez ou vinte minutos.

A US-015 implementa o mecanismo de propagação; esta história implementa a **interação na cozinha**, que precisa caber em um toque e não pode exigir digitação livre.

## 3. Escopo

### 3.1 Dentro desta história

- Ação de marcar indisponível a partir do cartão ou por código
- Motivo escolhido de lista curta (acabou, equipamento, qualidade)
- Propagação imediata a todos os canais
- Alerta a garçom, caixa e gestor
- Lista de itens indisponíveis sempre visível no KDS
- Retorno à disponibilidade pelo próprio KDS

### 3.2 Fora desta história

- Mecanismo de propagação (US-015)
- Bloqueio automático por saldo de insumo (RF-EST-12, Fase 2)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Marcar indisponível pelo KDS

  Cenário: Marcação em um toque
    Dado um item na fila cujo insumo acabou
    Quando o operador acionar a marcação de indisponível e escolher o motivo
    Então o produto deve ficar indisponível em todos os canais em até 2 segundos
    E nenhuma digitação livre deve ter sido necessária

  Cenário: Pedidos já confirmados não mudam
    Dado três pedidos pendentes com o produto marcado como indisponível
    Quando a marcação ocorrer
    Então os pedidos existentes devem permanecer na fila
    E o operador deve ser orientado a tratá-los pelo fluxo de cancelamento

  Cenário: Alerta direcionado
    Dado a marcação de indisponibilidade
    Quando o alerta for disparado
    Então garçom, caixa e gestor devem ser notificados
    E o cliente da mesa que tinha o item no carrinho deve ser avisado

  Cenário: Lista de indisponíveis sempre visível
    Dado três produtos marcados como indisponíveis
    Quando o KDS for exibido
    Então os três devem aparecer em área fixa da tela

  Cenário: Retorno à disponibilidade
    Dado um produto marcado como indisponível
    Quando o operador reativá-lo pelo KDS
    Então deve voltar a todos os canais em até 2 segundos

  Cenário: Marcação com internet caída
    Dado que a loja está sem internet
    Quando o operador marcar um item como indisponível
    Então todos os dispositivos da rede local devem refletir imediatamente
    E o cardápio de delivery só refletirá após a sincronização
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-012 | Produto sem insumo disponível é bloqueado em todos os canais simultaneamente | **[HIPÓTESE]** — nesta fase o bloqueio é manual, disparado pela cozinha |
| RN-003 | Cada transição gera alerta aos perfis envolvidos | Garçom, caixa e gestor notificados |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-012 | `order.item.unavailable_flagged` | Cozinha sinalizou falta | variantId, orderItemId | ↑ |
| EVT-051 | `product.availability_changed` | Disponibilidade alterada | variantId, isAvailable, reason | ↕ |

> Reação normativa: `order.item.unavailable_flagged` → produto indisponível em **todos os canais**, +1 ruptura na métrica.

## 7. Contrato de API

```http
POST /v1/kds/products/{variantId}/unavailable
Idempotency-Key: <uuid>
{ "reason": "OUT_OF_STOCK", "autoRestoreNextDay": true }
→ 200 { "variant": { "isAvailable": false, "unavailableSince": "..." },
        "affectedPendingItems": 3 }

POST /v1/kds/products/{variantId}/available
GET  /v1/kds/unavailable-products
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `product_variant` | Estado de disponibilidade | `is_available`, `unavailable_since`, `unavailable_reason`, `unavailable_by` |
| `alert` | Alerta de ruptura | `type=PRODUCT_UNAVAILABLE` |
| `domain_event` | Histórico de rupturas | `type=product.availability_changed` |

## 9. Comportamento offline

Funciona integralmente na rede local: cozinha marca, edge propaga por WebSocket, todos os dispositivos da loja refletem em menos de 2 segundos.

O cardápio de delivery, que roda na nuvem, só reflete após a sincronização. Essa limitação precisa ser comunicada ao gestor na implantação — durante uma queda de internet, o delivery pode continuar vendendo um item que acabou.

## 10. Interface e experiência

- Marcação acessível por tecla dedicada seguida do código, sem mouse
- Motivo escolhido por número (1 acabou, 2 equipamento, 3 qualidade), não por texto
- Confirmação visual clara — é uma ação com consequência em todos os canais
- Lista de indisponíveis em área fixa, para que o operador saiba o que já foi marcado

## 11. Métricas, alertas e observabilidade

- Rupturas por produto e por faixa horária — insumo direto de compra e de ficha técnica
- Duração média da indisponibilidade
- Venda perdida estimada por ruptura
- Alerta ao gestor a cada marcação

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Propagação a todos os canais em menos de 2 s |
| Integração | Pedidos já confirmados permanecem na fila |
| Integração | Alerta chega a garçom, caixa e gestor |
| Usabilidade | Marcação executada apenas com teclado numérico |
| Caos offline | Marcação propaga na LAN com internet caída |

## 13. Dependências

**Depende de:** US-015, US-040  
**Habilita:** US-108

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

- Item marcado e esquecido é venda perdida silenciosa. O retorno automático no novo dia operacional e a lista fixa na tela mitigam.

---

*US-044 · Épico E-04 · Pacote 004_DonaBetinha · Replay Studio.*