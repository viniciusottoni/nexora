---
title: US-053 — Validar alteração do tipo de treino
sidebar_position: 53
---

# US-053 — Validar alteração do tipo de treino

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-053 |
| Épico | EPIC-007 — Edição de Treino Antes da Quest |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema e usuário com acesso ativo |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Validador de tipo de treino |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema do AWAKEN**,

quero **validar a alteração do tipo de treino antes de aplicá-la**,

para **garantir que o novo treino seja permitido, compatível e coerente**.

---

## 3. Contexto

A única alteração permitida no EPIC-007 é trocar o tipo do treino inteiro. Mesmo assim, o sistema precisa validar acesso, status da quest, tipo escolhido, programa selecionado e compatibilidade do treino gerado.

---

## 4. Objetivo

Centralizar a validação da troca entre Personalizado Individual, Treino de Regeneração e Programas disponíveis.

---

## 5. Escopo

### Entra nesta US

- Validar status de acesso.
- Validar que a quest ainda não iniciou.
- Validar tipo de treino escolhido.
- Validar programa escolhido quando o tipo for Programa.
- Gerar treino completo compatível com o tipo.
- Recalcular XP e duração.
- Bloquear qualquer edição manual de exercício ou volume.

### Fora desta US

- Alterar exercício individual.
- Alterar séries, repetições, tempo ou descanso.
- Criar treino do zero.
- Execução da quest.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Alteração só é válida antes da quest iniciar. |
| RN-002 | Apenas tipos permitidos podem ser escolhidos. |
| RN-003 | Programas devem estar ativos e disponíveis. |
| RN-004 | O treino gerado deve respeitar perfil, limitações e catálogo. |
| RN-005 | XP e duração devem ser recalculados após troca. |
| RN-006 | Qualquer tentativa de edição manual deve ser rejeitada. |
| RN-007 | Acesso expirado deve bloquear a alteração. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema | Pode validar alteração. |
| Usuário em Trial | Pode alterar se acesso ativo. |
| Premium Mensal | Pode alterar se acesso ativo. |
| Premium Anual | Pode alterar se acesso ativo. |
| Trial expirado | Não pode alterar. |
| Assinatura expirada | Não pode alterar. |

---

## 8. Fluxo principal

1. Usuário escolhe novo tipo de treino.
2. App envia solicitação para backend.
3. Backend valida acesso e status da quest.
4. Backend valida tipo/programa escolhido.
5. Backend gera treino completo conforme opção.
6. Backend recalcula XP e duração.
7. App exibe novo treino para revisão.

---

## 9. Fluxos alternativos

### 9.1. Tipo inválido

Backend rejeita com mensagem funcional.

### 9.2. Programa indisponível

Backend rejeita e mantém treino atual.

### 9.3. Quest iniciada

Backend bloqueia a alteração.

---

## 10. Estados esperados

- validando tipo;
- tipo válido;
- tipo inválido;
- programa indisponível;
- treino regenerado;
- acesso bloqueado;
- quest iniciada.

---

## 11. Impacto no Frontend Flutter

- Exibir estado de validação.
- Mostrar erro amigável quando tipo/programa não for permitido.
- Atualizar prévia do treino após sucesso.
- Não exibir controles manuais de edição.

---

## 12. Impacto no Backend

- Serviço de validação de tipo de treino.
- Endpoint para troca de tipo.
- Recalcular XP/duração.
- Bloquear edição manual antiga.

---

## 13. Impacto no Banco de Dados

Entidades:

- Quest;
- QuestExercise;
- Program;
- UserProfile;
- Subscription.

---

## 14. Impacto em Gamificação

- Mantém XP coerente com o treino final.
- Evita abuso por manipulação manual.
- A troca de tipo não concede XP.

---

## 15. Impacto em Monetização

- Disponível apenas com acesso ativo.
- Acesso expirado direciona para paywall.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de validação. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
POST /api/quests/{questId}/validate-training-type-change
```

Response conceitual:

```json
{
  "valid": true,
  "estimatedXp": 90,
  "estimatedDurationMinutes": 18
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| workout_type_change_validated | Quando troca de tipo é validada. |
| workout_type_change_rejected | Quando troca de tipo é rejeitada. |

---

## 19. Critérios de aceite

### CA-001 — Tipo válido

Dado que o usuário escolhe um tipo permitido,

Quando validar,

Então o sistema deve gerar o treino compatível.

### CA-002 — Edição manual recusada

Dado que uma tentativa tenta alterar séries ou exercícios,

Quando chegar ao backend,

Então deve ser recusada.

---

## 20. Critérios de teste para QA

- validar Personalizado Individual;
- validar Treino de Regeneração;
- validar Caminho de Saitama;
- validar Perfect 2;
- programa indisponível;
- tipo inválido;
- tentativa de edição manual;
- acesso expirado;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> A validação do EPIC-007 deve aceitar apenas alteração do tipo de treino inteiro e rejeitar qualquer edição manual de exercício ou volume.
