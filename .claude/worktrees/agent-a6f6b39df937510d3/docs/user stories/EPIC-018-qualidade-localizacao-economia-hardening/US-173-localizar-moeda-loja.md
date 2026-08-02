---
title: US-173 — Localizar valores pela moeda da loja
sidebar_position: 173
---

# US-173 — Localizar valores pela moeda da loja

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-173 |
| Épico | EPIC-018 — Qualidade, Localização, Economia e Hardening Pós-MVP |
| Prioridade | P0 |
| Fase | Endurecimento pré–teste aberto |
| Perfil principal | Usuário em Trial ou assinante |
| Plataforma | Flutter Android + RevenueCat |
| Status | Planejada |

## 2. História do usuário

Como **usuário em qualquer país**,
quero **ver preços na moeda correta da minha loja**,
para **entender o valor real da assinatura ou compra sem confusão com BRL fixo**.

## 3. Contexto

O app não deve fixar moeda ou texto como BRL no frontend. O preço exibido deve vir da loja/RevenueCat usando `StoreProduct.priceString` e `currencyCode`.

## 4. Objetivo

Remover moeda fixa dos textos e centralizar a exibição de preços conforme produto retornado pela loja.

## 5. Escopo

### Entra nesta US

- Remover textos fixos de moeda como BRL.
- Usar `StoreProduct.priceString` para preço exibido.
- Usar `currencyCode` quando necessário.
- Aplicar em planos, paywall, loja e IAP.
- Garantir fallback seguro quando produto não carregar.

### Fora desta US

- Definição de preço por país.
- A/B test de preços.
- Conversão cambial manual no app.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Nenhum texto de UI pode fixar moeda ou símbolo monetário. |
| RN-002 | O preço exibido deve vir do produto da loja. |
| RN-003 | O app não deve converter moeda manualmente. |
| RN-004 | Se o produto não carregar, exibir estado de erro ou carregamento, não preço inventado. |
| RN-005 | Textos devem funcionar em PT-BR, EN e ES. |

## 7. Fluxo principal

1. App carrega produtos pelo RevenueCat/loja.
2. App recebe `priceString` e `currencyCode`.
3. UI renderiza o preço retornado pela loja.
4. Usuário vê o valor localizado do país/conta da loja.

## 8. Impacto Flutter

- Centralizar componente de preço.
- Remover texto fixo em `pricingFooter` e similares.
- Garantir loading/error states.
- Ajustar i18n para não incluir moeda fixa.

## 9. Impacto Backend

- Não calcular preço de loja no backend para o MVP.
- Apenas armazenar plano/entitlement confirmado quando compra ocorrer.

## 10. Impacto QA

- Validar conta brasileira.
- Validar conta de outro país quando possível.
- Validar falha de carregamento de produto.
- Validar ausência de texto BRL fixo.

## 11. Critérios de aceite

### CA-001 — Preço localizado

Dado que a loja retorna preço em moeda local,
quando o paywall for exibido,
então o preço deve usar `priceString`.

### CA-002 — Sem moeda fixa

Dado que o app está em outro país,
quando abrir planos,
então não deve aparecer texto fixo de BRL.

## 12. Decisão registrada

> A loja é a fonte de verdade de moeda e preço exibido; o app não faz conversão manual nem fixa BRL em texto.
