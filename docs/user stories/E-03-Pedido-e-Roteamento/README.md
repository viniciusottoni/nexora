# E-03 · Pedido e Roteamento

|  |  |
|---|---|
| **Fase** | 1 — MVP |
| **Histórias** | 6 |
| **Pontos** | 53 |
| **Sprints previstas** | Sprint 1 (domínio) e Sprint 2 (API e roteamento) |
| **Aplicações afetadas** | web-menu, web-pos, api-edge, api-cloud |
| **Pacotes do monorepo** | packages/domain, packages/events, packages/contracts, packages/db |

---

## 1. Objetivo do épico

Resolver a dor central declarada na descoberta: ***"o pedido é feito e não chega para cozinha"***.

Este é o épico do núcleo. O pedido é a entidade da qual derivam quatro coisas ao mesmo tempo — estado operacional, métrica, alerta e sincronização (doc. 04, seção 1). Errar a modelagem aqui contamina todo o resto do produto.

Três decisões estruturam o épico: **idempotência obrigatória** em toda escrita (ADR-020), porque em rede instável o garçom toca "enviar" duas vezes; **seis carimbos de tempo por item** (T0 a T5), porque média única esconde o gargalo; e **operação integralmente offline**, porque a loja não pode parar.

## 2. Valor entregue

- Nenhum pedido se perde entre salão e cozinha — meta declarada de 100%
- Cada etapa cronometrada com autor, dispositivo e horário de ocorrência
- Cancelamento com autorização e motivo, rastreável na auditoria
- Operação completa com internet caída, sem funcionalidade bloqueada
- Base de eventos da qual nascem todas as métricas do painel do dono

## 3. Histórias

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-030](./US-030-Criar-pedido-com-itens-modificadores-e-fracoes.md) | Criar pedido com itens modificadores e fracoes | M | 13 | RF-PED-01, RF-PED-08, RF-SAL-03, RF-SAL-04 |
| [US-031](./US-031-Roteamento-simultaneo-para-cozinha-e-caixa.md) | Roteamento simultaneo para cozinha e caixa | M | 8 | RF-KDS-01, RF-CXA-01 |
| [US-032](./US-032-Carimbos-de-tempo-T0-a-T5.md) | Carimbos de tempo T0 a T5 | M | 8 | RF-PED-02, RF-PED-03 |
| [US-033](./US-033-Cancelar-item-ou-pedido-com-autorizacao.md) | Cancelar item ou pedido com autorizacao | M | 8 | RF-PED-04, RF-PED-05 |
| [US-034](./US-034-Operar-pedido-integralmente-offline.md) | Operar pedido integralmente offline | M | 13 | RF-PED-09, RF-OFF-01, RF-OFF-02, RF-OFF-05 |
| [US-035](./US-035-Bloquear-fechamento-com-item-pendente.md) | Bloquear fechamento com item pendente | S | 3 | RF-PED-06 |

## 4. Ordem de execução recomendada

1. US-032 — carimbos de tempo, porque definem o modelo do item
2. US-030 — criação de pedido com itens, modificadores e frações
3. US-031 — roteamento simultâneo para cozinha e caixa
4. US-033 — cancelamento com autorização
5. US-034 — operação offline de ponta a ponta
6. US-035 — bloqueio de fechamento com item pendente

## 5. Dependências do épico

**Depende de:** E-00, E-01, E-02  
**Habilita:** E-04, E-05, E-06, E-07, E-10

## 6. Definition of Done do épico

- [ ] Pedido criado pela mesa e pelo garçom chegando ao KDS em menos de 2 s
- [ ] Idempotência validada com reenvio real em rede instável
- [ ] Os seis carimbos gravados com autor, dispositivo e `occurredAt`
- [ ] Cancelamento com autorização de perfil superior funcionando
- [ ] Fluxo completo salão→cozinha→caixa operando com internet derrubada
- [ ] Máquina de estados do documento 04 implementada com transições proibidas bloqueadas

## 7. Riscos do épico

| Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|
| Modelagem de item sem os seis carimbos inviabilizar o diagnóstico de gargalo | Baixa | Crítico | Decisão registrada (ERD, decisão 5); constraint `ck_item_sequence` impede duração negativa |
| Idempotência mal implementada duplicar pedidos na cozinha | Média | Alto | Chave de idempotência obrigatória, guardada 24 h; teste de reenvio no CI |
| Latência acima de 2 s no roteamento em rede Wi-Fi ruim | Média | Alto | Rede cabeada para KDS e caixa; risco T3 do doc. 02 |

---

*Épico E-03 · Pacote 004_DonaBetinha · Replay Studio.*