---
title: US-062 — Registrar treino concluído
sidebar_position: 62
---

# US-062 — Registrar treino concluído

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-062 |
| Épico | EPIC-008 — Execução da Quest e Registro do Treino |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | QuestLog |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema do AWAKEN**,

quero **registrar o treino concluído em um log confiável**,

para **alimentar histórico, gamificação, auditoria e tela de recompensa**.

---

## 3. Contexto

O QuestLog é o registro histórico da execução. Ele precisa guardar tipo da quest, XP geral, XP interno de atributos, pontos visíveis de atributo concedidos, itens, penalidades e dados suficientes para o EPIC-011 exibir histórico básico.

---

## 4. Objetivo

Criar QuestLog ao concluir quest, garantindo consistência, idempotência e rastreabilidade da recompensa.

---

## 5. Escopo

### Entra nesta US

- Criar QuestLog para daily.
- Criar QuestLog para dungeon.
- Criar QuestLog para raid.
- Salvar `questType`.
- Salvar XP ganho.
- Salvar XP interno de atributos ganho.
- Salvar pontos visíveis de atributos concedidos quando houver conversão.
- Salvar itens ganhos quando aplicável.
- Salvar penalidade quando quest foi cancelada/parcial, se aplicável.

### Fora desta US

- Tela de histórico detalhado.
- Relatórios avançados.
- Ranking social.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Quest concluída deve gerar QuestLog. |
| RN-002 | QuestLog deve conter `questType`: `daily`, `dungeon` ou `raid`. |
| RN-003 | QuestLog deve armazenar `xpEarned`. |
| RN-004 | QuestLog deve armazenar `attributeXpEarned`. |
| RN-005 | QuestLog deve armazenar `itemsEarned` quando dungeon conceder itens. |
| RN-006 | QuestLog deve indicar `xpPenaltyApplied` quando houver penalidade. |
| RN-007 | A mesma quest não pode gerar múltiplos QuestLogs de conclusão. |
| RN-008 | QuestLog deve armazenar `attributePointsGranted` quando 10 XP internos forem convertidos em ponto visível de atributo. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema | Pode criar QuestLog. |
| Usuário em Trial | Pode gerar log ao concluir quest. |
| Premium Mensal | Pode gerar log ao concluir quest. |
| Premium Anual | Pode gerar log ao concluir quest. |
| Visitante | Não gera log. |

---

## 8. Fluxo principal

1. Quest é concluída com sucesso.
2. Backend consolida exercícios concluídos.
3. Backend calcula XP geral, XP interno de atributos, pontos visíveis de atributo, streak e itens.
4. Backend cria QuestLog.
5. Backend retorna dados para tela de recompensa.
6. EPIC-011 pode consultar esse log no histórico.

---

## 9. Fluxos alternativos

### 9.1. QuestLog já existe

Sistema deve retornar log existente sem duplicar registro nem recompensa.

### 9.2. Falha ao registrar log

Conclusão deve falhar de forma controlada ou usar transação para evitar inconsistência entre recompensa e log.

---

## 10. Estados esperados

- log pendente;
- log criado;
- log existente;
- erro de registro;
- transação revertida.

---

## 11. Impacto no Frontend Flutter

- Recebe dados consolidados para recompensa.
- Não cria log diretamente.
- Pode exibir erro se registro falhar.

---

## 12. Impacto no Backend

- Criar QuestLog na conclusão.
- Garantir idempotência por questId.
- Usar transação com atualização de progresso e inventário.
- Expor dados para recompensa e histórico.

---

## 13. Impacto no Banco de Dados

Entidade principal:

- QuestLog.

Campos:

- questId;
- userId;
- questType;
- xpEarned;
- attributeXpEarned;
- attributePointsGranted;
- itemsEarned;
- xpPenaltyApplied;
- completedAt;
- createdAt.

---

## 14. Impacto em Gamificação

- Fonte de verdade para histórico e recompensa.
- Evita duplicidade de XP e itens.
- Registra XP interno de atributos e pontos visíveis concedidos por treino.

---

## 15. Impacto em Monetização

- Preserva valor histórico mesmo em mudanças futuras de assinatura.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Dados podem ser exibidos no histórico. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

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
  "attributeXpEarned": {
    "strength": 9,
    "vitality": 5,
    "wisdom": 7
  },
  "attributePointsGranted": {
    "strength": 1,
    "vitality": 0,
    "wisdom": 0
  },
  "itemsEarned": ["scroll_reforge"]
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| quest_log_created | Quando QuestLog é criado. |

---

## 19. Critérios de aceite

### CA-001 — Log criado

Dado que uma quest foi concluída,

Quando a conclusão for processada,

Então deve existir QuestLog com tipo, XP geral, XP interno de atributos e pontos visíveis concedidos.

### CA-002 — Log sem duplicidade

Dado que uma quest já possui QuestLog,

Quando a conclusão for repetida,

Então não deve criar novo log nem duplicar recompensa.

---

## 20. Critérios de teste para QA

- log de daily;
- log de dungeon com itens;
- log de raid;
- validar `questType`;
- validar XP geral, XP interno de atributos e pontos visíveis concedidos;
- validar ausência de duplicidade;
- validar falha transacional.

---

## ✅ Decisão registrada

> QuestLog é o registro confiável da execução concluída e deve sustentar histórico, recompensa, auditoria e prevenção de duplicidade.
