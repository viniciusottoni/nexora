---
title: US-067 — Evoluir de Rank (E→SSS) via RankScore e curva exponencial
sidebar_position: 67
---

# US-067 — Evoluir de Rank (E→SSS) via RankScore e curva exponencial

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-067 |
| Épico | EPIC-009 — Sistema de XP, Level, Rank, Atributos e Streak |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema e usuário com acesso ativo |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | HunterProgress (RankScore, Rank) |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **evoluir de Rank conforme meu RankScore cresce**,

para **ver minha evolução física acumulada refletida em um patamar claro (E→SSS)**.

---

## 3. Contexto

O Rank representa o patamar de evolução física acumulada e é derivado do **RankScore** (soma dos pontos reais dos 6 atributos). A progressão é aproximadamente exponencial: rápida no início e lenta nos Ranks altos, de modo que o SSS represente cerca de 3 anos de treino constante.

---

## 4. Objetivo

Calcular o RankScore, recalcular o Rank por `calculateRank` e aplicar a curva exponencial e o teto inicial do onboarding.

---

## 5. Escopo

### Entra nesta US

- Cálculo do RankScore = soma dos pontos reais dos 6 atributos.
- `calculateRank(rankScore)` com os limiares E→SSS.
- Recálculo do Rank sempre que o RankScore muda.
- Respeito ao teto inicial do onboarding (Rank B / RankScore 48).
- Fontes de RankScore: atributos (treino) e bônus controlado de streak (US-069).
- Nomes narrativos opcionais por Rank (exibição em EPIC-010).

### Fora desta US

- Diminishing returns e limite mensal (US-154).
- Proteção contra abuso (US-155).
- Cálculo do Rank inicial no onboarding (EPIC-004 / US-156).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | RankScore = soma dos pontos reais dos 6 atributos válidos para Rank. |
| RN-002 | O Rank é recalculado por `calculateRank(rankScore)` sempre que o RankScore muda. |
| RN-003 | Limiares: E 6–17, D 18–29, C 30–47, B 48–83, A 84–155, S 156–299, SS 300–587, SSS 588+. |
| RN-004 | A curva é aproximadamente exponencial; o SSS exige cerca de 3 anos de treino constante. |
| RN-005 | Rank A ou superior só pode ser obtido com treino real (teto do onboarding é B). |
| RN-006 | RankScore não pode ser comprado nem concedido sem esforço real. |
| RN-007 | O Rank é exibido como progresso, nunca como julgamento físico. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Evolui Rank com treino real. |
| Premium Mensal/Anual | Evolui Rank com treino real. |
| Acesso expirado | Mantém Rank, sem novo ganho. |

---

## 8. Fluxo principal

1. Atributos do usuário evoluem (US-068).
2. Sistema recalcula o RankScore.
3. Aplica `calculateRank(rankScore)`.
4. Se o Rank mudou, registra e exibe o rank up.

---

## 9. Fluxos alternativos

### 9.1. Teto de onboarding

No onboarding, o RankScore é limitado a 48 (Rank B) — ver EPIC-004 / US-156.

### 9.2. Queda de Rank

Se o RankScore efetivo cair (ex.: ajuste), o Rank é recalculado para baixo de forma coerente.

---

## 10. Estados esperados

- RankScore atualizado;
- rank up;
- sem mudança de Rank;
- teto aplicado (onboarding).

---

## 11. Impacto no Frontend Flutter

- Indicador de Rank e progresso de RankScore até o próximo Rank.
- Animação de rank up.

---

## 12. Impacto no Backend

- Serviço de RankScore e `calculateRank`.
- Recálculo transacional ao mudar atributos.

---

## 13. Impacto no Banco de Dados

Entidade: `HunterProgress`.

Campos: `rankScore`, `rank`.

---

## 14. Impacto em Gamificação

- Patamar central de evolução física acumulada.

---

## 15. Impacto em Monetização

- Progressão de longo prazo incentiva assinatura contínua.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Rótulos de Rank e nomes narrativos. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/hunter/progress
```

Response conceitual:

```json
{ "rank": "C", "rankScore": 34, "rankScoreToNext": 48 }
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| rank_score_changed | Quando o RankScore muda. |
| rank_changed | Quando o usuário sobe ou desce de Rank. |

---

## 19. Critérios de aceite

### CA-001 — Rank por RankScore

Dado um usuário com RankScore 40,

Quando o Rank for calculado,

Então deve ser Rank C (30–47).

### CA-002 — Rank A por treino real

Dado que um usuário está em Rank B,

Quando acumular RankScore suficiente por treino real,

Então poderá subir para Rank A.

---

## 20. Critérios de teste para QA

### Backend

- RankScore = soma dos atributos;
- `calculateRank` retorna o Rank correto em cada faixa;
- recálculo ocorre ao mudar atributos;
- A+ não é alcançável pelo onboarding.

---

## ✅ Decisão registrada

> O Rank deriva do RankScore via curva exponencial; A+ exige treino real e o SSS representa cerca de 3 anos de consistência.
