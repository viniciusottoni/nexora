---
title: US-130 — Acumular XP interno de atributo e subir o Level ao atingir 10
sidebar_position: 130
---

# US-130 — Acumular XP interno de atributo e subir o Level ao atingir 10

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-130 |
| Épico | EPIC-009 — Sistema de XP, Level, Rank, Atributos e Streak |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | Não aplicável (cálculo interno) |
| Dependência principal | HunterAttributes |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **acumular XP interno de atributo e subir o Level ao atingir 10**,

para **converter o XP de atributo em evolução visível (0–10 por atributo)**.

---

## 3. Contexto

Cada atributo tem Level (0–10) e XP interno (0–9). Ao acumular 10 XP internos, o Level sobe em 1 e o XP interno volta a 0, preservando excedente quando houver. Cada ponto de Level conta para o RankScore (US-067).

---

## 4. Objetivo

Aplicar a conversão de XP interno em Level de atributo e disparar o recálculo de RankScore.

---

## 5. Escopo

### Entra nesta US

- Acúmulo de XP interno por atributo.
- Conversão: 10 XP internos → +1 Level, XP interno volta a 0, preservando excedente.
- Disparo do recálculo de RankScore (US-067).

### Fora desta US

- Aplicação do XP por exercício (US-068).
- Exibição (US-074/US-134).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Cada atributo tem Level (0–10) e XP interno (0–9). |
| RN-002 | Ao acumular 10 XP internos, o Level sobe em 1 e o XP interno volta a 0, preservando excedente. |
| RN-003 | Múltiplos level ups de uma vez são aplicados quando o ganho excede 10. |
| RN-004 | Cada level up de atributo atualiza o RankScore. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema | Aplica a conversão. |
| Usuário final | Vê o Level do atributo subir. |

---

## 8. Fluxo principal

1. Sistema recebe XP interno de atributo (US-068).
2. Acumula XP interno.
3. Ao atingir 10, sobe o Level e ajusta o XP interno restante.
4. Recalcula o RankScore (US-067).

---

## 9. Fluxos alternativos

### 9.1. Ganho grande

Se o ganho exceder múltiplos de 10, aplica vários level ups.

### 9.2. Teto do atributo

Ao atingir Level 10, o atributo respeita o teto definido.

---

## 10. Estados esperados

- XP interno acumulado;
- level up de atributo;
- múltiplos level ups;
- teto atingido.

---

## 11. Impacto no Frontend Flutter

- Barra interna do atributo (US-134) e feedback (US-071).

---

## 12. Impacto no Backend

- Serviço de conversão XP interno → Level.

---

## 13. Impacto no Banco de Dados

Entidade: `HunterAttributes`.

Campos: `[attr]Xp`/`[attr]Points` interno (0–9), `[attr]Level` (0–10).

---

## 14. Impacto em Gamificação

- Converte esforço em evolução visível e alimenta o RankScore.

---

## 15. Impacto em Monetização

- Progresso granular aumenta engajamento.

---

## 16. Impacto em Internacionalização

- Cálculo interno; rótulos traduzidos na exibição.

---

## 17. Contrato de API sugerido

```txt
(interno) applyAttributeXp(attr, xp)
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| attribute_level_up | Quando um atributo sobe de Level. |

---

## 19. Critérios de aceite

### CA-001 — Level up de atributo

Dado um atributo com 8 XP internos,

Quando ganhar 5 XP internos,

Então deve subir 1 Level e ficar com 3 XP internos.

### CA-002 — RankScore atualizado

Dado um level up de atributo,

Quando aplicado,

Então o RankScore deve ser recalculado.

---

## 20. Critérios de teste para QA

### Backend

- 10 XP internos sobem 1 Level e ajustam o saldo interno;
- ganhos grandes aplicam múltiplos level ups;
- teto de Level respeitado;
- RankScore recalculado a cada level up.

---

## ✅ Decisão registrada

> XP interno de atributo (0–9) converte-se em Level de atributo (0–10) ao atingir 10, alimentando o RankScore.
