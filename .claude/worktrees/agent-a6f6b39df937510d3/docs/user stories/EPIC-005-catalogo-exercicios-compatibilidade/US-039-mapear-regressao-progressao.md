---
title: US-039 — Mapear variantes de regressão e progressão
sidebar_position: 39
---

# US-039 — Mapear variantes de regressão e progressão

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-039 |
| Épico | EPIC-005 — Catálogo de Exercícios e Regras de Compatibilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | ExerciseCatalog |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **mapear variantes de regressão e progressão dos exercícios**,

para **facilitar ou dificultar o treino conforme nível, dor e desempenho do usuário**.

---

## 3. Contexto

Ajustar o treino sem trocar de exercício depende de variantes ligadas: uma mais fácil (regressão) e uma mais difícil (progressão). Ex.: flexão tradicional → flexão inclinada → flexão na parede.

---

## 4. Objetivo

Preencher `RegressionExerciseId`, `ProgressionExerciseId` e `RelatedExerciseIds` de cada exercício.

---

## 5. Escopo

### Entra nesta US

- Relação de regressão (variante mais fácil).
- Relação de progressão (variante mais difícil).
- Exercícios relacionados.
- Uso na substituição por dor/limitação e no ajuste por feedback.

### Fora desta US

- Lógica de substituição em tempo real (EPIC-006/007/008).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Regressão e progressão devem manter o mesmo padrão de movimento sempre que possível. |
| RN-002 | A regressão deve ter dificuldade/impacto menores que o exercício base. |
| RN-003 | A progressão deve ter dificuldade/complexidade maiores que o exercício base. |
| RN-004 | Variantes referenciadas devem existir e estar no catálogo. |
| RN-005 | Quando houver regressão segura, ela é preferida à remoção por dor/limitação. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema/Admin | Mapeia e revisa. |
| Usuário final | Usa variantes via geração/edição. |

---

## 8. Fluxo principal

1. Sistema identifica variantes do mesmo padrão de movimento.
2. Ordena por dificuldade/impacto.
3. Define regressão e progressão.
4. Salva no `ExerciseCatalog`.

---

## 9. Fluxos alternativos

### 9.1. Sem variante disponível

Exercício sem regressão fica elegível apenas para usuários compatíveis; sem progressão, mantém-se como teto da cadeia.

---

## 10. Estados esperados

- mapeado;
- sem variante;
- referência inválida (em revisão).

---

## 11. Impacto no Frontend Flutter

- Indicação de variante na tela de exercício (quando houver).

---

## 12. Impacto no Backend

- Serviço de mapeamento de variantes.
- Suporte à substituição por regressão.

---

## 13. Impacto no Banco de Dados

Entidade: `ExerciseCatalog`.

Campos: `RegressionExerciseId`, `ProgressionExerciseId`, `RelatedExerciseIds`.

---

## 14. Impacto em Gamificação

- Concluir progressão pode conceder XP de Sabedoria/atributo (EPIC-008/009).

---

## 15. Impacto em Monetização

- Adaptação contínua aumenta aderência e retenção.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Nomes das variantes. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/exercises/{id}/variants
```

Response conceitual:

```json
{
  "regression": "exr_flexao_parede",
  "progression": "exr_flexao_tradicional"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| exercise_sanitized | Mapeamento compõe a sanitização. |

---

## 19. Critérios de aceite

### CA-001 — Variantes coerentes

Dado um exercício com regressão e progressão,

Quando salvo,

Então a regressão deve ser mais fácil e a progressão mais difícil que o base.

### CA-002 — Substituição por regressão

Dado que o usuário sente dor leve,

Quando o sistema ajustar o treino,

Então deve preferir a regressão à remoção, se houver.

---

## 20. Critérios de teste para QA

### Backend

- regressão/progressão respeitam ordem de dificuldade;
- referências inexistentes são rejeitadas;
- substituição usa regressão quando disponível.

---

## ✅ Decisão registrada

> Cada exercício mapeia variantes de regressão e progressão do mesmo padrão de movimento, permitindo ajuste seguro sem trocar o tipo de estímulo.
