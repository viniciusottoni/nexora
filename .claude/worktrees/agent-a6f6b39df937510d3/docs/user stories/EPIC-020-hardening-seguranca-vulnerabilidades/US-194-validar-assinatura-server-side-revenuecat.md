---
title: US-194 — Validar assinatura premium server-side via RevenueCat
sidebar_position: 194
---

# US-194 — Validar assinatura premium server-side via RevenueCat

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-194 |
| Épico | EPIC-020 — Hardening de Segurança e Fechamento de Vulnerabilidades |
| Prioridade | P0 |
| Fase | Bloqueador pré-produção / pré-teste aberto |
| Perfil principal | Usuário em trial, assinante, assinatura expirada, backend e RevenueCat |
| Plano | Trial, Mensal, Anual e Assinatura expirada |
| Idiomas impactados | PT-BR / EN / ES / FR |
| Dependência principal | RevenueCat, Subscription, SyncEntitlement, Auth Session |
| Status | Planejada |

## 2. História do usuário

Como **usuário que assinou legitimamente o AWAKEN**,

quero **que meu acesso premium seja liberado apenas após validação real da compra no servidor**,

para **impedir fraude e garantir que o acesso reflita o status verdadeiro da assinatura**.

## 3. Contexto

O endpoint atual de sincronização de assinatura aceita `RevenueCatCustomerId`, `Entitlement`, `Plan` e `ExpiresAt` enviados pelo app. Esse desenho torna o cliente uma fonte de verdade indevida para acesso pago. A assinatura deve ser validada server-side, por webhook assinado do RevenueCat e/ou consulta server-to-server à API do RevenueCat.

## 4. Objetivo

Garantir que o backend seja a única autoridade para ativar, renovar, expirar ou restaurar assinatura premium.

## 5. Escopo

### Entra nesta US

- Criar endpoint seguro para webhook RevenueCat.
- Validar assinatura/autenticidade do webhook conforme configuração RevenueCat.
- Persistir evento recebido de forma idempotente.
- Atualizar `Subscription` apenas a partir de evento/consulta server-side confiável.
- Alterar `/api/subscriptions/sync` para não aceitar `Plan`/`ExpiresAt` como verdade do app.
- Criar testes negativos de tentativa de fraude via request manual.

### Fora desta US

- Fluxo visual do paywall.
- Mudança de preço.
- Política de reembolso.
- Antifraude comportamental avançado.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O app pode solicitar sincronização, mas não pode definir plano, entitlement ou expiração. |
| RN-002 | O backend só ativa assinatura com evento RevenueCat válido ou consulta server-side válida. |
| RN-003 | Eventos duplicados devem ser idempotentes por `eventId`/`transactionId`/`originalTransactionId`. |
| RN-004 | Se a assinatura estiver expirada no RevenueCat, o backend deve retornar `subscription_expired`. |
| RN-005 | Webhook sem assinatura/autorização válida deve retornar 401/403 e não alterar dados. |
| RN-006 | Payload completo sensível não deve ser logado em texto puro. |
| RN-007 | Toda mudança de assinatura deve gerar auditoria sem dados de pagamento sensíveis. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode sincronizar assinatura. |
| Trial | Pode solicitar sync, mas sem definir status. |
| Premium Mensal/Anual | Pode solicitar sync; backend confirma status real. |
| Assinatura expirada | Pode solicitar restore/sync; backend confirma status real. |
| Admin interno | Pode consultar status, não forçar ativação por esse endpoint. |
| Sistema/RevenueCat | Pode enviar webhook autenticado. |

## 8. Fluxo principal

1. RevenueCat envia webhook de compra/renovação/cancelamento/expiração.
2. Backend valida autenticidade do evento.
3. Backend normaliza usuário pelo `app_user_id`/customer id.
4. Backend persiste evento idempotente.
5. Backend atualiza `Subscription`.
6. Backend registra auditoria.
7. App chama status/sync e recebe o estado calculado pelo servidor.

## 9. Fluxos alternativos

### Evento duplicado

O backend identifica evento já processado e retorna sucesso sem aplicar novamente.

