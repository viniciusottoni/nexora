---
title: US-052 — Bloquear ajuste manual de séries, repetições e tempo
sidebar_position: 52
---

# US-052 — Bloquear ajuste manual de séries, repetições e tempo

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-052 |
| Épico | EPIC-007 — Edição de Treino Antes da Quest |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | QuestExercise e regras de pré-treino |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema do AWAKEN**,

quero **bloquear ajuste manual de séries, repetições, tempo e descanso**,

para **garantir que a edição antes da quest seja limitada à troca do tipo de treino inteiro**.

---

## 3. Contexto

A regra do EPIC-007 foi redefinida: não é mais permitido editar exercícios individuais nem alterar volume manualmente. O usuário só pode trocar entre tipos de treino completos.

---

## 4. Objetivo

Remover e bloquear qualquer ação que permita modificar manualmente séries, repetições, duração ou descanso de exercícios da quest.

---

## 5. Escopo

### Entra nesta US

- Remover controles de edição manual de séries.
- Remover controles de edição manual de repetições.
- Remover controles de edição manual de tempo e descanso.
- Bloquear endpoints antigos de ajuste manual.
- Garantir que a única ação editável seja alterar tipo do treino.

### Fora desta US

- Alterar tipo do treino, tratado na US-051.
- Execução da quest.
- Ajustes automáticos feitos pelo sistema ao gerar o treino.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Usuário não pode alterar séries manualmente. |
| RN-002 | Usuário não pode alterar repetições manualmente. |
| RN-003 | Usuário não pode alterar tempo ou descanso manualmente. |
| RN-004 | Usuário não pode substituir exercício individual. |
| RN-005 | Backend deve rejeitar tentativa direta de ajuste manual. |
| RN-006 | Apenas alteração de tipo de treino é permitida antes da quest iniciar. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode ajustar. |
| Usuário em Trial | Não pode ajustar manualmente. |
| Premium Mensal | Não pode ajustar manualmente. |
| Premium Anual | Não pode ajustar manualmente. |
| Trial expirado | Não pode editar. |
| Assinatura expirada | Não pode editar. |

---

## 8. Fluxo principal

1. Usuário abre o pré-treino.
2. App exibe exercícios apenas para revisão.
3. App não mostra controles de séries, repetições, tempo ou descanso editáveis.
4. Usuário pode apenas alterar o tipo do treino inteiro ou confirmar o treino atual.

---

## 9. Fluxos alternativos

### 9.1. Tentativa via endpoint antigo

Backend deve recusar com erro funcional.

### 9.2. Versão antiga do app

Se uma versão antiga tentar ajuste manual, backend deve bloquear para preservar regra de negócio.

---

## 10. Estados esperados

- pré-treino sem edição manual;
- tentativa bloqueada;
- endpoint recusado;
- ação permitida de alteração de tipo.

---

## 11. Impacto no Frontend Flutter

- Remover botões de editar volume.
- Remover steppers/inputs de séries, repetições, tempo e descanso.
- Remover ação de substituir exercício individual.
- Manter ação “Alterar tipo de treino”.

---

## 12. Impacto no Backend

- Desativar ou proteger endpoints de ajuste manual.
- Retornar erro padronizado para tentativas inválidas.
- Validar que mudanças só ocorrem por troca do tipo de treino.

---

## 13. Impacto no Banco de Dados

Não há novo campo obrigatório.

QuestExercise continua existindo, mas seus campos não são editáveis manualmente pelo usuário no pré-treino.

---

## 14. Impacto em Gamificação

- Evita manipulação manual de volume para influenciar XP.
- Mantém XP calculado pelo treino gerado pelo tipo escolhido.

---

## 15. Impacto em Monetização

- Não altera planos.
- Reforça consistência do produto e evita abuso.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de bloqueio. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

Erro esperado para tentativa inválida:

```json
{
  "code": "MANUAL_WORKOUT_EDIT_NOT_ALLOWED",
  "message": "Não é possível alterar séries, repetições ou tempo. Altere o tipo do treino."
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| manual_workout_edit_blocked | Quando uma tentativa de ajuste manual é bloqueada. |

---

## 19. Critérios de aceite

### CA-001 — Sem controles manuais

Dado que o usuário abre o pré-treino,

Quando visualizar os exercícios,

Então não deve existir controle para alterar séries, repetições, tempo ou descanso.

### CA-002 — Backend bloqueia

Dado que uma chamada direta tenta alterar volume,

Quando chegar ao backend,

Então deve ser recusada.

---

## 20. Critérios de teste para QA

- verificar ausência de edição manual no app;
- tentativa de alterar séries via API;
- tentativa de alterar repetições via API;
- tentativa de alterar tempo via API;
- tentativa de substituir exercício individual;
- validar que alteração de tipo ainda funciona;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> A edição manual de exercícios e volume foi removida: o usuário só pode alterar o tipo do treino inteiro antes de iniciar a quest.
