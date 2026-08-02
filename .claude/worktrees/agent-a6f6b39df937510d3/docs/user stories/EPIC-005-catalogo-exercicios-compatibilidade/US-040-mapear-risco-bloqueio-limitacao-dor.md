---
title: US-040 — Mapear tags de risco, articulação e bloqueio por limitação/dor
sidebar_position: 40
---

# US-040 — Mapear tags de risco, articulação e bloqueio por limitação/dor

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-040 |
| Épico | EPIC-005 — Catálogo de Exercícios e Regras de Compatibilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | Não aplicável (tags internas) |
| Dependência principal | EPIC-004 (limitações e dores) → ExerciseCatalog |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **mapear tags de risco, articulação e bloqueio por limitação/dor**,

para **filtrar exercícios incompatíveis com as limitações e dores do usuário**.

---

## 3. Contexto

Limitações físicas (filtro forte, crônico) e dores (ajuste imediato, transitório) precisam casar com tags do exercício. Sem esse mapeamento, o gerador não consegue remover ou regredir exercícios perigosos. Esta US substitui e expande a antiga "contraindicações básicas".

---

## 4. Objetivo

Preencher `JointStressTags`, `ContraindicationTags`, `LimitationBlockTags`, `PainBlockTags` e `RiskTags` de cada exercício, alinhados às limitações/dores coletadas no onboarding (EPIC-004).

---

## 5. Escopo

### Entra nesta US

- `jointStressTags` (articulações exigidas).
- `riskTags` (ex.: `knee_high_stress`, `lumbar_high_stress`, `shoulder_high_stress`, `wrist_high_stress`, `ankle_high_stress`, `hip_high_stress`, `cervical_high_stress`, `high_impact`, `high_technical_complexity`, `requires_spotter`, `requires_load_control`).
- `limitationBlockTags` (bloqueio por limitação).
- `painBlockTags` (bloqueio/regressão por dor).
- `contraindicationTags` práticas.
- Compatibilidade com as opções de limitações/dores do onboarding.

### Fora desta US

- Coleta de limitações/dores (EPIC-004).
- Aplicação do filtro na geração (EPIC-006).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Exercícios com risco para uma região devem receber as tags correspondentes. |
| RN-002 | As tags devem ser compatíveis com as opções de limitação/dor do onboarding. |
| RN-003 | Limitações têm prioridade sobre objetivo, preferência e intensidade. |
| RN-004 | Dor relatada durante o treino tem prioridade sobre a resposta antiga do onboarding. |
| RN-005 | Quando houver regressão segura, o exercício pode ser substituído em vez de removido. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema/Admin | Mapeia e revisa. |
| Usuário final | Beneficia-se do filtro na geração. |

---

## 8. Fluxo principal

1. Sistema analisa articulações exigidas e padrões de risco do exercício.
2. Atribui `jointStressTags`, `riskTags`, `contraindicationTags`.
3. Define `limitationBlockTags` e `painBlockTags` por região.
4. Salva no `ExerciseCatalog`.

---

## 9. Fluxos alternativos

### 9.1. Sem regressão disponível

Se não houver regressão segura, o exercício é apenas removido para usuários incompatíveis.

---

## 10. Estados esperados

- mapeado;
- em revisão;
- erro de mapeamento.

---

## 11. Impacto no Frontend Flutter

- Indireto: usuário recebe treino sem exercícios contraindicados.

---

## 12. Impacto no Backend

- Serviço de mapeamento de risco/bloqueio.
- Relacionamento lógico com `UserProfile.physicalLimitations` e `UserProfile.physicalPains`.

---

## 13. Impacto no Banco de Dados

Entidade: `ExerciseCatalog`.

Campos: `JointStressTags`, `RiskTags`, `ContraindicationTags`, `LimitationBlockTags`, `PainBlockTags`.

---

## 14. Impacto em Gamificação

- Garante que a gamificação não incentive esforço inseguro.

---

## 15. Impacto em Monetização

- Demonstra personalização segura e real durante o trial.

---

## 16. Impacto em Internacionalização

- Tags internas; mensagens de substituição traduzidas quando exibidas.

---

## 17. Contrato de API sugerido

```txt
GET /api/exercises?excludeRisk=knee_high_stress,high_impact
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| exercise_sanitized | Mapeamento compõe a sanitização. |

---

## 19. Critérios de aceite

### CA-001 — Tags de risco presentes

Dado um exercício com estresse de joelho,

Quando mapeado,

Então deve receber `knee_high_stress` e `limitationBlockTags`/`painBlockTags` correspondentes.

### CA-002 — Filtro por limitação

Dado que o usuário marcou limitação no joelho,

Quando a quest for gerada,

Então exercícios com `knee_high_stress` e alto impacto devem ser removidos ou substituídos.

---

## 20. Critérios de teste para QA

### Backend

- tags casam com as opções de limitação/dor do onboarding;
- exercício com risco recebe as tags corretas;
- regressão é usada quando disponível, caso contrário remoção.

---

## ✅ Decisão registrada

> As tags de risco e bloqueio conectam o exercício às limitações e dores do usuário; limitação é filtro forte e dor relatada em execução prevalece sobre o onboarding.