### App tenta forjar assinatura

Request com `expiresAt` futuro ou `plan` manual é ignorado/rejeitado. Backend mantém status real.

### RevenueCat indisponível

O backend retorna erro controlado e preserva o último estado conhecido sem conceder acesso indevido.

## 10. Estados esperados

- assinatura ativa;
- assinatura expirada;
- sync pendente;
- webhook inválido;
- webhook duplicado;
- RevenueCat indisponível;
- usuário não encontrado;
- erro inesperado com `correlationId`.

## 11. Impacto no Frontend Flutter

- Remover qualquer dependência de `expiresAt`/`plan` como autoridade final.
- Exibir estado de validação pendente quando a compra foi concluída mas backend ainda não recebeu confirmação.
- Tratar restore sem prometer acesso imediato antes da validação.
- Localizar mensagens de erro e pendência.

## 12. Impacto no Backend

- Novo controller/endpoint de webhook RevenueCat.
- Serviço de validação server-side de entitlement.
- Refatoração de `SyncEntitlementCommandHandler`.
- Auditoria de eventos de assinatura.
- Testes de segurança para payload forjado.

## 13. Impacto no Banco de Dados

Entidades/campos sugeridos:

```txt
RevenueCatEvent
Id
EventId
AppUserId
OriginalTransactionId
ProductId
Type
ProcessedAtUtc
PayloadHash
CreatedAtUtc
```

Restrições:

- índice único em `EventId`;
- índice em `AppUserId`;
- payload sensível minimizado ou mascarado;
- auditoria sem cartão, token ou recibo bruto.

## 14. Impacto em Gamificação

- Usuário só acessa recursos premium, quests ilimitadas, slots ou benefícios após assinatura confirmada.

## 15. Impacto em Monetização

- Protege receita contra ativação premium fraudulenta.
- Mantém restore legítimo via RevenueCat.

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de validação pendente e erro de assinatura. |
| EN | Mesmas mensagens localizadas. |
| ES | Mesmas mensagens localizadas. |
| FR | Mesmas mensagens localizadas. |

## 17. Contrato de API sugerido

### Endpoint webhook

```txt
POST /api/webhooks/revenuecat
```

### Endpoint de sync do app

```txt
POST /api/subscriptions/sync
```

Request do app deve conter no máximo um pedido de atualização, sem `plan` ou `expiresAt` confiáveis.

### Erros esperados

```json
{
  "code": "SUBSCRIPTION_VALIDATION_FAILED",
  "message": "Não foi possível validar sua assinatura agora.",
  "correlationId": "uuid"
}
```

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| subscription_validation_started | App solicita sync. |
| subscription_validation_completed | Backend confirma status. |
| subscription_validation_failed | Backend não consegue validar. |
| subscription_fraud_attempt_blocked | Payload incompatível/forjado é rejeitado. |

## 19. Critérios de aceite

### CA-001 — Assinatura válida

Dado que o RevenueCat informa assinatura ativa,
Quando o backend processa o evento,
Então o usuário fica com `subscription_active`.

### CA-002 — Payload forjado pelo app

Dado que um usuário envia `expiresAt` futuro manualmente,
Quando chama sync,
Então o backend não ativa a assinatura sem validação server-side.

### CA-003 — Webhook inválido

Dado que um webhook chega sem autenticação válida,
Quando o endpoint é chamado,
Então retorna 401/403 e nenhuma assinatura é alterada.

### CA-004 — Idempotência

Dado que o mesmo evento RevenueCat chega duas vezes,
Quando processado,
Então a assinatura não duplica efeitos nem gera estados conflitantes.

## 20. Critérios de teste para QA

- compra válida;
- restore válido;
- assinatura expirada;
- webhook sem assinatura;
- webhook duplicado;
- app tentando ativar premium manualmente;
- RevenueCat indisponível;
- mensagens PT-BR/EN/ES/FR;
- regressão de paywall.

## ✅ Decisão registrada

O backend não aceitará mais `plan`, `entitlement` ou `expiresAt` enviados pelo app como fonte de verdade para liberar assinatura premium.