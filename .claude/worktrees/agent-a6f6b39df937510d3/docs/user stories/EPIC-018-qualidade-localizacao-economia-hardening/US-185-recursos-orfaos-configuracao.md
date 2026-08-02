---
title: US-185 — Auditar recursos órfãos e ajustes de configuração
sidebar_position: 185
---

# US-185 — Auditar recursos órfãos e ajustes de configuração

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-185 |
| Épico | EPIC-018 — Qualidade, Localização, Economia e Hardening Pós-MVP |
| Prioridade | P1 |
| Fase | Endurecimento pré–teste aberto |
| Perfil principal | Engenharia, QA e Produto |
| Plataforma | Flutter Android + Backend .NET 10 |
| Status | Planejada |

## 2. História do usuário

Como **time de engenharia e produto**,
quero **auditar recursos órfãos e configurações pendentes**,
para **remover código morto, ligar fluxos incompletos e reduzir risco antes do teste aberto**.

## 3. Objetivo

Mapear chaves l10n, DTOs, endpoints, tiles, mocks e configurações que existem no código, mas não estão ligados a fluxos reais ou estão parcialmente implementados.

## 4. Escopo

### Entra nesta US

- Auditar chaves l10n órfãs.
- Auditar DTOs não usados.
- Auditar tiles sem ação.
- Auditar itens de loja mock.
- Auditar endpoints deferidos ou não conectados.
- Remover, documentar ou ligar recursos ao fluxo correto.

### Fora desta US

- Reescrita total do app.
- Refatoração ampla sem ligação com recurso órfão.
- Implementar EPIC-017 inteiro.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Recurso órfão deve ser removido, documentado ou ligado a uma US. |
| RN-002 | Tela não deve conter ação visual sem comportamento. |
| RN-003 | Mock de loja não deve parecer funcional em ambiente real. |
| RN-004 | Configurações pendentes devem ter destino claro. |
| RN-005 | QA deve validar que não restam fluxos mortos visíveis ao usuário. |

## 6. Impacto Flutter

- Revisar l10n.
- Revisar widgets não usados.
- Revisar rotas e tiles.
- Revisar mocks visíveis.

## 7. Impacto Backend

- Revisar DTOs não usados.
- Revisar endpoints sem consumidor.
- Revisar configs por ambiente.
- Documentar recursos deferidos.

## 8. Impacto QA

- Navegação completa pelo app.
- Verificar todos os botões visíveis.
- Verificar loja sem mock enganoso.
- Verificar configuração por ambiente.

## 9. Critérios de aceite

### CA-001 — Recurso órfão tratado

Dado que um recurso órfão foi identificado,
quando a auditoria for concluída,
então ele deve estar removido, conectado ou documentado.

### CA-002 — Sem ação morta visível

Dado que o usuário vê um botão ou tile,
quando tocar,
então deve existir ação funcional ou feedback claro.

## 10. Decisão registrada

> Recursos órfãos e ajustes pendentes devem ser saneados antes do teste aberto para evitar percepção de produto inacabado.
