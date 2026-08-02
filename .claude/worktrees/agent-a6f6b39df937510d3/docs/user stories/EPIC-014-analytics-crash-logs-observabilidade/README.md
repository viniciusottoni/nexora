---
title: EPIC-014 — Analytics, Crash, Logs e Observabilidade
sidebar_position: 14
---

# EPIC-014 — Analytics, Crash, Logs e Observabilidade

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | EPIC-014 |
| Fase | MVP Android Fitness Gamificado |
| Prioridade | P0 |
| Perfil principal | Produto, Engenharia e QA |
| Planos impactados | Trial, Mensal e Anual |
| Integrações principais | Firebase Analytics, Crashlytics e logs de backend |
| Status | Planejado |

## 2. Objetivo

Coletar eventos, falhas e logs suficientes para medir ativação, retenção, uso de quests, impacto da gamificação, expiração do trial, conversão para assinatura e estabilidade do app.

## 3. Contexto de produto

O MVP precisa validar hipóteses rapidamente. Sem analytics e observabilidade, não será possível entender abandono, engajamento, conversão e falhas críticas.

## 4. Escopo

### Entra neste épico

- Eventos de onboarding.
- Eventos de trial, planos e paywall.
- Eventos de geração, início e conclusão de quest.
- Eventos de XP, penalidade, level, rank, streak e atributos.
- Eventos de dungeon e itens.
- Crashlytics.
- Logs de backend com rastreabilidade básica.
- Identificação de quedas em funis críticos.

### Fora deste épico

- BI avançado.
- Data warehouse.
- A/B tests.
- Segmentação avançada.

## 5. User Stories relacionadas

| ID | Título | Prioridade | Documento |
|---|---|---|---|
| US-096 | Rastrear eventos de onboarding | P0 | [Abrir](./US-096-eventos-onboarding.md) |
| US-097 | Rastrear geração, início e conclusão de quest | P0 | [Abrir](./US-097-eventos-quest.md) |
| US-098 | Rastrear XP, penalidade de XP, level up, rank up e streak | P0 | [Abrir](./US-098-eventos-xp-level-rank-streak.md) |
| US-099 | Rastrear visualização de trial, planos e paywall | P0 | [Abrir](./US-099-eventos-trial-planos-paywall.md) |
| US-100 | Registrar falhas no Crashlytics | P0 | [Abrir](./US_100-falhas-app.md) |
| US-101 | Registrar logs com correlationId no backend | P0 | [Abrir](./US-101-logs-correlation-id-backend.md) |
| US-102 | Identificar queda em funis críticos | P1 | [Abrir](./US-102-identificar-queda-funis-criticos.md) |
| US-125 | Rastrear início, contagem e expiração do trial | P0 | [Abrir](./US_125-trial.md) |
| US-126 | Rastrear escolha de plano mensal ou anual | P0 | [Abrir](./US-126-eventos-plano-mensal-anual.md) |
| US-136 | Rastrear level up de atributo e qual atributo evoluiu | P0 | [Abrir](./US_136-level-up-atributo.md) |
| US-137 | Rastrear quest diária não completada e penalidade de XP aplicada | P0 | [Abrir](./US_137-quest-nao-completada-xp.md) |
| US-138 | Rastrear item ganho em dungeon | P0 | [Abrir](./US_138-item-ganho-dungeon.md) |
| US-139 | Rastrear ativação de dungeon pelo usuário | P0 | [Abrir](./US_139-ativacao-dungeon.md) |

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-EPIC-014-001 | Todo evento deve ter nome padronizado e payload mínimo. |
| RN-EPIC-014-002 | Eventos não devem expor dados sensíveis de saúde ou limitações físicas. |
| RN-EPIC-014-003 | Fluxos P0 devem possuir eventos de início, sucesso e falha quando aplicável. |
| RN-EPIC-014-004 | Falhas devem ser capturadas desde os testes internos. |
| RN-EPIC-014-005 | Logs de backend devem permitir investigar erros sem expor dados pessoais desnecessários. |

## 7. Impactos técnicos

### Flutter

- Integração Firebase Analytics.
- Integração de falhas Firebase.
- Serviço central de tracking.
- Eventos em telas e ações críticas.

### Backend

- Logs estruturados.
- CorrelationId em requisições críticas.
- Logs de mudanças comerciais e conclusão de quest.
- Tratamento padronizado de erro.

### Banco de dados

- Sem entidade obrigatória para MVP, salvo auditoria se definida.

### Analytics

Eventos principais:

- `trial_started`.
- `trial_expired`.
- `paywall_after_trial_viewed`.
- `subscription_started`.
- `daily_quest_generated`.
- `dungeon_generated`.
- `dungeon_viewed`.
- `quest_started`.
- `exercise_completed`.
- `quest_completed`.
- `daily_quest_missed`.
- `xp_penalty_applied`.
- `xp_earned`.
- `attribute_level_up`.
- `item_earned`.
- `level_up`.
- `rank_up`.
- `streak_updated`.
- `hunter_card_shared`.

### QA

- Validar disparo dos eventos críticos.
- Validar relatório de falha em ambiente de teste.
- Validar logs de erro de API.
- Validar que dados sensíveis não são enviados em eventos.

## 8. Dependências

- Firebase configurado.
- Backend com middleware de logs.
- Fluxos comerciais, onboarding e quest implementados.

## 9. Critérios de aceite do épico

- Eventos P0 são disparados corretamente.
- Falhas são capturadas.
- Logs de backend ajudam a investigar erro.
- Eventos comerciais medem trial e assinatura.
- Eventos de dungeon, penalidade de XP, atributos e itens são rastreados.
- Não há exposição indevida de dados sensíveis.

## 10. Decisão registrada

Analytics e observabilidade são obrigatórios no MVP para medir produto, retenção, conversão e estabilidade antes de escalar aquisição de usuários.
