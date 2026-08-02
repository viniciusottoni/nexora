# ADR-006 — RevenueCat para assinaturas

Status: Aceito

## Contexto

O AWAKEN terá modelo freemium com plano premium mensal e anual. Implementar Google Play Billing e App Store Billing manualmente aumenta risco de erro, perda de receita e inconsistência entre Android e iOS.

## Decisão

Usar RevenueCat como camada oficial de assinatura, entitlement e sincronização de status premium.

## Implementação

- Usar `purchases_flutter` no app Flutter.
- Criar entitlements `free_hunter` e `s_rank`.
- Configurar produtos mensal e anual.
- Usar webhooks do RevenueCat no backend.
- Backend deve armazenar status local da assinatura.
- App pode atualizar UI com SDK, mas backend decide acesso final a recursos premium.
- Validar assinatura de webhook antes de processar eventos.

## Consequências

A monetização fica mais simples e preparada para Android e iOS. A equipe passa a depender de um serviço externo, portanto deve criar logs, auditoria e fallback de leitura do status premium.

## Critérios de aceite

- Compra premium libera entitlement no app.
- Webhook atualiza assinatura no backend.
- Cancelamento remove acesso premium após o período válido.
- Endpoint `/api/subscriptions/status` retorna estado confiável.
