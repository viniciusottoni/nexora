# US-102 · Sub-receitas de preparo intermediario

|  |  |
|---|---|
| **Épico** | [E-10 · Estoque e Ficha Tecnica](./README.md) |
| **Fase** | 2 — Custo e controle |
| **Prioridade** | S — Should have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Fase 2 |
| **Requisitos funcionais** | RF-EST-03 |
| **Regras de negócio** | RN-006 |
| **ADRs** | — |
| **Eventos** | EVT-053 |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** cadastrar preparos intermediários como massa e molho, usados em vários produtos,
> **para** que eu não repita os mesmos insumos em dezenas de fichas.

## 2. Contexto e motivação

Numa pizzaria, massa e molho entram em quase todos os produtos. Sem sub-receita, cada ficha repete os insumos da massa — e um reajuste no preço da farinha exige editar sessenta fichas.

A sub-receita também representa melhor a realidade da cozinha: a massa é produzida em lote, não pizza a pizza.

## 3. Escopo

### 3.1 Dentro desta história

- Cadastro de sub-receita com rendimento e insumos
- Uso de sub-receita como item de outra ficha
- Custo recursivo, calculado a partir dos insumos da sub-receita
- Produção de lote de sub-receita, com baixa dos insumos e entrada do preparo
- Prevenção de referência circular
- Profundidade máxima configurável

### 3.2 Fora desta história

- Planejamento de produção de lotes
- Controle de validade de preparo (coberto pela US-108)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Sub-receitas

  Cenário: Ficha usando sub-receita
    Dado a sub-receita "Massa" com rendimento de 20 discos
    Quando a ficha da pizza usar 1 disco de massa
    Então o custo deve ser o custo da sub-receita dividido pelo rendimento

  Cenário: Custo recursivo
    Dado a sub-receita "Molho" usada dentro da sub-receita "Base Pronta"
    Quando o custo da pizza for calculado
    Então deve percorrer recursivamente até os insumos básicos

  Cenário: Referência circular
    Dado a tentativa de usar a sub-receita A dentro de B, e B dentro de A
    Quando o cadastro for salvo
    Então deve ser recusado com explicação

  Cenário: Produção de lote
    Dado um lote de massa produzido
    Quando a produção for registrada
    Então os insumos devem ser baixados
    E o preparo deve entrar no estoque como saldo próprio

  Cenário: Reajuste propagado
    Dado um aumento no custo da farinha
    Quando o custo das fichas for recalculado
    Então todos os produtos que usam massa devem refletir o novo custo
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-006 | Cada produto possui ficha técnica que determina a baixa | Sub-receita é ficha de ficha |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-053 | `recipe.updated` | Sub-receita alterada | recipeId, items[], version | ↓ |
| EVT-040 | `stock.deducted` | Baixa de insumos na produção do lote | ingredientId, qty, cost | ↑ |
| EVT-041 | `stock.received` | Entrada do preparo produzido | items[], totalCost | ↑ |

## 7. Contrato de API

```http
POST /v1/sub-recipes
{ "name": "Massa", "yieldQuantity": 20, "yieldUom": "UN",
  "items": [ { "ingredientId": "<farinha>", "quantity": 3.0, "uom": "KG" } ] }

PUT /v1/recipes/{variantId}
{ "items": [ { "subRecipeId": "<massa>", "quantity": 1, "uom": "UN" } ] }

POST /v1/sub-recipes/{id}/produce
{ "batches": 3 }
→ 201 { "deducted": [...], "produced": { "quantity": 60, "cost": 4200 } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `recipe` | Sub-receita marcada como tal | `is_sub_recipe`, `yield_quantity`, `yield_uom` |
| `recipe_item` | Item que pode ser insumo ou sub-receita | `ingredient_id` ou `sub_recipe_id` |
| `stock_movement` | Baixa dos insumos e entrada do preparo | `type=PRODUCTION` |

> Referência do ERD: `variant_cost(variant_id)` é função recursiva sobre `recipe_item`.

## 9. Comportamento offline

Replicada ao edge como parte da ficha técnica, para que a baixa recursiva funcione localmente.

## 10. Interface e experiência

- Sub-receita visualmente distinta de insumo na composição da ficha
- Custo expandido mostrando a composição recursiva, para conferência
- Registro de produção de lote em tela simples, pensada para quem está na cozinha

## 11. Métricas, alertas e observabilidade

- Custo por sub-receita e sua evolução
- Rendimento real versus rendimento cadastrado dos lotes
- Produtos afetados por variação de custo de cada sub-receita

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo recursivo de custo com múltiplos níveis |
| Unitário | Detecção de referência circular |
| Integração | Produção de lote baixa insumos e cria saldo do preparo |
| Integração | Reajuste de insumo propaga a todas as fichas dependentes |

## 13. Dependências

**Depende de:** US-101  
**Habilita:** US-109

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

- Cálculo recursivo sem limite de profundidade pode gerar consulta pesada. Limite configurável e cache do custo calculado.

---

*US-102 · Épico E-10 · Pacote 004_DonaBetinha · Replay Studio.*