# US-100 · Cadastro de insumos e fornecedores

|  |  |
|---|---|
| **Épico** | [E-10 · Estoque e Ficha Tecnica](./README.md) |
| **Fase** | 2 — Custo e controle |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Fase 2 |
| **Requisitos funcionais** | RF-EST-01, RF-EST-11 |
| **Regras de negócio** | — |
| **ADRs** | ADR-017 |
| **Eventos** | — |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** estoquista / comprador (P7) e gestor (P8),
> **quero** cadastrar os insumos que compro, com unidade, custo e fornecedor,
> **para** que eu tenha a base para saber quanto custa cada produto que vendo.

## 2. Contexto e motivação

É a fundação do épico. Toda a apuração de custo depende de o insumo existir com unidade de medida correta e custo atualizado.

O ponto de cuidado é a **unidade de medida**: a mussarela é comprada em quilo e consumida em grama; a cerveja é comprada em caixa e vendida em unidade. Conversões erradas aqui contaminam todo o CMV.

## 3. Escopo

### 3.1 Dentro desta história

- CRUD de insumo com nome, categoria, unidade de estoque e unidade de consumo
- Fator de conversão entre unidades
- Custo médio ponderado, atualizado pelas entradas
- CRUD de fornecedor e vínculo com insumos
- Histórico de custo por insumo e fornecedor
- Estoque mínimo e ponto de reposição
- Marcação de insumo como componente de CMV

### 3.2 Fora desta história

- Ficha técnica (US-101)
- Entradas de compra (US-104)
- Sugestão de lista de compras (RF-EST-10, Fase 3)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Cadastro de insumos e fornecedores

  Cenário: Insumo com conversão de unidade
    Dado a mussarela comprada em quilo e consumida em grama
    Quando o insumo for cadastrado com fator de conversão 1000
    Então 180 g na ficha técnica devem equivaler a 0,180 kg no estoque

  Cenário: Custo médio ponderado
    Dado 10 kg em estoque a R$ 32,00/kg
    E uma entrada de 5 kg a R$ 38,00/kg
    Quando o custo médio for recalculado
    Então deve ser R$ 34,00/kg

  Cenário: Histórico de custo
    Dado várias entradas do mesmo insumo ao longo do tempo
    Quando o histórico for consultado
    Então deve mostrar a evolução do custo por fornecedor e data

  Cenário: Insumo vinculado a fornecedores
    Dado um insumo comprado de dois fornecedores
    Quando o cadastro for consultado
    Então ambos devem constar, com o último custo de cada

  Cenário: Exclusão com movimento
    Dado um insumo com movimentos de estoque registrados
    Quando o gestor tentar excluí-lo
    Então a exclusão deve ser recusada e a desativação oferecida
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-006 | Cada produto possui ficha técnica que determina a baixa de insumo | O insumo é o objeto da baixa |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
POST /v1/ingredients
{ "name": "Mussarela", "categoryId": "...",
  "stockUom": "KG", "consumptionUom": "G", "conversionFactor": 1000,
  "minimumStock": 5.0, "reorderPoint": 8.0, "isCmv": true }

GET   /v1/ingredients?lowStock=true
PATCH /v1/ingredients/{id}
GET   /v1/ingredients/{id}/cost-history

POST /v1/suppliers          { "name": "...", "document": "...", "contact": {...} }
POST /v1/ingredients/{id}/suppliers   { "supplierId": "...", "lastCost": 3400 }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `ingredient` | Insumo | `name`, `category_id`, `stock_uom`, `consumption_uom`, `conversion_factor`, `avg_cost`, `minimum_stock`, `is_cmv` |
| `supplier` | Fornecedor | `name`, `document`, `contact` |
| `ingredient_supplier` | Vínculo e último custo | `ingredient_id`, `supplier_id`, `last_cost`, `last_purchase_at` |
| `unit_of_measure` | Tabela global | `code`, `name`, `dimension` |

## 9. Comportamento offline

Cadastro na nuvem, replicado ao edge apenas no que a operação precisa (o saldo espelhado, para verificação de disponibilidade). O cadastro em si não é operação de tempo real.

## 10. Interface e experiência

- Conversão de unidade explicada com exemplo na própria tela — é o campo que mais gera erro
- Custo médio exibido junto com o último custo, para evidenciar variação de preço de compra
- Cadastro em lote na carga inicial, com importação de planilha
- Categoria de insumo para organizar listas longas

## 11. Métricas, alertas e observabilidade

- Evolução do custo por insumo — insumo de negociação com fornecedor
- Insumos abaixo do mínimo
- Participação de cada insumo no CMV total

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Conversão entre unidades em ambos os sentidos |
| Unitário | Cálculo do custo médio ponderado com entradas sucessivas |
| Integração | Exclusão bloqueada com movimentos registrados |

## 13. Dependências

**Depende de:** US-002  
**Habilita:** US-101, US-104, US-107

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

- **Material pendente do cliente** — lista de insumos e fornecedores (Visão Geral 20.2). Sem ela, o épico não começa.
- Conversão de unidade errada é o erro mais caro deste épico: contamina custo, margem e CMV sem gerar sintoma óbvio.

---

*US-100 · Épico E-10 · Pacote 004_DonaBetinha · Replay Studio.*