---
title: EPIC-003 — Trial, Planos, Assinatura e Bloqueio de Acesso
sidebar_position: 3
---

# EPIC-003 — Trial, Planos, Assinatura e Bloqueio de Acesso

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | EPIC-003 |
| Fase | MVP Android Fitness Gamificado |
| Prioridade | P0 |
| Perfil principal | Visitante, usuário em trial, assinante e usuário bloqueado |
| Planos impactados | Trial, Mensal e Anual |
| Integração principal | RevenueCat SDK + Backend AWAKEN + Webhook |
| Modelo de paywall | Tela própria Flutter dentro do AWAKEN, sem uso obrigatório do RevenueCat Paywall Builder |
| Ambientes | Sandbox e produção chaveáveis por configuração |
| Status | Planejado |

## 2. Objetivo

Implementar o modelo comercial vigente do AWAKEN: o usuário pode testar gratuitamente por 7 dias sem cartão e, após esse período, precisa assinar o plano mensal (R$ 14,90/mês) ou anual (R$ 99,90/ano) para continuar usando. A escolha do plano deve acontecer antes do cadastro, na tela pricing, ser salva e depois aplicada à conta criada.

O AWAKEN deve usar a RevenueCat como motor de assinatura, produtos, entitlements, compras, restauração e sincronização, mas a experiência visual de monetização deve ficar dentro do próprio sistema, por meio de telas Flutter alinhadas ao design system do AWAKEN. A tela pricing é o único canal para escolha do plano; o paywall posterior só confirma a opção salva e executa a compra vinculada à conta.

Durante o trial, itens conquistados em dungeons não podem ser recebidos nem usados — ficam armazenados e são liberados ao assinar. A compra só acontece depois do cadastramento da conta e da vinculação da opção salva na pricing.

## 3. Contexto de produto

Este épico substitui o conceito de freemium permanente. O AWAKEN não terá Free Hunter contínuo no MVP. O acesso gratuito é temporário e deve ser comunicado antes do onboarding para evitar dark pattern. A restrição de itens durante o trial cria incentivo de conversão vinculado ao sistema de dungeons.

A experiência de paywall deve ser própria do AWAKEN, não uma tela genérica criada no RevenueCat Dashboard. A RevenueCat deve fornecer os dados comerciais e transacionais; o AWAKEN deve controlar narrativa, layout, copy, estados de tela e bloqueio de rotas protegidas.

## 4. Planos e benefícios

| Plano | Preço de referência | Benefícios |
|---|---|---|
| Teste grátis | R$ 0 por 7 dias — sem cartão | Acesso total ao S-Rank por 7 dias; quests ilimitadas e personalizadas por IA; itens de dungeons ficam armazenados (não podem ser recebidos nem usados) |
| Mensal | R$ 14,90/mês | Tudo do S-Rank, sempre liberado; Master Quests; nutrição completa; sem anúncios; cancele quando quiser |
| Anual | R$ 99,90/ano (= R$ 8,32/mês) | Tudo do plano mensal + card de perfil animado + prioridade no suporte; quase 2 meses grátis vs. mensal; desconto de 45% |

> O desconto de 45% no plano anual deve ser destacado visivelmente na tela de planos.

> Na tela final do app, o preço exibido ao usuário deve vir preferencialmente do produto retornado pelo RevenueCat SDK / Google Play, para evitar divergência entre documentação, loja e aplicativo. Os valores acima são referência de produto e QA.

## 5. Arquitetura de monetização adotada

### 5.1. Decisão principal

O AWAKEN **não deve depender do RevenueCat Paywall Builder** para exibir a tela de assinatura no MVP.

O modelo adotado é:

```txt
Flutter AWAKEN
↓
Telas próprias do sistema
↓
RevenueCat SDK
↓
Google Play Billing / RevenueCat Test Store
↓
RevenueCat Entitlement
↓
Backend AWAKEN sincroniza e valida status
```

### 5.2. Responsabilidades por camada

| Camada | Responsabilidade |
|---|---|
| Flutter AWAKEN | Exibir telas próprias de trial, planos, bloqueio, assinatura, erro e restauração |
| RevenueCat SDK | Buscar offerings/packages/produtos, iniciar compra, restaurar compras e retornar `CustomerInfo` |
| Google Play Billing | Processar a compra Android real ou sandbox da loja |
| RevenueCat Dashboard | Manter produtos, entitlement, offering, packages, eventos e histórico de cliente |
| Backend AWAKEN | Ser fonte de verdade do status de acesso, bloquear endpoints protegidos e sincronizar webhooks |
| Banco de dados | Persistir status comercial, trial, expiração, plano, customer id e trilha de auditoria |

