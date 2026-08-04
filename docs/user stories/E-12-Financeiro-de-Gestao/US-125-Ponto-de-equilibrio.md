# US-125 · Ponto de equilibrio

|  |  |
|---|---|
| **Épico** | [E-12 · Financeiro de Gestao](./README.md) |
| **Fase** | 3 — Financeiro de gestão |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Fase 3 |
| **Requisitos funcionais** | RF-FIN-06 |
| **Regras de negócio** | — |
| **ADRs** | ADR-012 |
| **Eventos** | — |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor / proprietário (P8),
> **quero** saber quanto preciso vender para não ter prejuízo,
> **para** que eu tenha uma meta de faturamento com fundamento, não um chute.

## 2. Contexto e motivação

O ponto de equilíbrio é custo fixo dividido pela margem de contribuição percentual. É o número que transforma custo fixo em meta de venda — e um dos mais úteis que um dono de restaurante pode ter.

Todos os insumos já existem: custo fixo vem da US-122, margem de contribuição vem da US-109 e da apuração de CMV.

## 3. Escopo

### 3.1 Dentro desta história

- Cálculo do ponto de equilíbrio mensal
- Ponto de equilíbrio diário e por dia útil
- Margem de contribuição percentual do período
- Comparação entre faturamento realizado e ponto de equilíbrio
- Projeção do mês corrente com base no ritmo
- Simulação de impacto de mudança de custo fixo ou de margem

### 3.2 Fora desta história

- Resultado do período (US-127)
- Fluxo de caixa (US-126)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Ponto de equilíbrio

  Cenário: Cálculo mensal
    Dado custo fixo de R$ 18.420 e margem de contribuição de 58,8%
    Quando o ponto de equilíbrio for calculado
    Então deve ser aproximadamente R$ 31.327

  Cenário: Ponto de equilíbrio diário
    Dado 26 dias de operação no mês
    Quando o valor diário for calculado
    Então deve dividir o mensal pelos dias de operação

  Cenário: Comparação com o realizado
    Dado faturamento do mês de R$ 48.200
    Quando comparado ao ponto de equilíbrio
    Então deve indicar quanto está acima
    E deve mostrar a partir de que dia do mês ele foi atingido

  Cenário: Projeção do mês corrente
    Dado o mês em andamento no dia 18
    Quando a projeção for calculada
    Então deve estimar o fechamento com base no ritmo
    E deve indicar se o ponto de equilíbrio será atingido

  Cenário: Simulação de custo fixo
    Dado um aumento de aluguel de R$ 300
    Quando a simulação for feita
    Então o novo ponto de equilíbrio deve ser exibido
    E o faturamento adicional necessário deve ficar claro

  Cenário: Margem insuficiente
    Dado margem de contribuição muito baixa
    Quando o ponto de equilíbrio for calculado
    Então o valor será desproporcional ao faturamento típico
    E o sistema deve sinalizar o problema de margem
```

## 5. Regras de negócio aplicáveis

_Não se aplica a esta história._

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET /v1/finance/break-even?period=2026-07
→ { "fixedCost": 1842000,
    "contributionMarginPercent": 58.8,
    "breakEven": 3132700,
    "breakEvenDaily": 120490,
    "operatingDays": 26,
    "actualRevenue": 4820000,
    "aboveBreakEven": 1687300,
    "breakEvenReachedOn": "2026-07-19",
    "projection": { "estimatedRevenue": 4950000, "willReach": true } }

POST /v1/finance/break-even/simulate
{ "fixedCostChange": 30000 }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `recurring_cost` | Custo fixo mensal | `amount`, `frequency` |
| `financial_entry` | Despesas fixas do período | `type`, `category_id` |
| `metric_daily` | Receita e CMV para a margem | `revenue`, `cost` |

> Ponto de equilíbrio = custo fixo ÷ margem de contribuição percentual (ERD, seção 4).

## 9. Comportamento offline

Consulta de nuvem.

## 10. Interface e experiência

- Ponto de equilíbrio mensal e diário lado a lado — o diário é o que orienta a operação
- Barra de progresso do mês contra o ponto de equilíbrio
- Data em que foi atingido, no histórico — informação que o dono lembra e usa
- Simulação com resultado imediato, sem salvar

## 11. Métricas, alertas e observabilidade

- Ponto de equilíbrio mensal e diário
- Dia do mês em que foi atingido
- Margem de segurança (quanto o faturamento supera o ponto de equilíbrio)

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Cálculo com margens e custos fixos variados |
| Unitário | Cálculo diário considerando dias de operação |
| Integração | Projeção do mês corrente pelo ritmo |
| Validação | Conferência manual em um mês fechado |

## 13. Dependências

**Depende de:** US-122, US-124  
**Habilita:** US-127

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

- Ponto de equilíbrio depende de custo fixo completo. Se faltar categoria cadastrada, o número fica otimista — exibir a cobertura de custos fixos junto com o indicador.

---

*US-125 · Épico E-12 · Pacote 004_DonaBetinha · Replay Studio.*