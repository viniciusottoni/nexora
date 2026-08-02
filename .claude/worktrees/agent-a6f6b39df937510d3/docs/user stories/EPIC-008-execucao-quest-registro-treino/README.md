---
title: EPIC-008 — Execução da Quest e Registro do Treino
sidebar_position: 8
---

# EPIC-008 — Execução da Quest e Registro do Treino

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | EPIC-008 |
| Fase | MVP Android Fitness Gamificado |
| Prioridade | P0 |
| Perfil principal | Usuário em Trial ou assinante |
| Planos impactados | Trial, Mensal e Anual |
| Status | Planejado |

## 2. Objetivo

Permitir que o usuário inicie, acompanhe, marque progresso, cancele ou conclua quests (diária, dungeon ou raid), registrando o treino para alimentar histórico e gamificação. Cada exercício concede XP geral, XP interno em 1 ou 2 atributos específicos visíveis no exercício, e +1 XP interno de Sabedoria por padrão, sem exibir Sabedoria no card/lista do exercício. A cada 10 XP internos, o atributo recebe 1 ponto visível. A conclusão de uma dungeon pode conceder itens, e a execução de raids segue o mesmo fluxo base de registro e recompensa.

## 3. Contexto de produto

A execução é o momento em que o AWAKEN deixa de ser promessa e vira treino real. A experiência precisa ser simples, rápida e clara, evitando distrações durante o exercício.

## 4. Escopo

### Entra neste épico

- Iniciar quest diária, dungeon ou raid.
- Acompanhar exercício por exercício.
- Marcar exercício como concluído.
- Pausar e retomar como P1.
- Cancelar quest.
- Concluir quest.
- Registrar conclusão.
- Exibir tela de recompensa.

### Fora deste épico

- Cronômetro **avançado** por exercício: contagem automática de repetições, timer por repetição/tempo-sob-tensão, detecção de cadência. (Os cronômetros simples de descanso entre séries e de tempo total da sessão, client-side, entram em US-057.)
- Integração com wearables.
- Sensor automático de execução.
- Vídeo-aulas próprias.

## 5. User Stories relacionadas

| ID | Título | Prioridade | Documento |
|---|---|---|---|
| US-056 | Iniciar quest | P0 | [Abrir](./US-056-iniciar-quest.md) |
| US-057 | Acompanhar exercício por exercício | P0 | [Abrir](./US-057-acompanhar-exercicio-por-exercicio.md) |
| US-058 | Marcar exercício como concluído | P0 | [Abrir](./US-058-marcar-exercicio-concluido.md) |
| US-059 | Pausar e retomar quest | P1 | [Abrir](./US-059-pausar-retomar-quest.md) |
| US-060 | Cancelar quest em andamento | P0 | [Abrir](./US-060-cancelar-quest-em-andamento.md) |
| US-061 | Concluir quest | P0 | [Abrir](./US-061-concluir-quest.md) |
| US-062 | Registrar treino concluído | P0 | [Abrir](./US-062-registrar-treino-concluido.md) |
| US-063 | Ver tela de recompensa | P0 | [Abrir](./US-063-ver-tela-recompensa.md) |

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-EPIC-008-001 | Apenas usuário com acesso ativo pode iniciar quest. |
| RN-EPIC-008-002 | Quest cancelada não deve conceder XP completo. |
| RN-EPIC-008-003 | Quest concluída deve gerar QuestLog com `questType` (daily, dungeon ou raid). |
| RN-EPIC-008-004 | Conclusão de exercício deve disparar cálculo de XP geral, XP interno de atributos, pontos visíveis de atributo quando houver conversão, e Sabedoria. A conclusão da quest fecha o ciclo e consolida o resultado. |
| RN-EPIC-008-005 | O sistema deve evitar conclusão duplicada da mesma quest. |
| RN-EPIC-008-006 | Tela de recompensa deve mostrar resultado de forma clara e motivadora. |
| RN-EPIC-008-007 | Cada exercício concede XP geral (`xpReward`) e XP interno em 1 ou 2 atributos listados em `attributeImpacts`. Esses são os atributos visíveis no exercício e não incluem Sabedoria. |
| RN-EPIC-008-010 | Cada atributo visível impactado recebe de 1 a 4 XP internos conforme a dificuldade efetiva montada para o exercício. |
| RN-EPIC-008-011 | Todo exercício concluído concede +1 XP interno de Sabedoria por padrão, como aprendizado inato da execução, sem exibir Sabedoria no card/lista do exercício. |
| RN-EPIC-008-012 | A cada 10 XP internos acumulados em um atributo, o jogador ganha 1 ponto visível naquele atributo, conforme EPIC-009. |
| RN-EPIC-008-008 | Dungeons podem conceder itens ao ser concluídas. A tela de recompensa exibe os itens ganhos quando aplicável. |
| RN-EPIC-008-009 | Raids seguem o mesmo contrato de execução e registro, com `questType = raid` no log e na telemetria. |

## 7. Impactos técnicos

### Flutter

- Tela de execução.
- Lista ordenada de exercícios.
- Marcação de conclusão.
- Estado de quest em andamento, cancelada e concluída.
- Tela final de recompensa.

### Backend

- Endpoint para iniciar quest.
- Endpoint para registrar progresso.
- Endpoint para concluir ou cancelar quest.
- Criação de QuestLog.
- Idempotência básica na conclusão.

### Banco de dados

Entidades principais:

- Quest (com campo `type`: daily | dungeon | raid).
- QuestExercise.
- QuestLog (com campos `questType`, `xpEarned`, `attributeXpEarned`, `attributePointsGranted`, `itemsEarned`, `xpPenaltyApplied`).
- HunterProgress.
- HunterAttributes.
- HunterInventory (itens concedidos por dungeons).

### Analytics

- `quest_started` (com propriedade `quest_type`).
- `exercise_completed` (com propriedades `xp_earned`, `attribute_xp_earned`, `attribute_points_granted`, `exercise_id`).
- `quest_completed` (com propriedades `quest_type`, `xp_earned`, `attribute_xp_earned`, `attribute_points_granted`, `items_earned`).

### QA

- Iniciar quest diária, dungeon e raid.
- Marcar exercícios e verificar XP geral, XP interno de atributo e pontos visíveis concedidos por conversão.
- Cancelar quest.
- Concluir quest diária e verificar recompensa sem itens.
- Concluir dungeon e verificar recompensa com itens no inventário.
- Concluir raid e verificar recompensa/registro com `questType = raid`.
- Validar que quest duplicada não gera XP duplicado.
- Validar que `questType` aparece corretamente no QuestLog.

## 8. Dependências

- EPIC-003.
- EPIC-006.
- EPIC-007.
- EPIC-009.

## 9. Critérios de aceite do épico

- Usuário executa quest do início ao fim.
- Conclusão gera log.
- Recompensa aparece.
- Quest cancelada não concede recompensa indevida.
- Usuário bloqueado não executa quest.

## 10. Decisão registrada

A execução da quest deve ser simples, objetiva e confiável. O foco do MVP é registrar treino real e alimentar a gamificação sem complexidade desnecessária, cobrindo o contrato base de daily, dungeon e raid.
