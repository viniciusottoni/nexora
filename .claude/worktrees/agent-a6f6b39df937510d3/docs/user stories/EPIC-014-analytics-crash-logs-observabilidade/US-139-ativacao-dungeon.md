---
title: US-139 — Rastrear ativação de dungeon pelo usuário
sidebar_position: 139
---

# US-139 — Rastrear ativação de dungeon pelo usuário

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-139 |
| Épico | EPIC-014 — Analytics, Crash, Logs e Observabilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Produto e Engenharia |
| Integrações | Firebase Analytics e logs de backend |
| Status | Planejada |

## 2. História do usuário

Como **time de produto**, quero **rastrear ativação de dungeon**, para **medir interesse, uso e conversão desse modo em relação à quest diária**.

## 3. Objetivo

Registrar quando usuário visualiza, ativa, inicia e conclui dungeon.

## 4. Escopo

### Entra nesta US

- Dungeon visualizada.
- Dungeon ativada pelo usuário.
- Dungeon iniciada.
- Dungeon concluída.
- Falha de ativação.
- Propriedades de origem e resultado.

### Fora desta US

- Ranking social de dungeons.
- Eventos avançados de economia.
- BI em tempo real.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Visualização de dungeon deve gerar `dungeon_viewed`. |
| RN-002 | Ativação deve gerar `dungeon_activated`. |
| RN-003 | Início e conclusão devem reutilizar contrato de quest com `quest_type=dungeon`. |
| RN-004 | Falha deve informar código funcional genérico. |
| RN-005 | Eventos não devem expor dados sensíveis do usuário. |

## 6. Eventos sugeridos

| Evento | Quando dispara |
|---|---|
| dungeon_viewed | Quando a dungeon é visualizada. |
| dungeon_activated | Quando usuário ativa dungeon. |
| quest_started | Quando dungeon inicia. |
| quest_completed | Quando dungeon conclui. |
| dungeon_activation_failed | Quando ativação falha. |

## 7. Payload mínimo

```json
{
  "quest_type": "dungeon",
  "source": "home",
  "result": "success"
}
```

## 8. Impacto Flutter

- Instrumentar entrada da dungeon.
- Enviar evento de visualização/ativação sem duplicar por rebuild.
- Tratar deep link quando houver.

## 9. Impacto Backend

- Logar ativação e falhas.
- Associar dungeon ao QuestLog quando concluída.
- Usar correlationId em falhas.

## 10. Critérios de aceite

### CA-001 — Dungeon ativada

Dado que o usuário ativa uma dungeon,
Quando a ativação for confirmada,
Então deve existir evento `dungeon_activated`.

### CA-002 — Contrato de quest

Dado que dungeon foi iniciada,
Quando o evento `quest_started` for enviado,
Então deve conter `quest_type=dungeon`.

## 11. Decisão registrada

Dungeons precisam ser medidas separadamente para validar se o modo aumenta engajamento e valor percebido no AWAKEN.
