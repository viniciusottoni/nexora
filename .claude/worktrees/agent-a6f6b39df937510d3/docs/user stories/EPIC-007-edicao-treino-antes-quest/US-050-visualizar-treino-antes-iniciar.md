---
title: US-050 — Visualizar treino antes de iniciar
sidebar_position: 50
---

# US-050 — Visualizar treino antes de iniciar

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-050 |
| Épico | EPIC-007 — Edição de Treino Antes da Quest |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Quest gerada e status de acesso |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **visualizar o treino antes de iniciar a quest**,

para **decidir se mantenho o treino atual ou altero o tipo do treino inteiro**.

---

## 3. Contexto

O pré-treino não é mais um editor de exercício individual. Ele serve para revisar a quest gerada e permitir apenas a troca do tipo de treino: Personalizado Individual, Treino de Regeneração ou Programa.

---

## 4. Objetivo

Exibir o treino antes do início, com detalhes suficientes para revisão, mantendo como única ação de edição a alteração do tipo do treino inteiro.

---

## 5. Escopo

### Entra nesta US

- Tela de pré-treino antes de iniciar a quest.
- Exibição do tipo de treino atual.
- Exibição dos exercícios gerados para leitura.
- Exibição de séries, repetições, tempo e descanso apenas como informação.
- Exibição de estimativa de XP e duração.
- Ação para alterar tipo do treino.
- Ação para confirmar e iniciar.

### Fora desta US

- Substituir exercício individual.
- Alterar séries, repetições, tempo ou descanso.
- Criar treino do zero.
- Editor avançado.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Usuário só pode visualizar pré-treino com acesso ativo. |
| RN-002 | A única edição permitida é alterar o tipo do treino inteiro. |
| RN-003 | O usuário pode escolher entre Personalizado Individual, Treino de Regeneração ou Programa. |
| RN-004 | Programas iniciais incluem Caminho de Saitama e Perfect 2. |
| RN-005 | Séries, repetições, tempo e descanso são somente leitura no pré-treino. |
| RN-006 | Após iniciar a quest, nenhuma alteração de tipo deve ser permitida. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode visualizar. |
| Usuário em Trial | Pode visualizar pré-treino com acesso ativo. |
| Premium Mensal | Pode visualizar. |
| Premium Anual | Pode visualizar. |
| Trial expirado | Não pode visualizar pré-treino editável. |
| Assinatura expirada | Não pode visualizar pré-treino editável. |

---

## 8. Fluxo principal

1. Sistema gera a quest.
2. Usuário acessa tela de pré-treino.
3. App exibe tipo do treino atual e exercícios em modo leitura.
4. Usuário pode confirmar treino ou alterar tipo.
5. Se confirmar, a quest segue para execução.

---

## 9. Fluxos alternativos

### 9.1. Acesso expirado

App deve bloquear ações e direcionar para paywall.

### 9.2. Quest já iniciada

App deve impedir alteração de tipo e direcionar para execução ou histórico.

---

## 10. Estados esperados

- carregando treino;
- treino pronto;
- modo somente leitura;
- acesso expirado;
- quest já iniciada;
- erro de carregamento.

---

## 11. Impacto no Frontend Flutter

- Tela de pré-treino.
- Cards de exercício somente leitura.
- CTA de alterar tipo de treino.
- CTA de confirmar/iniciar.
- Remover botões de editar exercício ou volume.

---

## 12. Impacto no Backend

- Endpoint para buscar prévia da quest.
- Retornar tipo de treino atual.
- Indicar se alteração de tipo é permitida.
- Retornar estimativas de XP e duração.

---

## 13. Impacto no Banco de Dados

Entidades:

- Quest;
- QuestExercise;
- Program;
- UserProfile;
- Subscription.

---

## 14. Impacto em Gamificação

- Exibe XP estimado antes da execução.
- XP depende do treino final gerado pelo tipo escolhido.
- Não concede XP no pré-treino.

---

## 15. Impacto em Monetização

- Disponível para trial e assinantes ativos.
- Acesso expirado direciona para assinatura.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Textos de pré-treino e tipos. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/quests/{questId}/preview
```

Response conceitual:

```json
{
  "questId": "uuid",
  "trainingType": "personalized_individual",
  "estimatedXp": 120,
  "estimatedDurationMinutes": 28,
  "canChangeTrainingType": true
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| quest_viewed | Quando o pré-treino é exibido. |
| access_blocked | Quando acesso expirado tenta abrir pré-treino. |

---

## 19. Critérios de aceite

### CA-001 — Pré-treino visível

Dado que o usuário possui acesso ativo,

Quando a quest for gerada,

Então deve visualizar o treino antes de iniciar.

### CA-002 — Somente tipo editável

Dado que o usuário está no pré-treino,

Quando visualizar ações disponíveis,

Então deve existir apenas ação de alterar tipo de treino, sem edição de exercícios ou volume.

---

## 20. Critérios de teste para QA

- visualizar treino gerado;
- visualizar tipo atual;
- validar que exercícios estão em modo leitura;
- validar ausência de edição manual;
- quest já iniciada;
- acesso expirado;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> O pré-treino serve para revisão e troca do tipo do treino inteiro, não para edição manual de exercícios, séries, repetições ou tempo.
