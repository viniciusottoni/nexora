---
title: US-128 — Gerar dungeon compatível com perfil e equipamentos
sidebar_position: 128
---

# US-128 — Gerar dungeon compatível com perfil e equipamentos

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-128 |
| Épico | EPIC-006 — Geração de Quests (Diária e Dungeon) |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | ExerciseCatalog, UserProfile |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **gerar a dungeon compatível com o perfil e equipamentos**,

para **entregar uma side quest segura e adequada ao usuário**.

---

## 3. Contexto

A dungeon usa o mesmo pipeline de segurança da quest diária (nível efetivo, filtro eliminatório, pontuação, prescrição), apenas com `type=dungeon`, respeitando o modo previamente escolhido (`solo` ou `grupo`) e a parametrização própria (ex.: foco temático ou duração distinta). A geração só acontece na janela semanal de dungeon (segundas, quartas e sextas) ou após o consumo de uma `Pedra de Dungeon`.

---

## 4. Objetivo

Gerar uma quest `type=dungeon` aplicando os mesmos filtros e regras de segurança da daily, a partir do modo `solo` ou `grupo` selecionado antes da ativação e respeitando a janela semanal ou o uso de `Pedra de Dungeon`.

---

## 5. Escopo

### Entra nesta US

- Reuso do pipeline (nível efetivo, filtro, pontuação, prescrição).
- Respeito a equipamentos, limitações e dores.
- Respeito ao modo da dungeon definido previamente (`solo` ou `grupo`).
- Validação da janela semanal de dungeon ou do consumo de `Pedra de Dungeon`.
- Parametrização específica de dungeon (tema/duração).

### Fora desta US

- Ativação manual (US-127).
- Penalidade de XP (US-129) — não se aplica a dungeon.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A dungeon respeita nível efetivo, equipamentos, tempo, limitações e dores. |
| RN-002 | A dungeon respeita o modo definido antes da ativação: `solo` ou `grupo`. |
| RN-003 | A dungeon usa apenas exercícios aprovados. |
| RN-004 | Segurança tem prioridade máxima, igual à daily. |
| RN-005 | A dungeon só pode ser gerada nas segundas, quartas e sextas, ou quando uma `Pedra de Dungeon` for consumida. |
| RN-006 | A `Pedra de Dungeon`, quando usada, é consumida no momento da geração. |
| RN-007 | A dungeon não tem penalidade por não conclusão. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Gera dungeon. |
| Premium Mensal/Anual | Gera dungeon. |
| Acesso expirado | Não gera (US-043). |

---

## 8. Fluxo principal

1. Ativação dispara a geração da dungeon.
2. Sistema valida a janela semanal ou o uso de `Pedra de Dungeon`.
3. Sistema aplica nível efetivo, filtro e pontuação.
4. Aplica a prescrição, o modo escolhido (`solo` ou `grupo`) e a parametrização de dungeon.
5. Persiste a dungeon.

---

## 9. Fluxos alternativos

### 9.1. Falha na geração

Usar fallback por template de dungeon compatível.

### 9.2. Fora da janela semanal sem pedra

Bloquear a geração com erro claro e não persistir dungeon.

---

## 10. Estados esperados

- gerando;
- dungeon pronta;
- fallback aplicado.
- bloqueada por disponibilidade semanal.

---

## 11. Impacto no Frontend Flutter

- Exibição da dungeon gerada.
- Estado de bloqueio quando a dungeon estiver fora da janela semanal e não houver `Pedra de Dungeon`.

---

## 12. Impacto no Backend

- Reuso do pipeline de geração com `type=dungeon`.
- Validação da janela semanal e consumo da `Pedra de Dungeon`.

---

## 13. Impacto no Banco de Dados

Entidades: `Quest`, `QuestExercise`.

Campos: `type=dungeon`, parâmetros de prescrição, controle de disponibilidade da ativação e consumo da `Pedra de Dungeon` quando aplicável.

---

## 14. Impacto em Gamificação

- Dungeon concluída concede XP/atributos e pode conceder itens.

---

## 15. Impacto em Monetização

- Conteúdo extra reforça engajamento e valor.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Textos da dungeon. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
POST /api/quests/dungeon/generate
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| dungeon_generated | Quando a dungeon é gerada. |

---

## 19. Critérios de aceite

### CA-001 — Dungeon segura

Dado um usuário com limitação no joelho,

Quando a dungeon for gerada,

Então exercícios com `knee_high_stress` e alto impacto não devem aparecer.

### CA-002 — Sem penalidade

Dado que o usuário não concluiu a dungeon,

Quando virar o dia,

Então não deve haver penalidade de XP pela dungeon.

### CA-003 — Fora da janela semanal

Dado que hoje não é segunda, quarta ou sexta,

Quando o sistema gerar a dungeon sem `Pedra de Dungeon`,

Então a geração deve ser bloqueada.

---

## 20. Critérios de teste para QA

### Frontend

- mostra o estado bloqueado fora da janela semanal quando não houver `Pedra de Dungeon`;
- mantém a daily separada da dungeon;
- exibe dungeon gerada com o modo escolhido.

### Backend

- dungeon reusa filtro/pontuação/prescrição;
- respeita o modo selecionado (`solo` ou `grupo`);
- respeita equipamentos/limitações/dores;
- valida janela semanal ou consumo de `Pedra de Dungeon`;
- usa apenas aprovados;
- não aplica penalidade.

### API

- `POST /api/quests/dungeon/activate` ou `POST /api/quests/dungeon/generate` recusa fora da janela semanal sem pedra;
- a resposta retorna erro claro quando a dungeon não está disponível;
- a pedra é consumida quando usada.

### E2E

- ativar dungeon em dia permitido gera treino seguro e compatível;
- fora da janela semanal, a dungeon só é liberada com `Pedra de Dungeon`.

---

## ✅ Decisão registrada

> A dungeon usa o mesmo pipeline de segurança da daily, com `type=dungeon` e sem penalidade por não conclusão.
