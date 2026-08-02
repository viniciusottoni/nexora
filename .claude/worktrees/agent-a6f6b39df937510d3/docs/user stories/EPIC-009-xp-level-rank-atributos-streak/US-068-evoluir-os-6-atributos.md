---
title: US-068 — Evoluir os 6 atributos
sidebar_position: 68
---

# US-068 — Evoluir os 6 atributos

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-068 |
| Épico | EPIC-009 — Sistema de XP, Level, Rank, Atributos e Streak |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário com acesso ativo |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | HunterAttributes, ExerciseAttributeContribution |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **evoluir os 6 atributos ao concluir exercícios**,

para **ver minha evolução física refletida em Força, Agilidade, Resistência, Vitalidade, Foco e Sabedoria**.

---

## 3. Contexto

Cada exercício concluído concede XP interno de atributo conforme a `ExerciseAttributeContribution` (EPIC-005). Cada exercício mostra 1 ou 2 atributos impactados, sem contar Sabedoria. Sabedoria recebe +1 XP interno automaticamente por exercício concluído, como aprendizagem inata da execução (US-131). A evolução dos atributos alimenta o RankScore (US-067).

---

## 4. Objetivo

Aplicar o XP interno de atributo de cada exercício concluído aos atributos impactados do usuário.

---

## 5. Escopo

### Entra nesta US

- Aplicação do XP interno de atributo por exercício concluído.
- Acúmulo nos 6 atributos.
- Aplicação de 1 a 4 XP internos por atributo visível impactado, conforme dificuldade efetiva.
- Aplicação de +1 XP interno de Sabedoria por exercício concluído.
- Disparo da conversão XP interno → Level (US-130) e do recálculo de RankScore (US-067).

### Fora desta US

- Definição da contribuição por exercício (EPIC-005).
- Conversão interna detalhada (US-130).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Cada exercício concede XP interno de atributo conforme `ExerciseAttributeContribution`. |
| RN-002 | Cada exercício deve ter 1 ou 2 atributos visíveis impactados, sem contar Sabedoria. |
| RN-003 | Cada atributo visível impactado recebe de 1 a 4 XP internos, conforme a dificuldade efetiva montada para o exercício. |
| RN-004 | Todo exercício concluído concede +1 XP interno de Sabedoria por padrão, sem aparecer como atributo visível do exercício. |
| RN-005 | Conclusão parcial concede XP interno de atributo proporcional. |
| RN-006 | Não há ganho de atributo por execução com dor forte ou conclusão falsa. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Evolui atributos. |
| Premium Mensal/Anual | Evolui atributos. |
| Acesso expirado | Mantém atributos, sem novo ganho. |

---

## 8. Fluxo principal

1. Usuário conclui um exercício.
2. Sistema lê a contribuição de atributos do exercício.
3. Aplica o XP interno aos atributos visíveis impactados e +1 XP interno de Sabedoria.
4. Dispara conversão (US-130) e RankScore (US-067).

---

## 9. Fluxos alternativos

### 9.1. Conclusão parcial

XP interno de atributo proporcional ao percentual concluído.

### 9.2. Dor forte

Sem ganho de atributo; pode conceder Sabedoria por feedback (US-131).

---

## 10. Estados esperados

- atributos atualizados;
- ganho proporcional;
- sem ganho (dor/conclusão falsa).

---

## 11. Impacto no Frontend Flutter

- Exibição dos 6 atributos atualizados.

---

## 12. Impacto no Backend

- Serviço de aplicação de XP interno de atributo por exercício.

---

## 13. Impacto no Banco de Dados

Entidades: `HunterAttributes`, `ExerciseAttributeContribution`.

Campos: `[attr]Points`, `[attr]Level`.

---

## 14. Impacto em Gamificação

- Alimenta diretamente o RankScore e o Rank.

---

## 15. Impacto em Monetização

- Evolução real reforça valor do app.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Labels de atributos. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
POST /api/exercises/{id}/complete
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| xp_earned | Inclui XP de atributo por exercício. |
| attribute_level_up | Quando um atributo sobe de Level (US-130). |

---

## 19. Critérios de aceite

### CA-001 — Atributos evoluem

Dado um exercício com `strengthXp`, `vitalityXp` e Sabedoria padrão,

Quando concluído,

Então Força e Vitalidade devem receber XP interno visível, e Sabedoria deve receber +1 XP interno por baixo dos panos.

### CA-002 — Proporcional na parcial

Dado conclusão parcial,

Quando o XP interno de atributo for aplicado,

Então deve ser proporcional ao concluído.

---

## 20. Critérios de teste para QA

### Backend

- XP interno dos 1 ou 2 atributos visíveis é aplicado corretamente;
- XP interno por atributo visível fica entre 1 e 4 conforme dificuldade efetiva;
- Sabedoria recebe +1 XP interno por exercício concluído;
- parcial gera proporcional;
- dor forte não concede atributo.

---

## ✅ Decisão registrada

> Os 6 atributos evoluem pela contribuição de cada exercício; essa evolução alimenta o RankScore e o Rank.
