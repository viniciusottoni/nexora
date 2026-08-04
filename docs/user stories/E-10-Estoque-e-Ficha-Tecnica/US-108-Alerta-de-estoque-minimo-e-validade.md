# US-108 · Alerta de estoque minimo e validade

|  |  |
|---|---|
| **Épico** | [E-10 · Estoque e Ficha Tecnica](./README.md) |
| **Fase** | 2 — Custo e controle |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Fase 2 |
| **Requisitos funcionais** | RF-EST-09, RF-EST-12 |
| **Regras de negócio** | RN-012 |
| **ADRs** | ADR-032 |
| **Eventos** | EVT-045, EVT-046 |
| **Aplicações** | api-cloud, api-edge, web-admin |
| **Autoridade do dado** | Nuvem (avaliação) → alerta em ambos os lados |

---

## 1. História

> **Como** estoquista (P7) e gestor (P8),
> **quero** ser avisado quando um insumo está acabando ou perto de vencer,
> **para** que eu compre antes de faltar e use antes de perder.

## 2. Contexto e motivação

Dois alertas com naturezas distintas. O de **estoque mínimo** é preditivo: avisa antes da ruptura, dando tempo de compra. O de **validade** é de aproveitamento: avisa antes da perda, dando tempo de uso.

Ambos derivam de dados que já existem depois das histórias anteriores — não exigem nenhuma entrada manual nova, o que é exatamente o princípio de métrica derivada.

## 3. Escopo

### 3.1 Dentro desta história

- Alerta de insumo abaixo do estoque mínimo
- Alerta de ponto de reposição, considerando consumo médio
- Alerta de lote com validade próxima
- Alerta de saldo negativo
- Limiares configuráveis por insumo e por tenant
- Preparação para bloqueio de venda por falta de insumo (RF-EST-12)

### 3.2 Fora desta história

- Sugestão de lista de compras (RF-EST-10, Fase 3)
- Pedido automático ao fornecedor

## 4. Critérios de aceite

```gherkin
Funcionalidade: Alertas de estoque

  Cenário: Estoque mínimo
    Dado um insumo com mínimo de 5 kg
    Quando o saldo cair abaixo de 5 kg
    Então o alerta stock.below_minimum deve ser disparado ao gestor

  Cenário: Ponto de reposição por consumo
    Dado um insumo com consumo médio de 3 kg por dia
    E prazo de entrega do fornecedor de 2 dias
    Quando o saldo cair abaixo da cobertura necessária
    Então deve ser alertado o ponto de reposição

  Cenário: Validade próxima
    Dado um lote com validade em 3 dias
    E o limiar de aviso configurado em 5 dias
    Quando a avaliação diária executar
    Então o alerta stock.expiring_soon deve ser disparado

  Cenário: Saldo negativo
    Dado um insumo com saldo negativo
    Quando a avaliação executar
    Então deve alertar como possível erro de ficha ou entrada não registrada

  Cenário: Resolução automática
    Dado um alerta de estoque mínimo ativo
    Quando uma entrada elevar o saldo acima do mínimo
    Então o alerta deve ser resolvido automaticamente

  Cenário: Bloqueio de venda configurável
    Dado a configuração de bloqueio por falta de insumo ativada
    E um insumo essencial com saldo zerado
    Quando um cliente tentar pedir o produto
    Então o produto deve estar indisponível em todos os canais
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-012 | Produto sem insumo disponível é bloqueado em todos os canais simultaneamente | **[HIPÓTESE]** — bloqueio configurável, desativado por padrão |
| RN-003 | Cada transição gera alerta aos perfis envolvidos | Gestor e estoquista notificados |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-045 | `stock.below_minimum` | Cruzou o mínimo | ingredientId, current, minimum | ↑ |
| EVT-046 | `stock.expiring_soon` | Validade próxima | ingredientId, lotId, expiresAt | ↑ |
| EVT-051 | `product.availability_changed` | Bloqueio por falta de insumo | variantId, reason=NO_STOCK | ↕ |

## 7. Contrato de API

```http
GET /v1/ingredients?lowStock=true
→ { "items": [ { "ingredientId": "...", "name": "Mussarela",
                 "balance": 3.2, "minimum": 5.0,
                 "avgDailyConsumption": 3.0, "coverageDays": 1.1,
                 "status": "BELOW_MINIMUM" } ] }

GET /v1/stock/expiring?days=5
→ { "lots": [ { "ingredientId": "...", "lotCode": "L-2026-07",
                "quantity": 4.0, "expiresAt": "2026-08-03",
                "daysToExpire": 3, "costAtRisk": 13600 } ] }

PATCH /v1/tenant/config
{ "stock": { "blockSaleWithoutStock": false, "expiryWarnDays": 5 } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `ingredient` | Limiares e saldo | `minimum_stock`, `reorder_point`, `current_stock` |
| `purchase_item` | Lotes e validades | `lot_code`, `expires_at`, `quantity` |
| `metric_product_daily` | Consumo médio para cobertura | consumo por insumo |
| `alert` | Alertas ativos | `type=STOCK_BELOW_MINIMUM` / `STOCK_EXPIRING` |

## 9. Comportamento offline

A avaliação de saldo mínimo acontece na nuvem, sobre dados consolidados. O saldo espelhado no edge permite que o bloqueio de venda (quando configurado) funcione localmente.

Alertas chegam ao gestor por push da nuvem, e à operação pelo WebSocket local quando ele estiver na loja.

## 10. Interface e experiência

- Alerta de mínimo com cobertura em dias, não só o saldo — "resta 1,1 dia" é mais acionável que "3,2 kg"
- Validade com o custo em risco exibido, tornando a urgência concreta
- Lista de compras sugerida acessível a partir do alerta (mesmo antes da US da Fase 3)
- Bloqueio de venda desativado por padrão — ligar sem entender a ficha técnica trava a operação

## 11. Métricas, alertas e observabilidade

- Rupturas evitadas (alerta seguido de compra antes da falta)
- Perda por validade após alerta — mede se o aviso está sendo usado
- Insumos com alertas recorrentes, indicando mínimo mal calibrado

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo de cobertura em dias a partir do consumo médio |
| Integração | Alerta disparado e resolvido automaticamente |
| Integração | Bloqueio de venda propagando a todos os canais quando ativado |

## 13. Dependências

**Depende de:** US-103, US-104, US-080  
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

- **RN-012 é hipótese e o bloqueio automático é perigoso.** Um erro de ficha técnica com bloqueio ativado tira produtos do cardápio sem motivo real. Desativado por padrão, ativável só depois que a ficha estiver calibrada.

---

*US-108 · Épico E-10 · Pacote 004_DonaBetinha · Replay Studio.*