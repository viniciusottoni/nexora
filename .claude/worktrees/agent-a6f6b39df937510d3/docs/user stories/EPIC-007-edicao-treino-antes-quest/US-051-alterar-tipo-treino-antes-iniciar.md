---
title: US-051 — Alterar tipo do treino antes de iniciar
sidebar_position: 51
---

# US-051 — Alterar tipo do treino antes de iniciar

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-051 |
| Épico | EPIC-007 — Edição de Treino Antes da Quest |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Quest, Program e status de acesso |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **alterar o tipo do treino antes de iniciar a quest**,

para **trocar o treino inteiro por uma opção adequada ao meu momento sem editar exercícios individualmente**.

---

## 3. Contexto

A edição do EPIC-007 não permite mais substituir exercícios isolados nem ajustar séries, repetições, tempo ou descanso. A única alteração permitida antes da quest é trocar o tipo do treino inteiro.

---

## 4. Objetivo

Permitir que o usuário escolha entre treino personalizado individual, treino de regeneração ou programa disponível antes de iniciar a quest.

---

## 5. Escopo

### Entra nesta US

- Alterar tipo do treino antes de iniciar.
- Opções iniciais: Personalizado Individual, Treino de Regeneração e Programa.
- Programas iniciais: Caminho de Saitama e Perfect 2.
- Preparar estrutura para programas futuros.
- Regenerar/substituir o treino inteiro conforme tipo escolhido.
- Recalcular estimativa de XP e duração com base no treino final.

### Fora desta US

- Substituir exercício individual.
- Alterar séries, repetições, tempo ou descanso manualmente.
- Criar treino do zero.
- Editor livre avançado.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A alteração só pode ocorrer antes da quest iniciar. |
| RN-002 | A única edição permitida é alterar o tipo do treino inteiro. |
| RN-003 | Tipos permitidos: personalizado individual, treino de regeneração e programa. |
| RN-004 | Programas iniciais disponíveis: Caminho de Saitama e Perfect 2. |
| RN-005 | Programas futuros devem usar a mesma estrutura de seleção. |
| RN-006 | O novo treino deve respeitar perfil, limitações, nível e regras do catálogo. |
| RN-007 | Após alterar o tipo, XP e duração estimados devem ser recalculados. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode alterar. |
| Usuário em Trial | Pode alterar com acesso ativo. |
| Premium Mensal | Pode alterar com acesso ativo. |
| Premium Anual | Pode alterar com acesso ativo. |
| Trial expirado | Não pode alterar. |
| Assinatura expirada | Não pode alterar. |

---

## 8. Fluxo principal

1. Usuário visualiza o treino antes de iniciar.
2. Toca em alterar tipo de treino.
3. App exibe opções: Personalizado Individual, Treino de Regeneração e Programa.
4. Se escolher Programa, app exibe programas disponíveis, como Caminho de Saitama e Perfect 2.
5. Usuário confirma a escolha.
6. Backend gera/substitui o treino inteiro conforme tipo escolhido.
7. App exibe o novo treino para revisão.

---

## 9. Fluxos alternativos

### 9.1. Usuário cancela

O treino atual permanece inalterado.

### 9.2. Tipo indisponível

App deve exibir mensagem e manter treino atual.

### 9.3. Quest já iniciada

A alteração deve ser bloqueada.

---

## 10. Estados esperados

- selecionando tipo;
- carregando programas;
- gerando novo treino;
- treino alterado;
- tipo indisponível;
- acesso bloqueado;
- quest já iniciada.

---

## 11. Impacto no Frontend Flutter

- Ação “Alterar tipo de treino”.
- Bottom sheet/modal com tipos de treino.
- Lista de programas disponíveis.
- Estado de carregamento durante troca.
- Remover ações de substituir exercício individual.

---

## 12. Impacto no Backend

- Endpoint para trocar tipo da quest antes do início.
- Validar status de acesso e status da quest.
- Gerar treino completo conforme tipo escolhido.
- Recalcular XP e duração.

---

## 13. Impacto no Banco de Dados

Entidades:

- Quest;
- QuestExercise;
- Program;
- UserProfile.

Campos sugeridos:

- Quest.trainingType;
- Quest.programId;
- Quest.estimatedXp;
- Quest.estimatedDurationMinutes.

---

## 14. Impacto em Gamificação

- XP deve refletir o treino final escolhido.
- Alterar tipo de treino não concede XP.
- Evita abuso por edição manual de volume.

---

## 15. Impacto em Monetização

- Disponível apenas para usuários com acesso ativo.
- Acesso expirado deve ir para paywall.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Tipos de treino e programas. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
PATCH /api/quests/{questId}/training-type
```

Request conceitual:

```json
{
  "trainingType": "program",
  "programId": "saitama_path"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| workout_type_change_started | Quando usuário abre troca de tipo. |
| workout_type_changed | Quando tipo do treino é alterado. |
| workout_type_change_failed | Quando alteração falha. |

---

## 19. Critérios de aceite

### CA-001 — Alterar para regeneração

Dado que o usuário possui acesso ativo,

Quando escolher Treino de Regeneração,

Então o treino inteiro deve ser substituído por um treino regenerativo compatível.

### CA-002 — Alterar para programa

Dado que o usuário escolhe Caminho de Saitama ou Perfect 2,

Quando confirmar,

Então o treino inteiro deve seguir o programa escolhido.

### CA-003 — Sem edição individual

Dado que o usuário está no pré-treino,

Quando visualizar ações disponíveis,

Então não deve existir ação de substituir exercício individual.

---

## 20. Critérios de teste para QA

- alterar para Personalizado Individual;
- alterar para Treino de Regeneração;
- alterar para Caminho de Saitama;
- alterar para Perfect 2;
- cancelar alteração;
- tentar alterar após iniciar quest;
- tentar com acesso expirado;
- validar recálculo de XP e duração;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> No EPIC-007, a única alteração permitida antes de iniciar a quest é trocar o tipo do treino inteiro; não há substituição de exercícios individuais nem edição manual de volume.
