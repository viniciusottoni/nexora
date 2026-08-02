---
title: US-127 — Ativar uma dungeon como side quest opcional
sidebar_position: 127
---

# US-127 — Ativar uma dungeon como side quest opcional

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-127 |
| Épico | EPIC-006 — Geração de Quests (Diária e Dungeon) |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário com acesso ativo |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Quest (type=dungeon) |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **ativar uma dungeon como side quest opcional quando ela estiver disponível ou com uma Pedra de Dungeon**,

para **treinar além da rotina diária quando ela estiver disponível ou quando eu usar uma Pedra de Dungeon**.

---

## 3. Contexto

As dungeons são treinos pontuais e opcionais. Elas aparecem nas segundas, quartas e sextas, ou fora dessa janela quando o usuário usa uma `Pedra de Dungeon`. Antes da ativação, o usuário escolhe se a dungeon será `solo` ou `grupo`. Elas complementam o loop diário, mas não substituem a quest diária nem desativam sua penalidade.

---

## 4. Objetivo

Permitir que o usuário ative uma dungeon nos dias permitidos ou consumindo uma `Pedra de Dungeon`, escolhendo antes o modo `solo` ou `grupo`, e então gerar uma quest `type=dungeon` compatível.

---

## 5. Escopo

### Entra nesta US

- Ativação manual de dungeon nas segundas, quartas e sextas, ou com `Pedra de Dungeon`.
- Escolha prévia do modo `solo` ou `grupo`.
- Bloqueio claro quando a dungeon estiver fora da janela semanal e não houver pedra disponível.
- Geração da dungeon compatível (US-128).
- Coexistência com a quest diária.

### Fora desta US

- Geração da dungeon em detalhe (US-128).
- Raids e Master Quests (Pós-MVP).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Dungeons são opcionais e ativadas pelo usuário. |
| RN-002 | Antes da ativação, o usuário deve escolher o modo da dungeon: `solo` ou `grupo`. |
| RN-003 | Dungeons só podem ser ativadas nas segundas, quartas e sextas, ou fora dessa janela com uma `Pedra de Dungeon`. |
| RN-004 | A `Pedra de Dungeon`, quando usada, é consumida para liberar a ativação fora da janela semanal. |
| RN-005 | Dungeon não substitui a quest diária. |
| RN-006 | Concluir dungeon concede XP/atributos, mas não evita a penalidade da daily não feita. |
| RN-007 | Requer acesso ativo para ativar/gerar. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Ativa dungeon. |
| Premium Mensal/Anual | Ativa dungeon. |
| Acesso expirado | Não ativa (US-043). |

---

## 8. Fluxo principal

1. Usuário escolhe ativar uma dungeon.
2. Usuário define se a dungeon será `solo` ou `grupo`.
3. Sistema valida se a dungeon está disponível na janela semanal ou se o usuário possui uma `Pedra de Dungeon`.
4. Se estiver fora da janela semanal, o usuário usa a pedra para liberar a ativação.
5. Sistema gera a dungeon compatível (US-128).
6. App exibe a dungeon como side quest, sem afetar a daily.

---

## 9. Fluxos alternativos

### 9.1. Daily ainda pendente

A dungeon não substitui nem oculta a quest diária pendente.

### 9.2. Fora da janela semanal sem pedra

A dungeon não é liberada. O app informa a indisponibilidade e mantém a daily intacta.

---

## 10. Estados esperados

- dungeon ativada;
- dungeon gerada;
- bloqueado por acesso.

---

## 11. Impacto no Frontend Flutter

- Entrada para ativar dungeon.
- Indicação clara de disponibilidade semanal e de uso de `Pedra de Dungeon`.
- Exibição separada da quest diária.

---

## 12. Impacto no Backend

- Endpoint de ativação/geração de dungeon (`type=dungeon`).
- Validação da janela semanal e do uso da `Pedra de Dungeon`.

---

## 13. Impacto no Banco de Dados

Entidade: `Quest`.

Campos: `type=dungeon`, `userId`, `createdAt`.

---

## 14. Impacto em Gamificação

- Dungeon concluída concede XP/atributos e pode conceder itens.

---

## 15. Impacto em Monetização

- Conteúdo opcional aumenta engajamento e valor percebido.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Textos da dungeon. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
POST /api/quests/dungeon/activate
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| dungeon_generated | Quando a dungeon é ativada/gerada. |

---

## 19. Critérios de aceite

### CA-001 — Ativação opcional

Dado um usuário com acesso ativo,

Quando ativar uma dungeon,

Então deve receber uma side quest sem afetar a quest diária, após escolher `solo` ou `grupo`.

### CA-002 — Não substitui a daily

Dado que a quest diária está pendente,

Quando o usuário ativar uma dungeon,

Então a daily deve permanecer pendente e visível, e o modo da dungeon deve ter sido definido antes da geração.

### CA-003 — Fora da janela semanal

Dado que hoje não é segunda, quarta ou sexta,

Quando o usuário tentar ativar uma dungeon sem `Pedra de Dungeon`,

Então a ativação deve ser bloqueada com mensagem clara de indisponibilidade.

---

## 20. Critérios de teste para QA

### Frontend

- exibe entrada de dungeon apenas quando disponível por calendário ou quando há `Pedra de Dungeon`;
- bloqueia ativação fora da janela semanal sem pedra;
- mantém a daily visível.

### Backend

- ativação gera `type=dungeon`;
- modo `solo` ou `grupo` é definido antes da geração;
- janela semanal é validada antes da geração;
- dungeon não altera a daily;
- acesso expirado bloqueia a ativação.

### API

- `POST /api/quests/dungeon/activate` aceita o modo da dungeon;
- a resposta bloqueia fora da janela semanal sem pedra;
- a resposta mantém a daily intacta;
- a pedra, quando usada, é consumida.

### E2E

- usuário ativa dungeon em dia permitido e a daily continua intacta;
- usuário fora da janela semanal só consegue ativar com `Pedra de Dungeon`.

---

## ✅ Decisão registrada

> Dungeons são side quests opcionais ativadas nos dias permitidos ou com `Pedra de Dungeon`; não substituem a quest diária nem evitam sua penalidade.
