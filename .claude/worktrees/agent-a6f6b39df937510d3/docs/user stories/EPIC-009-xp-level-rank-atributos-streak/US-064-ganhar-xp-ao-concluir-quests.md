---
title: US-064 — Ganhar XP ao concluir exercícios
sidebar_position: 64
---

# US-064 — Ganhar XP ao concluir exercícios

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-064 |
| Épico | EPIC-009 — Sistema de XP, Level, Rank, Atributos e Streak |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário com acesso ativo |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | HunterProgress |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **ganhar XP ao concluir meus exercícios**,

para **sentir progresso real a cada etapa do treino**.

---

## 3. Contexto

O XP é o motor da progressão de Level e Rank. Concluir cada exercício da quest diária ou dungeon deve conceder XP geral e XP de atributo, de forma confiável e sem duplicidade. A quest, por sua vez, fecha o ciclo e consolida o que já foi ganho.

---

## 4. Objetivo

Conceder XP ao concluir exercícios, de forma idempotente e auditável.

---

## 5. Escopo

### Entra nesta US

- Concessão de XP geral ao concluir exercício válido.
- Disparo do cálculo de XP de atributo (US-068) e de RankScore (US-067).
- Idempotência por exercício (sem XP duplicado).

### Fora desta US

- Cálculo do valor de XP (US-065).
- Penalidade por quest não completada (US-132).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | XP só é concedido após conclusão válida do exercício. |
| RN-002 | O mesmo exercício não pode gerar XP duplicado. |
| RN-003 | Exercícios de quest diária e dungeon concedem XP; apenas a diária tem penalidade por não conclusão da quest. |
| RN-004 | Requer acesso ativo. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Ganha XP. |
| Premium Mensal/Anual | Ganha XP. |
| Acesso expirado | Não gera/concluí quest (EPIC-003/006). |

---

## 8. Fluxo principal

1. Usuário conclui um exercício.
2. Backend valida a conclusão do exercício.
3. Backend concede XP geral e dispara XP de atributo/RankScore.
4. App exibe a recompensa do exercício.

---

## 9. Fluxos alternativos

### 9.1. Conclusão duplicada

Segunda chamada de conclusão do mesmo exercício não concede XP novamente.

### 9.2. Conclusão parcial

XP proporcional conforme regra do EPIC-008 (conclusão parcial).

---

## 10. Estados esperados

- concluindo;
- XP concedido;
- já concluída (sem novo XP);
- erro de processamento.

---

## 11. Impacto no Frontend Flutter

- Tela de recompensa com XP ganho.

---

## 12. Impacto no Backend

- Serviço de conclusão que concede XP de forma idempotente.

---

## 13. Impacto no Banco de Dados

Entidades: `HunterProgress`, `QuestLog`.

Campos: `xp`, `questId`, `completedAt`.

---

## 14. Impacto em Gamificação

- Base de Level, Rank e atributos.

---

## 15. Impacto em Monetização

- Progressão percebida sustenta o valor do trial.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de recompensa. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
POST /api/quests/{questId}/exercises/{questExerciseId}/complete
```

Response conceitual:

```json
{ "xpEarned": 100, "totalXp": 340 }
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| xp_earned | Quando XP é concedido (`source`: exercise \| dungeon). |

---

## 19. Critérios de aceite

### CA-001 — XP concedido

Dado que o usuário concluiu o exercício,

Quando a conclusão for válida,

Então deve receber XP geral e disparar XP de atributo.

### CA-002 — Sem duplicidade

Dado que a quest já foi concluída,

Quando a conclusão for reenviada,

Então não deve conceder XP novamente.

---

## 20. Critérios de teste para QA

### Backend

- conclusão válida do exercício concede XP;
- conclusão duplicada é idempotente;
- conclusão parcial concede XP proporcional;
- acesso expirado não conclui.

---

## ✅ Decisão registrada

> Concluir exercício concede XP de forma idempotente e dispara a evolução de atributos e RankScore.
