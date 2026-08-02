---
title: US-133 — Ver quais atributos evoluíram na tela de recompensa
sidebar_position: 133
---

# US-133 — Ver quais atributos evoluíram na tela de recompensa

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-133 |
| Épico | EPIC-009 — Sistema de XP, Level, Rank, Atributos e Streak |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário com acesso ativo |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | HunterAttributes |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **ver quais atributos evoluíram na tela de recompensa**,

para **entender o impacto do treino que acabei de fazer**.

---

## 3. Contexto

Ao concluir um exercício, a tela de recompensa deve mostrar o XP ganho, quais atributos receberam XP interno e quais atributos ganharam ponto visível/subiram de Level, reforçando a conexão entre exercício e evolução.

---

## 4. Objetivo

Exibir, na recompensa do exercício, os atributos que receberam XP interno e os que subiram de Level.

---

## 5. Escopo

### Entra nesta US

- Resumo de XP interno de atributo por exercício.
- Destaque dos atributos que subiram de Level.
- Indicação de progresso de Rank quando houver.

### Fora desta US

- Cálculo dos ganhos (US-068/130).
- Perfil detalhado (EPIC-010).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A recompensa deve refletir os ganhos reais de atributo. |
| RN-002 | Atributos que subiram de Level devem ser destacados. |
| RN-003 | A tela não deve poluir com excesso de informação. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Vê a recompensa. |
| Premium Mensal/Anual | Vê a recompensa. |
| Acesso expirado | Sem novas recompensas. |

---

## 8. Fluxo principal

1. Usuário conclui um exercício.
2. App exibe XP e atributos evoluídos.
3. Destaca level ups de atributo e progresso de Rank.

---

## 9. Fluxos alternativos

### 9.1. Sem level up

Mostrar apenas o XP interno de atributo ganho.

---

## 10. Estados esperados

- recompensa exibida;
- com level up de atributo;
- sem level up.

---

## 11. Impacto no Frontend Flutter

- Tela de recompensa com resumo de atributos.

---

## 12. Impacto no Backend

- Retorno dos ganhos de atributo na conclusão.

---

## 13. Impacto no Banco de Dados

Entidade: `HunterAttributes`.

---

## 14. Impacto em Gamificação

- Conecta esforço a evolução visível.

---

## 15. Impacto em Monetização

- Reforço de valor a cada treino.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Labels de atributos e recompensa. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
(retornado em) POST /api/quests/{questId}/exercises/{questExerciseId}/complete
```

Response conceitual:

```json
{
  "attributeXpEarned": {
    "strength": 3,
    "vitality": 1,
    "wisdom": 6
  },
  "attributePointsGranted": {
    "strength": 1,
    "vitality": 0,
    "wisdom": 0
  },
  "attributeLevelUps": ["strength"]
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| hunter_progress_viewed | Quando a recompensa é exibida. |

---

## 19. Critérios de aceite

### CA-001 — Atributos na recompensa

Dado que o usuário concluiu o exercício,

Quando a recompensa for exibida,

Então deve mostrar os atributos que receberam XP interno.

### CA-002 — Destaque de level up

Dado que um atributo subiu de Level,

Quando a recompensa for exibida,

Então esse atributo deve ser destacado.

---

## 20. Critérios de teste para QA

### Frontend (Flutter)

- recompensa mostra atributos ganhos;
- destaca level ups;
- sem poluição visual;
- textos em PT-BR, EN, ES.

---

## ✅ Decisão registrada

> A tela de recompensa mostra os atributos evoluídos e seus level ups, conectando o treino à evolução do Hunter.