### 5.3. O que deve existir na RevenueCat

Mesmo sem usar o paywall visual da RevenueCat, devem existir:

```txt
Entitlement: premium
Offering: default
Packages: Monthly e Annual
Produtos: mensal e anual conforme IDs configurados na RevenueCat/Google Play
```

Caso os IDs reais configurados na RevenueCat ou Google Play sejam diferentes, o app deve obtê-los por configuração de ambiente, sem hardcode espalhado nas telas.

### 5.4. Chaveamento de sandbox e produção

O app deve permitir alternar ambiente sem reescrever regra de negócio ou telas.

Configuração sugerida para o Flutter:

```txt
SUBSCRIPTION_MODE = test_store | google_sandbox | production
REVENUECAT_PUBLIC_KEY = valor público do SDK para o ambiente
REVENUECAT_ENTITLEMENT_ID = premium
REVENUECAT_OFFERING_ID = default
REVENUECAT_MONTHLY_PACKAGE = Monthly
REVENUECAT_ANNUAL_PACKAGE = Annual
```

Configuração sugerida para o backend:

```txt
RevenueCat:Environment = Sandbox | Production | Both
RevenueCat:EntitlementId = premium
RevenueCat:OfferingId = default
RevenueCat:WebhookValidation = valor seguro somente no servidor
```

Regras para ambientes:

| Ambiente | Uso | App usa | Backend aceita | Observação |
|---|---|---|---|---|
| `test_store` | Smoke test rápido sem Google Play | chave pública do RevenueCat Test Store | eventos de teste | Útil para validar UI, entitlement e fluxo sem Play Store |
| `google_sandbox` | Teste real Android em track de teste | chave pública Android da RevenueCat | eventos sandbox | Usa Google Play Billing sandbox e conta testadora |
| `production` | App publicado | chave pública Android da RevenueCat | eventos de produção | Não deve usar chave de Test Store |

Chaves privadas da RevenueCat não podem ir para o app Flutter. Qualquer credencial privada deve ficar somente no backend.

## 6. Escopo

### Entra neste épico

- Tela inicial explicando o teste gratuito de 7 dias (sem cartão).
- Início do trial.
- Registro de início e fim do trial.
- Contador de dias restantes.
- Exibição dos 3 planos: trial, mensal (R$ 14,90) e anual (R$ 99,90), com a escolha do mensal/anual concentrada na tela pricing.
- Paywall obrigatório próprio do AWAKEN após expiração.
- Bloqueio de acesso para trial ou assinatura expirada.
- Reativação após assinatura.
- Preservação de progresso mesmo com acesso bloqueado.
- Liberação dos itens armazenados ao assinar.
- Sincronização com RevenueCat.
- Fluxo sandbox facilmente chaveável para produção.
- Telas próprias Flutter para trial, planos, paywall obrigatório, erro, carregamento e restauração.

### Fora deste épico

- Plano gratuito permanente.
- Cupons, descontos ou gift cards.
- A/B test de preço.
- Recuperação avançada de churn.
- Painel financeiro interno.
- Dependência obrigatória do RevenueCat Paywall Builder como UI final do app.

## 7. User Stories relacionadas

| ID | Título | Prioridade | Documento |
|---|---|---|---|
| US-014 | Entender teste gratuito e assinatura antes do onboarding | P0 | [Abrir](./US-014-entender-trial-planos-antes-onboarding.md) |
| US-015 | Iniciar teste gratuito de 7 dias | P0 | [Abrir](./US-015-iniciar-trial-7-dias.md) |
| US-016 | Registrar início e fim do trial | P0 | [Abrir](./US-016-registrar-inicio-fim-trial.md) |
| US-017 | Visualizar benefícios e preços dos planos mensal e anual | P0 | [Abrir](./US-017-visualizar-beneficios-planos.md) |
| US-018 | Sincronizar entitlement com RevenueCat | P0 | [Abrir](./US-018-sincronizar-entitlement-revenuecat.md) |
| US-019 | Reconhecer acesso de assinante mensal ou anual | P0 | [Abrir](./US-019-reconhecer-acesso-assinante.md) |
| US-020 | Exibir paywall obrigatório próprio para trial ou assinatura expirada | P0 | [Abrir](./US-020-paywall-obrigatorio-acesso-expirado.md) |
| US-021 | Exibir paywall próprio, honesto e previsível | P0 | [Abrir](./US-021-paywall-honesto-previsivel.md) |
| US-116 | Visualizar dias restantes do trial | P0 | [Abrir](./US-116-visualizar-dias-restantes-trial.md) |
| US-117 | Receber avisos de fim do trial | P1 | [Abrir](./US-117-avisos-fim-trial.md) |
| US-118 | Assinar plano mensal | P0 | [Abrir](./US-118-assinar-plano-mensal.md) |
| US-119 | Assinar plano anual | P0 | [Abrir](./US-119-assinar-plano-anual.md) |
| US-120 | Reativar acesso após assinatura | P0 | [Abrir](./US-120-reativar-acesso-apos-assinatura.md) |
| US-121 | Preservar progresso após bloqueio | P0 | [Abrir](./US-121-preservar-progresso-apos-bloqueio.md) |
| US-122 | Impedir reinício indevido de trial | P0 | [Abrir](./US-122-impedir-reinicio-indevido-trial.md) |

