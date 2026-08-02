---
title: US-156 — Calcular Rank e RankScore iniciais ao concluir o onboarding
sidebar_position: 156
---

# US-156 — Calcular Rank e RankScore iniciais ao concluir o onboarding

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-156 |
| Épico | EPIC-004 — Onboarding e Perfil Inicial do Hunter |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | UserProfile, HunterProgress, HunterAttributes |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **calcular o Rank e o RankScore iniciais ao concluir o onboarding**,

para **posicionar o usuário em um ponto de partida justo, sem premiar Rank alto sem histórico**.

---

## 3. Contexto

O onboarding é baseado em declaração do usuário (experiência, tempo de treino, tipo de corpo, dados físicos). Ele pode reconhecer um usuário mais avançado, mas não comprova consistência, execução nem progressão. Por isso o Rank inicial tem teto **B (RankScore 48)** e o Level inicial é sempre **1**. A curva e a função `calculateRank` pertencem ao EPIC-009.

---

## 4. Objetivo

Derivar atributos iniciais a partir das respostas do onboarding, calcular o RankScore (limitado a 48), aplicar `calculateRank` e definir o Rank inicial e Level 1.

---

## 5. Escopo

### Entra nesta US

- Derivação de pontos iniciais dos 6 atributos a partir das respostas.
- Cálculo do RankScore inicial = soma dos atributos iniciais.
- Aplicação do teto: RankScore inicial máximo 48 (Rank B).
- Aplicação de `calculateRank(rankScore)` (EPIC-009).
- Definição de Level 1.
- Persistência em `HunterProgress`/`HunterAttributes`.

### Fora desta US

- Curva e `calculateRank` (definidos no EPIC-009).
- Evolução pós-onboarding de Rank (EPIC-009 / US-067).
- Concessão de XP (onboarding não concede XP).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O RankScore inicial é a soma dos atributos iniciais derivados do onboarding. |
| RN-002 | O RankScore inicial é limitado a 48 (Rank B). |
| RN-003 | Ranks A, S, SS e SSS não podem ser atribuídos pelo onboarding. |
| RN-004 | O Level inicial é sempre 1, mesmo iniciando em Rank B. |
| RN-005 | O cálculo deve respeitar conflitos do perfil (usar nível efetivo conservador). |
| RN-006 | O onboarding não concede XP geral. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Recebe Rank inicial. |
| Premium Mensal/Anual | Recebe Rank inicial. |
| Acesso expirado | Não conclui onboarding (US-033). |

---

## 8. Fluxo principal

1. Usuário conclui o onboarding (US-033).
2. Sistema deriva os atributos iniciais das respostas.
3. Calcula o RankScore inicial e aplica o teto 48.
4. Aplica `calculateRank` e define Level 1.
5. Persiste o progresso inicial.

---

## 9. Fluxos alternativos

### 9.1. Perfil muito avançado declarado

Mesmo com respostas avançadas, o Rank inicial não passa de B (RankScore 48).

### 9.2. Conflito de nível

Aplica o nível efetivo conservador na derivação dos atributos.

---

## 10. Estados esperados

- atributos iniciais derivados;
- RankScore com teto aplicado;
- Rank inicial e Level 1 definidos.

---

## 11. Impacto no Frontend Flutter

- Exibição do Rank/Level inicial na primeira visita ao perfil.

---

## 12. Impacto no Backend

- Serviço de derivação de atributos iniciais.
- Reuso de `calculateRank` e do teto (EPIC-009).

---

## 13. Impacto no Banco de Dados

Entidades: `UserProfile`, `HunterProgress`, `HunterAttributes`.

Campos: `[attr]Points`, `[attr]Level`, `rankScore`, `rank`, `level`.

---

## 14. Impacto em Gamificação

- Define o ponto de partida da progressão, sem premiar Rank alto sem histórico.

---

## 15. Impacto em Monetização

- Ponto de partida coerente melhora a percepção de valor no trial.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Rótulos de Rank/atributos. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
POST /api/users/me/profile/complete-onboarding
```

Response conceitual:

```json
{
  "rank": "B",
  "rankScore": 48,
  "level": 1,
  "attributes": { "strength": 8, "agility": 8, "endurance": 8, "vitality": 8, "focus": 8, "wisdom": 8 }
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| rank_cap_applied_onboarding | Quando o teto 48 é aplicado no onboarding. |
| rank_changed | Quando o Rank inicial é definido. |

---

## 19. Critérios de aceite

### CA-001 — Teto do onboarding

Dado que o usuário concluiu o onboarding,

Quando o Rank inicial for calculado,

Então o RankScore não pode ultrapassar 48 e o Rank máximo é B.

### CA-002 — Level inicial 1

Dado que o Rank inicial é B,

Quando o perfil for criado,

Então o Level inicial deve ser 1.

---

## 20. Critérios de teste para QA

### Backend

- RankScore inicial = soma dos atributos derivados;
- teto 48 aplicado mesmo com respostas avançadas;
- Rank máximo inicial é B;
- Level inicial é 1;
- onboarding não concede XP geral.

### E2E

- usuário avançado começa no máximo em Rank B / Level 1;
- evento `rank_cap_applied_onboarding` é emitido quando o teto incide.

---

## ✅ Decisão registrada

> O onboarding define o ponto de partida com teto Rank B / RankScore 48 e Level 1; Ranks A+ exigem treino real registrado no app (EPIC-009).
