# US-122 · Custos fixos recorrentes

|  |  |
|---|---|
| **Épico** | [E-12 · Financeiro de Gestao](./README.md) |
| **Fase** | 3 — Financeiro de gestão |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Fase 3 |
| **Requisitos funcionais** | RF-FIN-03 |
| **Regras de negócio** | — |
| **ADRs** | ADR-018 |
| **Eventos** | — |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor / proprietário (P8),
> **quero** cadastrar meus custos fixos uma vez e vê-los lançados todo mês,
> **para** que eu não esqueça nada na hora de calcular o resultado.

## 2. Contexto e motivação

O cliente listou explicitamente: *aluguel, imposto, CMO*. São despesas previsíveis e recorrentes, e relançá-las manualmente todo mês é trabalho garantido e esquecimento provável.

Custo fixo também é o denominador do **ponto de equilíbrio** (US-125): sem ele cadastrado corretamente, o indicador não existe.

## 3. Escopo

### 3.1 Dentro desta história

- Cadastro de custo recorrente com valor, periodicidade e vigência
- Geração automática do lançamento a cada competência
- Ajuste de valor com histórico (reajuste de aluguel)
- Encerramento de recorrência
- Lançamento de valor diferente do previsto em um mês específico
- Painel de custos fixos com total mensal

### 3.2 Fora desta história

- Folha de pagamento (US-123)
- Controle de pagamento e vencimento (fase posterior)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Custos fixos recorrentes

  Cenário: Geração automática mensal
    Dado o aluguel de R$ 4.500,00 cadastrado como recorrente mensal
    Quando a competência de agosto iniciar
    Então o lançamento de agosto deve ser criado automaticamente

  Cenário: Reajuste com histórico
    Dado o aluguel reajustado para R$ 4.800,00 a partir de setembro
    Quando os lançamentos forem gerados
    Então agosto deve manter R$ 4.500,00 e setembro deve usar R$ 4.800,00

  Cenário: Valor diferente em um mês
    Dado a conta de energia como recorrente com valor estimado
    Quando o valor real do mês for informado
    Então o lançamento deve ser ajustado
    E a diferença deve ficar visível

  Cenário: Encerramento de recorrência
    Dado um contrato encerrado em outubro
    Quando a recorrência for encerrada
    Então nenhum lançamento deve ser gerado a partir de novembro

  Cenário: Total de custo fixo
    Dado todos os custos fixos cadastrados
    Quando o painel for exibido
    Então deve mostrar o total mensal
    E deve alimentar o cálculo de ponto de equilíbrio
```

## 5. Regras de negócio aplicáveis

_Não se aplica a esta história._

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
POST /v1/finance/recurring-costs
{ "categoryId": "...", "description": "Aluguel",
  "amount": 450000, "frequency": "MONTHLY",
  "startDate": "2026-01-01", "endDate": null,
  "dueDay": 10 }

PATCH /v1/finance/recurring-costs/{id}
{ "amount": 480000, "effectiveFrom": "2026-09-01" }

GET /v1/finance/recurring-costs
→ { "items": [...], "monthlyTotal": 1842000 }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `recurring_cost` | Custo recorrente | `category_id`, `description`, `amount`, `frequency`, `start_date`, `end_date`, `due_day` |
| `recurring_cost_history` | Histórico de valores | `amount`, `effective_from` |
| `financial_entry` | Lançamento gerado | `source_type=recurring_cost`, `is_automatic` |

## 9. Comportamento offline

Operação de nuvem.

## 10. Interface e experiência

- Lista de custos fixos com total mensal sempre visível
- Reajuste com data de vigência, nunca alterando o passado
- Aviso quando um custo recorrente não tem lançamento no mês corrente
- Custos estimados marcados como tal, para lembrar de informar o real

## 11. Métricas, alertas e observabilidade

- Custo fixo mensal total e sua evolução
- Custo fixo como percentual da receita
- Insumo direto do ponto de equilíbrio

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Geração de lançamentos por periodicidade e vigência |
| Integração | Reajuste com vigência não altera competências passadas |
| Integração | Encerramento interrompe a geração |

## 13. Dependências

**Depende de:** US-121  
**Habilita:** US-125, US-127

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

- **Material pendente do cliente** — a lista de custos fixos precisa ser levantada antes da Fase 3.
- Impostos entram como despesa cadastrada enquanto o regime tributário for pendência aberta.

---

*US-122 · Épico E-12 · Pacote 004_DonaBetinha · Replay Studio.*