## 8. Regras de negócio

| ID | Regra |
|---|---|
| RN-COM-001 | Todo novo usuário tem direito a um único trial de 7 dias, sem necessidade de cartão. |
| RN-COM-002 | A tela pricing apresenta o trial de 7 dias antes do cadastro, de forma clara e sem dark pattern, e é o único canal de escolha do plano mensal ou anual. |
| RN-COM-003 | Após 7 dias, o acesso às funcionalidades principais deve ser bloqueado se não houver assinatura ativa. |
| RN-COM-004 | O trial inicia automaticamente após o cadastro, sem necessidade de pagamento, e a escolha feita na pricing fica salva para uso posterior. |
| RN-COM-005 | Após o cadastro, o sistema usa a escolha salva na pricing para direcionar a compra via RevenueCat. A assinatura só é ativada após confirmação do pagamento. |
| RN-COM-006 | O progresso não deve ser apagado quando trial ou assinatura expirar. |
| RN-COM-007 | O paywall deve ser claro, obrigatório e sem linguagem enganosa. |
| RN-COM-008 | O backend deve ser a fonte de verdade para status de acesso. |
| RN-COM-009 | Durante o trial, o usuário não pode receber nem usar itens de dungeons. Os itens gerados ficam armazenados e são liberados ao assinar qualquer plano. |
| RN-COM-010 | O plano anual deve exibir o percentual de desconto (45%) e o equivalente mensal (R$ 8,32/mês) de forma destacada. |
| RN-COM-011 | O plano anual concede ao assinante: card de perfil animado e prioridade no suporte. |
| RN-COM-012 | A UI final do paywall deve ser própria do AWAKEN em Flutter, não uma tela genérica do RevenueCat Paywall Builder. |
| RN-COM-013 | O paywall exibido após expiração do trial deve confirmar o plano salvo e seguir a compra do RevenueCat, sem permitir nova escolha fora da pricing. |
| RN-COM-014 | O backend deve usar webhook e/ou API da RevenueCat para sincronizar status de assinatura. |
| RN-COM-015 | O ambiente de assinatura deve ser chaveável por configuração (`test_store`, `google_sandbox`, `production`). |
| RN-COM-016 | Credenciais privadas da RevenueCat não podem ser expostas no app Flutter. |
| RN-COM-017 | A pricing screen é o único canal para escolher o revenue; o paywall apenas confirma e executa a compra do plano já salvo. |
| RN-COM-018 | Preços exibidos na tela final devem preferencialmente vir do produto retornado pela loja via RevenueCat SDK. |

## 9. Impactos técnicos

### Flutter

- Tela de trial e planos antes do onboarding.
- Exibição dos 3 planos (trial, mensal, anual) com preços, benefícios e destaque do desconto anual.
- Paywall obrigatório próprio do AWAKEN.
- Contador de dias restantes do trial.
- Estados de acesso: visitante, trial ativo, assinatura ativa, trial expirado e assinatura expirada.
- Bloqueio de rotas protegidas.
- Indicador visual de itens armazenados para usuário em trial.
- Camada de configuração de assinatura por ambiente.
- Serviço de assinatura usando RevenueCat SDK.
- Busca de offering atual ou offering configurado.
- Mapeamento de packages mensal/anual para cards do sistema.
- Compra via package selecionado.
- Restauração de compras.
- Tratamento de loading, erro de planos, compra cancelada, compra concluída e sincronização pendente.

