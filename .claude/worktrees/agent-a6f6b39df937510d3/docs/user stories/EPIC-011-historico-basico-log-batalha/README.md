---
title: EPIC-011 — Histórico Básico e Log de Batalha
sidebar_position: 11
---

# EPIC-011 — Histórico Básico e Log de Batalha

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | EPIC-011 |
| Fase | MVP Android Fitness Gamificado |
| Prioridade | P0 |
| Perfil principal | Usuário em Trial ou assinante |
| Planos impactados | Trial, Mensal e Anual |
| Status | Planejado |

## 2. Objetivo

Registrar quests concluídas e apresentar um histórico simples que mostre consistência, XP recebido e evolução do usuário.

## 3. Contexto de produto

O usuário precisa perceber que sua jornada está sendo registrada. O histórico funciona como um log de batalha, reforçando progresso e ajudando a manter motivação.

## 4. Escopo

### Entra neste épico

- Listagem de quests concluídas recentemente, com tipo: diária, dungeon ou raid quando houver log.
- Histórico durante trial.
- Histórico completo para assinantes como P1.
- XP recebido por quest.
- Itens ganhos em dungeons.
- Registro consistente de conclusão.

### Fora deste épico

- Gráficos avançados.
- Comparações semanais profundas.
- Exportação de dados.
- Relatórios para treinador.

## 5. User Stories relacionadas

| ID | Título | Prioridade | Documento |
|---|---|---|---|
| US-081 | Ver quests concluídas recentemente | P0 | [Abrir](./US_081-ver-quests-concluidas-recentemente.md) |
| US-082 | Ver histórico durante trial | P0 | [Abrir](./US_082-ver-historico-durante-trial.md) |
| US-083 | Ver histórico completo como assinante | P1 | [Abrir](./US_083-ver-historico-completo-assinante.md) |
| US-084 | Ver XP recebido em cada quest | P0 | [Abrir](./US_084-xp-por-quest.md) |
| US-085 | Registrar logs de conclusão de quest | P0 | [Abrir](./US_085-logs-conclusao-quest.md) |

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-EPIC-011-001 | Toda quest concluída deve gerar log com `questType` (daily, dungeon ou raid). |
| RN-EPIC-011-002 | Histórico deve refletir apenas quests válidas. |
| RN-EPIC-011-003 | XP exibido deve bater com XP aplicado. |
| RN-EPIC-011-004 | Acesso expirado pode mostrar histórico limitado com CTA de assinatura. |
| RN-EPIC-011-005 | Logs não devem ser apagados quando trial ou assinatura expirar. |
| RN-EPIC-011-006 | Entradas de dungeon devem exibir itens ganhos na conclusão, quando houver. |

## 7. Impactos técnicos

### Flutter

- Tela de histórico.
- Lista de quests concluídas.
- Empty state quando não houver histórico.
- Estado limitado para acesso expirado.

### Backend

- Endpoint de histórico.
- Registro de QuestLog.
- Consulta paginada ou limitada.
- Validação de acesso.

### Banco de dados

Entidades principais:

- QuestLog (com campos `questType`, `xpEarned`, `itemsEarned`).
- Quest.
- HunterProgress.
- HunterInventory (para exibição de itens ganhos em dungeons).

### Analytics

- Uso indireto em `quest_completed`, `xp_earned` e `hunter_profile_viewed`.

### QA

- Concluir quest diária e verificar que aparece no histórico com tipo "diária".
- Concluir dungeon e verificar que aparece no histórico com tipo "dungeon" e itens ganhos.
- Concluir raid, quando houver, e verificar que aparece no histórico com tipo "raid".
- Conferir XP exibido.
- Ver histórico sem dados.
- Ver estado com acesso expirado.
- Confirmar preservação após expiração.

## 8. Dependências

- EPIC-008 para conclusão.
- EPIC-009 para XP.
- EPIC-003 para acesso.

## 9. Critérios de aceite do épico

- Quest concluída aparece no histórico.
- XP por quest é exibido corretamente.
- Histórico não desaparece após expiração.
- Usuário bloqueado recebe CTA claro para assinar.

## 10. Decisão registrada

O histórico do MVP deve ser simples, mas confiável. Ele existe para reforçar continuidade e dar ao usuário sensação de jornada registrada.
