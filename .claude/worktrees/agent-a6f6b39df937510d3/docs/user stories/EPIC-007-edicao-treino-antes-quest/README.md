---
title: EPIC-007 — Alteração de Tipo de Treino Antes da Quest
sidebar_position: 7
---

# EPIC-007 — Alteração de Tipo de Treino Antes da Quest

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | EPIC-007 |
| Fase | MVP Android Fitness Gamificado |
| Prioridade | P0 |
| Perfil principal | Usuário em Trial ou assinante |
| Planos impactados | Trial, Mensal e Anual |
| Status | Planejado |

## 2. Objetivo

Permitir que o usuário visualize o treino antes de iniciar a quest e, se necessário, altere **apenas o tipo do treino inteiro**, escolhendo entre Personalizado Individual, Treino de Regeneração ou um Programa disponível, como Caminho de Saitama, Perfect 2 ou outros programas futuros.

## 3. Contexto de produto

A regra do EPIC-007 foi redefinida. O pré-treino não é mais um editor de exercício individual e não permite ajuste manual de séries, repetições, tempo ou descanso.

A única ação permitida antes de iniciar a quest é trocar o **tipo do treino inteiro**:

- Personalizado Individual;
- Treino de Regeneração;
- Programa.

No MVP, os programas iniciais são:

- Caminho de Saitama, com progressão que varia conforme o rank do jogador;
- Perfect 2, um treino específico com apenas dois exercícios ideais por grupo muscular.

A estrutura deve permitir inclusão de novos programas no futuro sem mudar o fluxo principal.

## 4. Escopo

### Entra neste épico

- Visualização do treino antes de iniciar.
- Exibição do treino em modo somente leitura.
- Alteração do tipo do treino inteiro antes da quest iniciar.
- Escolha entre Personalizado Individual, Treino de Regeneração ou Programa.
- Seleção de programa, inicialmente Caminho de Saitama e Perfect 2.
- Validação da alteração de tipo.
- Regeração/substituição do treino completo conforme tipo escolhido.
- Recálculo de XP e duração estimada com base no treino final.
- Bloqueio de alteração para acesso expirado.
- Salvamento de preferência de tipo de treino como P1.

### Fora deste épico

- Substituição de exercício individual.
- Ajuste manual de séries.
- Ajuste manual de repetições.
- Ajuste manual de tempo ou descanso.
- Editor avançado de treino livre.
- Criação completa de treino do zero.
- Marketplace de treinos.
- Templates públicos de outros usuários.

## 5. User Stories relacionadas

| ID | Título | Prioridade | Documento |
|---|---|---|---|
| US-050 | Visualizar treino antes de iniciar | P0 | [Abrir](./US-050-visualizar-treino-antes-iniciar.md) |
| US-051 | Alterar tipo do treino antes de iniciar | P0 | [Abrir](./US-051-alterar-tipo-treino-antes-iniciar.md) |
| US-052 | Bloquear ajuste manual de séries, repetições e tempo | P0 | [Abrir](./US-052-bloquear-ajuste-manual-volume.md) |
| US-053 | Validar alteração do tipo de treino | P0 | [Abrir](./US-053-validar-alteracoes-treino.md) |
| US-054 | Salvar preferência de tipo de treino | P1 | [Abrir](./US-054-salvar-preferencias-edicao.md) |
| US-055 | Bloquear alteração de treino para acesso expirado | P0 | [Abrir](./US-055-bloquear-edicao-acesso-expirado.md) |

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-EPIC-007-001 | A alteração só é permitida antes da quest iniciar. |
| RN-EPIC-007-002 | A única alteração permitida é trocar o tipo do treino inteiro. |
| RN-EPIC-007-003 | Tipos permitidos: Personalizado Individual, Treino de Regeneração e Programa. |
| RN-EPIC-007-004 | Programas iniciais: Caminho de Saitama e Perfect 2. |
| RN-EPIC-007-005 | A estrutura deve aceitar novos programas futuramente. |
| RN-EPIC-007-006 | Não é permitido substituir exercício individual. |
| RN-EPIC-007-007 | Não é permitido alterar séries, repetições, tempo ou descanso manualmente. |
| RN-EPIC-007-008 | Usuário com trial ou assinatura expirada não pode alterar tipo de treino. |
| RN-EPIC-007-009 | O treino gerado pelo novo tipo deve respeitar perfil, limitações físicas, nível e catálogo. |
| RN-EPIC-007-010 | A alteração deve recalcular XP e duração estimada com base no treino final. |
| RN-EPIC-007-011 | O pré-treino deve exibir exercícios e volume em modo somente leitura. |

