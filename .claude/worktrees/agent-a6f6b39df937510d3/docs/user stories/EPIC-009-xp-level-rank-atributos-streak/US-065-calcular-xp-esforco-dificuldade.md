---
title: US-065 — Calcular XP por esforço, dificuldade e conclusão do exercício
sidebar_position: 65
---

# US-065 — Calcular XP por esforço, dificuldade e conclusão do exercício

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-065 |
| Épico | EPIC-009 — Sistema de XP, Level, Rank, Atributos e Streak |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | Não aplicável (cálculo interno) |
| Dependência principal | HunterProgress |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **calcular o XP por esforço, dificuldade e grau de conclusão do exercício**,

para **recompensar de forma justa e proporcional ao treino realizado**.

---

## 3. Contexto

O valor do XP deve refletir o esforço real: dificuldade do exercício, percentual concluído e qualidade da execução. Recompensas devem ser justas, sem incentivar execução com dor.

---

## 4. Objetivo

Definir a fórmula de XP por exercício, considerando dificuldade, conclusão e esforço.

---

## 5. Escopo

### Entra nesta US

- Cálculo de XP geral por exercício.
- Proporcionalidade por conclusão do exercício (100%, parcial).
- Influência de dificuldade/intensidade.
- Base para penalidade (valor de referência do ganho médio).

### Fora desta US

- Concessão (US-064).
- XP de atributo (US-068).
- RankScore (US-067).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | XP cresce com dificuldade e grau de conclusão do exercício. |
| RN-002 | Conclusão parcial gera XP proporcional. |
| RN-003 | O cálculo não pode recompensar execução com dor forte. |
| RN-004 | O ganho médio da quest é a referência para a penalidade (US-132). |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema | Calcula o XP. |
| Usuário final | Recebe o XP resultante. |

---

## 8. Fluxo principal

1. Sistema recebe dados de conclusão do exercício (sets/reps/tempo/RPE/feedback).
2. Calcula XP por dificuldade e percentual concluído.
3. Retorna o XP a ser concedido.

---

## 9. Fluxos alternativos

### 9.1. Dor forte relatada

O cálculo não aumenta XP por esforço com dor forte; prioriza segurança.

---

## 10. Estados esperados

- calculado;
- proporcional (parcial);
- ajustado por segurança.

---

## 11. Impacto no Frontend Flutter

- Exibição do XP calculado na recompensa.

---

## 12. Impacto no Backend

- Serviço de cálculo de XP por esforço/dificuldade/conclusão.

---

## 13. Impacto no Banco de Dados

Entidades: `HunterProgress`, `QuestLog`.

---

## 14. Impacto em Gamificação

- Define o ritmo de progressão de Level.

---

## 15. Impacto em Monetização

- Recompensa justa melhora retenção.

---

## 16. Impacto em Internacionalização

- Cálculo interno; sem textos.

---

## 17. Contrato de API sugerido

```txt
(interno) calculateExerciseXp(completionData)
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| xp_earned | Inclui o `amount` calculado. |

---

## 19. Critérios de aceite

### CA-001 — Proporcional à conclusão

Dado que o usuário concluiu 50% do exercício,

Quando o XP for calculado,

Então deve receber XP proporcional, conforme a regra do backend.

### CA-002 — Segurança no cálculo

Dado que houve dor forte,

Quando o XP for calculado,

Então o esforço com dor não deve aumentar o XP.

---

## 20. Critérios de teste para QA

### Backend

- XP cresce com dificuldade/conclusão do exercício;
- conclusão parcial gera proporcional;
- dor forte não aumenta XP;
- ganho médio fica disponível para a penalidade.

---

## ✅ Decisão registrada

> O XP é proporcional ao esforço, à dificuldade e ao grau de conclusão do exercício, sem recompensar execução com dor.
