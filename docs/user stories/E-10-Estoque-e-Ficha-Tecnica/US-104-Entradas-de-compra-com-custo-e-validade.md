# US-104 · Entradas de compra com custo e validade

|  |  |
|---|---|
| **Épico** | [E-10 · Estoque e Ficha Tecnica](./README.md) |
| **Fase** | 2 — Custo e controle |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Fase 2 |
| **Requisitos funcionais** | RF-EST-05 |
| **Regras de negócio** | — |
| **ADRs** | ADR-008, ADR-017 |
| **Eventos** | EVT-041 |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** estoquista / comprador (P7),
> **quero** registrar as compras que entram no estoque, com custo e validade,
> **para** que eu finalmente saiba quais foram as entradas — algo que hoje não existe.

## 2. Contexto e motivação

Responde diretamente a *"não se sabe quais foram as entradas e precisa controlar"*. É a metade que falta do controle: sem registro de entrada, o saldo é só uma sequência de saídas e o CMV real não pode ser apurado.

A entrada também é o que atualiza o custo médio ponderado, alimentando toda a apuração de custo do produto.

## 3. Escopo

### 3.1 Dentro desta história

- Registro de compra com fornecedor, itens, quantidades e custos
- Data de validade e identificação de lote por item
- Recálculo do custo médio ponderado
- Rateio de frete e despesas acessórias sobre os itens
- Recebimento parcial
- Vínculo com nota fiscal do fornecedor (referência, não integração)
- Geração de despesa no financeiro (Fase 3)

### 3.2 Fora desta história

- Integração com nota fiscal eletrônica
- Pedido de compra e cotação
- Sugestão de lista de compras (RF-EST-10, Fase 3)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Entradas de compra

  Cenário: Entrada com atualização de custo médio
    Dado 10 kg de mussarela em estoque a R$ 32,00/kg
    Quando entrarem 5 kg a R$ 38,00/kg
    Então o saldo deve ser 15 kg
    E o custo médio deve passar a R$ 34,00/kg

  Cenário: Validade por lote
    Dado uma entrada com validade informada
    Quando o lote for registrado
    Então a validade deve ficar vinculada ao lote
    E deve alimentar o alerta de validade próxima

  Cenário: Rateio de frete
    Dado uma compra de R$ 1.000,00 com R$ 80,00 de frete
    Quando o rateio proporcional for aplicado
    Então o custo de cada item deve incluir sua parcela do frete

  Cenário: Recebimento parcial
    Dado uma compra de 20 kg com apenas 15 kg recebidos
    Quando o recebimento for registrado
    Então devem entrar 15 kg
    E a pendência de 5 kg deve ficar registrada

  Cenário: Entrada corrige saldo negativo
    Dado um insumo com saldo negativo de 2 kg
    Quando entrarem 10 kg
    Então o saldo deve passar a 8 kg
    E o alerta de saldo negativo deve ser resolvido

  Cenário: Despesa no financeiro
    Dado uma compra registrada
    Quando a entrada for confirmada
    Então deve ser gerada despesa correspondente no financeiro
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-004 | Toda ação registra autor, horário e dispositivo | Entrada registra quem recebeu |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-041 | `stock.received` | Entrada de compra | purchaseId, items[], totalCost | ↑ |

> Reação normativa: `stock.received` → saldo recalculado, custo médio atualizado, **entrada de estoque** e **despesa no financeiro**.

## 7. Contrato de API

```http
POST /v1/purchases
{ "supplierId": "...", "invoiceRef": "NF 12345",
  "freightAmount": 8000,
  "items": [ { "ingredientId": "...", "quantity": 5.0, "uom": "KG",
               "unitCost": 3800, "lotCode": "L-2026-07",
               "expiresAt": "2026-09-15" } ] }
→ 201 { "purchase": {...}, "movements": [...],
        "newAvgCosts": [ { "ingredientId": "...", "avgCost": 3400 } ] }

POST /v1/purchases/{id}/receive     { "items": [ { "id": "...", "receivedQuantity": 15.0 } ] }
GET  /v1/purchases?from=...&to=...
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `purchase` | Compra | `supplier_id`, `invoice_ref`, `total_cost`, `freight_amount`, `received_at` |
| `purchase_item` | Item da compra | `ingredient_id`, `quantity`, `unit_cost`, `lot_code`, `expires_at` |
| `stock_movement` | Entrada gerada | `type=PURCHASE`, `quantity` (positiva), `unit_cost` |
| `ingredient` | Custo médio atualizado | `avg_cost`, `current_stock` |
| `financial_entry` | Despesa gerada (Fase 3) | `type=EXPENSE`, `category`, `amount` |

## 9. Comportamento offline

Registro na nuvem. Entrada de compra não é operação de tempo real e acontece tipicamente pela manhã, no recebimento.

O saldo resultante desce ao edge pelo pull. Como o saldo é derivado de movimentos, uma entrada registrada na nuvem e uma baixa feita offline convivem sem conflito (ADR-008).

## 10. Interface e experiência

- Lançamento por fornecedor, com os insumos daquele fornecedor já sugeridos
- Custo anterior exibido ao lado do novo, evidenciando variação de preço
- Validade opcional, mas destacada para insumos perecíveis
- Rateio de frete automático, com opção de ajuste manual

## 11. Métricas, alertas e observabilidade

- Volume e valor de compras por período e por fornecedor
- Variação de custo por insumo entre compras
- Insumos com maior participação no custo de compra
- Base do CMV real (US-107)

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Recálculo do custo médio ponderado, incluindo saldo negativo inicial |
| Unitário | Rateio proporcional de frete |
| Integração | Entrada gera movimento e atualiza saldo derivado |
| Integração | Recebimento parcial registra pendência |
| Integração | Despesa criada no financeiro |

## 13. Dependências

**Depende de:** US-100  
**Habilita:** US-103, US-107, US-108, US-121

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

- **Material pendente do cliente** — o relatório de sobras e compras mencionado na reunião (Visão Geral 20.2) ajudaria a validar o desenho.
- Registro de entrada depende de disciplina do recebimento. Sem ele, o CMV real fica errado — e é justamente esse controle que hoje não existe.

---

*US-104 · Épico E-10 · Pacote 004_DonaBetinha · Replay Studio.*