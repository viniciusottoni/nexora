---
title: US-136 — Rastrear level up de atributo
sidebar_position: 136
---

# US-136 — Rastrear level up de atributo e qual atributo evoluiu

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-136 |
| Épico | EPIC-014 — Analytics, Crash, Logs e Observabilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Produto e Engenharia |
| Integrações | Firebase Analytics e logs de backend |
| Status | Planejada |

## 2. História do usuário

Como **time de produto**, quero **rastrear level up de atributos**, para **entender quais atributos evoluem mais e se os treinos estão equilibrados**.

## 3. Contexto

O AWAKEN possui atributos como Força, Agilidade, Resistência, Vitalidade, Foco e Sabedoria. A evolução deles precisa ser medida para calibrar catálogo e quests.

## 4. Objetivo

Registrar evento sempre que um atributo sobe de nível, informando atributo e novo nível.

## 5. Escopo

### Entra nesta US

- Rastrear level up de cada atributo.
- Informar atributo evoluído.
- Informar novo nível.
- Informar fonte do ganho quando possível.
- Evitar duplicidade em reprocessamento.

### Fora desta US

- Envio de dados físicos sensíveis.
- Exposição de limitações físicas.
- Fórmula completa de progressão.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Evento deve informar `attribute`. |
| RN-002 | Evento deve informar `new_level`. |
| RN-003 | Evento deve ser emitido apenas quando houver level up real. |
| RN-004 | Sabedoria deve ser rastreada como atributo quando evoluir. |
| RN-005 | Reprocessamento não pode duplicar evento. |

## 7. Evento sugerido

| Evento | Quando dispara |
|---|---|
| attribute_level_up | Quando um atributo sobe de nível. |

## 8. Payload mínimo

```json
{
  "attribute": "strength",
  "new_level": 3,
  "source": "quest"
}
```

## 9. Impacto Flutter

- Exibir evolução recebida do backend.
- Disparar evento apenas após confirmação da recompensa.

## 10. Impacto Backend

- Calcular level up de atributo.
- Garantir idempotência por QuestLog.
- Logar evolução aplicada.

## 11. Critérios de aceite

### CA-001 — Atributo evoluiu

Dado que Força subiu de nível,
Quando a recompensa for aplicada,
Então deve ser enviado `attribute_level_up` com `attribute=strength`.

### CA-002 — Sem evolução

Dado que nenhum atributo subiu de nível,
Quando a recompensa for aplicada,
Então nenhum evento de level up de atributo deve ser enviado.

## 12. Decisão registrada

Evolução de atributos precisa ser rastreada para calibrar treinos e progressão do Hunter.