### Backend

- Controle de início e fim do trial.
- Verificação de status de acesso.
- Sincronização com RevenueCat.
- Endpoint de status de assinatura.
- Endpoint opcional de sync após compra/restauração.
- Webhook RevenueCat com validação de origem/evento.
- Reativação de acesso após assinatura.
- Liberação de itens armazenados ao assinar.
- Logs de mudança de status comercial.
- Configuração de ambiente sandbox/produção.

### Banco de dados

Entidades principais:

- Subscription.
- AccessStatus.
- User.

Campos relevantes:

- plan (trial | monthly | annual).
- status (active | expired | blocked).
- accessStatus.
- trialStartedAt.
- trialEndsAt.
- expiresAt.
- revenueCatCustomerId.
- revenueCatEntitlementId.
- revenueCatOfferingId.
- revenueCatProductId.
- revenueCatEnvironment (sandbox | production).
- lastRevenueCatEventAt.
- lastSyncedAt.

### Analytics

- `trial_offer_viewed`.
- `trial_started`.
- `trial_day_count_viewed`.
- `trial_expired`.
- `paywall_after_trial_viewed`.
- `paywall_custom_viewed`.
- `monthly_plan_selected`.
- `annual_plan_selected`.
- `subscription_purchase_started`.
- `subscription_purchase_cancelled`.
- `subscription_purchase_failed`.
- `subscription_started`.
- `purchase_restored`.
- `access_blocked`.
- `access_restored`.
- `stored_items_released` (ao assinar e liberar itens armazenados do trial).

### QA

- Início de trial sem cartão.
- Expiração de trial.
- Bloqueio após expiração.
- Verificar que itens de dungeon não são recebidos nem usáveis durante trial.
- Verificar que itens ficam armazenados e são liberados ao assinar.
- Compra mensal em sandbox.
- Compra anual em sandbox.
- Compra mensal em produção controlada.
- Compra anual em produção controlada.
- Verificar desconto e equivalente mensal exibidos corretamente no plano anual.
- Verificar preços vindos da loja/RevenueCat SDK.
- Chaveamento `test_store` → `google_sandbox` → `production` sem alterar telas.
- Reativação após assinatura.
- Preservação de progresso.
- Tentativa de acesso com assinatura expirada.
- Restauração de compra.
- Webhook recebido e status sincronizado.

## 10. Dependências

- EPIC-001 para navegação e estados.
- EPIC-002 para usuário autenticado.
- EPIC-008/009 para sistema de itens (HunterInventory).
- RevenueCat configurado com entitlement, produtos, offering e packages.
- Google Play configurado para sandbox real Android quando necessário.
- Backend com controle de status de acesso.
- Webhook RevenueCat configurável por ambiente.
- Configuração segura de chaves públicas no app e credenciais privadas somente no backend.

## 11. Critérios de aceite do épico

- Usuário entende antes do onboarding que o teste dura 7 dias e não precisa de cartão.
- Usuário inicia trial.
- Sistema bloqueia acesso após expiração sem assinatura.
- Itens de dungeons conquistados no trial ficam armazenados e não podem ser usados até assinar.
- Usuário consegue escolher mensal (R$ 14,90) ou anual (R$ 99,90 com -45% destacado).
- Paywall exibido após expiração é uma tela própria do AWAKEN, não uma UI genérica do RevenueCat.
- App consegue buscar planos pelo RevenueCat SDK e renderizar os cards no design system do AWAKEN.
- App consegue testar em sandbox e ser chaveado para produção por configuração.
- Ao assinar, acesso e itens são restaurados.
- Progresso não é perdido.
- Eventos comerciais são rastreados.

## 12. Decisão registrada

O AWAKEN não possui plano gratuito permanente no MVP. O acesso gratuito é limitado ao trial de 7 dias sem cartão. Após esse período, o usuário precisa assinar plano mensal (R$ 14,90/mês) ou anual (R$ 99,90/ano). Itens de dungeons ficam bloqueados durante o trial para criar incentivo de conversão vinculado ao sistema de recompensas. O plano anual concede exclusivos: card de perfil animado e prioridade no suporte.

A decisão técnica para o MVP é usar a RevenueCat como infraestrutura de assinatura e usar telas próprias do AWAKEN para monetização. O RevenueCat Paywall Builder não é obrigatório para a UI final do app. O fluxo deve suportar sandbox e produção por configuração, preservando a mesma experiência visual e as mesmas regras de negócio.
