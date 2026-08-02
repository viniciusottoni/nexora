---
title: US-085 — Registrar logs de conclusão de quest
sidebar_position: 85
---

# US-085 — Registrar logs de conclusão de quest

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-085 |
| Épico | EPIC-011 — Histórico Básico e Log de Batalha |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Status | Planejada |

## 2. História do usuário

Como **sistema do AWAKEN**, quero **registrar logs confiáveis de conclusão de quest**, para **alimentar histórico, recompensa e auditoria de progresso**.

## 3. Contexto

O histórico depende diretamente do QuestLog criado na conclusão. Sem um log consistente, o usuário perde confiança e o sistema pode exibir XP, itens ou tipos de quest incorretos.

## 4. Objetivo

Garantir que toda quest concluída gere um QuestLog único, consistente e preservado.

## 5. Escopo

### Entra nesta US

- Registrar log de conclusão de quest diária.
- Registrar log de conclusão de dungeon.
- Registrar log de raid quando existir.
- Salvar `questType`.
- Salvar `xpEarned`.
- Salvar `itemsEarned` para dungeons quando houver.
- Salvar `xpPenaltyApplied` quando aplicável.
- Garantir idempotência para evitar duplicidade.

### Fora desta US

- Tela de recompensa, tratada no EPIC-008.
- Histórico avançado.
- Exportação de dados.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Toda quest concluída deve gerar QuestLog. |
| RN-002 | QuestLog deve conter `questType`. |
| RN-003 | QuestLog deve conter XP recebido. |
| RN-004 | Dungeons devem registrar itens ganhos quando houver. |
| RN-005 | O mesmo `questId` não pode gerar múltiplos logs de conclusão. |
| RN-006 | Logs não devem ser apagados por expiração de trial ou assinatura. |
| RN-007 | Quest cancelada não deve gerar log como concluída. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema | Pode registrar QuestLog. |
| Usuário em Trial | Pode gerar log ao concluir quest válida. |
| Premium Mensal | Pode gerar log ao concluir quest válida. |
| Premium Anual | Pode gerar log ao concluir quest válida. |
| Visitante | Não gera log. |

## 8. Fluxo principal

1. Usuário conclui quest no EPIC-008.
2. Backend consolida XP, atributos e itens.
3. Backend cria QuestLog em transação.
4. Log fica disponível para histórico e recompensa.
5. Histórico consulta o log posteriormente.

## 9. Fluxos alternativos

### 9.1. Log já existe

Retornar log existente sem duplicar XP, itens ou histórico.

### 9.2. Falha transacional

Rollback deve evitar recompensa sem log ou log sem recompensa aplicada.

## 10. Estados esperados

- log criado;
- log já existente;
- log inválido;
- erro transacional;
- quest cancelada rejeitada.

## 11. Impacto Flutter

- Não cria log diretamente.
- Consome logs no histórico.
- Exibe erro se o backend não conseguir consolidar a conclusão.

## 12. Impacto Backend

- Criar QuestLog de forma transacional.
- Garantir idempotência por `questId`.
- Retornar dados para histórico/recompensa.
- Preservar logs mesmo após expiração.

## 13. Impacto DB

Entidade principal:

- QuestLog.

Campos:

- id;
- userId;
- questId;
- questType;
- xpEarned;
- attributePointsEarned;
- itemsEarned;
- xpPenaltyApplied;
- completedAt;
- createdAt.

Restrições sugeridas:

- índice único por `questId` para log de conclusão.

## 14. Impacto Gamificação

- Fonte de verdade para XP e histórico.
- Previne duplicidade de recompensa.
- Alimenta perfil, histórico e tela de recompensa.

## 15. Impacto Monetização

- Histórico preservado aumenta confiança.
- Expiração não remove dados conquistados.

## 16. Contrato API sugerido

Uso interno na conclusão:

```txt
POST /api/quests/{questId}/logs
```

Response conceitual:

```json
{
  "questLogId": "uuid",
  "questType": "dungeon",
  "xpEarned": 220,
  "xpPenaltyApplied": 20,
  "itemsEarned": ["scroll_reforge"]
}
```

## 17. Eventos Analytics

| Evento | Quando dispara |
|---|---|
| quest_log_created | Quando QuestLog é criado. |
| quest_log_duplicate_prevented | Quando duplicidade é evitada. |

## 18. Critérios de aceite

### CA-001 — Log criado

Dado que uma quest válida foi concluída,
Quando a conclusão for processada,
Então deve existir um QuestLog associado.

### CA-002 — Sem duplicidade

Dado que já existe QuestLog para a quest,
Quando a conclusão for chamada novamente,
Então não deve criar novo log nem duplicar recompensa.

## 19. Critérios de teste QA

- log de daily;
- log de dungeon com item;
- log de raid, se houver;
- tentativa duplicada;
- quest cancelada;
- expiração de acesso após log;
- consistência entre log e XP aplicado.

## 20. Decisão registrada

QuestLog é a fonte de verdade do histórico e deve ser único, confiável e preservado após expiração de acesso.
