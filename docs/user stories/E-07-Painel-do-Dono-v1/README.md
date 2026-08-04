# E-07 · Painel do Dono v1

|  |  |
|---|---|
| **Fase** | 1 — MVP |
| **Histórias** | 8 |
| **Pontos** | 47 |
| **Sprints previstas** | Sprint 7 |
| **Aplicações afetadas** | web-admin, api-cloud |
| **Pacotes do monorepo** | packages/metrics, packages/contracts |

---

## 1. Objetivo do épico

Entregar a primeira versão da camada que dá nome à diretriz do produto: **controle e métrica total para o dono**.

A Visão Geral é explícita (seção 7): *a camada de medição não é um relatório no fim do mês — é o produto*. O painel se organiza para responder quatro perguntas em ordem: o que está acontecendo agora, como foi o período, estou ganhando dinheiro, e o que está fora do lugar.

A v1 cobre as duas primeiras. Resultado e custo dependem da ficha técnica (Fase 2) e do financeiro (Fase 3).

Regra de ouro que atravessa todo o épico: **nenhuma métrica é digitada; toda métrica é derivada**. Se um indicador exigir entrada manual, ele está mal desenhado.

## 2. Valor entregue

- O dono passa a decidir por indicador, não por intuição
- Tempos por etapa revelam onde está o gargalo real — a dor mais explícita da descoberta
- Faturamento com comparativo, acessível do celular, de qualquer lugar
- Drill-down do número até o pedido individual em três toques
- Atraso de sincronização explícito, para que nenhum dado defasado passe por tempo real

## 3. Histórias

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-070](./US-070-Pulso-em-tempo-real-no-celular.md) | Pulso em tempo real no celular | M | 8 | RF-BI-01, RF-BI-14 |
| [US-071](./US-071-Tempos-por-etapa-com-media-e-p90.md) | Tempos por etapa com media e p90 | M | 8 | RF-BI-02, RF-BI-03 |
| [US-072](./US-072-Aderencia-ao-prazo-OTD.md) | Aderencia ao prazo OTD | M | 5 | RF-BI-04 |
| [US-073](./US-073-Faturamento-com-comparativo.md) | Faturamento com comparativo | M | 5 | RF-BI-05 |
| [US-074](./US-074-Venda-por-canal-produto-e-categoria.md) | Venda por canal produto e categoria | M | 5 | RF-BI-06 |
| [US-075](./US-075-Ticket-medio-giro-de-mesa-e-ocupacao.md) | Ticket medio giro de mesa e ocupacao | M | 5 | RF-BI-07 |
| [US-076](./US-076-Drill-down-do-numero-ate-o-pedido.md) | Drill-down do numero ate o pedido | M | 8 | RF-BI-11 |
| [US-077](./US-077-Resumo-diario-automatico.md) | Resumo diario automatico | S | 3 | RF-BI-12 |

## 4. Ordem de execução recomendada

1. US-071 — tempos por etapa (a dor mais explícita)
2. US-073 — faturamento com comparativo
3. US-074 — venda por canal, produto e categoria
4. US-075 — ticket médio, giro e ocupação
5. US-072 — aderência ao prazo
6. US-070 — pulso em tempo real, que consolida os anteriores
7. US-076 — drill-down
8. US-077 — resumo diário automático

## 5. Dependências do épico

**Depende de:** E-00, E-03, E-05, E-06  
**Habilita:** E-10, E-11, E-12

## 6. Definition of Done do épico

- [ ] Painel carregando em menos de 3 segundos no celular
- [ ] Todos os indicadores derivados de eventos, sem nenhuma entrada manual
- [ ] Drill-down do número ao pedido em no máximo 3 toques
- [ ] Atraso de sincronização visível em toda visão
- [ ] Agregados corretos após operação offline prolongada
- [ ] Validação com o gestor real de que os números fazem sentido

## 7. Riscos do épico

| Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|
| Métrica sem qualidade de dado — indicador errado é pior que indicador nenhum | Média | Alto | Risco 10 da Visão Geral — instrumentação obrigatória por evento; validação no piloto |
| Indicadores prioritários da v1 não definidos pelo cliente | Alta | Médio | Pendência 9 do índice — workshop de indicadores antes da Sprint 7 |
| Consulta ao painel degradar com volume de eventos | Média | Médio | Agregados pré-calculados (ADR-012); painel nunca consulta evento bruto |

---

*Épico E-07 · Pacote 004_DonaBetinha · Replay Studio.*