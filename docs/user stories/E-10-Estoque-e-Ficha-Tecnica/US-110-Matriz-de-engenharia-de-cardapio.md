# US-110 · Matriz de engenharia de cardapio

|  |  |
|---|---|
| **Épico** | [E-10 · Estoque e Ficha Tecnica](./README.md) |
| **Fase** | 2 — Custo e controle |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Fase 2 |
| **Requisitos funcionais** | RF-BI-09 |
| **Regras de negócio** | — |
| **ADRs** | ADR-012 |
| **Eventos** | — |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor / proprietário (P8),
> **quero** ver meus produtos classificados por volume e margem,
> **para** que eu saiba o que promover, o que reprecificar e o que tirar do cardápio.

## 2. Contexto e motivação

É a síntese de todo o épico e a entrega de maior valor gerencial do produto. A matriz cruza **volume de venda** com **margem de contribuição** e classifica cada produto em quatro quadrantes, cada um com uma ação recomendada distinta:

| Quadrante | Volume | Margem | Ação típica |
|---|:-:|:-:|---|
| **Estrela** | Alto | Alta | Proteger, destacar no cardápio, manter qualidade |
| **Cavalo de batalha** | Alto | Baixa | Reduzir custo ou aumentar preço com cautela |
| **Quebra-cabeça** | Baixo | Alta | Promover, reposicionar, destacar |
| **Abacaxi** | Baixo | Baixa | Reformular ou tirar do cardápio |

É o que transforma dado em decisão de cardápio.

## 3. Escopo

### 3.1 Dentro desta história

- Cálculo dos quadrantes por período
- Corte de volume e margem configurável (padrão: média do período)
- Recomendação de ação por quadrante
- Contagem proporcional de frações no volume
- Comparação entre períodos, mostrando produtos que mudaram de quadrante
- Drill-down até os pedidos de cada produto

### 3.2 Fora desta história

- Execução automática de mudança de preço ou de cardápio
- Previsão de impacto de retirada de produto

## 4. Critérios de aceite

```gherkin
Funcionalidade: Matriz de engenharia de cardápio

  Cenário: Classificação em quadrantes
    Dado produtos com volume e margem apurados no período
    Quando a matriz for gerada
    Então cada produto deve ser classificado como
         Estrela, Cavalo de batalha, Quebra-cabeça ou Abacaxi
    E deve haver recomendação de ação por quadrante

  Cenário: Contagem proporcional de frações
    Dado pizzas vendidas majoritariamente como meio a meio
    Quando o volume for calculado
    Então deve usar a quantidade fracionada, não a contagem de itens

  Cenário: Corte configurável
    Dado o corte padrão na média do período
    Quando o gestor alterar para a mediana
    Então a classificação deve ser recalculada

  Cenário: Mudança de quadrante entre períodos
    Dado um produto que era Estrela no mês anterior e virou Cavalo de batalha
    Quando o comparativo for exibido
    Então a mudança deve estar destacada
    E a causa (queda de margem ou de volume) deve ficar evidente

  Cenário: Produto sem ficha técnica
    Dado um produto sem custo apurado
    Quando a matriz for gerada
    Então ele deve ficar fora da classificação
    E deve constar em lista separada de pendências

  Cenário: Drill-down por produto
    Dado um produto classificado como Abacaxi
    Quando o gestor tocar nele
    Então deve ver os pedidos que o contêm no período
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-009 | Todo indicador permite navegação até o evento de origem | Drill-down obrigatório por produto |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET /v1/metrics/menu-engineering?from=...&to=...&cut=AVERAGE
→ { "items": [ { "variantId": "...", "name": "Pizza G Mussarela",
                 "quantity": 84, "fractionQuantity": 71.5,
                 "revenue": 388800, "cost": 60200,
                 "margin": 328600, "marginPercent": 84.5,
                 "quadrant": "STAR",
                 "recommendation": "Proteger. Manter destaque no cardápio." } ],
    "cuts": { "volume": 42.0, "marginPercent": 76.2 },
    "withoutRecipe": [ { "variantId": "...", "name": "..." } ] }

GET /v1/metrics/menu-engineering/compare?periodA=2026-06&periodB=2026-07
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `metric_product_daily` | Volume e margem agregados | `quantity`, `fraction_quantity`, `revenue`, `cost` |
| `recipe` | Custo, via `variant_cost()` | — |
| `price` | Preço por canal | `amount` |

> `fraction_quantity` é o que impede contar meia pizza como unidade inteira e distorcer a classificação (decisão 8 do ERD).

## 9. Comportamento offline

Consulta de nuvem, sobre agregados consolidados.

## 10. Interface e experiência

- Matriz como gráfico de dispersão com os quatro quadrantes nomeados
- Cada ponto tocável, levando ao detalhe e ao drill-down
- Recomendação em linguagem direta, não em jargão de consultoria
- Comparativo entre períodos mostrando setas de movimento entre quadrantes
- Produtos sem ficha em lista separada — não classificar sem custo é honestidade, não limitação

## 11. Métricas, alertas e observabilidade

- Distribuição de produtos por quadrante e sua evolução
- Participação de cada quadrante no faturamento e na margem total
- Produtos que mudaram de quadrante entre períodos

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Classificação nos quatro quadrantes com cortes variados |
| Unitário | Uso de `fractionQuantity` no volume |
| Integração | Produtos sem ficha ficam fora da classificação |
| Integração | Comparativo entre períodos identifica mudanças de quadrante |
| Validação | Classificação conferida com o gestor contra sua percepção do cardápio |

## 13. Dependências

**Depende de:** US-074, US-109  
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

- A matriz só é confiável com fichas técnicas completas e calibradas. Apresentá-la com cobertura baixa gera decisão errada — exibir a cobertura junto com a matriz.

---

*US-110 · Épico E-10 · Pacote 004_DonaBetinha · Replay Studio.*