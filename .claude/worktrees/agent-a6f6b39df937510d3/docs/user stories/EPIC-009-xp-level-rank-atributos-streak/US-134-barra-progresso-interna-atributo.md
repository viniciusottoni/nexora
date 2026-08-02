---
title: US-134 — Visualizar barra de progresso interna de cada atributo no perfil
sidebar_position: 134
---

# US-134 — Visualizar barra de progresso interna de cada atributo no perfil

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-134 |
| Épico | EPIC-009 — Sistema de XP, Level, Rank, Atributos e Streak |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário com acesso ativo |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | HunterAttributes |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **ver a barra de progresso interna de cada atributo**,

para **entender quanto falta para o próximo Level de cada atributo**.

---

## 3. Contexto

Cada atributo tem XP interno (0–9) até o próximo Level. Exibir esse progresso ajuda o usuário a perceber a evolução granular, complementando os Levels exibidos no perfil (US-074).

---

## 4. Objetivo

Exibir a barra de XP interno (0–9) de cada um dos 6 atributos no perfil.

---

## 5. Escopo

### Entra nesta US

- Barra de progresso interna por atributo (0–9).
- Indicação de quanto falta para o próximo Level.

### Fora desta US

- Cálculo do XP interno (US-130).
- Exibição de Level por atributo (US-074).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A barra reflete o XP interno real (0–9) do atributo. |
| RN-002 | A barra deve indicar a proximidade do próximo Level. |
| RN-003 | Acesso expirado exibe estado limitado. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Vê as barras. |
| Premium Mensal/Anual | Vê as barras. |
| Acesso expirado | Estado limitado. |

---

## 8. Fluxo principal

1. Usuário acessa o perfil.
2. App carrega XP interno dos atributos.
3. Exibe a barra de progresso de cada atributo.

---

## 9. Fluxos alternativos

### 9.1. Atributo recém-evoluído

Barra reinicia em 0 após o level up.

---

## 10. Estados esperados

- barras carregadas;
- atributo recém-evoluído;
- estado limitado.

---

## 11. Impacto no Frontend Flutter

- Componente de barra interna por atributo.

---

## 12. Impacto no Backend

- Retorno do XP interno por atributo.

---

## 13. Impacto no Banco de Dados

Entidade: `HunterAttributes`.

Campos: `[attr]Xp`/`[attr]Points` interno (0–9).

---

## 14. Impacto em Gamificação

- Mostra evolução granular e motiva o próximo treino.

---

## 15. Impacto em Monetização

- Detalhe de progresso aumenta engajamento.

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
GET /api/hunter/progress
```

Response conceitual:

```json
{ "attributes": { "strength": { "level": 3, "xp": 4 } } }
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| hunter_progress_viewed | Quando o perfil é exibido. |

---

## 19. Critérios de aceite

### CA-001 — Barra por atributo

Dado que o usuário acessa o perfil,

Quando as barras forem exibidas,

Então cada atributo deve mostrar seu XP interno (0–9).

### CA-002 — Reinício após level up

Dado um atributo que acabou de subir de Level,

Quando a barra for exibida,

Então deve reiniciar conforme o XP interno restante.

---

## 20. Critérios de teste para QA

### Frontend (Flutter)

- barra reflete XP interno;
- reinício após level up;
- estado limitado em acesso expirado;
- textos em PT-BR, EN, ES.

---

## ✅ Decisão registrada

> A barra de progresso interna mostra o XP interno (0–9) de cada atributo, complementando os Levels exibidos no perfil.
