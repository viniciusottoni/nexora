---
title: US-066 — Subir de level
sidebar_position: 66
---

# US-066 — Subir de level

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-066 |
| Épico | EPIC-009 — Sistema de XP, Level, Rank, Atributos e Streak |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário com acesso ativo |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | HunterProgress |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **subir de Level conforme acumulo XP**,

para **ver minha progressão geral evoluir**.

---

## 3. Contexto

O Level representa a progressão geral do Hunter, separada do Rank. Todo usuário começa no Level 1, inclusive quem inicia em Rank B pelo onboarding.

---

## 4. Objetivo

Evoluir o Level quando o XP acumulado atinge o limiar do próximo Level.

---

## 5. Escopo

### Entra nesta US

- Acúmulo de XP geral.
- Limiar de XP por Level.
- Level up e XP restante para o próximo Level.

### Fora desta US

- Rank (US-067).
- XP de atributo (US-068).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Todo usuário começa no Level 1. |
| RN-002 | O Level sobe quando o XP atinge o limiar do próximo Level. |
| RN-003 | Level é independente do Rank. |
| RN-004 | O progresso de Level é preservado mesmo com acesso bloqueado. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Sobe de Level. |
| Premium Mensal/Anual | Sobe de Level. |
| Acesso expirado | Mantém Level, sem novo ganho. |

---

## 8. Fluxo principal

1. Usuário ganha XP.
2. Sistema verifica o limiar do próximo Level.
3. Se atingido, sobe o Level e ajusta o XP restante.
4. App exibe o level up (US-071).

---

## 9. Fluxos alternativos

### 9.1. Múltiplos levels de uma vez

Se o XP ganho ultrapassar mais de um limiar, aplicar os levels correspondentes.

---

## 10. Estados esperados

- XP acumulado;
- level up;
- sem level up.

---

## 11. Impacto no Frontend Flutter

- Barra de XP e indicador de Level.
- Animação de level up.

---

## 12. Impacto no Backend

- Serviço de progressão de Level.

---

## 13. Impacto no Banco de Dados

Entidade: `HunterProgress`.

Campos: `level`, `xp`, `xpToNextLevel`.

---

## 14. Impacto em Gamificação

- Indicador central de progressão geral.

---

## 15. Impacto em Monetização

- Sensação de avanço sustenta retenção.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de level up. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/hunter/progress
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| level_up | Quando o usuário sobe de Level. |

---

## 19. Critérios de aceite

### CA-001 — Level up

Dado que o XP atingiu o limiar do próximo Level,

Quando o progresso for atualizado,

Então o Level deve subir e o XP restante ser ajustado.

### CA-002 — Início no Level 1

Dado um usuário recém-onboarded,

Quando o perfil for criado,

Então o Level inicial deve ser 1, independentemente do Rank.

---

## 20. Critérios de teste para QA

### Backend

- level sobe ao atingir o limiar;
- múltiplos levels de uma vez são aplicados;
- Level inicial é 1;
- progresso preservado após bloqueio.

---

## ✅ Decisão registrada

> O Level reflete a progressão geral, começa em 1 para todos e evolui por limiares de XP, independente do Rank.
