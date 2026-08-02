---
title: US-070 — Preservar streak com regra clara de virada de dia
sidebar_position: 70
---

# US-070 — Preservar streak com regra clara de virada de dia

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-070 |
| Épico | EPIC-009 — Sistema de XP, Level, Rank, Atributos e Streak |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | HunterProgress |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **aplicar uma regra clara de virada de dia para o streak**,

para **que o usuário entenda quando o streak é mantido, reiniciado ou penalizado**.

---

## 3. Contexto

A virada de dia define se o streak continua ou reinicia e dispara a penalidade de XP da daily não completada (US-132). A regra precisa ser previsível e justa, sem punição visual agressiva.

---

## 4. Objetivo

Definir e aplicar a regra de virada de dia para streak e penalidade, de forma previsível.

---

## 5. Escopo

### Entra nesta US

- Definição do fuso/horário de virada de dia.
- Manutenção ou reinício do streak.
- Acionamento da penalidade da daily não completada (US-132).
- Aplicação apenas com acesso ativo.

### Fora desta US

- Cálculo do valor da penalidade (US-132).
- Bônus de streak (US-069).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A virada de dia usa um critério de fuso/horário consistente. |
| RN-002 | Concluir a daily mantém/incrementa o streak; não concluir reinicia conforme a regra. |
| RN-003 | A penalidade só é aplicada a usuários com acesso ativo. |
| RN-004 | Falhar um dia não deve gerar punição visual agressiva. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Sujeito à regra. |
| Premium Mensal/Anual | Sujeito à regra. |
| Acesso expirado | Sem penalidade; streak não avança. |

---

## 8. Fluxo principal

1. Job de virada de dia executa.
2. Verifica conclusão da daily do dia anterior.
3. Mantém/reinicia o streak.
4. Aciona penalidade quando aplicável (US-132).

---

## 9. Fluxos alternativos

### 9.1. Acesso expirado

Sem penalidade e sem avanço de streak.

---

## 10. Estados esperados

- streak mantido;
- streak reiniciado;
- penalidade acionada;
- sem ação (acesso expirado).

---

## 11. Impacto no Frontend Flutter

- Mensagem leve sobre manutenção/reinício do streak.

---

## 12. Impacto no Backend

- Job de virada de dia (streak + penalidade).

---

## 13. Impacto no Banco de Dados

Entidade: `HunterProgress`.

Campos: `streakDays`, `lastQuestCompletionDate`.

---

## 14. Impacto em Gamificação

- Torna o streak previsível e justo.

---

## 15. Impacto em Monetização

- Regra justa reduz frustração e abandono.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de streak. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
(interno) job: daily_rollover
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| streak_updated | Quando o streak é mantido ou reiniciado. |
| daily_quest_missed | Quando a daily não foi concluída. |

---

## 19. Critérios de aceite

### CA-001 — Streak mantido

Dado que o usuário concluiu a daily,

Quando o dia virar,

Então o streak deve ser mantido/incrementado.

### CA-002 — Sem punição agressiva

Dado que o usuário falhou um dia,

Quando o dia virar,

Então a comunicação não deve ser visualmente agressiva.

---

## 20. Critérios de teste para QA

### Backend

- virada mantém/reinicia o streak conforme regra;
- penalidade só com acesso ativo;
- mensagens não agressivas.

---

## ✅ Decisão registrada

> A virada de dia é previsível: mantém ou reinicia o streak e aciona a penalidade apenas para acesso ativo, sem punição visual agressiva.
