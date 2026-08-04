# E-10 · Estoque e Ficha Tecnica

|  |  |
|---|---|
| **Fase** | 2 — Custo e controle |
| **Histórias** | 11 |
| **Pontos** | 84 |
| **Sprints previstas** | Fase 2 — sprints 9 a 12 |
| **Aplicações afetadas** | web-admin, api-cloud, api-edge |
| **Pacotes do monorepo** | packages/domain, packages/db, packages/metrics |

---

## 1. Objetivo do épico

Épico de **maior retorno financeiro do projeto**. Responde à dor registrada com mais clareza na descoberta:

> *"Cada pizza precisa ser cadastrada o quanto é preciso para fazê-la. Hoje há um relatório de quanto sobrou após comprar em quantidade, mas não se sabe quanto é necessário. Não se sabe quais foram as entradas e precisa controlar."*

Sem ficha técnica não há custo; sem custo não há margem; sem margem o dono não sabe o que dá lucro. Este épico é o que transforma o produto de sistema operacional em instrumento de gestão.

Duas decisões estruturam tudo: a baixa acontece na **conclusão da produção**, não no lançamento do pedido (RN-007), e o saldo é sempre **derivado da soma dos movimentos**, nunca um número armazenado (ADR-008) — é o que elimina o único conflito real de sincronização.

## 2. Valor entregue

- Custo de produção conhecido por produto, pela primeira vez
- Margem de contribuição real, revelando o que dá lucro e o que só dá volume
- CMV teórico contra real, expondo perda e desvio
- Baixa automática de insumo, sem digitação
- Alerta de estoque mínimo e de validade próxima
- Matriz de engenharia de cardápio, com recomendação de ação por produto

## 3. Histórias

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-100](./US-100-Cadastro-de-insumos-e-fornecedores.md) | Cadastro de insumos e fornecedores | M | 5 | RF-EST-01, RF-EST-11 |
| [US-101](./US-101-Ficha-tecnica-por-variacao.md) | Ficha tecnica por variacao | M | 8 | RF-EST-02 |
| [US-102](./US-102-Sub-receitas-de-preparo-intermediario.md) | Sub-receitas de preparo intermediario | S | 8 | RF-EST-03 |
| [US-103](./US-103-Baixa-automatica-na-conclusao-do-item.md) | Baixa automatica na conclusao do item | M | 13 | RF-EST-04 |
| [US-104](./US-104-Entradas-de-compra-com-custo-e-validade.md) | Entradas de compra com custo e validade | M | 8 | RF-EST-05 |
| [US-105](./US-105-Registro-de-perda-com-motivo-classificado.md) | Registro de perda com motivo classificado | M | 5 | RF-EST-06 |
| [US-106](./US-106-Contagem-ciclica-e-divergencia.md) | Contagem ciclica e divergencia | M | 8 | RF-EST-07 |
| [US-107](./US-107-CMV-teorico-versus-real.md) | CMV teorico versus real | M | 8 | RF-EST-08 |
| [US-108](./US-108-Alerta-de-estoque-minimo-e-validade.md) | Alerta de estoque minimo e validade | M | 5 | RF-EST-09, RF-EST-12 |
| [US-109](./US-109-Custo-e-margem-por-produto.md) | Custo e margem por produto | M | 8 | RF-EST-13 |
| [US-110](./US-110-Matriz-de-engenharia-de-cardapio.md) | Matriz de engenharia de cardapio | M | 8 | RF-BI-09 |

## 4. Ordem de execução recomendada

1. US-100 — insumos e fornecedores
2. US-101 — ficha técnica por variação
3. US-104 — entradas de compra (necessário para haver saldo)
4. US-103 — baixa automática na conclusão
5. US-105 — registro de perda
6. US-108 — alerta de mínimo e validade
7. US-106 — contagem cíclica
8. US-107 — CMV teórico contra real
9. US-109 — custo e margem por produto
10. US-110 — matriz de engenharia de cardápio
11. US-102 — sub-receitas (pode entrar depois)

## 5. Dependências do épico

**Depende de:** E-01, E-03, E-04, E-06  
**Habilita:** E-12

## 6. Definition of Done do épico

- [ ] 100% dos produtos com ficha técnica cadastrada (meta do PRD)
- [ ] Baixa automática funcionando, inclusive proporcional em meio a meio
- [ ] CMV teórico e real calculados, com divergência abaixo de 5% (meta do PRD em 90 dias)
- [ ] Custo e margem por produto validados manualmente contra uma amostra
- [ ] Matriz de engenharia de cardápio gerada e interpretada com o gestor
- [ ] Saldo sempre derivado de movimentos, nunca armazenado

## 7. Riscos do épico

| Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|
| Carga inicial de fichas técnicas depender do cliente e atrasar o épico | Alta | Alto | Risco 11 da Visão Geral — iniciar a carga em paralelo à Fase 1; definir responsável e prazo |
| RN-007 e RN-008 são hipóteses não validadas | Média | Alto | Confirmar com o cliente antes da implementação: momento da baixa e tratamento de cancelamento |
| Ficha técnica imprecisa produzir CMV enganoso | Alta | Alto | Divergência teórico versus real é justamente o mecanismo de detecção; calibrar nos primeiros 90 dias |

---

*Épico E-10 · Pacote 004_DonaBetinha · Replay Studio.*