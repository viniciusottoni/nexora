---
title: US-055 — Bloquear alteração de treino para acesso expirado
sidebar_position: 55
---

# US-055 — Bloquear alteração de treino para acesso expirado

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-055 |
| Épico | EPIC-007 — Edição de Treino Antes da Quest |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Trial expirado e assinatura expirada |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | EPIC-003 — status de acesso |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema do AWAKEN**,

quero **bloquear alteração de tipo de treino para usuário com acesso expirado**,

para **proteger o modelo comercial e manter consistência de acesso**.

---

## 3. Contexto

A alteração de tipo de treino é recurso protegido. Usuários com trial ou assinatura expirada não podem trocar para Personalizado Individual, Treino de Regeneração ou Programas até reativar acesso.

---

## 4. Objetivo

Impedir qualquer alteração no pré-treino quando o usuário não possui acesso ativo e direcionar para paywall/assinatura.

---

## 5. Escopo

### Entra nesta US

- Bloquear alteração do tipo do treino.
- Bloquear acesso a troca para Personalizado Individual, Regeneração ou Programas.
- Bloquear qualquer edição manual legada.
- Exibir CTA para assinatura.
- Preservar progresso e treino já gerado.

### Fora desta US

- Paywall completo, tratado no EPIC-003.
- Cancelamento de assinatura.
- Exclusão de conta.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Trial expirado não pode alterar tipo de treino. |
| RN-002 | Assinatura expirada não pode alterar tipo de treino. |
| RN-003 | Backend deve validar acesso antes de aceitar alteração. |
| RN-004 | Frontend deve bloquear ações visíveis quando status expirado for conhecido. |
| RN-005 | Progresso e treino já salvo não devem ser apagados por bloqueio. |
| RN-006 | Usuário deve ser direcionado para paywall ou tela de planos. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode alterar. |
| Usuário em Trial | Pode alterar se trial ativo. |
| Premium Mensal | Pode alterar se assinatura ativa. |
| Premium Anual | Pode alterar se assinatura ativa. |
| Trial expirado | Não pode alterar. |
| Assinatura expirada | Não pode alterar. |

---

## 8. Fluxo principal

1. Usuário tenta alterar tipo do treino.
2. App consulta ou usa status de acesso atual.
3. Se acesso expirou, ação é bloqueada.
4. App exibe mensagem e CTA de assinatura.
5. Backend recusa qualquer tentativa direta de alteração.

---

## 9. Fluxos alternativos

### 9.1. Status expira durante a tela aberta

Ao tentar confirmar alteração, backend deve recusar e app atualizar para estado bloqueado.

### 9.2. Usuário reativa assinatura

Após sincronização, alteração de tipo volta a ficar disponível.

---

## 10. Estados esperados

- acesso ativo;
- acesso expirado;
- ação bloqueada;
- CTA para assinatura;
- acesso restaurado;
- erro de sincronização.

---

## 11. Impacto no Frontend Flutter

- Guard da ação “Alterar tipo de treino”.
- Estado visual bloqueado.
- Mensagem de acesso expirado.
- CTA para paywall/planos.

---

## 12. Impacto no Backend

- Validar acesso em endpoint de troca de tipo.
- Rejeitar endpoints legados de edição manual.
- Retornar erro funcional padronizado para acesso expirado.

---

## 13. Impacto no Banco de Dados

Entidades:

- Subscription;
- Quest;
- QuestExercise.

Não deve apagar dados por bloqueio.

---

## 14. Impacto em Gamificação

- Bloqueio impede novas alterações.
- Progresso anterior permanece preservado.

---

## 15. Impacto em Monetização

- Protege o modelo trial + assinatura.
- Direciona usuário para reativação.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de bloqueio e CTA. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

Erro esperado:

```json
{
  "code": "ACCESS_EXPIRED",
  "message": "Sua assinatura ou teste expirou. Assine para alterar o tipo do treino.",
  "correlationId": "uuid"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| access_blocked | Quando usuário expirado tenta alterar tipo. |
| paywall_after_trial_viewed | Quando CTA leva ao paywall pós-trial. |

---

## 19. Critérios de aceite

### CA-001 — Trial expirado bloqueado

Dado que o trial expirou,

Quando usuário tentar alterar tipo de treino,

Então a ação deve ser bloqueada.

### CA-002 — Backend protege regra

Dado que usuário expirado chama endpoint diretamente,

Quando tentar alterar o tipo,

Então backend deve recusar a alteração.

---

## 20. Critérios de teste para QA

- trial expirado tentando trocar tipo;
- assinatura expirada tentando trocar tipo;
- tentativa direta no backend;
- reativação de acesso;
- progresso preservado;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Alteração de tipo de treino é recurso protegido: trial ou assinatura expirada bloqueia a ação até reativação do acesso.
