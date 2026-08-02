---
title: US-138 — Rastrear item ganho em dungeon
sidebar_position: 138
---

# US-138 — Rastrear item ganho em dungeon

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-138 |
| Épico | EPIC-014 — Analytics, Crash, Logs e Observabilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Produto e Engenharia |
| Integrações | Firebase Analytics e logs de backend |
| Status | Planejada |

## 2. História do usuário

Como **time de produto**, quero **rastrear item ganho em dungeon**, para **medir valor percebido, raridade e impacto da recompensa na retenção**.

## 3. Objetivo

Registrar evento quando dungeon concluída concede item ao usuário.

## 4. Escopo

### Entra nesta US

- Rastrear item ganho.
- Informar `item_id`.
- Informar raridade.
- Informar origem como dungeon.
- Evitar duplicidade quando conclusão for reprocessada.

### Fora desta US

- Economia avançada de itens.
- Marketplace.
- Troca entre usuários.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Item ganho deve gerar evento `item_earned`. |
| RN-002 | Evento deve informar `item_id`, `rarity` e `source`. |
| RN-003 | Duplicidade de conclusão não pode duplicar item nem evento. |
| RN-004 | Trial users não devem usar item bloqueado fora da regra comercial vigente. |

## 6. Evento sugerido

| Evento | Quando dispara |
|---|---|
| item_earned | Quando item é concedido ao usuário. |

## 7. Payload mínimo

```json
{
  "item_id": "scroll_reforge",
  "rarity": "rare",
  "source": "dungeon"
}
```

## 8. Impacto Backend

- Conceder item de forma transacional.
- Registrar item no inventário.
- Disparar/logar evento uma única vez.

## 9. Impacto Flutter

- Exibir item na tela de recompensa.
- Não recalcular item no app.

## 10. Critérios de aceite

### CA-001 — Item rastreado

Dado que uma dungeon concedeu item,
Quando a recompensa for aplicada,
Então deve existir evento `item_earned` com `item_id` e `rarity`.

### CA-002 — Sem duplicidade

Dado que a conclusão da dungeon foi reprocessada,
Quando o resultado for retornado,
Então o item e o evento não devem duplicar.

## 11. Decisão registrada

Itens de dungeon devem ser mensurados para entender impacto da recompensa e calibrar retenção sem criar economia complexa no MVP.
