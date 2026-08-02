---
title: US-235 — Exibir moldura dourada animada para plano anual
sidebar_position: 235
---

# US-235 — Exibir moldura dourada animada para plano anual

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-235 |
| Épico | EPIC-021 — Refinamento de Loja, Programas e Perfil |
| Prioridade | P1 |
| Fase | Refinamento funcional pós-fundação de economia |
| Perfil principal | Premium Anual |
| Dependências | EPIC-003, EPIC-010 |
| Status | Planejada |

## 2. História do usuário

Como **assinante anual do AWAKEN**, quero **ter uma moldura dourada com brilho suave no meu perfil/card**, para **perceber um benefício visual premium sem afetar a progressão do jogo**.

## 3. Contexto

O plano anual possui benefício visual diferenciado. A moldura deve ser dourada, premium e discreta, seguindo o visual dark/RPG do AWAKEN sem poluir a tela.

## 4. Objetivo

Exibir moldura dourada animada com brilho suave para usuários Premium Anual no Perfil Hunter e card compartilhável.

## 5. Escopo

### Entra nesta US

- Detectar entitlement Premium Anual ativo.
- Aplicar moldura dourada no Perfil Hunter.
- Aplicar moldura dourada no card compartilhável.
- Criar brilho suave/loop discreto.
- Remover/ocultar quando anual não estiver ativo.
- Fallback estático quando animação estiver desabilitada.

### Fora desta US

- Benefício de XP para plano anual.
- Pay-to-win.
- Moldura customizável livre.
- Animação pesada que prejudique performance.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Apenas Premium Anual ativo recebe moldura dourada. |
| RN-002 | Plano mensal não recebe a moldura anual. |
| RN-003 | Trial não recebe a moldura anual. |
| RN-004 | Moldura anual é benefício cosmético, sem XP, item ou progressão. |
| RN-005 | Se assinatura anual expirar, moldura deixa de aparecer. |
| RN-006 | Animação deve ser suave e respeitar performance/acessibilidade. |

## 7. Fluxo principal

1. Usuário anual abre Perfil Hunter.
2. App consulta status de entitlement.
3. Backend/RevenueCat confirma plano anual ativo.
4. App renderiza moldura dourada com brilho suave.
5. Card compartilhável usa a mesma identidade visual.

## 8. Impacto Flutter

- Componente `AnnualGoldenFrame` ou equivalente.
- Animação leve de brilho.
- Fallback estático.
- Aplicação no Perfil Hunter e card compartilhável.
- Teste em aparelhos Android de menor desempenho.

## 9. Impacto Backend

- Retornar status de plano anual no perfil/entitlement.
- Garantir que plano mensal e trial não sejam confundidos com anual.

## 10. Impacto UX/UI

- Dourado premium com glow discreto.
- Sem excesso de brilho.
- Compatível com fundo dark do AWAKEN.
- Não competir com rank/atributos.

## 11. Critérios de aceite

### CA-001 — Anual ativo vê moldura

Dado que o usuário possui plano anual ativo,
quando abrir o Perfil Hunter,
então deve ver moldura dourada com brilho suave.

### CA-002 — Mensal não vê moldura anual

Dado que o usuário possui plano mensal,
quando abrir o Perfil Hunter,
então não deve ver moldura dourada anual.

### CA-003 — Expiração remove benefício

Dado que o plano anual expirou,
quando o app sincronizar status,
então a moldura anual deve deixar de aparecer.

## 12. Decisão registrada

A moldura dourada do plano anual é benefício cosmético premium, com brilho suave e sem vantagem de progressão.
