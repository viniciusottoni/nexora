# US-127 · Resultado do periodo com composicao

|  |  |
|---|---|
| **Épico** | [E-12 · Financeiro de Gestao](./README.md) |
| **Fase** | 3 — Financeiro de gestão |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Fase 3 |
| **Requisitos funcionais** | RF-FIN-08 |
| **Regras de negócio** | — |
| **ADRs** | ADR-012, ADR-018 |
| **Eventos** | — |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor / proprietário (P8),
> **quero** ver se tive lucro ou prejuízo no período, e por quê,
> **para** que eu finalmente saiba se o meu negócio está indo bem.

## 2. Contexto e motivação

É a resposta final ao *"quero saber a saúde financeira"* e o fecho da camada 4 do ecossistema.

O número sozinho não basta — a **composição** é o que gera ação. Um resultado ruim causado por CMV alto leva a uma decisão; causado por custo fixo, a outra completamente diferente.

A apresentação segue a estrutura de DRE gerencial: receita, menos CMV, igual margem de contribuição; menos despesas operacionais e folha, igual resultado.

## 3. Escopo

### 3.1 Dentro desta história

- Resultado do período em estrutura de DRE gerencial
- Composição: receita, CMV, margem de contribuição, folha, custo fixo, outras despesas
- Percentual de cada linha sobre a receita
- Comparativo com o período anterior, linha a linha
- Drill-down de cada linha até a origem
- Identificação da linha que mais explica a variação
- Visão mensal, trimestral e anual

### 3.2 Fora desta história

- Demonstrativos contábeis formais
- Apuração de impostos

## 4. Critérios de aceite

```gherkin
Funcionalidade: Resultado do período

  Cenário: Estrutura de DRE gerencial
    Dado um mês fechado
    Quando o resultado for exibido
    Então deve mostrar receita, CMV, margem de contribuição,
         despesas operacionais, folha e resultado
    E cada linha deve trazer o percentual sobre a receita

  Cenário: Comparativo linha a linha
    Dado o mês atual e o anterior
    Quando o comparativo for exibido
    Então cada linha deve mostrar a variação absoluta e percentual

  Cenário: Explicação da variação
    Dado uma queda de resultado em relação ao mês anterior
    Quando a análise for exibida
    Então a linha que mais contribuiu para a queda deve estar destacada

  Cenário: Drill-down por linha
    Dado a linha de CMV
    Quando o gestor tocar nela
    Então deve chegar à apuração de CMV com composição por insumo

  Cenário: Período com dados incompletos
    Dado um mês sem folha lançada
    Quando o resultado for solicitado
    Então deve indicar claramente que está incompleto
    E deve listar o que falta

  Cenário: Prejuízo
    Dado um período com resultado negativo
    Quando for exibido
    Então o prejuízo deve estar claro
    E a composição deve indicar as principais causas
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-009 | Todo indicador permite navegação até a origem | Drill-down por linha do resultado |
| RN-020 | Métrica usa `ocorrido_em` | Competência pelo dia operacional |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET /v1/finance/summary?period=2026-07
→ { "revenue":    { "amount": 4820000, "percent": 100.0, "variance": 8.2 },
    "cmv":        { "amount": 1987000, "percent": 41.2,  "variance": 2.1 },
    "contributionMargin": { "amount": 2833000, "percent": 58.8 },
    "labor":      { "amount": 963000,  "percent": 20.0,  "variance": 0.4 },
    "fixed":      { "amount": 1842000, "percent": 38.2,  "variance": 6.7 },
    "other":      { "amount": 128000,  "percent": 2.7 },
    "primeCost":  { "percent": 61.2 },
    "breakEven":  3132700,
    "result":     { "amount": -100000, "percent": -2.1, "variance": -142.0 },
    "mainDriver": "fixed",
    "isComplete": true, "missing": [] }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `financial_entry` | Todas as linhas | `type`, `category_id`, `amount`, `competence_date` |
| `expense_category` | Separação CMV e operacional | `is_cmv`, `cost_type` |
| `payroll` | Folha | `total_cost` |
| `metric_daily` | Receita consolidada | `revenue` |

## 9. Comportamento offline

Consulta de nuvem, sobre dados consolidados.

## 10. Interface e experiência

- Estrutura de DRE reconhecível, mas em linguagem de gestão, não contábil
- Percentual sobre receita em toda linha — é o que permite comparar meses de faturamento diferente
- Linha que mais explica a variação destacada automaticamente
- Cada linha tocável, levando à origem
- Aviso inequívoco quando o período está incompleto, com a lista do que falta

## 11. Métricas, alertas e observabilidade

- Resultado do período em valor e percentual
- Margem de contribuição
- Evolução do resultado ao longo dos meses
- Composição percentual de cada linha

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Montagem da DRE gerencial com todas as linhas |
| Unitário | Identificação da linha que mais explica a variação |
| Integração | Drill-down funcionando em cada linha |
| Integração | Período incompleto sinalizado com a lista de pendências |
| Validação | Conferência contra apuração manual de um mês fechado |

## 13. Dependências

**Depende de:** US-120, US-122, US-123, US-124  
**Habilita:** US-128

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
- [ ] Resultado conferido contra apuração manual de um mês real, com o gestor

## 15. Riscos, premissas e pendências

- Resultado gerencial diferente do resultado contábil gera desconfiança. Comunicar a diferença de forma explícita e permanente na tela.

---

*US-127 · Épico E-12 · Pacote 004_DonaBetinha · Replay Studio.*