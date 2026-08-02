---
title: US-049 — Registrar motivo e auditoria da geração
sidebar_position: 49
---

# US-049 — Registrar motivo e auditoria da geração

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-049 |
| Épico | EPIC-006 — Geração de Quests (Diária e Dungeon) |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | Não aplicável (dados internos) |
| Dependência principal | Quest |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **registrar o motivo e a auditoria da geração da quest**,

para **verificar se ela respeitou o perfil e a segurança do usuário**.

---

## 3. Contexto

Para auditar a personalização e a segurança, cada geração deve registrar como foi montada: método (IA, regras, fallback), perfil considerado, filtros aplicados e por que cada exercício entrou.

---

## 4. Objetivo

Persistir metadados de auditoria da geração, incluindo motivo, método e fatores de decisão.

---

## 5. Escopo

### Entra nesta US

- Registro de `generationReason` (ai | rules | fallback | regeneration).
- Registro do snapshot de perfil considerado (nível efetivo, tempo, limitações, dores, objetivo).
- Registro dos filtros aplicados e dos motivos de inclusão/exclusão.

### Fora desta US

- Exibição de auditoria ao usuário final.
- Geração em si (US-042/151).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Toda geração deve registrar o motivo/método. |
| RN-002 | O registro deve permitir auditar respeito a limitações e dores. |
| RN-003 | O registro não deve conter dados sensíveis além do necessário (LGPD). |
| RN-004 | A auditoria deve identificar gerações por fallback e por regeneração. |
| RN-005 | Quando a regeneração consumir o item "Pergaminho da Reforja" (US-048), o `generationMethod` deve registrar esse gatilho (ex.: sufixo `+reforge_scroll`). |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema/Admin | Acessa a auditoria. |
| Usuário final | Não acessa a auditoria. |

---

## 8. Fluxo principal

1. A geração produz a quest.
2. Sistema registra motivo, método e fatores de decisão.
3. Auditoria fica disponível para consulta interna.

---

## 9. Fluxos alternativos

### 9.1. Geração por fallback

Registrar `generationReason = fallback` e o gatilho da falha.

---

## 10. Estados esperados

- auditoria registrada;
- erro de registro (não bloqueia a quest).

---

## 11. Impacto no Frontend Flutter

- Sem impacto direto.

---

## 12. Impacto no Backend

- Serviço de auditoria de geração.
- Registro de snapshot do perfil e dos filtros.

---

## 13. Impacto no Banco de Dados

Entidade: `Quest` (ou `QuestGenerationLog`).

Campos: `generationReason`, `generationMethod`, `profileSnapshot`, `appliedFilters`, `createdAt`.

---

## 14. Impacto em Gamificação

- Indireto: garante integridade da personalização que gera XP.

---

## 15. Impacto em Monetização

- Auditabilidade sustenta a confiança no produto.

---

## 16. Impacto em Internacionalização

- Dados internos; sem textos ao usuário.

---

## 17. Contrato de API sugerido

```txt
GET /api/admin/quests/{id}/audit
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| daily_quest_generated | Inclui metadados de auditoria. |
| quest_generation_failed | Registra o motivo da falha. |

---

## 19. Critérios de aceite

### CA-001 — Motivo registrado

Dado que uma quest foi gerada,

Quando consultada a auditoria,

Então deve constar o motivo/método e o perfil considerado.

### CA-002 — Auditoria de segurança

Dado um usuário com dor lombar,

Quando auditar a quest,

Então deve ser possível verificar que exercícios de risco lombar foram filtrados.

---

## 20. Critérios de teste para QA

### Backend

- toda geração registra motivo/método;
- snapshot permite auditar limitações/dores;
- registro respeita LGPD;
- fallback e regeneração são identificáveis.

---

## ✅ Decisão registrada

> Toda geração registra motivo e auditoria suficientes para verificar respeito ao perfil e à segurança, sem dados sensíveis desnecessários.
