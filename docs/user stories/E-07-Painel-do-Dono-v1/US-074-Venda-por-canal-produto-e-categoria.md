# US-074 · Venda por canal produto e categoria

|  |  |
|---|---|
| **Épico** | [E-07 · Painel do Dono v1](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 7 |
| **Requisitos funcionais** | RF-BI-06 |
| **Regras de negócio** | — |
| **ADRs** | ADR-012 |
| **Eventos** | — |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor / proprietário (P8),
> **quero** ver quanto cada canal, produto e categoria vendeu,
> **para** que eu saiba o que puxa o faturamento e o que só ocupa espaço no cardápio.

## 2. Contexto e motivação

É a base da curva ABC e o insumo da matriz de engenharia de cardápio da Fase 2. Na v1, sem custo apurado, a análise é por volume e receita; com a ficha técnica, ela ganha a dimensão de margem e vira decisão de cardápio.

Um detalhe de modelagem importa muito aqui: **meio a meio precisa contar proporcionalmente**. Contar meia pizza de mussarela como uma unidade inteira distorce a curva ABC (decisão 8 do ERD).

## 3. Escopo

### 3.1 Dentro desta história

- Venda por canal (salão, delivery, balcão)
- Venda por produto e por variação
- Venda por categoria
- Curva ABC por receita e por volume
- Contagem proporcional de frações
- Comparativo com o período anterior
- Itens mais e menos vendidos

### 3.2 Fora desta história

- Margem por produto (US-109, Fase 2)
- Matriz de engenharia de cardápio (US-110, Fase 2)
- Mapa de calor de demanda (US-119, Fase 2)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Venda por dimensão

  Cenário: Venda por canal
    Dado vendas no salão e no delivery
    Quando a visão por canal for exibida
    Então cada canal deve mostrar receita, pedidos e ticket médio
    E a participação percentual deve estar visível

  Cenário: Contagem proporcional de frações
    Dado quatro pizzas meio a meio, todas com metade de Mussarela
    Quando a venda por produto for calculada
    Então Mussarela deve contar 2 unidades, não 4
    E a receita deve ser atribuída proporcionalmente

  Cenário: Curva ABC
    Dado 60 produtos vendidos no período
    Quando a curva ABC for gerada
    Então os produtos devem ser classificados em A, B e C por receita acumulada
    E deve ficar visível quantos produtos respondem por 80% da receita

  Cenário: Itens menos vendidos
    Dado produtos com venda muito baixa no período
    Quando a visão for exibida
    Então devem ser destacados como candidatos a revisão de cardápio

  Cenário: Comparativo por produto
    Dado a venda de um produto neste mês e no anterior
    Quando o comparativo for exibido
    Então a variação deve estar visível por produto
```

## 5. Regras de negócio aplicáveis

_Não se aplica a esta história._

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET /v1/metrics/sales?from=...&to=...&dimension=channel
→ { "items": [ { "key": "DINE_IN", "revenue": 318000, "orders": 124,
                 "avgTicket": 2564, "sharePercent": 66.0,
                 "variancePercent": 8.2 } ] }

GET /v1/metrics/sales?dimension=product
→ { "items": [ { "variantId": "...", "name": "Pizza G Mussarela",
                 "quantity": 84, "fractionQuantity": 71.5,
                 "revenue": 388800, "abcClass": "A" } ] }

GET /v1/metrics/sales?dimension=category
GET /v1/metrics/sales?dimension=operator
```

> `quantity` conta itens; `fractionQuantity` conta a soma ponderada das frações. A curva ABC usa a segunda.

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `metric_product_daily` | Agregado por produto e dia | `variant_id`, `quantity`, `fraction_quantity`, `revenue` |
| `metric_daily` | Agregado por canal | `channel`, `revenue`, `orders` |
| `order_item_fraction` | Peso das frações | `weight`, `variant_id` |

> `fraction_quantity` existe em `metric_product_daily` exatamente porque contar meio a meio como unidade inteira distorce a curva ABC (decisão 8 do ERD).

## 9. Comportamento offline

Consulta de nuvem, sobre agregados.

## 10. Interface e experiência

- Tabela ordenável, com participação percentual sempre visível
- Curva ABC com o corte de 80% destacado
- Itens de baixa venda em seção própria, com sugestão de revisão
- Comparativo por linha, não em tela separada

## 11. Métricas, alertas e observabilidade

- Receita e volume por canal, produto, variação e categoria
- Classificação ABC
- Ticket médio por canal
- Produtos sem venda no período — candidatos a saída do cardápio

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Contagem proporcional de frações e atribuição de receita |
| Unitário | Cálculo da curva ABC com corte configurável |
| Integração | Soma por dimensão bate com o faturamento total |
| Desempenho | Consulta com 200 produtos e 90 dias em menos de 3 s |

## 13. Dependências

**Depende de:** US-073  
**Habilita:** US-110, US-119

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

- Atribuição de receita em meio a meio depende da regra de precificação escolhida (RN-009). Documentar explicitamente como a receita é rateada entre as frações.

---

*US-074 · Épico E-07 · Pacote 004_DonaBetinha · Replay Studio.*