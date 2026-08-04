# US-109 · Custo e margem por produto

|  |  |
|---|---|
| **Épico** | [E-10 · Estoque e Ficha Tecnica](./README.md) |
| **Fase** | 2 — Custo e controle |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Fase 2 |
| **Requisitos funcionais** | RF-EST-13 |
| **Regras de negócio** | — |
| **ADRs** | ADR-017 |
| **Eventos** | — |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor / proprietário (P8),
> **quero** saber quanto custa e quanto rende cada produto que eu vendo,
> **para** que eu pare de vender no prejuízo sem saber.

## 2. Contexto e motivação

É a resposta à pergunta do painel 3: *estou ganhando dinheiro? Em quê?*

Custo vem da ficha técnica; preço vem do cadastro por canal; a margem de contribuição é a diferença. O resultado costuma surpreender: produtos de alto volume com margem baixa e produtos esquecidos no cardápio com margem excelente.

A margem precisa ser calculada **por canal**, porque o mesmo produto tem preços diferentes no salão e no delivery.

## 3. Escopo

### 3.1 Dentro desta história

- Custo por variação, calculado recursivamente pela ficha
- Margem de contribuição em valor e percentual, por canal
- Composição do custo por insumo
- Alerta de produto com margem negativa
- Simulação de impacto de mudança de preço ou de custo
- Ranking por margem e por margem total do período

### 3.2 Fora desta história

- Matriz de engenharia de cardápio (US-110)
- Custo de pessoal e prime cost (Fase 3)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Custo e margem por produto

  Cenário: Margem de contribuição
    Dado uma pizza com custo de R$ 8,42 e preço de R$ 45,00 no salão
    Quando a margem for calculada
    Então deve ser R$ 36,58, equivalente a 81,3%

  Cenário: Margem por canal
    Dado a mesma pizza a R$ 52,00 no delivery
    Quando a margem por canal for exibida
    Então deve mostrar margens distintas para salão e delivery

  Cenário: Composição do custo
    Dado um produto com cinco insumos na ficha
    Quando a composição for exibida
    Então cada insumo deve mostrar sua participação no custo
    E o de maior peso deve aparecer primeiro

  Cenário: Margem negativa
    Dado um produto cujo custo superou o preço após reajuste de insumo
    Quando a avaliação executar
    Então o gestor deve ser alertado
    E o produto deve aparecer destacado na lista

  Cenário: Simulação de preço
    Dado um produto com margem de 60%
    Quando o gestor simular um aumento de preço de 10%
    Então a nova margem deve ser exibida
    E o impacto no resultado do período deve ser estimado

  Cenário: Produto sem ficha técnica
    Dado um produto sem ficha cadastrada
    Quando a margem for consultada
    Então deve indicar que o custo é desconhecido
    E não deve exibir margem estimada como se fosse real
```

## 5. Regras de negócio aplicáveis

_Não se aplica a esta história._

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET /v1/products/{variantId}/cost
→ { "cost": 842, "recipeVersion": 4,
    "breakdown": [ { "ingredientId": "...", "name": "Mussarela",
                     "quantity": 0.18, "cost": 612, "sharePercent": 72.7 } ],
    "prices": [ { "channel": "DINE_IN",  "price": 4500,
                  "margin": 3658, "marginPercent": 81.3 },
                { "channel": "DELIVERY", "price": 5200,
                  "margin": 4358, "marginPercent": 83.8 } ] }

GET /v1/metrics/margins?from=...&to=...&sortBy=totalMargin
POST /v1/products/{variantId}/simulate
{ "priceChange": { "channel": "DINE_IN", "newPrice": 4950 } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `recipe` / `recipe_item` | Fonte do custo | Função `variant_cost(variant_id)`, recursiva |
| `price` | Preço por canal | `channel`, `amount` |
| `metric_product_daily` | Volume e margem total do período | `quantity`, `fraction_quantity`, `revenue`, `cost` |

> Margem = `price.amount − variant_cost()` (ERD, seção 4). O custo é recursivo sobre `recipe_item`, cobrindo sub-receitas.

## 9. Comportamento offline

Consulta de nuvem.

## 10. Interface e experiência

- Custo, preço e margem na mesma linha, por canal
- Composição do custo em gráfico simples — costuma revelar que um insumo domina
- Margem negativa em vermelho e no topo da lista, sempre
- Simulação com resultado imediato, sem salvar nada
- Produtos sem ficha claramente marcados como custo desconhecido, nunca com estimativa disfarçada

## 11. Métricas, alertas e observabilidade

- Margem de contribuição por produto, variação e canal
- Margem total do período por produto (margem unitária × volume)
- Produtos com margem negativa
- Evolução da margem conforme o custo dos insumos varia

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo recursivo do custo com sub-receitas |
| Unitário | Margem por canal com preços distintos |
| Integração | Alerta de margem negativa |
| Integração | Simulação não persiste alteração |
| Validação | Conferência manual do custo de um produto contra a ficha física |

## 13. Dependências

**Depende de:** US-101, US-102, US-014  
**Habilita:** US-110, US-124

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

- Margem calculada sobre ficha imprecisa induz decisão errada de cardápio. Exibir a data da última revisão da ficha junto com a margem.

---

*US-109 · Épico E-10 · Pacote 004_DonaBetinha · Replay Studio.*