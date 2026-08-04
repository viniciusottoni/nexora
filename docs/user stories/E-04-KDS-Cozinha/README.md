# E-04 · KDS Cozinha

|  |  |
|---|---|
| **Fase** | 1 — MVP |
| **Histórias** | 9 |
| **Pontos** | 50 |
| **Sprints previstas** | Sprint 4 |
| **Aplicações afetadas** | web-kds, api-edge |
| **Pacotes do monorepo** | packages/domain, packages/contracts, packages/ui |

---

## 1. Objetivo do épico

Entregar o painel de cozinha que substitui o papel e cronometra a produção. A referência declarada pelo cliente é o KDS do McDonald's: *pedido chega direto, com controle de produção*.

O contexto de uso define tudo aqui. Persona P3 — mãos ocupadas, sujas, sob pressão, calor e ruído. Isso impõe restrições que não são negociáveis: **operação por teclado numérico**, zero digitação livre, alvos grandes, alto contraste, resposta visual em menos de 300 ms e legibilidade a 1,5 metro de distância.

Um KDS que exige mouse ou leitura atenta é um KDS que a cozinha abandona na primeira semana de pico.

## 2. Valor entregue

- Fim do pedido em papel entre salão e cozinha
- Cronômetro visível por pedido — responde diretamente ao "quantos minutos minha pizza tá sendo feita"
- Registro obrigatório de início e conclusão, origem de MET-002 a MET-007
- Escalonamento de cor que aponta o atraso antes de o cliente reclamar
- Sinalização de falta de insumo propagada a todos os canais em 2 segundos

## 3. Histórias

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-040](./US-040-Fila-de-pedidos-com-cartoes-e-cronometro.md) | Fila de pedidos com cartoes e cronometro | M | 13 | RF-KDS-02, RF-KDS-03, RF-KDS-05 |
| [US-041](./US-041-Avancar-estado-com-um-toque-via-teclado-numerico.md) | Avancar estado com um toque via teclado numerico | M | 8 | RF-KDS-04, RF-KDS-05 |
| [US-042](./US-042-Filtro-por-praca-de-producao.md) | Filtro por praca de producao | M | 5 | RF-KDS-06 |
| [US-043](./US-043-Contagem-consolidada-all-day.md) | Contagem consolidada all-day | S | 5 | RF-KDS-07 |
| [US-044](./US-044-Marcar-item-indisponivel-pelo-KDS.md) | Marcar item indisponivel pelo KDS | M | 5 | RF-KDS-10 |
| [US-045](./US-045-Alerta-sonoro-de-pedido-novo-e-de-atraso.md) | Alerta sonoro de pedido novo e de atraso | M | 3 | RF-KDS-13 |
| [US-046](./US-046-Historico-do-turno-no-KDS.md) | Historico do turno no KDS | S | 3 | RF-KDS-14 |
| [US-047](./US-047-Modo-pico-com-simplificacao-automatica.md) | Modo pico com simplificacao automatica | C | 5 | — |
| [US-048](./US-048-Fallback-de-polling-se-WebSocket-cair.md) | Fallback de polling se WebSocket cair | M | 3 | RF-KDS-01 |

## 4. Ordem de execução recomendada

1. US-040 — fila com cartões e cronômetro, o coração do épico
2. US-041 — avanço de estado por teclado numérico
3. US-048 — fallback de polling (requisito, não otimização)
4. US-042 — filtro por praça
5. US-045 — sinais sonoros
6. US-044 — marcar item indisponível
7. US-043 — contagem all-day
8. US-046 — histórico do turno
9. US-047 — modo pico

## 5. Dependências do épico

**Depende de:** E-00, E-01, E-03  
**Habilita:** E-07, E-10, E-11

## 6. Definition of Done do épico

- [ ] Pedido aparecendo no KDS em menos de 2 s após a confirmação
- [ ] Avanço de estado por teclado numérico com resposta visual em menos de 300 ms
- [ ] Legibilidade validada a 1,5 m com 12 pedidos na fila, em monitor real
- [ ] Fallback de polling funcionando com o WebSocket derrubado
- [ ] Operação completa sem mouse, validada com a equipe da cozinha
- [ ] Todos os carimbos T1 a T4 sendo gravados corretamente

## 7. Riscos do épico

| Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|
| Hardware do KDS (monitor e teclado numérico) ainda indefinido | Média | Alto | Risco de backlog registrado no doc. 07 — validar teclado e monitor na Sprint 0 |
| Cozinha rejeitar a ferramenta por atrito de uso | Média | Crítico | Risco 13 da Visão Geral — validação com a equipe real antes do piloto; interface por código, sem digitação |
| Limiares mal calibrados deixarem tudo vermelho e a cor perder significado | Alta | Médio | Calibrar com dados reais nas duas primeiras semanas do piloto (US-016) |

---

*Épico E-04 · Pacote 004_DonaBetinha · Replay Studio.*