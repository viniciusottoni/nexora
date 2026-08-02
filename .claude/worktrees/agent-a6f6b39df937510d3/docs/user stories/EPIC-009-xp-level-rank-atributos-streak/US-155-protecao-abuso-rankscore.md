---
title: US-155 — Proteger o RankScore contra ganho artificial e abuso
sidebar_position: 155
---

# US-155 — Proteger o RankScore contra ganho artificial e abuso

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-155 |
| Épico | EPIC-009 — Sistema de XP, Level, Rank, Atributos e Streak |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | Não aplicável (regra interna) |
| Dependência principal | HunterProgress, QuestLog |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **impedir ganho artificial de RankScore e detectar abuso**,

para **que o Rank represente esforço real e consistência**.

---

## 3. Contexto

O RankScore não pode ser farmado. O sistema deve reduzir ou anular ganhos vindos de treinos repetidos artificialmente, conclusões falsas, execução com dor forte, ausência de progressão ou treinos incompatíveis usados só por XP.

---

## 4. Objetivo

Aplicar regras anti-abuso ao ganho de RankScore e sinalizar comportamentos anormais.

---

## 5. Escopo

### Entra nesta US

- Não conceder RankScore por: treino curto repetido para farmar, conclusão falsa, ignorar dor forte, mesmo exercício sem progressão, pular grande parte da quest, treino incompatível só por XP.
- Reduzir ganho por: treino parcial, fácil demais repetido, sem progressão, repetido artificialmente, dor forte, baixa qualidade de execução.
- Exigência de variedade mínima e progressão real.
- Sinalização de abuso para validação.

### Fora desta US

- Diminishing returns/limite mensal (US-154).
- Cálculo do Rank (US-067).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | RankScore não é concedido por ações sem esforço real. |
| RN-002 | Treino repetido artificialmente tem ganho reduzido ou anulado. |
| RN-003 | Execução com dor forte ou conclusão falsa não gera RankScore. |
| RN-004 | Ausência de progressão e baixa qualidade reduzem o ganho. |
| RN-005 | Comportamento anormal deve ser sinalizado para validação. |
| RN-006 | A proteção nunca incentiva o usuário a treinar com dor para "provar" esforço. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema | Aplica a proteção. |
| Usuário final | Tem progressão justa. |

---

## 8. Fluxo principal

1. Sistema avalia o contexto do ganho (histórico, variedade, progressão, feedback).
2. Anula/reduz ganhos artificiais.
3. Sinaliza padrões anormais para validação.

---

## 9. Fluxos alternativos

### 9.1. Padrão suspeito

Emite `rank_abuse_suspected` e reduz o ganho até validação.

---

## 10. Estados esperados

- ganho válido;
- ganho reduzido;
- ganho anulado;
- abuso sinalizado.

---

## 11. Impacto no Frontend Flutter

- Sem incentivo a treino com dor; comunicação neutra.

---

## 12. Impacto no Backend

- Heurísticas anti-abuso e sinalização.
- Registro em `RankScoreLog`.

---

## 13. Impacto no Banco de Dados

Entidades: `HunterProgress`, `QuestLog`, `RankScoreLog`.

---

## 14. Impacto em Gamificação

- Garante que o Rank reflita esforço real.

---

## 15. Impacto em Monetização

- Integridade da progressão sustenta a confiança no produto.

---

## 16. Impacto em Internacionalização

- Regra interna; mensagens neutras quando exibidas.

---

## 17. Contrato de API sugerido

```txt
(interno) validateRankScoreGain(userId, context)
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| rank_abuse_suspected | Quando um padrão anormal é detectado. |
| rank_score_changed | Quando o ganho (reduzido/anulado) é aplicado. |

---

## 19. Critérios de aceite

### CA-001 — Sem farm artificial

Dado que o usuário repete um treino curto só para farmar,

Quando o ganho for avaliado,

Então o RankScore não deve ser concedido de forma plena.

### CA-002 — Sem recompensa por dor

Dado que houve dor forte,

Quando o ganho for avaliado,

Então não deve haver RankScore por execução com dor.

---

## 20. Critérios de teste para QA

### Backend

- treino repetido artificialmente é reduzido/anulado;
- conclusão falsa/dor forte não geram RankScore;
- ausência de progressão reduz o ganho;
- abuso é sinalizado;
- proteção não incentiva treino com dor.

---

## ✅ Decisão registrada

> O RankScore é protegido contra ganho artificial e abuso, garantindo que o Rank represente esforço real e consistência, sem nunca incentivar treino com dor.
