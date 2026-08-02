---
title: US-020 — Exibir paywall obrigatório próprio para acesso expirado
sidebar_position: 20
---

# US-020 — Exibir paywall obrigatório próprio para acesso expirado

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-020 |
| Épico | EPIC-003 — Trial, Planos, Assinatura e Bloqueio de Acesso |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Trial expirado e assinatura expirada |
| Plano | Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | AccessStatus, tela própria de planos, RevenueCat SDK e backend de assinaturas |
| Modelo de UI | Tela própria Flutter dentro do AWAKEN |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com trial ou assinatura expirada**,

quero **visualizar uma tela obrigatória de assinatura dentro do próprio AWAKEN**,

para **assinar mensal ou anual, recuperar meu acesso e continuar minha evolução sem perder progresso**.

---

## 3. Contexto

Após os 7 dias gratuitos, o acesso ao app deve ser bloqueado até assinatura. O paywall precisa explicar o motivo do bloqueio e confirmar o plano salvo na pricing, sem nova escolha ali.

A escolha de mensal ou anual deve ter sido feita antes, na tela pricing. O paywall desta US não é o ponto de seleção do plano; ele apenas confirma e executa a compra do plano salvo.

A decisão técnica desta US é que o paywall obrigatório **não deve depender do RevenueCat Paywall Builder**. A RevenueCat será usada como infraestrutura de assinatura, produtos, packages, entitlement, compra, restauração e sincronização. A tela visual será construída no Flutter, seguindo o design system do AWAKEN.

---

## 4. Objetivo

Bloquear recursos protegidos para usuários sem acesso ativo e direcioná-los para a compra do plano já salvo na pricing usando uma tela própria do AWAKEN, com dados comerciais vindos do RevenueCat SDK e status final validado pelo backend.

O fluxo de compra só deve começar depois da conta existir e do plano salvo ser vinculado ao usuário.

---

## 5. Escopo

### Entra nesta US

- Paywall próprio Flutter para trial expirado.
- Paywall próprio Flutter para assinatura expirada.
- Mensagem clara sobre bloqueio.
- Opções mensal e anual renderizadas como confirmação do plano salvo.
- Busca de offering/packages via RevenueCat SDK.
- Exibição de preço retornado pela loja/RevenueCat sempre que disponível.
- CTA para iniciar a compra do plano salvo.
- Confirmação do plano salvo na pricing.
- Preservação visual de progresso como motivação.
- Links mínimos para conta, termos, privacidade e suporte.
- Estados de carregamento, erro de planos, compra em andamento e sincronização.
- Chaveamento de ambiente para `test_store`, `google_sandbox` e `production` por configuração.

### Fora desta US

- Implementação detalhada da compra mensal, coberta pela US-118.
- Implementação detalhada da compra anual, coberta pela US-119.
- Notificações push.
- Descontos e cupons.
- Churn survey.
- Uso obrigatório do RevenueCat Paywall Builder como tela final do app.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Trial expirado deve bloquear recursos protegidos. |
| RN-002 | Assinatura expirada deve bloquear recursos protegidos. |
| RN-003 | O paywall deve confirmar o plano salvo na pricing e seguir com a compra correspondente. |
| RN-004 | O progresso do usuário não deve ser apagado. |
| RN-005 | O usuário deve poder acessar conta, termos, política de privacidade e suporte mínimo. |
| RN-006 | O paywall não deve sugerir que ainda existe uso gratuito permanente. |
| RN-007 | O paywall deve ser uma tela própria do AWAKEN em Flutter. |
| RN-008 | O RevenueCat Paywall Builder não deve ser dependência obrigatória para a UI final desta US. |
| RN-009 | O app deve buscar packages/produtos pelo RevenueCat SDK antes de renderizar a confirmação do plano salvo. |
| RN-010 | A compra deve ser iniciada pelo SDK a partir do package salvo, respeitando US-118 e US-119. |
| RN-011 | O backend deve continuar sendo fonte de verdade do status de acesso. |
| RN-012 | O ambiente de assinatura deve ser chaveado por configuração, sem alterar regra de negócio da tela. |
| RN-013 | Credenciais privadas da RevenueCat não podem ser expostas no app. |
| RN-014 | Preços, moeda, duração e trial da loja devem ser exibidos a partir dos dados retornados pelo SDK sempre que disponíveis. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Vê tela inicial de trial, não este paywall. |
| Usuário em Trial | Não deve ver paywall obrigatório. |
| Premium Mensal | Não deve ver paywall obrigatório. |
| Premium Anual | Não deve ver paywall obrigatório. |
| Trial expirado | Deve ver paywall obrigatório próprio do AWAKEN. |
| Assinatura expirada | Deve ver paywall obrigatório próprio do AWAKEN. |

