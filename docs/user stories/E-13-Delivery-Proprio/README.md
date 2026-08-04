# E-13 · Delivery Proprio

|  |  |
|---|---|
| **Fase** | 4 — Delivery próprio |
| **Histórias** | 9 |
| **Pontos** | 68 |
| **Sprints previstas** | Fase 4 |
| **Aplicações afetadas** | web-menu, web-pos, web-admin, api-cloud, api-edge |
| **Pacotes do monorepo** | packages/domain, packages/contracts |

---

## 1. Objetivo do épico

Criar o canal de venda próprio, reduzindo a dependência do iFood e a comissão que ele cobra.

A meta declarada pelo cliente é explícita: **entregar pizza na casa das pessoas em 25 minutos**. Todo o épico se organiza em torno de tornar essa meta mensurável e alcançável.

Diferente dos épicos anteriores, este depende de internet por natureza — pedido online, pagamento online e rastreio não têm caminho offline. A degradação é esperada e precisa ser comunicada com clareza ao cliente final.

## 2. Valor entregue

- Canal de venda com marca própria, sem comissão de marketplace
- Pagamento online integrado, reduzindo atrito no fechamento
- Medição ponta a ponta da meta de 25 minutos
- Rastreio pelo cliente, reduzindo ligações à loja
- Base de clientes própria, com endereço salvo e histórico

## 3. Histórias

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-130](./US-130-Canal-publico-de-pedido-com-marca-propria.md) | Canal publico de pedido com marca propria | M | 13 | RF-DEL-01 |
| [US-131](./US-131-Zonas-de-entrega-e-taxa.md) | Zonas de entrega e taxa | M | 8 | RF-DEL-02 |
| [US-132](./US-132-Prazo-dinamico-ao-cliente-de-delivery.md) | Prazo dinamico ao cliente de delivery | M | 5 | RF-DEL-03 |
| [US-133](./US-133-Acompanhamento-de-status-pelo-cliente.md) | Acompanhamento de status pelo cliente | M | 5 | RF-DEL-04 |
| [US-134](./US-134-Pagamento-online-integrado.md) | Pagamento online integrado | M | 13 | RF-CXA-09 |
| [US-135](./US-135-Endereco-salvo-e-repetir-pedido.md) | Endereco salvo e repetir pedido | S | 5 | RF-DEL-05 |
| [US-136](./US-136-Atribuicao-e-app-do-entregador.md) | Atribuicao e app do entregador | M | 8 | RF-DEL-06, RF-DEL-07, RF-DEL-10 |
| [US-137](./US-137-Aviso-de-pedido-proximo-de-sair.md) | Aviso de pedido proximo de sair | S | 3 | RF-DEL-08 |
| [US-138](./US-138-Agrupamento-de-entregas-proximas.md) | Agrupamento de entregas proximas | C | 8 | RF-DEL-09 |

## 4. Ordem de execução recomendada

1. US-131 — zonas de entrega e taxa (define onde se pode vender)
2. US-130 — canal público de pedido
3. US-134 — pagamento online
4. US-132 — prazo dinâmico ao cliente
5. US-133 — acompanhamento de status
6. US-136 — atribuição e app do entregador
7. US-135 — endereço salvo e repetir pedido
8. US-137 — aviso de pedido próximo de sair
9. US-138 — agrupamento de entregas

## 5. Dependências do épico

**Depende de:** E-01, E-03, E-05, E-11  
**Habilita:** —

## 6. Definition of Done do épico

- [ ] Pedido de delivery completo, do carrinho à entrega concluída
- [ ] Pagamento online funcionando com o provedor definido
- [ ] Tempo total de delivery medido, com p90 acompanhado contra a meta de 25 minutos
- [ ] Entregador registrando saída e conclusão pelo celular
- [ ] Degradação sem internet comunicada de forma explícita

## 7. Riscos do épico

| Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|
| Credenciais e modalidade do Mercado Pago não definidas | Alta | Alto | Dependência externa do PRD 8 — bloqueia US-134 |
| Gestão de entregadores (próprios ou terceirizados) não definida | Alta | Médio | Pendência da Visão Geral 6.2 (M5) — define o desenho da US-136 |
| Meta de 25 minutos ser requisito do sistema em vez de objetivo de negócio | Média | Alto | Pendência do PRD 7 — confirmar antes de assumir compromisso contratual |

---

*Épico E-13 · Pacote 004_DonaBetinha · Replay Studio.*