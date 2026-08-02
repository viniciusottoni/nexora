---
title: US-098 — Rastrear XP, penalidade de XP, level up, rank up e streak
sidebar_position: 98
---

# US-098 — Rastrear XP, penalidade de XP, level up, rank up e streak

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-098 |
| Épico | EPIC-014 — Analytics, Crash, Logs e Observabilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Produto, Engenharia e QA |
| Integrações | Firebase Analytics e logs de backend |
| Status | Planejada |

## 2. História do usuário

Como **time de produto**, quero **rastrear evolução de XP, level, rank e streak**, para **entender se a gamificação está motivando retorno e progressão**.

## 3. Contexto

XP, level, rank e streak são centrais no AWAKEN. O MVP precisa medir ganhos, perdas, evolução e pontos de abandono sem expor dados pessoais sensíveis.

## 4. Objetivo

Registrar eventos de XP ganho, penalidade, level up, rank up e atualização de streak.

## 5. Escopo

### Entra nesta US

- XP ganho por fonte.
- Penalidade de XP aplicada.
- Level up do Hunter.
- Rank up.
- Streak atualizado.
- Fonte da recompensa: quest, dungeon, raid, penalty.

### Fora desta US

- Fórmula completa de progressão em analytics.
- Dados sensíveis do perfil físico.
- BI avançado.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Evento `xp_earned` deve ter `source` e `amount`. |
| RN-002 | Penalidade deve ter evento próprio. |
| RN-003 | Level up e rank up devem ser rastreados apenas quando realmente ocorrerem. |
| RN-004 | Atualização de streak deve ser rastreada sem expor rotina sensível. |
| RN-005 | Eventos devem ser idempotentes quando a conclusão da quest for repetida. |

## 7. Eventos sugeridos

| Evento | Quando dispara |
|---|---|
| xp_earned | Quando XP é ganho. |
| xp_penalty_applied | Quando penalidade é aplicada. |
| level_up | Quando o Hunter sobe de nível. |
| rank_up | Quando o Hunter sobe de rank. |
| streak_updated | Quando streak muda. |

## 8. Payload mínimo

```json
{
  "source": "quest",
  "amount": 120,
  "quest_type": "daily",
  "new_level": 4,
  "new_rank": "D"
}
```

## 9. Impacto Flutter

- Disparar eventos quando a resposta de recompensa confirmar mudança.
- Não recalcular recompensa no app.
- Garantir que eventos não sejam duplicados em refresh de tela.

## 10. Impacto Backend

- Fonte de verdade para cálculo de XP e progressão.
- Logs de aplicação de XP e penalidade.
- Idempotência por QuestLog.

## 11. Impacto QA

- XP ganho.
- Penalidade aplicada.
- Level up.
- Rank up.
- Streak atualizado.
- Ausência de duplicidade.

## 12. Critérios de aceite

### CA-001 — XP rastreado

Dado que o usuário ganhou XP,
Quando a recompensa for aplicada,
Então deve existir evento `xp_earned` com fonte e quantidade.

### CA-002 — Penalidade rastreada

Dado que uma penalidade foi aplicada,
Quando o processamento terminar,
Então deve existir evento `xp_penalty_applied`.

## 13. Decisão registrada

A gamificação só será validada se XP, penalidade, level, rank e streak forem mensurados com eventos consistentes e sem duplicidade.
