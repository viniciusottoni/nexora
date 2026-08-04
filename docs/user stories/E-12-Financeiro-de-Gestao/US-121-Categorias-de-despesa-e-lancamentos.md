# US-121 · Categorias de despesa e lancamentos

|  |  |
|---|---|
| **Épico** | [E-12 · Financeiro de Gestao](./README.md) |
| **Fase** | 3 — Financeiro de gestão |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Fase 3 |
| **Requisitos funcionais** | RF-FIN-02 |
| **Regras de negócio** | — |
| **ADRs** | ADR-017 |
| **Eventos** | — |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor / proprietário (P8),
> **quero** organizar minhas despesas em categorias e lançá-las,
> **para** que eu enxergue para onde meu dinheiro está indo.

## 2. Contexto e motivação

Estrutura sobre a qual todo o resto do épico se apoia. A distinção mais importante do modelo é a marcação `is_cmv`: **separa o que compõe o custo da mercadoria vendida do que é despesa operacional** (decisão 11 do ERD).

Sem essa separação, o prime cost e a margem de contribuição ficam errados — e são justamente os indicadores que o setor usa para avaliar saúde.

## 3. Escopo

### 3.1 Dentro desta história

- CRUD de categorias de despesa, com hierarquia de dois níveis
- Marcação `is_cmv` por categoria
- Classificação em fixa ou variável
- Lançamento manual de despesa, com competência e vencimento
- Anexo de comprovante
- Despesa gerada automaticamente por compra de insumo
- Categorias padrão semeadas por modelo de negócio

### 3.2 Fora desta história

- Custos fixos recorrentes (US-122)
- Folha de pagamento (US-123)
- Contas a pagar com controle de vencimento (fase posterior)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Categorias e lançamentos de despesa

  Cenário: Categoria de CMV
    Dado a categoria "Insumos" marcada como is_cmv
    Quando o CMV for apurado
    Então as despesas dessa categoria devem compor o CMV
    E não devem entrar como despesa operacional

  Cenário: Categoria operacional
    Dado a categoria "Aluguel" não marcada como is_cmv
    Quando o resultado for apurado
    Então deve compor a despesa operacional, não o CMV

  Cenário: Despesa automática por compra
    Dado uma entrada de compra de insumos registrada
    Quando a compra for confirmada
    Então deve ser criada despesa na categoria de insumos
    E deve estar vinculada à compra de origem

  Cenário: Lançamento manual com comprovante
    Dado uma despesa de manutenção
    Quando for lançada com anexo do comprovante
    Então deve ficar registrada com competência, vencimento e anexo

  Cenário: Categorias padrão
    Dado um tenant criado com modelo PIZZERIA
    Quando o financeiro for acessado pela primeira vez
    Então devem existir categorias padrão do ramo

  Cenário: Exclusão de categoria em uso
    Dado uma categoria com lançamentos
    Quando o gestor tentar excluí-la
    Então a exclusão deve ser recusada e a desativação oferecida
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-016 | Configuração, não código | Categorias são dados por tenant |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
POST /v1/finance/expense-categories
{ "name": "Insumos", "parentId": null,
  "isCmv": true, "costType": "VARIABLE" }

POST /v1/finance/entries
{ "type": "EXPENSE", "categoryId": "...", "amount": 450000,
  "competenceDate": "2026-07-01", "dueDate": "2026-07-10",
  "description": "Aluguel julho", "isRecurring": false }

POST /v1/finance/entries/{id}/attachment    (multipart)
GET  /v1/finance/entries?type=EXPENSE&period=2026-07&categoryId=...
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `expense_category` | Categoria | `name`, `parent_id`, `is_cmv`, `cost_type`, `is_active` |
| `financial_entry` | Lançamento | `type`, `category_id`, `amount`, `competence_date`, `due_date`, `description`, `attachment_url`, `source_type`, `source_id` |
| `purchase` | Origem da despesa automática | `total_cost` |

> `is_cmv` em `expense_category` é o que separa o que entra no CMV do que é despesa operacional (decisão 11 do ERD).

## 9. Comportamento offline

Operação de nuvem. Financeiro não é operação crítica de tempo real.

## 10. Interface e experiência

- Categorias padrão do ramo já semeadas — o gestor não começa de tela em branco
- Marcação `is_cmv` explicada em linguagem simples: "esta despesa faz parte do custo do que eu vendo?"
- Lançamento rápido, com categoria e valor em destaque
- Despesa automática identificada como tal, com link à compra de origem

## 11. Métricas, alertas e observabilidade

- Despesa por categoria e período
- Participação de cada categoria na despesa total
- Evolução por categoria ao longo dos meses

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Separação entre despesa de CMV e operacional |
| Integração | Compra de insumo gera despesa automática vinculada |
| Integração | Exclusão bloqueada com lançamentos |

## 13. Dependências

**Depende de:** US-104  
**Habilita:** US-122, US-124, US-127

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

- Categorização inconsistente distorce todos os indicadores derivados. Categorias padrão bem desenhadas reduzem esse risco.

---

*US-121 · Épico E-12 · Pacote 004_DonaBetinha · Replay Studio.*