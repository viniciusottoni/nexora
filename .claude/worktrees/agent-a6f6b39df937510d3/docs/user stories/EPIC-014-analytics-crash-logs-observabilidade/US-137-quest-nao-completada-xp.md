---
title: US-137 — Rastrear quest diária não completada
sidebar_position: 137
---

# US-137 — Rastrear quest diária não completada e ajuste de XP aplicado

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-137 |
| Épico | EPIC-014 — Analytics, Crash, Logs e Observabilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Produto e Engenharia |
| Integrações | Firebase Analytics e logs de backend |
| Status | Planejada |

## 2. História do usuário

Como **time de produto**, quero **rastrear quando a quest diária não foi completada e houve ajuste negativo de XP**, para **medir abandono diário e calibrar a mecânica de retenção**.

## 3. Objetivo

Registrar evento de quest diária não completada e evento de ajuste de XP aplicado, com quantidade e origem.

## 4. Escopo

### Entra nesta US

- Rastrear quest diária não concluída.
- Rastrear ajuste negativo de XP aplicado.
- Registrar quantidade do ajuste.
- Registrar origem como daily quest missed.
- Evitar duplicidade em reprocessamento.

### Fora desta US

- Fórmula completa do ajuste em analytics.
- Dados sensíveis do perfil.
- Mensagens negativas para o usuário.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Quest diária não completada deve gerar evento `daily_quest_missed`. |
| RN-002 | Ajuste de XP deve gerar evento `xp_penalty_applied`. |
| RN-003 | Evento deve informar `amount`. |
| RN-004 | A mesma quest não pode duplicar ajuste nem evento. |

## 6. Eventos sugeridos

| Evento | Quando dispara |
|---|---|
| daily_quest_missed | Quando a quest diária anterior não foi concluída. |
| xp_penalty_applied | Quando o ajuste de XP é aplicado. |

## 7. Payload mínimo

```json
{
  "source": "daily_quest_missed",
  "amount": 25,
  "quest_date": "2026-06-27"
}
```

## 8. Impacto Backend

- Job pós-virada de dia.
- Aplicação idempotente do ajuste.
- Logar quest não completada e XP ajustado.

## 9. Impacto Flutter

- Não recalcular ajuste.
- Exibir informação apenas a partir do backend quando necessário.

## 10. Critérios de aceite

### CA-001 — Quest não completada rastreada

Dado que o usuário não concluiu a quest diária,
Quando o job processar o dia anterior,
Então deve registrar `daily_quest_missed`.

### CA-002 — Ajuste de XP rastreado

Dado que o ajuste foi aplicado,
Quando o processamento terminar,
Então deve registrar `xp_penalty_applied` com `amount`.

## 11. Decisão registrada

Quest diária não completada e ajuste de XP devem ser medidos para calibrar retenção sem expor dados sensíveis.
