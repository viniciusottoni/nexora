---
title: US-120 — Reativar acesso após assinatura
sidebar_position: 120
---

# US-120 — Reativar acesso após assinatura

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-120 |
| Épico | EPIC-003 — Trial, Planos, Assinatura e Bloqueio de Acesso |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Trial expirado ou assinatura expirada |
| Plano | Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Sincronização de assinatura |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário que assinou após ficar bloqueado**,

quero **recuperar imediatamente meu acesso**,

para **continuar minha evolução de onde parei**.

---

## 3. Contexto

A experiência pós-compra precisa ser imediata. Se o usuário paga e continua bloqueado, a confiança no produto é prejudicada.

---

## 4. Objetivo

Liberar rotas e recursos protegidos assim que uma assinatura mensal ou anual ativa for reconhecida.

---

## 5. Escopo

### Entra nesta US

- Reconhecer assinatura após compra.
- Atualizar status para `subscription_active`.
- Desbloquear recursos protegidos.
- Redirecionar para tela adequada.
- Restaurar acesso ao progresso existente.

### Fora desta US

- Compra mensal/anual em si.
- Reembolso.
- Mudança de plano.
- Suporte manual.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Assinatura ativa deve reativar acesso imediatamente após sincronização. |
| RN-002 | O progresso anterior deve estar disponível após reativação. |
| RN-003 | Usuário sem onboarding completo deve voltar ao onboarding. |
| RN-004 | Usuário com onboarding completo deve ir para Home/Quest. |
| RN-005 | Falha temporária de sincronização deve permitir nova tentativa. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Trial expirado | Pode reativar assinando. |
| Assinatura expirada | Pode reativar assinando. |
| Premium Mensal | Deve ter acesso reativado. |
| Premium Anual | Deve ter acesso reativado. |
| Usuário em Trial | Já possui acesso temporário. |

---

## 8. Fluxo principal

1. Usuário bloqueado conclui assinatura.
2. App sincroniza status.
3. Backend registra assinatura ativa.
4. App atualiza estado global de acesso.
5. Usuário é redirecionado para onboarding ou Home.

---

## 9. Fluxos alternativos

### 9.1. Sincronização falha

App deve exibir opção de tentar novamente ou restaurar compra.

### 9.2. Usuário fechou o app após compra

Ao reabrir, o status deve ser sincronizado e o acesso liberado.

---

## 10. Estados esperados

- aguardando sincronização;
- assinatura ativa;
- acesso restaurado;
- erro de sincronização;
- restaurar compra.

---

## 11. Impacto no Frontend Flutter

- Atualizar estado de acesso.
- Remover paywall quando acesso estiver ativo.
- Redirecionar para tela correta.
- Exibir feedback de sucesso.

---

## 12. Impacto no Backend

- Persistir status ativo.
- Expor status consolidado.
- Manter progresso associado ao usuário.

---

## 13. Impacto no Banco de Dados

Entidades:

- Subscription;
- UserProfile;
- HunterProgress;
- QuestLog.

---

## 14. Impacto em Gamificação

- Progresso anterior deve voltar a ser visível.
- Usuário pode voltar a gerar quest e evoluir.

---

## 15. Impacto em Monetização

- Garante entrega imediata do valor comprado.
- Reduz frustração pós-pagamento.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagem de acesso restaurado. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
POST /api/subscriptions/sync
```

Response conceitual:

```json
{
  "accessStatus": "subscription_active",
  "accessRestored": true
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| access_restored | Quando acesso é restaurado. |
| subscription_started | Quando assinatura é reconhecida. |

---

## 19. Critérios de aceite

### CA-001 — Acesso restaurado

Dado que o usuário concluiu assinatura,

Quando o status for sincronizado,

Então o acesso deve ser liberado.

### CA-002 — Progresso preservado

Dado que o usuário tinha progresso antes do bloqueio,

Quando o acesso for restaurado,

Então o progresso deve continuar disponível.

---

## 20. Critérios de teste para QA

- reativar após trial expirado;
- reativar após assinatura expirada;
- reabrir app após compra;
- falha de sincronização;
- progresso visível após reativação.

---

## ✅ Decisão registrada

> Assinatura reconhecida deve restaurar acesso imediatamente e preservar a jornada do usuário.
