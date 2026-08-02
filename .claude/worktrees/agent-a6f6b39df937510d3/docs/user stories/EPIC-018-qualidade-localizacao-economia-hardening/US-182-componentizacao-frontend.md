---
title: US-182 — Componentização do frontend
sidebar_position: 182
---

# US-182 — Componentização do frontend (Clean Arch / DRY / KISS)

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-182 |
| Épico | EPIC-018 — Qualidade, Localização, Economia e Hardening Pós-MVP |
| Prioridade | P1 |
| Fase | Endurecimento pré–teste aberto |
| Perfil principal | Engenharia Flutter |
| Plataforma | Flutter Android |
| Status | Planejada |

## 2. História do usuário

Como **engenharia Flutter**,
quero **componentizar o frontend e reforçar camadas simples**,
para **reduzir duplicação, facilitar manutenção e manter o design system AWAKEN consistente**.

## 3. Objetivo

Aplicar Clean Architecture lite, DRY e KISS sem superengenharia, extraindo componentes comuns e padronizando responsabilidades.

## 4. Escopo

### Entra nesta US

- Extrair componentes repetidos.
- Padronizar cards, botões, barras e estados.
- Separar data/domain/presentation quando fizer sentido.
- Centralizar formatação de data, moeda e XP.
- Enforçar design system dark/RPG.

### Fora desta US

- Reescrita total do app.
- Arquitetura complexa desnecessária.
- Mudança visual fora do padrão AWAKEN.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Componentes repetidos devem ser extraídos para design system. |
| RN-002 | Regra de negócio não deve ficar escondida em widget. |
| RN-003 | Formatação de moeda/data deve ficar centralizada. |
| RN-004 | Componentização não deve alterar comportamento do usuário. |
| RN-005 | KISS prevalece sobre abstração excessiva. |

## 6. Impacto Flutter

- Criar/organizar design system.
- Revisar telas com cards semelhantes.
- Extrair widgets reutilizáveis.
- Padronizar loading, empty, error e blocked states.
- Ajustar imports/camadas.

## 7. Impacto QA

- Regressão visual.
- Regressão de navegação.
- Verificar responsividade Android.
- Verificar textos PT-BR, EN e ES.

## 8. Critérios de aceite

### CA-001 — Componentes reutilizados

Dado que duas ou mais telas usam o mesmo padrão visual,
quando a refatoração for aplicada,
então devem usar componente compartilhado.

### CA-002 — Sem mudança funcional

Dado que uma tela foi componentizada,
quando QA validar o fluxo,
então o comportamento deve permanecer equivalente.

## 9. Decisão registrada

> A componentização deve aumentar qualidade e consistência sem transformar o MVP em arquitetura pesada.
