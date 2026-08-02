---
title: US-207 — Refatorar jobs recorrentes com paginação e batching
sidebar_position: 207
---

# US-207 — Refatorar jobs recorrentes com paginação e batching

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-207 |
| Épico | EPIC-020 — Hardening de Segurança, Performance e Escalabilidade do MVP |
| Prioridade | P0 |
| Fase | Bloqueador do MVP em produção |
| Perfil principal | Worker, notificações, progressão, banco e push |
| Plano | Trial, Mensal e Anual |
| Dependência principal | Hangfire, DailyQuestPenaltyJob, DailyQuestReminderJob, MissedDailyQuestNotificationJob |
| Status | Planejada |

## 2. História do usuário

Como **usuário do AWAKEN**,

quero **que jobs de lembrete, streak e penalidade rodem de forma confiável mesmo com muitos usuários**,

para **receber notificações e manter minha progressão sem lentidão ou falhas em massa**.

## 3. Contexto

Jobs recorrentes não devem carregar toda a base em memória nem executar múltiplas consultas por usuário em loop. Para o MVP em produção, esses jobs precisam operar por páginas, lotes e checkpoints, permitindo retentativa idempotente.

## 4. Objetivo

Refatorar jobs recorrentes para processamento em lote, com paginação, projeções leves, checkpoints e salvamento incremental.

## 5. Escopo

### Entra nesta US

- Processar notificações em páginas.
- Processar penalidades diárias em páginas.
- Evitar N+1 queries em jobs.
- Usar projeções com dados necessários para elegibilidade.
- Salvar progresso por lote.
- Garantir idempotência por usuário/data/tipo.
- Registrar métricas de quantidade, duração e falhas.

### Fora desta US

- Motor avançado de fila distribuída fora do Hangfire.
- Personalização avançada de horário por IA.
- Campanhas marketing/push promocional.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Job não pode carregar toda a base elegível em memória. |
| RN-002 | Job deve salvar progresso por lote. |
| RN-003 | Reexecução do mesmo job não pode duplicar penalidade, log ou notificação indevida. |
| RN-004 | Falha em um usuário não pode interromper todo o lote. |
| RN-005 | Envio push deve respeitar consentimento e limite diário. |
| RN-006 | Penalidade diária deve continuar respeitando acesso ativo. |

## 7. Modelo de processamento sugerido

```txt
batchSize: 500 a 1000
checkpoint: data + último Id processado
saveChanges: por lote
retry: por lote ou item, conforme criticidade
```

## 8. Fluxo principal

1. Job inicia com data de referência.
2. Busca candidatos em página ordenada por chave estável.
3. Processa cada candidato com regras de elegibilidade.
4. Registra decisão/resultado.
5. Salva lote.
6. Avança checkpoint.
7. Repete até finalizar.

## 9. Fluxos alternativos

- Falha no envio push: registra falha e continua.
- Falha no banco: job falha e reexecuta a partir de checkpoint seguro.
- Lote vazio: job finaliza sem erro.

## 10. Impacto no Backend

- Criar métodos de consulta paginada para jobs.
- Substituir `GetAllWithPushEnabledAsync` por busca em lotes.
- Refatorar handlers de notificação e penalidade.
- Adicionar métricas e logs por lote.

## 11. Impacto no Banco

Índices sugeridos:

```txt
quests(Type, QuestDateUtc, PenaltyCheckedAtUtc, Status, Id)
notification_preferences(PushEnabled, PreferredReminderTime, UserId)
notification_logs(UserId, NotificationType, AttemptedAtUtc)
```

## 12. Impacto no Flutter

Sem impacto direto.

## 13. Critérios de aceite

- Job de lembrete processa usuários em lotes.
- Job de penalidade processa quests em lotes.
- Nenhum job carrega toda a base elegível em memória.
- Falha individual não interrompe todo o processamento.
- Reexecução não duplica efeitos.
- Métricas de duração, processados, enviados, ignorados e falhas são registradas.

## 14. Critérios de teste para QA

- base simulada com milhares de preferências;
- base simulada com milhares de quests pendentes;
- falha parcial de push;
- reexecução do job;
- cancelamento por timeout;
- validação de logs e métricas.

## ✅ Decisão registrada

Jobs recorrentes do MVP devem ser paginados, idempotentes e executados em lotes; loops que carregam toda a base ficam proibidos para produção.