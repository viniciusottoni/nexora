---
title: US-061 — Concluir quest
sidebar_position: 61
---

# US-061 — Concluir quest

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-061 |
| Épico | EPIC-008 — Execução da Quest e Registro do Treino |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | QuestExercise e HunterProgress |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário que executou a quest**,

quero **concluir a quest**,

para **receber o resultado final do treino e alimentar minha evolução no AWAKEN**.

---

## 3. Contexto

A conclusão fecha o ciclo da execução. Ela deve ser confiável e idempotente, consolidando o treino já recompensado por exercício, atualizando o estado final da quest, o streak e as recompensas aplicáveis.

---

## 4. Objetivo

Permitir concluir uma quest executada, garantindo que a conclusão não seja duplicada e que o resultado final seja consolidado corretamente.

---

## 5. Escopo

### Entra nesta US

- Concluir daily.
- Concluir dungeon.
- Concluir raid.
- Validar exercícios concluídos.
- Registrar `completedAt`.
- Consolidar o resultado final do treino já acumulado pelos exercícios concluídos.
- Atualizar streak e preparar os dados finais para QuestLog e tela de recompensa.
- Impedir conclusão duplicada.

### Fora desta US

- Exibição detalhada da recompensa, tratada na US-063.
- Histórico detalhado, tratado no EPIC-011.
- Ranking social.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Apenas quest em andamento ou pausada pode ser concluída. |
| RN-002 | Quest cancelada não pode ser concluída. |
| RN-003 | Quest já concluída não pode gerar recompensa novamente. |
| RN-004 | Conclusão deve gerar ou preparar QuestLog. |
| RN-005 | Conclusão deve disparar cálculo de XP geral, XP interno de atributos, pontos visíveis de atributo quando houver conversão, e streak. |
| RN-006 | Quest deve manter `questType`: `daily`, `dungeon` ou `raid`. |
| RN-007 | Dungeons podem gerar itens ao concluir, conforme regra de recompensa. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Pode concluir quest iniciada com acesso válido. |
| Premium Mensal | Pode concluir quest própria. |
| Premium Anual | Pode concluir quest própria. |
| Trial expirado | Não inicia novas quests; conclusão em andamento segue regra vigente. |
| Assinatura expirada | Não inicia novas quests; conclusão em andamento segue regra vigente. |
| Visitante | Não pode concluir. |

---

## 8. Fluxo principal

1. Usuário conclui os exercícios necessários.
2. App habilita ação de concluir quest.
3. Usuário toca em concluir.
4. Backend valida estado da quest.
5. Backend calcula resultado final.
6. Backend marca quest como concluída.
7. App direciona para tela de recompensa.

---

## 9. Fluxos alternativos

### 9.1. Quest já concluída

Backend retorna resultado idempotente sem duplicar recompensa.

### 9.2. Quest cancelada

Backend rejeita conclusão.

### 9.3. Exercícios pendentes

Sistema pode bloquear conclusão ou aplicar regra de conclusão parcial, conforme decisão do produto.

---

## 10. Estados esperados

- pronta para concluir;
- concluindo;
- concluída;
- já concluída;
- cancelada;
- erro de conclusão.

---

## 11. Impacto no Frontend Flutter

- CTA “Concluir quest”.
- Estado de loading.
- Prevenção de múltiplos toques.
- Navegação para recompensa.
- Mensagens de erro funcionais.

---

## 12. Impacto no Backend

- Endpoint de conclusão.
- Idempotência por quest.
- Consolidação final do treino já recompensado por exercício.
- Atualização de streak.
- Preparação de dados para QuestLog e tela de recompensa.

---

## 13. Impacto no Banco de Dados

Entidades:

- Quest;
- QuestExercise;
- QuestLog;
- HunterProgress;
- HunterAttributes;
- HunterInventory.

Campos:

- Quest.status;
- Quest.completedAt;
- QuestLog.questType;
- QuestLog.xpEarned;
- QuestLog.attributeXpEarned;
- QuestLog.attributePointsGranted;
- QuestLog.itemsEarned.

---

## 14. Impacto em Gamificação

- Fecha cálculo de recompensa da quest.
- Atualiza streak.
- Pode conceder itens em dungeon.
- Raid usa o mesmo contrato base com `questType = raid`.

---

## 15. Impacto em Monetização

- Conclusão faz parte do valor central do trial e planos pagos.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de conclusão. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
POST /api/quests/{questId}/complete
```

Response conceitual:

```json
{
  "questId": "uuid",
  "questType": "raid",
  "status": "completed",
  "xpEarned": 180,
  "attributeXpEarned": {
    "strength": 8,
    "vitality": 4,
    "wisdom": 6
  },
  "attributePointsGranted": {
    "strength": 1,
    "vitality": 0,
    "wisdom": 0
  },
  "completedAt": "2026-06-23T19:10:00Z"
}
```

> Os campos de XP e atributos nesse resumo representam o total já acumulado pelos exercícios concluídos. A conclusão da quest não gera uma nova rodada de XP.

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| quest_completed | Quando quest é concluída. |

Propriedades:

- `quest_type`;
- `xp_earned`;
- `attribute_xp_earned`;
- `attribute_points_granted`;
- `items_earned`.

---

## 19. Critérios de aceite

### CA-001 — Conclusão válida

Dado que a quest está apta para conclusão,

Quando o usuário concluir,

Então o status deve virar concluída e o resultado deve ser calculado.

### CA-002 — Sem duplicidade

Dado que a quest já foi concluída,

Quando a conclusão for chamada novamente,

Então recompensa não deve duplicar.

---

## 20. Critérios de teste para QA

- concluir daily;
- concluir dungeon;
- concluir raid;
- validar XP final;
- validar XP interno de atributos e pontos visíveis concedidos;
- validar streak;
- tentar concluir quest cancelada;
- tentar duplicar conclusão;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> A conclusão da quest deve ser idempotente e disparar o cálculo final de gamificação sem permitir recompensa duplicada.
