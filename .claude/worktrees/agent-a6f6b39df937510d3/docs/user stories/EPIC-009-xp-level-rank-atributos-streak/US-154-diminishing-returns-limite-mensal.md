---
title: US-154 — Aplicar diminishing returns e limite mensal de RankScore
sidebar_position: 154
---

# US-154 — Aplicar diminishing returns e limite mensal de RankScore

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-154 |
| Épico | EPIC-009 — Sistema de XP, Level, Rank, Atributos e Streak |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | Não aplicável (cálculo interno) |
| Dependência principal | HunterProgress (RankScore) |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **aplicar diminishing returns por Rank e limite mensal de RankScore**,

para **impedir que o usuário alcance Ranks altos rápido demais e manter a economia de progressão**.

---

## 3. Contexto

A curva de Rank deve ser exponencial e o SSS deve representar cerca de 3 anos de treino constante. Para isso, a partir do Rank A aplica-se um multiplicador de progresso, e o ganho mensal de RankScore acima do saudável sofre redução.

---

## 4. Objetivo

Aplicar multiplicadores de diminishing returns por Rank e limitar o ganho mensal de RankScore.

---

## 5. Escopo

### Entra nesta US

- Multiplicadores por Rank: B 0.90, A 0.80, S 0.70, SS 0.60 (E/D/C = 1.00).
- Cálculo do RankScore efetivo após multiplicador.
- Limite mensal saudável por perfil e redução acima de ~24/mês.
- Registro de auditoria (`RankScoreLog`).

### Fora desta US

- Cálculo do Rank (US-067).
- Detecção de abuso comportamental (US-155).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A partir do Rank A, aplicar multiplicador de progresso ao ganho de RankScore. |
| RN-002 | Multiplicadores: B 0.90, A 0.80, S 0.70, SS 0.60; E/D/C = 1.00. |
| RN-003 | Os atributos continuam evoluindo normalmente; apenas o RankScore desacelera. |
| RN-004 | Ganho mensal acima de ~24 RankScore sofre redução/validação. |
| RN-005 | O bônus de streak (US-069) também está sujeito a estes limites. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema | Aplica os multiplicadores/limites. |
| Usuário final | Tem progressão equilibrada. |

---

## 8. Fluxo principal

1. Sistema calcula o ganho bruto de RankScore.
2. Aplica o multiplicador do Rank atual.
3. Verifica o acumulado mensal.
4. Acima do saudável, aplica redução; registra no `RankScoreLog`.

---

## 9. Fluxos alternativos

### 9.1. Acima de 24/mês

Aplica diminishing returns mais forte e sinaliza para validação.

---

## 10. Estados esperados

- ganho pleno;
- ganho reduzido por Rank;
- ganho reduzido por limite mensal.

---

## 11. Impacto no Frontend Flutter

- Indicação opcional de progresso de RankScore mais lento em Ranks altos.

---

## 12. Impacto no Backend

- Serviço de diminishing returns e limite mensal.
- Registro em `RankScoreLog`.

---

## 13. Impacto no Banco de Dados

Entidades: `HunterProgress`, `RankScoreLog`.

Campos: `rankScore`, `monthlyRankScoreGain`, `source`, `rawGain`, `multiplier`, `effectiveGain`.

---

## 14. Impacto em Gamificação

- Protege os Ranks altos e mantém a curva exponencial.

---

## 15. Impacto em Monetização

- Progressão de longo prazo sustenta assinatura contínua.

---

## 16. Impacto em Internacionalização

- Cálculo interno; sem textos.

---

## 17. Contrato de API sugerido

```txt
(interno) applyRankScoreGain(userId, rawGain, source)
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| rank_diminishing_returns_applied | Quando há redução por Rank alto. |
| rank_progress_monthly_limit_reached | Quando o limite mensal é atingido. |

---

## 19. Critérios de aceite

### CA-001 — Diminishing returns

Dado um usuário Rank S que ganhou 10 pontos válidos,

Quando o ganho for aplicado,

Então o RankScore efetivo deve ser 7 (10 × 0.70).

### CA-002 — Limite mensal

Dado um ganho mensal acima de ~24 RankScore,

Quando processado,

Então o sistema deve aplicar redução/validação.

---

## 20. Critérios de teste para QA

### Backend

- multiplicadores corretos por Rank;
- atributos não são afetados pelo multiplicador;
- redução acima de 24/mês;
- bônus de streak também limitado;
- auditoria registrada.

---

## ✅ Decisão registrada

> A partir do Rank A, o ganho de RankScore sofre diminishing returns, e o ganho mensal acima do saudável é reduzido — preservando a curva exponencial sem afetar a evolução dos atributos.
