# E-01 · Catalogo e Cardapio

|  |  |
|---|---|
| **Fase** | 1 — MVP |
| **Histórias** | 8 |
| **Pontos** | 42 |
| **Sprints previstas** | Sprint 1 e 2 |
| **Aplicações afetadas** | web-admin, web-menu, web-pos, api-cloud, api-edge |
| **Pacotes do monorepo** | packages/domain, packages/db, packages/contracts, packages/ui |

---

## 1. Objetivo do épico

Construir o catálogo que alimenta todos os canais de venda. É o épico que define **o que pode ser pedido, por quanto, com quais opções, em quanto tempo e em qual praça de produção** — e é pré-requisito de qualquer pedido.

A complexidade real aqui não é o CRUD. É a pizza meio a meio: um item com frações de peso variável, regra de precificação configurável e baixa proporcional de estoque. Modelar isso como "campo sabor 1 e campo sabor 2" custaria a reescrita do modelo na Fase 2, quando a ficha técnica chegasse.

## 2. Valor entregue

- Cardápio único alimentando mesa, garçom e delivery, sem duplicação de cadastro
- Suporte estrutural a meio a meio com 2, 3 ou 4 sabores, sem mudança de schema
- Preço por canal, permitindo margem diferente no salão e no delivery
- Indisponibilidade propagada a todos os canais em até 2 segundos
- Tempo de preparo e praça por produto — insumo do roteamento (E-03) e do fire time (E-11)

## 3. Histórias

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-010](./US-010-Cadastrar-categorias-e-produtos.md) | Cadastrar categorias e produtos | M | 5 | RF-CAT-01 |
| [US-011](./US-011-Variacoes-de-produto-com-preco-proprio.md) | Variacoes de produto com preco proprio | M | 5 | RF-CAT-02 |
| [US-012](./US-012-Grupos-de-modificadores.md) | Grupos de modificadores | M | 8 | RF-CAT-03 |
| [US-013](./US-013-Pizza-meio-a-meio-com-fracoes.md) | Pizza meio a meio com fracoes | M | 8 | RF-CAT-04, RF-CAT-05 |
| [US-014](./US-014-Preco-por-canal-de-venda.md) | Preco por canal de venda | M | 3 | RF-CAT-06 |
| [US-015](./US-015-Marcar-produto-indisponivel-com-propagacao-imediata.md) | Marcar produto indisponivel com propagacao imediata | M | 5 | RF-CAT-07 |
| [US-016](./US-016-Tempo-de-preparo-e-praca-por-produto.md) | Tempo de preparo e praca por produto | M | 3 | RF-CAT-08, RF-CAT-09 |
| [US-017](./US-017-Cadastro-de-pracas-de-producao.md) | Cadastro de pracas de producao | M | 5 | RF-CAT-09, RF-KDS-06 |

## 4. Ordem de execução recomendada

1. US-017 — praças de produção, porque produto precisa de destino
2. US-010 — categorias e produtos
3. US-011 — variações com preço próprio
4. US-014 — preço por canal
5. US-012 — grupos de modificadores
6. US-013 — pizza meio a meio (depende de variação e de modificadores)
7. US-016 — tempo de preparo e praça por produto
8. US-015 — indisponibilidade com propagação em tempo real

## 5. Dependências do épico

**Depende de:** E-00  
**Habilita:** E-02, E-03, E-04, E-10

## 6. Definition of Done do épico

- [ ] Cardápio completo da Dona Betinha cadastrado e validado pelo cliente
- [ ] Meio a meio funcionando com as três regras de precificação
- [ ] Preço por canal validado com pelo menos dois canais ativos
- [ ] Indisponibilidade propagando por WebSocket em menos de 2 segundos
- [ ] Todo produto com tempo de preparo e praça definidos
- [ ] Cardápio disponível offline no edge

## 7. Riscos do épico

| Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|
| Carga inicial do cardápio depender de disponibilidade do cliente | Alta | Médio | Iniciar a carga em paralelo à Sprint 1; solicitar cardápio com preços na lista de materiais do PRD 8 |
| Regra de precificação de meio a meio não confirmada pelo cliente | Média | Médio | RN-009 está marcada como [HIPÓTESE]; implementar as três regras e deixar configurável |
| Fotos de produto pesadas degradarem o carregamento em 4G | Média | Médio | Pipeline de otimização de imagem no upload; servir por CDN em formatos modernos |

---

*Épico E-01 · Pacote 004_DonaBetinha · Replay Studio.*