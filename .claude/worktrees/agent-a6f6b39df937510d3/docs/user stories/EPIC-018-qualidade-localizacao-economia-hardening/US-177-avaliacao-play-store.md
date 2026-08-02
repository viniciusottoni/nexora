---
title: US-177 — Solicitar avaliação na Play Store após assinar
sidebar_position: 177
---

# US-177 — Solicitar avaliação na Play Store após assinar

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-177 |
| Épico | EPIC-018 — Qualidade, Localização, Economia e Hardening Pós-MVP |
| Prioridade | P1 |
| Fase | Endurecimento pré–teste aberto |
| Perfil principal | Assinante mensal ou anual |
| Plataforma | Flutter Android |
| Status | Planejada |

## 2. História do usuário

Como **assinante do AWAKEN**,
quero **ser convidado a avaliar o app no momento certo**,
para **compartilhar minha experiência sem ser interrompido de forma invasiva**.

## 3. Objetivo

Solicitar avaliação na Play Store após assinatura e/ou momento positivo, respeitando política e quota do In-App Review.

## 4. Escopo

### Entra nesta US

- Integrar `in_app_review` ou mecanismo equivalente.
- Solicitar avaliação após assinatura confirmada.
- Evitar solicitação repetitiva.
- Não condicionar recompensa à avaliação.
- Registrar tentativa local/servidor quando aplicável.

### Fora desta US

- Compra de avaliação.
- Recompensa por avaliar.
- Controle total sobre exibição do diálogo da loja.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Solicitação deve respeitar a política e quota da Google. |
| RN-002 | Avaliação nunca pode ser forçada. |
| RN-003 | Avaliação não pode conceder XP, item ou benefício. |
| RN-004 | Usuário não deve ser incomodado repetidamente. |
| RN-005 | O app deve tolerar quando a Play Store não exibir o diálogo. |

## 6. Fluxo principal

1. Usuário assina plano mensal ou anual.
2. App identifica momento elegível.
3. App solicita avaliação usando API da loja.
4. Se a loja exibir ou não o diálogo, app segue normalmente.
5. Sistema registra tentativa para evitar repetição.

## 7. Impacto Flutter

- Adicionar dependência de review.
- Criar serviço central de review.
- Controlar elegibilidade local.
- Não bloquear fluxo se a API não mostrar prompt.

## 8. Impacto Backend

- Opcional: armazenar `lastReviewPromptAt`.
- Opcional: controlar elegibilidade por usuário.

## 9. Analytics

Eventos sugeridos:

- `review_prompt_requested`.
- `review_prompt_eligible`.
- `review_prompt_skipped`.

## 10. Critérios de aceite

### CA-001 — Solicitação pós-assinatura

Dado que o usuário acabou de assinar,
quando atingir momento elegível,
então o app deve solicitar avaliação sem bloquear o fluxo.

### CA-002 — Sem recompensa

Dado que o usuário recebeu prompt de avaliação,
quando interagir ou ignorar,
então não deve receber XP, item ou benefício por isso.

## 11. Decisão registrada

> Avaliação na Play Store é um convite opcional e sem recompensa, respeitando a política da loja.
