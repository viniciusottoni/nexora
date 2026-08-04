# E-12 · Financeiro de Gestao

|  |  |
|---|---|
| **Fase** | 3 — Financeiro de gestão |
| **Histórias** | 9 |
| **Pontos** | 55 |
| **Sprints previstas** | Fase 3 |
| **Aplicações afetadas** | web-admin, api-cloud |
| **Pacotes do monorepo** | packages/domain, packages/db, packages/metrics |

---

## 1. Objetivo do épico

Responder à pergunta que hoje não tem resposta: *"como está a saúde financeira?"*

O cliente foi específico sobre o escopo: *"quero uma gestão financeira (salários de funcionários, custos com insumos e custo com CMO — aluguel, imposto)"*.

A camada financeira **consome** o que as fases anteriores produziram: receita vem automaticamente dos pagamentos (E-05), CMV vem da ficha técnica (E-10), e o que sobra é o que precisa ser cadastrado — folha, custos fixos e despesas.

Isso não substitui o contador. É gestão de resultado, não contabilidade formal — a distinção precisa ficar clara ao cliente.

## 2. Valor entregue

- Resultado do período apurado, com composição — pela primeira vez visível
- Prime cost (CMV + folha sobre receita), o indicador mais usado do setor
- Ponto de equilíbrio: quanto precisa vender para não ter prejuízo
- Fluxo de caixa realizado e projetado
- Exportação para o contador, reduzindo trabalho manual dos dois lados

## 3. Histórias

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-120](./US-120-Receita-automatica-a-partir-de-pagamentos.md) | Receita automatica a partir de pagamentos | M | 5 | RF-FIN-01 |
| [US-121](./US-121-Categorias-de-despesa-e-lancamentos.md) | Categorias de despesa e lancamentos | M | 5 | RF-FIN-02 |
| [US-122](./US-122-Custos-fixos-recorrentes.md) | Custos fixos recorrentes | M | 5 | RF-FIN-03 |
| [US-123](./US-123-Folha-de-pagamento.md) | Folha de pagamento | M | 8 | RF-FIN-04 |
| [US-124](./US-124-CMV-custo-de-pessoal-e-prime-cost.md) | CMV custo de pessoal e prime cost | M | 8 | RF-FIN-05 |
| [US-125](./US-125-Ponto-de-equilibrio.md) | Ponto de equilibrio | M | 5 | RF-FIN-06 |
| [US-126](./US-126-Fluxo-de-caixa-realizado-e-projetado.md) | Fluxo de caixa realizado e projetado | S | 8 | RF-FIN-07 |
| [US-127](./US-127-Resultado-do-periodo-com-composicao.md) | Resultado do periodo com composicao | M | 8 | RF-FIN-08 |
| [US-128](./US-128-Exportacao-para-o-contador.md) | Exportacao para o contador | S | 3 | RF-FIN-09 |

## 4. Ordem de execução recomendada

1. US-121 — categorias de despesa (estrutura de tudo)
2. US-120 — receita automática a partir dos pagamentos
3. US-122 — custos fixos recorrentes
4. US-123 — folha de pagamento
5. US-124 — CMV, custo de pessoal e prime cost
6. US-127 — resultado do período
7. US-125 — ponto de equilíbrio
8. US-126 — fluxo de caixa
9. US-128 — exportação para o contador

## 5. Dependências do épico

**Depende de:** E-05, E-10  
**Habilita:** —

## 6. Definition of Done do épico

- [ ] Resultado do período conferido contra apuração manual de um mês fechado
- [ ] Receita gerada automaticamente, sem digitação
- [ ] Prime cost e ponto de equilíbrio calculados e validados com o gestor
- [ ] Exportação aceita pelo contador do cliente
- [ ] Nenhum indicador financeiro dependendo de entrada manual redundante

## 7. Riscos do épico

| Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|
| Confusão entre gestão financeira e contabilidade formal | Alta | Médio | Comunicar limite com clareza; o contador continua responsável pela contabilidade |
| Regime tributário e alíquotas não definidos | Alta | Médio | Pendência fiscal do índice; impostos entram como despesa cadastrada até a definição |
| Cadastro de custos fixos e folha depender do cliente | Média | Médio | Materiais pendentes; iniciar coleta antes da Fase 3 |

---

*Épico E-12 · Pacote 004_DonaBetinha · Replay Studio.*