## 7. Impactos técnicos

### Flutter

- Tela de pré-treino em modo leitura.
- Componente de card de exercício sem ações de edição individual.
- Ação principal: alterar tipo de treino.
- Modal/bottom sheet para escolher tipo de treino.
- Lista de programas disponíveis quando o tipo for Programa.
- Estados de validação, geração, erro e acesso bloqueado.
- Remover controles de séries, repetições, tempo, descanso e substituição de exercício.

### Backend

- Endpoint para buscar prévia da quest.
- Endpoint para alterar tipo de treino antes do início.
- Serviço para gerar/substituir o treino inteiro conforme tipo escolhido.
- Validação de status da quest e acesso.
- Validação de programa ativo/disponível.
- Recálculo de XP e duração estimada.
- Bloqueio de endpoints legados de edição manual, se existirem.

### Banco de dados

Entidades principais:

- Quest.
- QuestExercise.
- Program.
- Exercise.
- UserProfile.
- Subscription.

Campos relevantes:

- Quest.trainingType.
- Quest.programId.
- Quest.estimatedXp.
- Quest.estimatedDurationMinutes.

### Analytics

- `quest_viewed`.
- `workout_type_change_started`.
- `workout_type_changed`.
- `workout_type_change_failed`.
- `manual_workout_edit_blocked`.
- `access_blocked`, quando aplicável.

### QA

- Visualizar treino antes de iniciar.
- Confirmar que exercícios estão em modo somente leitura.
- Alterar para Personalizado Individual.
- Alterar para Treino de Regeneração.
- Alterar para Caminho de Saitama.
- Alterar para Perfect 2.
- Validar bloqueio de substituição de exercício individual.
- Validar bloqueio de ajuste manual de séries, repetições, tempo e descanso.
- Bloquear alteração após início.
- Bloquear alteração com acesso expirado.

## 8. Dependências

- EPIC-003 para status de acesso.
- EPIC-005 para catálogo.
- EPIC-006 para quest gerada e geração por tipo de treino.
- EPIC-009 para rank usado na progressão do Caminho de Saitama.

## 9. Critérios de aceite do épico

- Usuário vê treino antes de iniciar.
- Usuário consegue trocar o tipo do treino inteiro antes de iniciar.
- Usuário consegue escolher Personalizado Individual, Treino de Regeneração ou Programa disponível.
- Caminho de Saitama e Perfect 2 aparecem como programas iniciais.
- Exercícios, séries, repetições, tempo e descanso aparecem em modo somente leitura.
- Sistema bloqueia substituição de exercício individual.
- Sistema bloqueia ajuste manual de volume.
- Sistema valida alteração de tipo e recalcula XP/duração.
- Usuário bloqueado não altera tipo de treino.

## 10. Decisão registrada

O EPIC-007 não é mais um editor manual de treino. A única alteração permitida antes de iniciar a quest é trocar o tipo do treino inteiro: Personalizado Individual, Treino de Regeneração ou Programa. Substituição de exercícios individuais e ajustes manuais de séries, repetições, tempo ou descanso foram removidos do escopo para evitar incoerência, abuso de XP e complexidade excessiva no MVP.
