---
title: US-172 — Auditar e corrigir UTC em todo o código
sidebar_position: 172
---

# US-172 — Auditar e corrigir UTC em todo o código

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-172 |
| Épico | EPIC-018 — Qualidade, Localização, Economia e Hardening Pós-MVP |
| Prioridade | P0 |
| Fase | Endurecimento pré–teste aberto |
| Perfil principal | Engenharia e QA |
| Plataforma | Flutter Android + Backend .NET 10 |
| Status | Planejada |

## 2. História do usuário

Como **time de engenharia**,
quero **auditar e corrigir o uso de datas em todo o código**,
para **garantir que regras de negócio não dependam do relógio local do aparelho ou do servidor**.

## 3. Contexto

A US-016 cobre apenas o recorte de datas do ciclo de trial. Esta US cobre o restante do app: quests, streak, histórico, notificações, logs, inventário e demais fluxos.

## 4. Objetivo

Padronizar persistência e decisão de negócio em UTC, com exibição localizada apenas na camada de apresentação.

## 5. Escopo

### Entra nesta US

- Auditar usos diretos de `DateTime.UtcNow`, `DateTime.Now` e `DateTime.now()`.
- Substituir usos de domínio/backend por `IDateTimeService` ou serviço equivalente.
- Garantir persistência em UTC.
- Garantir exibição em fuso local apenas no app.
- Cobrir quests, streak, histórico, notificações, logs e inventário.

### Fora desta US

- Recorte de trial já tratado na US-016.
- Mudança de regras comerciais de assinatura.
- Observabilidade administrativa, proposta para EPIC-017.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Toda data persistida deve estar em UTC. |
| RN-002 | Backend é a fonte de verdade para regras de negócio baseadas em data. |
| RN-003 | Flutter pode exibir em fuso local, mas não decidir regra crítica pelo relógio do aparelho. |
| RN-004 | Domínio não deve depender de chamadas diretas ao relógio do sistema. |
| RN-005 | Testes devem cobrir mudança de dia, fuso e virada de data. |

## 7. Fluxo principal

1. Engenharia mapeia usos de data/hora no backend e Flutter.
2. Classifica usos como regra de negócio, persistência ou apresentação.
3. Substitui chamadas diretas por serviço de tempo quando necessário.
4. Ajusta serialização/contratos para UTC.
5. QA valida múltiplos fusos e virada de dia.

## 8. Impacto Flutter

- Criar/centralizar helper de formatação local.
- Remover decisão de regra crítica baseada em `DateTime.now()`.
- Exibir datas do backend no fuso local do usuário.

## 9. Impacto Backend

- Usar `IDateTimeService` em serviços e domínio.
- Persistir datas em UTC.
- Normalizar payloads de API com UTC.
- Testes unitários com relógio controlado.

## 10. Impacto DB

- Verificar colunas de data/hora.
- Garantir consistência UTC para logs, quests, streak, trial fora da US-016, tickets e inventário.

## 11. Analytics e logs

- Eventos devem receber timestamps consistentes.
- Logs devem usar UTC para correlação.

## 12. Critérios de aceite

### CA-001 — Persistência UTC

Dado que uma data é persistida,
quando ela for gravada no banco,
então deve estar em UTC.

### CA-002 — Sem regra crítica no relógio local

Dado que o app está em fuso diferente do servidor,
quando uma regra de streak ou quest for avaliada,
então a decisão deve vir do backend.

## 13. Critérios de teste QA

- testar UTC-3, UTC+0 e UTC+9;
- testar virada de dia;
- testar quest diária;
- testar streak;
- testar histórico;
- testar notificação agendada.

## 14. Decisão registrada

> Datas de regra de negócio pertencem ao backend em UTC. O app apenas apresenta a informação no fuso local.
