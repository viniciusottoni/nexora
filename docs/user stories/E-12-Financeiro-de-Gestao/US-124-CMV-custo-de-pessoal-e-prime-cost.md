# US-124 · CMV custo de pessoal e prime cost

|  |  |
|---|---|
| **Épico** | [E-12 · Financeiro de Gestao](./README.md) |
| **Fase** | 3 — Financeiro de gestão |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Fase 3 |
| **Requisitos funcionais** | RF-FIN-05 |
| **Regras de negócio** | — |
| **ADRs** | ADR-012 |
| **Eventos** | — |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor / proprietário (P8),
> **quero** ver o prime cost do meu negócio,
> **para** que eu compare com o padrão do setor e saiba se estou dentro ou fora.

## 2. Contexto e motivação

O **prime cost** — soma de CMV e custo de pessoal dividida pela receita — é o indicador mais usado na gestão de restaurantes, porque concentra as duas maiores linhas de custo e as duas mais controláveis.

A referência do setor gira em torno de 60% a 65%. Acima disso, o negócio dificilmente é sustentável, independentemente do faturamento.

Todos os componentes já existem: CMV vem do E-10, custo de pessoal vem da US-123, receita vem da US-120. Esta história apenas relaciona os três — que é exatamente como um indicador derivado deve nascer.

## 3. Escopo

### 3.1 Dentro desta história

- CMV do período, integrado à apuração do E-10
- Custo de pessoal do período
- Prime cost em valor e percentual
- Comparação com faixa de referência do setor
- Série histórica e tendência
- Composição e drill-down até a origem de cada componente
- Alerta quando ultrapassar o limiar configurado

### 3.2 Fora desta história

- Ponto de equilíbrio (US-125)
- Resultado do período (US-127)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Prime cost

  Cenário: Cálculo do prime cost
    Dado receita de R$ 48.200, CMV de R$ 19.870 e folha de R$ 9.630
    Quando o prime cost for calculado
    Então deve ser R$ 29.500, equivalente a 61,2%

  Cenário: Comparação com referência
    Dado a faixa de referência de 60% a 65%
    Quando o prime cost de 61,2% for exibido
    Então deve ser indicado como dentro da faixa

  Cenário: Alerta acima do limiar
    Dado o limiar configurado em 65%
    Quando o prime cost do mês atingir 68%
    Então o gestor deve ser alertado
    E a composição deve indicar qual componente cresceu

  Cenário: Composição
    Dado o prime cost apurado
    Quando a composição for exibida
    Então CMV e custo de pessoal devem aparecer separados, com percentuais

  Cenário: Drill-down
    Dado o CMV dentro do prime cost
    Quando o gestor tocar nele
    Então deve chegar à apuração de CMV com a composição por insumo

  Cenário: Período sem folha registrada
    Dado um mês sem folha lançada
    Quando o prime cost for solicitado
    Então deve indicar que o cálculo está incompleto
    E não deve apresentar número parcial como se fosse completo
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-009 | Todo indicador permite navegação até a origem | Drill-down obrigatório em cada componente |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET /v1/finance/prime-cost?period=2026-07
→ { "revenue": 4820000,
    "cmv": 1987000, "cmvPercent": 41.2,
    "laborCost": 963000, "laborPercent": 20.0,
    "primeCost": 2950000, "primeCostPercent": 61.2,
    "benchmark": { "min": 60.0, "max": 65.0, "status": "WITHIN" },
    "trend": [ { "period": "2026-05", "primeCostPercent": 63.8 } ] }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `financial_entry` | Receita e despesas | `type`, `amount`, `competence_date` |
| `expense_category` | Separação de CMV | `is_cmv` |
| `payroll` | Custo de pessoal | `total_cost` |
| `metric_daily` | Receita consolidada | `revenue` |

> Prime cost = (CMV + folha) ÷ receita (ERD, seção 4).

## 9. Comportamento offline

Consulta de nuvem, sobre dados consolidados.

## 10. Interface e experiência

- Prime cost como número principal, com a faixa de referência ao lado
- Composição em barra empilhada — a proporção entre CMV e folha é a informação
- Tendência de seis meses sempre visível
- Indicação clara quando algum componente está faltando, sem inventar número

## 11. Métricas, alertas e observabilidade

- Prime cost em valor e percentual, por período
- CMV e custo de pessoal como percentuais da receita
- Evolução e tendência
- Alerta acima do limiar

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo do prime cost e de seus componentes |
| Integração | CMV integrado à apuração do E-10 |
| Integração | Período incompleto sinalizado, sem número parcial |
| Validação | Conferência manual de um mês fechado |

## 13. Dependências

**Depende de:** US-107, US-120, US-123  
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

- Faixa de referência do setor varia por tipo de operação. Apresentar como referência, nunca como meta imposta.

---

*US-124 · Épico E-12 · Pacote 004_DonaBetinha · Replay Studio.*