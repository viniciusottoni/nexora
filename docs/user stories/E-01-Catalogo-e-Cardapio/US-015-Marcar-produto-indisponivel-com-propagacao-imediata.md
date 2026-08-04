# US-015 · Marcar produto indisponivel com propagacao imediata

|  |  |
|---|---|
| **Épico** | [E-01 · Catalogo e Cardapio](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 2 |
| **Requisitos funcionais** | RF-CAT-07 |
| **Regras de negócio** | RN-012 |
| **ADRs** | ADR-011, ADR-028 |
| **Eventos** | EVT-051, EVT-012 |
| **Aplicações** | web-admin, web-kds, web-menu, web-pos, api-edge |
| **Autoridade do dado** | Bidirecional — cozinha marca no local, gestão marca na nuvem |

---

## 1. História

> **Como** pizzaiolo (P3) e gestor (P8),
> **quero** marcar um produto como indisponível e ver isso refletido em todos os canais na hora,
> **para** que ninguém peça o que a cozinha não tem como fazer.

## 2. Contexto e motivação

É a situação clássica que gera frustração e retrabalho: acabou a calabresa às 20h, e o cardápio da mesa continua vendendo calabresa até alguém lembrar de avisar.

Esta é uma das poucas informações **bidirecionais** do sistema: a cozinha marca no edge, o gestor marca na nuvem, e as duas direções precisam convergir. A propagação em até 2 segundos é requisito, não meta — é o mesmo limite de latência do roteamento de pedido.

## 3. Escopo

### 3.1 Dentro desta história

- Marcação de indisponibilidade por variação, com motivo
- Propagação por WebSocket a mesa, garçom, delivery e caixa em até 2 s
- Retorno à disponibilidade, manual ou automático no início do próximo dia operacional
- Marcação a partir do KDS (US-044 detalha a interação na cozinha)
- Contagem de ruptura como métrica

### 3.2 Fora desta história

- Bloqueio automático por falta de insumo (RF-EST-12, Fase 2)
- Sugestão de produto substituto ao cliente

## 4. Critérios de aceite

```gherkin
Funcionalidade: Indisponibilidade de produto

  Cenário: Propagação imediata
    Dado um produto disponível em todos os canais
    Quando a cozinha marcá-lo como indisponível
    Então ele deve sumir do cardápio da mesa, do garçom e do delivery em até 2 segundos

  Cenário: Cliente com o item no carrinho
    Dado um cliente que já tinha o item no carrinho
    Quando o produto ficar indisponível
    Então o carrinho deve avisar de forma clara antes do envio
    E o item deve ser removido apenas com confirmação do cliente

  Cenário: Pedido já confirmado
    Dado um pedido confirmado contendo o produto
    Quando o produto for marcado como indisponível
    Então o pedido existente não deve ser alterado
    E a cozinha deve tratar o caso pelo fluxo de cancelamento de item

  Cenário: Retorno automático no novo dia operacional
    Dado um produto marcado como indisponível com retorno automático ativo
    Quando o próximo dia operacional iniciar
    Então o produto deve voltar a ficar disponível
    E o gestor deve ser informado no resumo diário

  Cenário: Queda do WebSocket
    Dado que a conexão em tempo real de um dispositivo caiu
    Quando um produto for marcado como indisponível
    Então o dispositivo deve refletir a mudança em no máximo 5 segundos, via polling

  Cenário: Marcação com internet caída
    Dado que a loja está sem internet
    Quando a cozinha marcar um item como indisponível
    Então todos os dispositivos da rede local devem refletir imediatamente
    E a nuvem deve receber a informação na próxima sincronização
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-012 | Produto sem insumo disponível é bloqueado em todos os canais simultaneamente | **[HIPÓTESE]** — nesta fase o bloqueio é manual; o automático por insumo vem na Fase 2 |
| RN-003 | Cada transição de estado gera alerta aos perfis envolvidos | Alerta a garçom, caixa e gestor |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-051 | `product.availability_changed` | Disponibilidade alterada | variantId, isAvailable, reason | ↕ |
| EVT-012 | `order.item.unavailable_flagged` | Cozinha sinalizou falta | variantId, orderItemId | ↑ |

> É o único evento de catálogo com direção bidirecional (↕). A convergência segue a RN-019: prevalece o menor `occurredAt`.

## 7. Contrato de API

```http
POST /v1/kds/products/{variantId}/unavailable
{ "reason": "OUT_OF_STOCK", "autoRestoreNextDay": true }
→ 200 { "variant": { "isAvailable": false, "unavailableSince": "..." } }

POST /v1/catalog/variants/{id}/availability
{ "isAvailable": true }

# WebSocket, para todos os canais:
{ "type": "product.unavailable",
  "data": { "variantId": "...", "reason": "OUT_OF_STOCK" } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `product_variant` | Estado de disponibilidade | `is_available`, `unavailable_since`, `unavailable_reason`, `auto_restore` |
| `domain_event` | Histórico de rupturas | `type=product.availability_changed` |

## 9. Comportamento offline

Funciona integralmente offline dentro da rede local: a cozinha marca, o edge propaga por WebSocket local, todos os dispositivos da loja refletem em menos de 2 segundos. O cardápio de delivery, que roda na nuvem, só reflete após a sincronização — e isso é uma limitação real que precisa ser comunicada ao gestor.

Fallback obrigatório: se o WebSocket cair, o dispositivo faz polling a cada 5 segundos (ADR-011).

## 10. Interface e experiência

- No KDS, a marcação precisa caber em um toque — a cozinha está com as mãos ocupadas (detalhado na US-044)
- No cardápio do cliente, item indisponível fica visível porém desabilitado, com o motivo — sumir sem explicação gera pergunta ao garçom
- Aviso ao cliente que já tinha o item no carrinho, antes do envio, nunca depois
- Lista de itens indisponíveis sempre visível ao garçom, no topo do cardápio

## 11. Métricas, alertas e observabilidade

- Contagem de rupturas por produto e por faixa horária — insumo direto de compra e de ficha técnica
- Duração média da indisponibilidade
- Alerta ao gestor a cada ruptura, com o produto e o horário
- Venda perdida estimada (itens que estavam no carrinho quando a ruptura ocorreu)

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Propagação a todos os canais em menos de 2 s |
| Integração | Fallback de polling entrega a mudança em no máximo 5 s com WebSocket caído |
| Integração | Marcação offline propaga na LAN e sobe na sincronização |
| Concorrência | Marcação simultânea no KDS e no painel converge por `occurredAt` |
| E2E | Cliente com item no carrinho recebe aviso antes de enviar o pedido |

## 13. Dependências

**Depende de:** US-010, US-011  
**Habilita:** US-044, US-108

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

- Retorno automático no novo dia operacional depende da definição de dia operacional do ADR-018 — confirmar o horário de virada com o cliente.
- Item indisponível marcado e esquecido é venda perdida silenciosa; o alerta e o resumo diário existem para mitigar.

---

*US-015 · Épico E-01 · Pacote 004_DonaBetinha · Replay Studio.*