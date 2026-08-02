---
title: US-047 — Salvar a quest do dia
sidebar_position: 47
---

# US-047 — Salvar a quest do dia

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-047 |
| Épico | EPIC-006 — Geração de Quests (Diária e Dungeon) |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário com acesso ativo |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Quest |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **que minha quest do dia fique salva**,

para **não perder o treino ao fechar o app**.

---

## 3. Contexto

A quest diária deve ser persistida assim que gerada, para que o usuário retome o treino do mesmo ponto, mesmo após fechar o app ou perder conexão.

---

## 4. Objetivo

Persistir a quest do dia e permitir recuperá-la durante o dia corrente.

---

## 5. Escopo

### Entra nesta US

- Persistência da quest no momento da geração.
- Recuperação da quest do dia.
- Idempotência: uma quest diária por dia por usuário.

### Fora desta US

- Regeneração (US-048).
- Execução/registro (EPIC-008).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A quest do dia deve ser persistida ao ser gerada. |
| RN-002 | Deve existir no máximo uma quest diária por usuário por dia. |
| RN-003 | A quest persistida deve ser recuperável durante o dia corrente. |
| RN-004 | O progresso de execução não deve ser perdido ao fechar o app. |
| RN-005 | A quest persistida nasce com estado "não confirmada" e só passa para "confirmada" após o usuário confirmar a notificação de quest gerada (US-042, RN-007). |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Quest salva. |
| Premium Mensal/Anual | Quest salva. |
| Acesso expirado | Não gera (US-043). |

---

## 8. Fluxo principal

1. Sistema gera a quest diária.
2. Persiste a quest e seus exercícios.
3. Ao reabrir o app, recupera a quest do dia.

---

## 9. Fluxos alternativos

### 9.1. Falha de rede ao gerar

Persistir assim que possível; evitar duplicar a quest do dia.

---

## 10. Estados esperados

- quest persistida;
- quest recuperada;
- erro de persistência.

---

## 11. Impacto no Frontend Flutter

- Recuperação da quest ao abrir o app.
- Estado de retomada do treino.

---

## 12. Impacto no Backend

- Persistência idempotente da quest diária.
- Endpoint de recuperação da quest do dia.

---

## 13. Impacto no Banco de Dados

Entidades: `Quest`, `QuestExercise`.

Campos: `userId`, `date`, `type=daily`, status de progresso, flag de confirmação da notificação (`confirmedAt`/`viewed`).

---

## 14. Impacto em Gamificação

- Garante continuidade do treino e do ganho de XP.

---

## 15. Impacto em Monetização

- Evita frustração e perda de treino, favorecendo retenção.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de retomada. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/quests/daily/today
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| quest_viewed | Quando a quest salva é recuperada e exibida. |

---

## 19. Critérios de aceite

### CA-001 — Persistência

Dado que a quest do dia foi gerada,

Quando o usuário fechar e reabrir o app,

Então a mesma quest deve ser recuperada.

### CA-002 — Uma por dia

Dado que já existe quest do dia,

Quando o app solicitar novamente,

Então deve retornar a mesma quest, sem duplicar.

---

## 20. Critérios de teste para QA

### Backend

- quest é persistida ao gerar;
- no máximo uma quest diária por dia;
- recuperação retorna a quest correta.

### E2E

- fechar/reabrir o app mantém a quest e o progresso.

---

## ✅ Decisão registrada

> A quest do dia é persistida e recuperável, com no máximo uma por usuário por dia, evitando perda de treino.