---

## 8. Arquitetura da tela

### 8.1. Componentes Flutter sugeridos

```txt
SubscriptionExpiredPage
SubscriptionPaywallView
SubscriptionPlanCard
SubscriptionProgressPreservedBanner
SubscriptionLegalLinks
SubscriptionRestorePurchasesButton
SubscriptionEnvironmentConfig
SubscriptionLoadingState
SubscriptionErrorState
```

### 8.2. Serviços envolvidos

```txt
AccessStatusService
RevenueCatService
SubscriptionSyncService
SubscriptionGuard
SubscriptionRepository
```

### 8.3. Fontes de dados

| Fonte | Uso |
|---|---|
| Backend AWAKEN | Verificar `accessStatus`, trial, assinatura e bloqueio de recursos |
| RevenueCat SDK | Buscar offering/packages/produtos, iniciar compra e restaurar compras |
| Google Play Billing / Test Store | Processar compra real, sandbox ou teste |
| Banco do AWAKEN | Persistir status comercial e auditoria |

---

## 9. Modelo de ambientes

A tela deve funcionar igual em sandbox e produção. A diferença deve estar na configuração.

| Modo | Objetivo | Compra processada por | Uso esperado |
|---|---|---|---|
| `test_store` | Smoke test rápido sem Google Play | RevenueCat Test Store | Validar tela, entitlement, fluxo e eventos sem loja real |
| `google_sandbox` | Teste Android real | Google Play Billing sandbox | Validar compra real com conta testadora e track de teste |
| `production` | App publicado | Google Play Billing produção | Uso final do usuário |

Configuração conceitual do app:

```txt
SUBSCRIPTION_MODE
REVENUECAT_PUBLIC_KEY
REVENUECAT_ENTITLEMENT_ID
REVENUECAT_OFFERING_ID
REVENUECAT_MONTHLY_PACKAGE
REVENUECAT_ANNUAL_PACKAGE
```

O app pode usar `dart-define`, flavor, arquivo de configuração por ambiente ou mecanismo equivalente definido na arquitetura. A decisão obrigatória é: **não alterar código de regra de negócio para alternar sandbox e produção**.

---

## 10. Fluxo principal

1. Usuário com acesso expirado abre o app.
2. App consulta o status de acesso no backend.
3. Backend retorna `trial_expired` ou `subscription_expired`.
4. App bloqueia rotas protegidas.
5. App inicializa/consulta RevenueCat SDK.
6. App busca offering configurado, preferencialmente `default`.
7. App extrai packages mensal e anual.
8. App renderiza tela própria de paywall com cards mensal e anual apenas como confirmação do plano salvo.
9. App mostra mensagem de progresso preservado.
10. Usuário confirma o plano salvo.
11. App direciona para o fluxo de compra da US-118 ou US-119.
12. Após compra/restauração, app recebe `CustomerInfo`.
13. App verifica entitlement `premium` ativo.
14. App solicita sincronização com backend ou aguarda webhook.
15. Backend atualiza status e libera recursos protegidos.

---

## 11. Fluxos alternativos

### 11.1. Usuário tenta acessar quest diretamente

Deve ser redirecionado para o paywall obrigatório próprio do AWAKEN.

### 11.2. Falha ao carregar planos

Paywall deve informar erro, manter o motivo do bloqueio e oferecer nova tentativa.

### 11.3. Compra cancelada

Tela deve retornar ao estado de confirmação do plano salvo, sem erro agressivo e sem bloquear conta/termos/suporte.

### 11.4. Compra concluída, mas backend ainda não sincronizou

Tela deve exibir estado de sincronização pendente e tentar atualizar status. O usuário não deve perder progresso.

### 11.5. RevenueCat indisponível

Tela deve informar indisponibilidade temporária dos planos e permitir nova tentativa.

### 11.6. Ambiente sandbox

Em builds internos, deve ser possível identificar o ambiente ativo para QA, sem exibir rótulos técnicos para usuários de produção.

---

## 12. Estados de tela ou estados esperados

- carregando status;
- acesso expirado;
- carregando configuração de assinatura;
- carregando planos;
- paywall exibido;
- erro de planos;
- selecionando plano;
- compra em andamento;
- compra cancelada;
- compra concluída;
- sincronizando backend;
- acesso restaurado;
- erro temporário.

---

## 13. Impacto no Frontend Flutter

- Tela de paywall obrigatório própria do AWAKEN.
- Guard de rotas protegidas.
- Cards de mensal e anual.
- CTA de assinatura.
- Estado de erro e loading.
- Integração com RevenueCat SDK.
- Configuração de ambientes.
- Leitura de packages por offering.
- Exibição de preço vindo da loja.
- Botão de restaurar compras.
- Links mínimos de conta, termos, política de privacidade e suporte.

