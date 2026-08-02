---
title: US-131 — Ver Sabedoria evoluir automaticamente ao completar qualquer treino
sidebar_position: 131
---

# US-131 — Ver Sabedoria evoluir automaticamente ao completar qualquer treino

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-131 |
| Épico | EPIC-009 — Sistema de XP, Level, Rank, Atributos e Streak |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário com acesso ativo |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | HunterAttributes |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **ver a Sabedoria evoluir automaticamente ao completar qualquer treino**,

para **ser recompensado por consistência, consciência corporal e feedback honesto**.

---

## 3. Contexto

Sabedoria representa aprendizado técnico, consciência corporal, consistência e feedback honesto. Todo exercício concluído concede +1 XP interno de Sabedoria por baixo dos panos, como aprendizagem inata da execução. Esse ganho não aparece como atributo visível no card/lista do exercício.

---

## 4. Objetivo

Conceder +1 XP interno de Sabedoria automaticamente em cada exercício concluído e em ações válidas de aprendizado.

---

## 5. Escopo

### Entra nesta US

- +1 XP interno de Sabedoria por exercício concluído.
- Sabedoria por feedback válido, regressão recomendada, marcação correta de dor e início de instrução (se a mecânica existir).
- Casos em que a Sabedoria não é concedida.

### Fora desta US

- Conversão XP interno → Level (US-130).
- Sistema de leitura para Sabedoria (Pós-MVP).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Todo exercício concluído concede +1 XP interno de Sabedoria. |
| RN-002 | Tentar executar e registrar feedback válido pode conceder Sabedoria. |
| RN-003 | Trocar para uma regressão recomendada ou marcar dor corretamente concede Sabedoria. |
| RN-004 | Não há Sabedoria por pular sem motivo, marcar conclusão falsa ou cancelar o treino sem executar. |
| RN-005 | Sabedoria não deve aparecer como atributo visível no card/lista do exercício, pois é ganho padrão e implícito. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Ganha Sabedoria. |
| Premium Mensal/Anual | Ganha Sabedoria. |
| Acesso expirado | Sem novo ganho. |

---

## 8. Fluxo principal

1. Usuário conclui um exercício (ou registra ação válida).
2. Sistema concede +1 XP interno de Sabedoria.
3. Dispara conversão (US-130) e RankScore (US-067).

---

## 9. Fluxos alternativos

### 9.1. Ação inválida

Pular sem motivo ou conclusão falsa não concede Sabedoria.

---

## 10. Estados esperados

- XP interno de Sabedoria concedido;
- sem concessão (ação inválida).

---

## 11. Impacto no Frontend Flutter

- Indicação de ganho de Sabedoria apenas em resumo/detalhe de recompensa, não no card/lista do exercício.

---

## 12. Impacto no Backend

- Concessão automática de XP interno de Sabedoria por exercício/ação válida.

---

## 13. Impacto no Banco de Dados

Entidade: `HunterAttributes`.

Campos: `wisdomPoints`, `wisdomLevel`.

---

## 14. Impacto em Gamificação

- Recompensa consistência e aprendizado; alimenta o RankScore.

---

## 15. Impacto em Monetização

- Reforça hábito e valor percebido.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de Sabedoria. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
(retornado em) POST /api/exercises/{id}/complete
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| xp_earned | Inclui ganho de Sabedoria. |
| attribute_level_up | Quando Sabedoria sobe de Level. |

---

## 19. Critérios de aceite

### CA-001 — Sabedoria automática

Dado que o usuário concluiu um exercício,

Quando a conclusão for válida,

Então deve receber +1 XP interno de Sabedoria sem exibir Sabedoria no card/lista do exercício.

### CA-002 — Sem concessão indevida

Dado que o usuário pulou o exercício sem motivo,

Quando a ação for registrada,

Então não deve receber Sabedoria.

---

## 20. Critérios de teste para QA

### Backend

- +1 XP interno de Sabedoria por exercício concluído;
- Sabedoria não aparece como atributo visível do exercício;
- Sabedoria por feedback válido/regressão/marcar dor;
- sem Sabedoria por pular/conclusão falsa.

---

## ✅ Decisão registrada

> Sabedoria é concedida automaticamente como +1 XP interno em todo exercício concluído e em ações válidas de aprendizado, mas fica implícita na execução e não aparece como atributo visível do exercício.
