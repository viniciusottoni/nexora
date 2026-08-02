---
title: US-071 — Receber feedback visual de level up e de atributo evoluído
sidebar_position: 71
---

# US-071 — Receber feedback visual de level up e de atributo evoluído

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-071 |
| Épico | EPIC-009 — Sistema de XP, Level, Rank, Atributos e Streak |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário com acesso ativo |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | HunterProgress, HunterAttributes |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **receber feedback visual de level up, rank up e atributo evoluído**,

para **sentir recompensa imediata pelo meu esforço**.

---

## 3. Contexto

O feedback visual reforça a sensação de progresso. Ele deve celebrar conquistas (level up, rank up, level up de atributo) de forma leve e motivadora, no momento em que o exercício é concluído, sem punição agressiva em caso de falha.

---

## 4. Objetivo

Exibir animações/mensagens de conquista ao subir Level, Rank ou Level de atributo.

---

## 5. Escopo

### Entra nesta US

- Feedback de level up (Hunter).
- Feedback de rank up.
- Feedback de level up de atributo.
- Mensagens localizadas e leves.

### Fora desta US

- Cálculo dos ganhos (US-064..068).
- Tela de recompensa detalhada (US-133).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O feedback deve refletir conquistas reais do progresso. |
| RN-002 | Animações devem ser leves e performáticas. |
| RN-003 | Falhas não devem gerar punição visual agressiva. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Recebe feedback. |
| Premium Mensal/Anual | Recebe feedback. |
| Acesso expirado | Sem novos ganhos a celebrar. |

---

## 8. Fluxo principal

1. O progresso muda (level/rank/atributo).
2. App exibe a animação/mensagem de conquista.

---

## 9. Fluxos alternativos

### 9.1. Múltiplas conquistas

Encadear ou agrupar feedbacks sem poluir a tela.

---

## 10. Estados esperados

- conquista exibida;
- múltiplas conquistas;
- sem conquista.

---

## 11. Impacto no Frontend Flutter

- Animações de level up, rank up e atributo.
- Mensagens localizadas.

---

## 12. Impacto no Backend

- Retorno das conquistas ocorridas na conclusão do exercício.

---

## 13. Impacto no Banco de Dados

Entidades: `HunterProgress`, `HunterAttributes`.

---

## 14. Impacto em Gamificação

- Reforço imediato de recompensa.

---

## 15. Impacto em Monetização

- Aumenta engajamento e retenção.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de conquista. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
(retornado em) POST /api/quests/{questId}/exercises/{questExerciseId}/complete
```

Response conceitual:

```json
{ "levelUp": true, "rankUp": false, "attributeLevelUps": ["strength"] }
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| level_up | Quando há level up. |
| rank_changed | Quando há rank up. |
| attribute_level_up | Quando um atributo sobe de Level. |

---

## 19. Critérios de aceite

### CA-001 — Feedback de conquista

Dado que o usuário subiu de Level,

Quando a recompensa for exibida,

Então deve aparecer o feedback de level up.

### CA-002 — Sem agressividade

Dado que o usuário falhou um dia,

Quando o app comunicar,

Então não deve haver punição visual agressiva.

---

## 20. Critérios de teste para QA

### Frontend (Flutter)

- feedback de level up, rank up e atributo;
- múltiplas conquistas agrupadas;
- mensagens em PT-BR, EN, ES;
- ausência de punição agressiva em falha.

---

## ✅ Decisão registrada

> O feedback visual celebra conquistas reais de forma leve e motivadora, sem punir agressivamente as falhas.
