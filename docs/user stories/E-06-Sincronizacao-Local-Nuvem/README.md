# E-06 · Sincronizacao Local-Nuvem

> ❌ **Épico cancelado em 06/08/2026.** Mudança de foco de negócio: o produto (agora iMenu) passa a operar 100% online, sem servidor local por loja e sem sincronização edge↔nuvem. Ver [ADR-040](../../adrs/ADR-040-arquitetura-100-online-api-unica.md) e [E-16 · iMenu Online](../E-16-iMenu-Online/README.md). As 9 histórias abaixo permanecem no repositório como registro histórico, cada uma com banner de cancelamento.

|  |  |
|---|---|
| **Fase** | 1 — MVP |
| **Histórias** | 9 |
| **Pontos** | 55 |
| **Sprints previstas** | Sprint 6 |
| **Aplicações afetadas** | api-edge, api-cloud |
| **Pacotes do monorepo** | packages/sync, packages/events, packages/db |

---

## 1. Objetivo do épico

Construir o componente mais delicado da arquitetura: a ponte entre a loja e a nuvem.

A ordem de construção é deliberada. O sync é a peça mais arriscada do MVP e só faz sentido construí-lo **depois** que o fluxo operacional já existe e gera eventos reais — construir antes significa testar a sincronização com dados fabricados, que é a forma mais eficiente de descobrir os problemas tarde.

O padrão é **outbox/inbox com log append-only**. Quatro garantias precisam ser demonstráveis, não presumidas: nada se perde, nada duplica, a ordem é preservada e o horário de ocorrência sobrevive.

## 2. Valor entregue

- Nenhum dado operacional se perde, mesmo com horas de internet caída
- Reenvio de lote não duplica registro — idempotência garantida por `event_id`
- Métrica correta mesmo com sincronização atrasada, pela preservação de `occurredAt`
- Cardápio, preço e configuração descendo da nuvem para a loja automaticamente
- Gestor e plataforma avisados quando o atraso ultrapassa o limite

## 3. Histórias

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-060](./US-060-Outbox-transacional.md) | Outbox transacional | M | 8 | RF-OFF-02 |
| [US-061](./US-061-Worker-de-envio-com-retry-e-cursor.md) | Worker de envio com retry e cursor | M | 8 | RF-OFF-02 |
| [US-062](./US-062-Recepcao-idempotente-na-nuvem.md) | Recepcao idempotente na nuvem | M | 8 | RF-OFF-03 |
| [US-063](./US-063-Pull-de-cardapio-e-configuracao.md) | Pull de cardapio e configuracao | M | 5 | RF-OFF-02 |
| [US-064](./US-064-Preservacao-do-horario-de-ocorrencia.md) | Preservacao do horario de ocorrencia | M | 5 | RF-OFF-04 |
| [US-065](./US-065-Indicador-de-conexao-e-atraso-de-sincronizacao.md) | Indicador de conexao e atraso de sincronizacao | M | 5 | RF-OFF-05, RF-BI-14 |
| [US-066](./US-066-Alerta-de-atraso-de-sincronizacao.md) | Alerta de atraso de sincronizacao | M | 3 | RF-OFF-06 |
| [US-067](./US-067-Registro-e-revisao-de-conflitos.md) | Registro e revisao de conflitos | M | 5 | RF-OFF-07 |
| [US-068](./US-068-Recuperacao-apos-reconexao-longa.md) | Recuperacao apos reconexao longa | M | 8 | RF-OFF-02, RF-OFF-03 |

## 4. Ordem de execução recomendada

1. US-060 — outbox transacional, a fundação
2. US-064 — preservação de `occurredAt` (define o contrato de horário)
3. US-061 — worker de envio com retry e cursor
4. US-062 — recepção idempotente na nuvem
5. US-063 — pull de cardápio e configuração
6. US-065 — indicador de conexão e atraso
7. US-066 — alerta de atraso
8. US-067 — registro e revisão de conflitos
9. US-068 — recuperação após reconexão longa

## 5. Dependências do épico

**Depende de:** E-00, E-03, E-05  
**Habilita:** E-07, E-12, E-14

## 6. Definition of Done do épico

- [ ] 6 horas offline com 4.000 eventos sincronizando em menos de 5 minutos
- [ ] Reenvio do mesmo lote não duplicando nenhum registro
- [ ] `occurredAt` preservado e métricas corretas por faixa horária após sincronização atrasada
- [ ] Cursor persistido nos dois lados, com retomada automática
- [ ] HMAC validado em toda requisição de sync
- [ ] Conflitos registrados e revisáveis pelo gestor

## 7. Riscos do épico

| Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|
| Complexidade do sync atrasar o MVP | Alta | Alto | Risco T6 do doc. 02 — fatiar: Fase 1 sincroniza só pedido e pagamento; demais domínios depois |
| Divergência de dados após sincronização longa | Média | Alto | Risco T2 — movimentos em vez de saldos (ADR-008); verificação de integridade diária |
| Ordem de eventos quebrada por relógio dessincronizado | Média | Alto | `deviceSeq` monotônico por instalação, independente de relógio (ADR-034) |

---

*Épico E-06 · Pacote 004_DonaBetinha · Replay Studio.*