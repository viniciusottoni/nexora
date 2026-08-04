# US-028 · Repetir item com um toque

|  |  |
|---|---|
| **Épico** | [E-02 · Mesa e Comanda](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | S — Should have |
| **Estimativa** | 2 pontos |
| **Sprint sugerida** | Sprint 3 |
| **Requisitos funcionais** | RF-SAL-11 |
| **Regras de negócio** | — |
| **ADRs** | ADR-020 |
| **Eventos** | EVT-003 |
| **Aplicações** | web-menu, web-pos, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** cliente do salão (P1) e garçom (P2),
> **quero** repetir um item já pedido com um toque,
> **para** que pedir a segunda cerveja não exija percorrer o cardápio de novo.

## 2. Contexto e motivação

Melhoria pequena de escopo e grande de impacto no ticket: reduzir o atrito da repetição aumenta o consumo incremental, que é justamente onde o salão ganha margem.

O item repetido carrega os mesmos modificadores e observações do original — repetir uma pizza meio a meio sem refazer a montagem é o caso que mais economiza tempo.

## 3. Escopo

### 3.1 Dentro desta história

- Ação de repetir em cada item já lançado na sessão
- Cópia de modificadores, frações e observações
- Confirmação em uma etapa, com o preço atual
- Disponível para cliente e para garçom

### 3.2 Fora desta história

- Sugestão automática de repetição
- Repetir pedido inteiro (isso é do delivery, US-135)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Repetir item

  Cenário: Repetição simples
    Dado uma sessão com uma cerveja já lançada
    Quando o cliente tocar em "repetir"
    Então um novo item idêntico deve ser adicionado ao pedido
    E deve seguir o fluxo normal de roteamento à praça

  Cenário: Repetição de item composto
    Dado um meio a meio com dois sabores e uma borda recheada
    Quando for repetido
    Então o novo item deve ter as mesmas frações, modificadores e observações

  Cenário: Item indisponível
    Dado um item cujo produto ficou indisponível
    Quando o cliente tentar repetir
    Então a ação deve ser bloqueada com a explicação

  Cenário: Preço atualizado
    Dado um item lançado antes de um reajuste de preço
    Quando for repetido
    Então o novo item deve usar o preço vigente
    E a diferença deve ficar visível antes da confirmação
```

## 5. Regras de negócio aplicáveis

_Não se aplica a esta história._

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-003 | `order.item.added` | Item repetido acrescentado | variantId, qty, modifiers, fractions, repeatedFrom | ↑ |

## 7. Contrato de API

```http
POST /v1/orders/{orderId}/items/{itemId}/repeat
Idempotency-Key: <uuid>
→ 201 { "item": { "id": "...", "unitPrice": 1800, "repeatedFrom": "..." } }
→ 422 { "code": "PRODUCT_UNAVAILABLE" }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `order_item` | Novo item com referência à origem | `repeated_from_item_id` |
| `order_item_fraction` / `order_item_modifier` | Cópia da composição | — |

## 9. Comportamento offline

Integralmente local. É uma criação de item comum, sujeita às mesmas garantias de idempotência da US-030.

## 10. Interface e experiência

- Ação de repetir visível na lista de consumo, sem submenu
- Confirmação única, com o preço atual em destaque quando houver diferença
- Sem confirmação dupla — o atrito é justamente o que a história elimina

## 11. Métricas, alertas e observabilidade

- Proporção de itens criados por repetição — mede a adoção
- Ticket médio de sessões com e sem uso da repetição

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cópia fiel de frações, modificadores e observações |
| Integração | Repetição bloqueada para produto indisponível |
| Integração | Preço vigente aplicado, não o preço do item original |

## 13. Dependências

**Depende de:** US-030  
**Habilita:** —

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

*US-028 · Épico E-02 · Pacote 004_DonaBetinha · Replay Studio.*