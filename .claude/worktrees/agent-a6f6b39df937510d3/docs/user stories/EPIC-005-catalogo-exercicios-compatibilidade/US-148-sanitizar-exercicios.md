---
title: US-148 — Sanitizar exercícios importados
sidebar_position: 148
---

# US-148 — Sanitizar exercícios importados

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-148 |
| Épico | EPIC-005 — Catálogo de Exercícios e Regras de Compatibilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR (validação de textos) |
| Dependência principal | ExerciseCatalog |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **sanitizar os exercícios importados aplicando validações obrigatórias**,

para **garantir que apenas exercícios completos e seguros sigam para aprovação**.

---

## 3. Contexto

A sanitização é a barreira de qualidade entre a importação/normalização e a aprovação. Ela consolida as classificações (músculo, equipamento, dificuldade, impacto, tags, atributos) e bloqueia exercícios incompletos.

---

## 4. Objetivo

Executar todas as validações obrigatórias do catálogo e definir `SanitizationStatus`, marcando exercícios aptos como `pending_review`.

---

## 5. Escopo

### Entra nesta US

- Validação de nome (não vazio, não duplicado, compreensível).
- Validação de músculo principal (>= 1).
- Validação de mídia (vídeo, GIF ou imagem; preferência vídeo).
- Validação de instruções mínimas.
- Validação de equipamento mapeado e tipo válido.
- Validação de dificuldade (1–5), impacto (0–5) e articulações.
- Validação de tags de limitação/dor quando necessário.
- Validação de afinidade com pelo menos 1 objetivo.
- Validação de contribuição de atributos (Sabedoria + 1 atributo).
- Definição de `SanitizationStatus` e marcação `pending_review`.

### Fora desta US

- Importação (US-143), normalização (US-144).
- Decisão final de aprovação (US-149).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Nome não pode ser vazio, duplicado ou incompreensível. |
| RN-002 | Deve existir pelo menos 1 grupo muscular principal. |
| RN-003 | Deve ter mídia válida (vídeo, GIF ou imagem). |
| RN-004 | Deve ter instrução mínima de execução. |
| RN-005 | Equipamento deve estar mapeado e tipo deve ser válido. |
| RN-006 | Deve ter dificuldade (1–5), impacto (0–5) e articulações. |
| RN-007 | Deve ter afinidade com pelo menos 1 objetivo (`goalTags`). |
| RN-008 | Deve ter contribuição de atributos com `wisdomXp >= 1` e 1 atributo extra > 0. |
| RN-009 | Exercício que falhar em qualquer validação não vira `pending_review`. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema/Admin | Executa sanitização. |
| Usuário final | Sem acesso direto. |

---

## 8. Fluxo principal

1. Sistema lê exercícios `normalized`.
2. Aplica todas as validações obrigatórias.
3. Marca aptos como `pending_review`.
4. Marca reprovados com motivo da falha.

---

## 9. Fluxos alternativos

### 9.1. Falha de validação

Exercício recebe status de falha com o motivo e fica fora do fluxo de aprovação.

### 9.2. Exceção manual de mídia

Falta de mídia só é tolerada por exceção manual explícita (ver US-149).

---

## 10. Estados esperados

- sanitizando;
- pending_review;
- reprovado na sanitização (com motivo).

---

## 11. Impacto no Frontend Flutter

- Sem impacto direto.

---

## 12. Impacto no Backend

- Motor de validação de sanitização.
- Definição de `SanitizationStatus`.

---

## 13. Impacto no Banco de Dados

Entidade: `ExerciseCatalog`.

Campos: `SanitizationStatus` e todos os campos validados.

---

## 14. Impacto em Gamificação

- Garante que XP de atributo só venha de exercícios consistentes.

---

## 15. Impacto em Monetização

- Mantém a qualidade do catálogo, base do valor percebido.

---

## 16. Impacto em Internacionalização

- Valida presença de textos PT-BR.

---

## 17. Contrato de API sugerido

```txt
POST /api/admin/exercises/sanitize
```

Response conceitual:

```json
{
  "pendingReview": 84,
  "failed": 6
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| exercise_sanitized | Quando o exercício passa pela sanitização. |

---

## 19. Critérios de aceite

### CA-001 — Exercício sanitizado

Dado que um exercício foi normalizado,

Quando passar pela sanitização,

Então deve consolidar nome, músculos, equipamento, mídia, dificuldade, impacto, tags e instruções e ficar `pending_review`.

### CA-002 — Falha bloqueia

Dado que um exercício não tem mídia válida,

Quando a sanitização rodar,

Então ele não pode virar `pending_review` (salvo exceção manual).

---

## 20. Critérios de teste para QA

### Backend

- cada validação obrigatória bloqueia exercícios incompletos;
- exercício completo vira `pending_review`;
- motivo de falha é registrado;
- exceção manual de mídia é auditável.

---

## ✅ Decisão registrada

> A sanitização é obrigatória e consolida todas as validações de qualidade; apenas exercícios completos chegam a `pending_review`.
