# US-101 · Ficha tecnica por variacao

|  |  |
|---|---|
| **Épico** | [E-10 · Estoque e Ficha Tecnica](./README.md) |
| **Fase** | 2 — Custo e controle |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Fase 2 |
| **Requisitos funcionais** | RF-EST-02 |
| **Regras de negócio** | RN-006 |
| **ADRs** | ADR-016 |
| **Eventos** | EVT-053 |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** cadastrar quanto de cada insumo entra em cada produto,
> **para** que eu saiba quanto custa produzir e quanto devo repor.

## 2. Contexto e motivação

É a peça central do épico e responde diretamente a *"cada pizza precisa ser cadastrada o quanto é preciso para fazê-la"*.

A ficha é vinculada à **variação**, não ao produto — a pizza grande consome mais que a média, e tratar as duas como o mesmo produto tornaria o custo inútil.

O percentual de perda por insumo (aparas, evaporação, quebra) é o que aproxima o consumo teórico do real. Sem ele, a divergência de CMV nasce artificialmente alta.

## 3. Escopo

### 3.1 Dentro desta história

- Ficha técnica por variação, com lista de insumos e quantidades
- Percentual de perda por insumo da ficha
- Ficha para modificadores que consomem insumo próprio
- Cálculo automático do custo da ficha
- Duplicação de ficha entre variações, com escalonamento de quantidade
- Versionamento: alteração de ficha não muda o custo de pedidos passados
- Indicador de cobertura: quantos produtos já têm ficha

### 3.2 Fora desta história

- Sub-receitas (US-102)
- Baixa automática (US-103)
- Cálculo de margem (US-109)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Ficha técnica

  Cenário: Ficha de uma variação
    Dado a variação "Pizza G Mussarela"
    Quando a ficha for cadastrada com 180 g de mussarela, 250 g de massa e 80 g de molho
    Então o custo da ficha deve ser calculado a partir do custo médio de cada insumo

  Cenário: Percentual de perda
    Dado um insumo com 2% de perda cadastrada
    Quando o custo da ficha for calculado
    Então deve considerar 2% a mais do que a quantidade líquida

  Cenário: Duplicação com escalonamento
    Dado a ficha da pizza média cadastrada
    Quando o gestor duplicá-la para a pizza grande com fator 1,4
    Então todas as quantidades devem ser multiplicadas por 1,4
    E devem ficar editáveis individualmente

  Cenário: Modificador com insumo
    Dado o adicional "Borda Catupiry" que consome 60 g de catupiry
    Quando o adicional for escolhido em um pedido
    Então o insumo do modificador deve entrar no custo do item

  Cenário: Versionamento da ficha
    Dado um pedido concluído com a ficha vigente à época
    Quando a ficha for alterada depois
    Então o custo do pedido antigo não deve mudar

  Cenário: Cobertura do cardápio
    Dado 60 produtos cadastrados e 44 com ficha técnica
    Quando o indicador de cobertura for exibido
    Então deve mostrar 73%
    E deve listar os produtos sem ficha
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-006 | Cada produto possui ficha técnica que determina a baixa de insumo | É o objeto desta história |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-053 | `recipe.updated` | Ficha técnica alterada | variantId, items[], version | ↓ |

## 7. Contrato de API

```http
GET /v1/recipes/{variantId}
PUT /v1/recipes/{variantId}
{ "items": [ { "ingredientId": "...", "quantity": 0.18, "uom": "KG",
               "wastePercent": 2 },
             { "ingredientId": "...", "quantity": 0.25, "uom": "KG",
               "wastePercent": 3 } ] }
→ 200 { "recipe": { "version": 4, "cost": 842 } }

POST /v1/recipes/{variantId}/duplicate
{ "toVariantId": "...", "scaleFactor": 1.4 }

GET /v1/recipes/coverage
→ { "total": 60, "withRecipe": 44, "coveragePercent": 73,
    "missing": [ { "variantId": "...", "name": "..." } ] }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `recipe` | Ficha técnica da variação | `variant_id`, `version`, `cost`, `updated_at` |
| `recipe_item` | Insumo e quantidade | `recipe_id`, `ingredient_id`, `quantity`, `uom`, `waste_percent` |
| `order_item` | Custo congelado no momento do pedido | `unit_cost`, `recipe_version` |

> O custo é congelado em `order_item.unit_cost` no momento da conclusão — alterar a ficha depois não deve alterar o histórico.

## 9. Comportamento offline

A ficha é replicada ao edge, porque a baixa automática (US-103) acontece **localmente**, no momento em que o item fica pronto. Se dependesse da nuvem, a baixa não aconteceria durante uma queda de internet e o estoque divergiria.

## 10. Interface e experiência

- Cadastro em linha, insumo a insumo, com custo parcial atualizado a cada linha
- Custo total da ficha e margem estimada sempre visíveis durante a edição
- Duplicação com escalonamento é essencial para a carga inicial — cadastrar três tamanhos do zero é trabalho triplicado
- Indicador de cobertura em destaque no painel, até chegar a 100%

## 11. Métricas, alertas e observabilidade

- Cobertura de fichas técnicas — meta de 100% (PRD, seção 7)
- Custo da ficha por produto e sua evolução
- Insumos mais usados entre as fichas

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo do custo com percentual de perda e conversão de unidade |
| Unitário | Duplicação com escalonamento |
| Integração | Alteração da ficha não altera o custo de pedidos passados |
| Integração | Modificador com insumo entra no custo do item |

## 13. Dependências

**Depende de:** US-100, US-011  
**Habilita:** US-102, US-103, US-109

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

- **Risco 11 da Visão Geral** — a carga inicial de fichas técnicas é trabalhosa e depende do cliente. Iniciar em paralelo à Fase 1, com responsável e prazo definidos.
- Ficha imprecisa gera CMV enganoso. A divergência teórico versus real (US-107) é o mecanismo de detecção e calibração.

---

*US-101 · Épico E-10 · Pacote 004_DonaBetinha · Replay Studio.*