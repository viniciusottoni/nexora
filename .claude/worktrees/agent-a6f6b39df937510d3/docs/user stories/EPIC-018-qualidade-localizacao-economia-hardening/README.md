---
title: EPIC-018 — Qualidade, Localização, Economia e Hardening Pós-MVP
sidebar_position: 18
---

# EPIC-018 — Qualidade, Localização, Economia e Hardening Pós-MVP

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | EPIC-018 |
| Fase | Endurecimento pré–teste aberto |
| Prioridade | P0 / P1 |
| Perfil principal | Engenharia, QA, Produto e Segurança |
| Planos impactados | Trial, Mensal e Anual |
| Plataforma | Flutter Android + Backend .NET 10 |
| Status | Planejado |

## 2. Objetivo

Consolidar mudanças transversais de qualidade, localização, economia real, configurações, avaliação, componentização, escalabilidade, performance, segurança e saneamento de recursos órfãos antes do teste aberto e do release Android.

## 3. Contexto

O EPIC-018 concentra itens que não pertencem ao escopo estreito da US-016 nem ao site admin EPIC-017, mas são pré-requisitos de qualidade para o release. A US-016 cobre apenas o recorte de UTC do ciclo de trial; o EPIC-017 consome rastreios e telas administrativas após aprovação.

## 4. Escopo

### Entra neste épico

- UTC cross-cutting fora do recorte de trial.
- Moeda localizada pela loja.
- Equipamento disponível no onboarding alimentando geração.
- Configurações completas e funcionais.
- Abertura de ticket pelo app.
- Avaliação na Play Store após assinatura.
- Catálogo de itens e valores.
- IAP real via RevenueCat para consumíveis e slots.
- RBAC para endpoints admin.
- Hardening básico de segurança.
- Componentização do frontend.
- Escalabilidade e performance.
- Auditoria de recursos órfãos.

### Fora deste épico

- Site admin e suas telas.
- Rastreio administrativo proposto para EPIC-017.
- Recorte de UTC do trial.
- Publicação Android e smoke test.
- Moeda virtual de jogo emitida por atividade.

## 5. User Stories relacionadas

| ID | Título | Prioridade | Documento |
|---|---|---|---|
| US-172 | Auditar e corrigir UTC em todo o código | P0 | [Abrir](./US-172-utc-cross-cutting.md) |
| US-173 | Localizar valores pela moeda da loja | P0 | [Abrir](./US-173-localizar-moeda-loja.md) |
| US-174 | Equipamento disponível no onboarding alimentando geração | P0 | [Abrir](./US-174-equipamento-onboarding-geracao.md) |
| US-175 | Tela de configurações completa e funcional | P0 | [Abrir](./US-175-configuracoes-funcional.md) |
| US-176 | Abrir ticket pelo app | P0 | [Abrir](./US_176-ticket-app.md) |
| US-177 | Solicitar avaliação na Play Store após assinar | P1 | [Abrir](./US-177-avaliacao-play-store.md) |
| US-178 | Catálogo e sugestão de itens e valores | P1 | [Abrir](./US_178-catalogo-itens-valores.md) |
| US-179 | Comprar consumíveis e slots via RevenueCat IAP | P1 | [Abrir](./US_179-iap-consumiveis-slots.md) |
| US-180 | RBAC e autorização por perfil nos endpoints admin | P0 | [Abrir](./US_180-rbac-admin.md) |
| US-181 | Hardening de segurança | P0 | [Abrir](./US_181-hardening-seguranca.md) |
| US-182 | Componentização do frontend | P1 | [Abrir](./US-182-componentizacao-frontend.md) |
| US-183 | Escalabilidade | P1 | [Abrir](./US-183-escalabilidade.md) |
| US-184 | Performance | P1 | [Abrir](./US-184-performance.md) |
| US-185 | Auditar recursos órfãos e ajustes de configuração | P1 | [Abrir](./US-185-recursos-orfaos-configuracao.md) |

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-EPIC-018-001 | Datas persistidas devem usar UTC; o app só exibe no fuso local. |
| RN-EPIC-018-002 | Preço exibido vem da loja do usuário, sem moeda fixa em texto. |
| RN-EPIC-018-003 | Compra de item/slot é validada no servidor via RevenueCat e idempotente. |
| RN-EPIC-018-004 | Endpoint admin exige perfil/claim de admin. |
| RN-EPIC-018-005 | Configurações não podem ter tile visível sem ação funcional. |
| RN-EPIC-018-006 | Ticket pelo app deve ter categoria, idioma e contexto seguro. |
| RN-EPIC-018-007 | Solicitação de avaliação respeita política da Play Store e não concede recompensa. |
| RN-EPIC-018-008 | Rotas públicas e autenticação devem ter limites e proteção mínima. |

## 7. Impactos técnicos

### Flutter

- Centralização de data, moeda e componentes de UI.
- Passo/edição de equipamento disponível.
- Configurações completas.
- Abertura de ticket.
- In-App Review.
- Loja sem mocks enganosos.
- Lazy loading/cache para mídia.

### Backend

- `IDateTimeService` e persistência UTC.
- Catálogo/IAP/ledger idempotente.
- SupportTicket.
- RBAC admin.
- Rate limiting, CORS, headers e dashboard operacional protegido.
- Índices, cache e APIs stateless.

### Banco de dados

- SupportTicket.
- Ledger de transações de loja.
- Roles/claims.
- Índices para consultas críticas.
- Campos de equipamento disponível e configurações saneadas.

### QA

- Datas em múltiplos fusos.
- Moeda correta pela loja.
- Configurações sem ações mortas.
- Ticket end-to-end.
- IAP idempotente.
- Não-admin bloqueado em endpoints admin.
- Performance em rotas críticas.
- Recursos órfãos tratados.

## 8. Dependências

- EPIC-002, EPIC-003, EPIC-004, EPIC-005, EPIC-006, EPIC-014 e EPIC-015.
- ADR de IAP de consumíveis/slots.
- ADR de RBAC/autorização administrativa.
- US-180 e US-176 habilitam partes do EPIC-017.

## 9. Critérios de aceite do épico

- Nenhuma regra de negócio depende do relógio local.
- Preço exibido corresponde à loja/país do usuário.
- IAP concede inventário de forma idempotente e validada no servidor.
- Equipamento disponível influencia geração de treino.
- Configurações funcionam ponta a ponta.
- Ticket pode ser aberto pelo app.
- Avaliação é solicitada conforme política da loja.
- Endpoints admin negam acesso a não-admin.
- Recursos órfãos foram removidos, ligados ou documentados.
- Frontend tem componentes reutilizáveis nas áreas críticas.

## 10. Decisão registrada

As mudanças transversais de localização, economia real, configuração, avaliação, qualidade de frontend, escalabilidade, performance, segurança e saneamento de órfãos ficam concentradas no EPIC-018. A US-016 cobre apenas UTC do trial; rastreios administrativos permanecem propostos para EPIC-017.
