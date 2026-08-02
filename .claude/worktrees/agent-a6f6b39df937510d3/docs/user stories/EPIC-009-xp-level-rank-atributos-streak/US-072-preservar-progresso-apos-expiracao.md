---
title: US-072 — Preservar progresso após trial ou assinatura expirada
sidebar_position: 72
---

# US-072 — Preservar progresso após trial ou assinatura expirada

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-072 |
| Épico | EPIC-009 — Sistema de XP, Level, Rank, Atributos e Streak |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | HunterProgress, Subscription |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário**,

quero **que meu progresso seja preservado mesmo com acesso expirado**,

para **não perder XP, Level, Rank e atributos ao deixar de assinar temporariamente**.

---

## 3. Contexto

O bloqueio comercial não pode apagar a evolução. Ao reativar o acesso, o usuário deve reencontrar XP, Level, Rank, RankScore, atributos e streak conforme estavam.

---

## 4. Objetivo

Garantir a persistência de todo o progresso durante o bloqueio e sua recuperação ao reativar.

---

## 5. Escopo

### Entra nesta US

- Persistência de XP, Level, Rank, RankScore, atributos e streak no bloqueio.
- Recuperação ao reativar o acesso.
- Bloqueio apenas de novos ganhos enquanto inativo.

### Fora desta US

- Regras de paywall/reativação (EPIC-003).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Progresso e Rank permanecem salvos mesmo com acesso bloqueado. |
| RN-002 | Durante o bloqueio, não há novos ganhos de XP/atributo/RankScore. |
| RN-003 | Ao reativar, o progresso é recuperado integralmente. |
| RN-004 | O streak segue a regra de virada de dia, sem avanço durante bloqueio. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Trial expirado | Mantém progresso, sem novos ganhos. |
| Assinatura expirada | Mantém progresso, sem novos ganhos. |
| Reativado | Recupera progresso e volta a ganhar. |

---

## 8. Fluxo principal

1. Acesso expira.
2. Sistema mantém todo o progresso persistido.
3. Ao reativar, o progresso é recuperado.

---

## 9. Fluxos alternativos

### 9.1. Longo período inativo

Progresso permanece; streak não avança durante o período.

---

## 10. Estados esperados

- progresso persistido;
- bloqueado (sem ganho);
- recuperado ao reativar.

---

## 11. Impacto no Frontend Flutter

- Exibição de estado limitado com CTA de assinatura (EPIC-003/010).

---

## 12. Impacto no Backend

- Persistência durável do progresso.
- Bloqueio de novos ganhos enquanto inativo.

---

## 13. Impacto no Banco de Dados

Entidades: `HunterProgress`, `HunterAttributes`, `Subscription`.

---

## 14. Impacto em Gamificação

- Protege a evolução acumulada.

---

## 15. Impacto em Monetização

- Reduz atrito de retorno e incentiva reativação.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de estado limitado. |
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
| hunter_progress_viewed | Quando o progresso é exibido (mesmo limitado). |

---

## 19. Critérios de aceite

### CA-001 — Progresso preservado

Dado que o acesso expirou,

Quando o usuário reativar,

Então XP, Level, Rank, atributos e streak devem ser recuperados.

### CA-002 — Sem ganho no bloqueio

Dado que o acesso está inativo,

Quando o usuário interagir,

Então não deve haver novos ganhos de progresso.

---

## 20. Critérios de teste para QA

### Backend

- progresso persiste no bloqueio;
- sem novos ganhos enquanto inativo;
- recuperação integral ao reativar.

---

## ✅ Decisão registrada

> O progresso (incluindo Rank e RankScore) é preservado durante o bloqueio e recuperado ao reativar, sem ganhos enquanto o acesso estiver inativo.
