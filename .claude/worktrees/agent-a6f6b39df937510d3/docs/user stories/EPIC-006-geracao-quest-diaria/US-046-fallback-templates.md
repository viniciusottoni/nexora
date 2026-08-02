---
title: US-046 — Usar fallback por templates
sidebar_position: 46
---

# US-046 — Usar fallback por templates

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-046 |
| Épico | EPIC-006 — Geração de Quests (Diária e Dungeon) |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | ExerciseCatalog (aprovado) |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **usar fallback por templates quando a geração principal falhar**,

para **nunca deixar um usuário com acesso ativo sem treino**.

---

## 3. Contexto

A quest diária precisa funcionar todos os dias. Se a IA ou a geração principal falhar, o sistema monta o treino a partir de templates compatíveis usando o catálogo aprovado, mantendo segurança e compatibilidade.

---

## 4. Objetivo

Garantir uma quest válida via templates compatíveis sempre que a geração principal falhar.

---

## 5. Escopo

### Entra nesta US

- Templates por nível efetivo, objetivo e tempo disponível.
- Uso do catálogo aprovado e respeito a limitações/dores.
- Acionamento automático em falha da geração principal.
- Sinalização de que a quest foi gerada por fallback.

### Fora desta US

- Geração principal (US-042/151).
- Auditoria da geração (US-049).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O fallback usa apenas exercícios aprovados. |
| RN-002 | O fallback respeita nível efetivo, tempo, limitações e dores. |
| RN-003 | O fallback é acionado automaticamente quando a geração principal falha. |
| RN-004 | A quest de fallback deve ser registrada com motivo `fallback` (US-049). |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Recebe fallback se necessário. |
| Premium Mensal/Anual | Recebe fallback se necessário. |
| Acesso expirado | Não gera (US-043). |

---

## 8. Fluxo principal

1. A geração principal falha.
2. Sistema seleciona um template compatível com o perfil.
3. Sistema preenche o template com exercícios aprovados.
4. App entrega a quest de fallback.

---

## 9. Fluxos alternativos

### 9.1. Template indisponível

Se nenhum template couber, escolher o template mais seguro de menor volume e registrar o caso.

---

## 10. Estados esperados

- geração principal falhou;
- fallback aplicado;
- fallback indisponível (caso registrado).

---

## 11. Impacto no Frontend Flutter

- Estado de erro com fallback transparente ao usuário.

---

## 12. Impacto no Backend

- Biblioteca de templates por perfil.
- Acionamento e preenchimento por catálogo aprovado.

---

## 13. Impacto no Banco de Dados

Entidades: `Quest`, `QuestExercise`, `ExerciseCatalog`.

Campo: `generationReason = fallback`.

---

## 14. Impacto em Gamificação

- Quest de fallback concede XP normalmente ao concluir.

---

## 15. Impacto em Monetização

- Confiabilidade diária protege a retenção.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de fallback. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
POST /api/quests/daily/generate
```

Response (fallback):

```json
{ "questId": "qst_002", "generationReason": "fallback" }
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| quest_generation_failed | Quando a geração principal falha. |
| daily_quest_generated | Quando o fallback entrega a quest. |

---

## 19. Critérios de aceite

### CA-001 — Fallback funcional

Dado que a geração principal falhou,

Quando o fallback for acionado,

Então o usuário deve receber uma quest compatível por template.

### CA-002 — Segurança no fallback

Dado um usuário com limitação no joelho,

Quando o fallback gerar a quest,

Então exercícios incompatíveis não devem aparecer.

---

## 20. Critérios de teste para QA

### Backend

- falha da geração principal aciona o fallback;
- template respeita nível, tempo, limitações e dores;
- quest de fallback é registrada com o motivo.

### E2E

- usuário recebe treino mesmo com falha da IA.

---

## ✅ Decisão registrada

> O fallback por templates garante quest diária sempre disponível, usando catálogo aprovado e respeitando segurança e perfil.