---

## 14. Impacto no Backend

- Retornar status `trial_expired` ou `subscription_expired`.
- Impedir uso de endpoints protegidos sem acesso ativo.
- Expor status de assinatura para o app.
- Receber webhook da RevenueCat.
- Sincronizar compra/restauração quando necessário.
- Persistir ambiente do evento: sandbox ou produção.
- Revalidar acesso após compra antes de liberar endpoints protegidos.

---

## 15. Impacto no Banco de Dados

Entidade principal: Subscription.

Campos relevantes:

- accessStatus;
- status;
- plan;
- expiresAt;
- trialEndsAt;
- revenueCatCustomerId;
- revenueCatEntitlementId;
- revenueCatOfferingId;
- revenueCatProductId;
- revenueCatEnvironment;
- lastSyncedAt.

---

## 16. Impacto em Gamificação

- Novas quests e XP ficam bloqueados.
- Progresso já conquistado permanece salvo.
- Itens de dungeon armazenados durante o trial permanecem guardados.
- Tela deve reforçar que XP, rank, histórico e conquistas não serão apagados.

---

## 17. Impacto em Monetização

- Paywall obrigatório é o principal ponto de conversão após trial.
- Deve ser transparente e sem dark pattern.
- Deve manter a identidade visual do AWAKEN.
- Deve usar dados de preço/produto vindos do RevenueCat SDK sempre que possível.
- Deve permitir smoke test em sandbox e troca para produção por configuração.

---

## 18. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagem de bloqueio, planos, CTAs, erro, restauração e links legais. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 19. Contrato de API sugerido

```txt
GET /api/subscriptions/status
```

Response conceitual:

```json
{
  "accessStatus": "trial_expired",
  "canAccessProtectedFeatures": false,
  "plan": null,
  "trialEndsAt": "2026-06-25T23:59:59Z"
}
```

Endpoint opcional após compra/restauração:

```txt
POST /api/subscriptions/sync
```

Payload conceitual:

```json
{
  "source": "revenuecat",
  "environment": "sandbox",
  "action": "purchase_completed"
}
```

---

## 20. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| paywall_after_trial_viewed | Quando paywall é exibido após trial. |
| paywall_custom_viewed | Quando tela própria do AWAKEN é exibida. |
| access_blocked | Quando acesso protegido é bloqueado. |
| subscription_plan_selected | Quando usuário seleciona mensal ou anual. |
| subscription_purchase_started | Quando compra é iniciada via SDK. |
| subscription_purchase_cancelled | Quando usuário cancela a compra. |
| subscription_purchase_failed | Quando compra falha. |
| subscription_sync_started | Quando app inicia sync com backend. |
| subscription_sync_completed | Quando backend confirma acesso. |

---

## 21. Critérios de aceite

### CA-001 — Trial expirado

Dado que o trial expirou,

Quando o usuário abrir o app,

Então deve ver paywall obrigatório próprio do AWAKEN.

### CA-002 — Recursos bloqueados

Dado que o acesso expirou,

Quando usuário tentar acessar quest,

Então deve ser redirecionado para paywall.

### CA-003 — Tela própria

Dado que o paywall é exibido,

Quando o QA avaliar a tela,

Então a UI deve ser do AWAKEN e não do RevenueCat Paywall Builder.

### CA-004 — Planos vindos da configuração

Dado que RevenueCat está configurado,

Quando o paywall carregar,

Então deve buscar offering/packages e exibir mensal e anual.

### CA-005 — Sandbox para produção

Dado que o app está em ambiente de teste,

Quando a configuração mudar para produção,

Então as telas e regras devem permanecer iguais, alterando apenas chaves, modo e origem dos produtos.

---

## 22. Critérios de teste para QA

- trial expirado;
- assinatura expirada;
- tentativa de acessar quest;
- tentativa de acessar perfil completo;
- planos indisponíveis;
- preço indisponível;
- compra cancelada;
- compra concluída em sandbox;
- restauração de compra;
- sincronização pendente com backend;
- mudança de `test_store` para `google_sandbox`;
- mudança de `google_sandbox` para `production`;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Após trial ou assinatura expirada, o AWAKEN deve bloquear recursos protegidos e exibir paywall obrigatório com opção mensal e anual. Esse paywall deve ser uma tela própria do sistema em Flutter, usando RevenueCat SDK para produtos/compra/entitlement e backend AWAKEN como fonte de verdade do status. O fluxo deve ser testável em sandbox e chaveável para produção por configuração.
