# US-126 · Fluxo de caixa realizado e projetado

|  |  |
|---|---|
| **Épico** | [E-12 · Financeiro de Gestao](./README.md) |
| **Fase** | 3 — Financeiro de gestão |
| **Prioridade** | S — Should have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Fase 3 |
| **Requisitos funcionais** | RF-FIN-07 |
| **Regras de negócio** | — |
| **ADRs** | ADR-012 |
| **Eventos** | — |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor / proprietário (P8),
> **quero** ver entradas e saídas ao longo do tempo, realizadas e previstas,
> **para** que eu saiba se vou ter dinheiro para pagar o que devo.

## 2. Contexto e motivação

Resultado e caixa são coisas diferentes: um mês pode ter lucro e faltar dinheiro, porque a receita de cartão entra em trinta dias e o fornecedor vence em sete.

O fluxo de caixa trabalha por **data de movimentação**, não por competência — e é justamente essa distinção que o torna útil.

## 3. Escopo

### 3.1 Dentro desta história

- Fluxo realizado por data de movimentação
- Projeção a partir de recorrentes e vencimentos conhecidos
- Previsão de recebimento de cartão pelo prazo do provedor
- Saldo acumulado ao longo do período
- Identificação de dias com saldo projetado negativo
- Visão por mês, semana e dia

### 3.2 Fora desta história

- Conciliação bancária
- Contas a pagar com controle de aprovação

## 4. Critérios de aceite

```gherkin
Funcionalidade: Fluxo de caixa

  Cenário: Fluxo realizado
    Dado entradas e saídas do mês
    Quando o fluxo for exibido
    Então deve mostrar por data de movimentação, não por competência
    E o saldo acumulado deve estar visível

  Cenário: Recebimento de cartão projetado
    Dado uma venda em crédito com prazo de 30 dias
    Quando o fluxo for projetado
    Então o recebimento deve aparecer 30 dias depois da venda

  Cenário: Projeção de recorrentes
    Dado custos fixos cadastrados com dia de vencimento
    Quando a projeção for gerada
    Então os vencimentos futuros devem aparecer nas datas corretas

  Cenário: Saldo projetado negativo
    Dado uma concentração de vencimentos em uma semana
    Quando a projeção indicar saldo negativo
    Então os dias afetados devem ser destacados
    E o gestor deve ser alertado

  Cenário: Distinção entre realizado e projetado
    Dado o fluxo exibindo passado e futuro
    Quando for visualizado
    Então realizado e projetado devem ser visualmente distintos

  Cenário: Comparação com o resultado
    Dado um mês com lucro e caixa apertado
    Quando as duas visões forem comparadas
    Então a diferença deve ser explicável pelos prazos de recebimento
```

## 5. Regras de negócio aplicáveis

_Não se aplica a esta história._

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET /v1/finance/cashflow?months=6&granularity=day
→ { "series": [ { "date": "2026-08-05",
                  "inflow": 182000, "outflow": 45000,
                  "net": 137000, "balance": 892000,
                  "isProjected": false } ],
    "negativeDays": [ { "date": "2026-08-22", "balance": -32000 } ],
    "projectionFrom": "2026-08-01" }

PATCH /v1/tenant/config
{ "payment": { "providers": [ { "code": "CIELO",
                                "settlementDays": { "CREDIT": 30, "DEBIT": 1 } } ] } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `financial_entry` | Entradas e saídas | `amount`, `competence_date`, `due_date`, `paid_at` |
| `payment` | Recebimentos com prazo do provedor | `method`, `provider`, `net_amount` |
| `recurring_cost` | Vencimentos projetados | `due_day`, `amount` |
| `tenant_config` | Prazos de repasse por provedor | `payment.providers[].settlementDays` |

## 9. Comportamento offline

Consulta de nuvem.

## 10. Interface e experiência

- Gráfico com saldo acumulado como linha principal
- Realizado e projetado distintos por cor e por textura
- Dias com saldo negativo em destaque, com alerta
- Alternância entre visão diária, semanal e mensal

## 11. Métricas, alertas e observabilidade

- Saldo projetado mínimo do período
- Dias com saldo negativo projetado
- Prazo médio de recebimento
- Diferença entre resultado e caixa

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Projeção de recebimento pelo prazo do provedor |
| Unitário | Projeção de recorrentes pelas datas de vencimento |
| Integração | Identificação de saldo negativo projetado |
| Integração | Distinção correta entre realizado e projetado |

## 13. Dependências

**Depende de:** US-120, US-122  
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

- Projeção depende de prazos de repasse cadastrados corretamente por provedor. Prazo errado produz projeção enganosa.

---

*US-126 · Épico E-12 · Pacote 004_DonaBetinha · Replay Studio.*