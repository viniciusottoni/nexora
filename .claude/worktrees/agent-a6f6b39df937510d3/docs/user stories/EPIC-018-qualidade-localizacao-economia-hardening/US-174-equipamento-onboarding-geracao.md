---
title: US-174 — Equipamento disponível no onboarding alimentando geração
sidebar_position: 174
---

# US-174 — Equipamento disponível no onboarding alimentando geração

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-174 |
| Épico | EPIC-018 — Qualidade, Localização, Economia e Hardening Pós-MVP |
| Prioridade | P0 |
| Fase | Endurecimento pré–teste aberto |
| Perfil principal | Usuário em Trial ou assinante |
| Dependências | EPIC-004, EPIC-005, EPIC-006 |
| Status | Planejada |

## 2. História do usuário

Como **usuário em onboarding**,
quero **informar quais equipamentos tenho disponíveis**,
para **receber quests compatíveis com minha realidade de treino**.

## 3. Contexto

O README do EPIC-018 indica que já existem chaves l10n e DTO `equipmentAvailable`, mas faltam tela e US. Esta história liga o dado ao onboarding e à geração de treino.

## 4. Objetivo

Adicionar etapa/configuração de equipamento disponível e fazer a geração respeitar peso corporal, pesos livres e máquinas.

## 5. Escopo

### Entra nesta US

- Capturar equipamento disponível no onboarding ou configuração pós-onboarding.
- Opções iniciais: peso corporal, pesos livres e máquinas.
- Persistir `equipmentAvailable` no perfil.
- Usar equipamentos na geração de quest.
- Filtrar catálogo conforme equipamento disponível.

### Fora desta US

- Catálogo profissional completo de academia.
- Detecção automática de equipamento.
- Recomendação de compra de equipamento.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A geração de treino deve respeitar equipamentos informados. |
| RN-002 | Peso corporal deve existir como fallback mínimo. |
| RN-003 | Exercícios que exigem equipamento indisponível não devem entrar na quest. |
| RN-004 | Usuário deve poder revisar equipamento em configuração futura. |
| RN-005 | DTO e l10n órfãos devem ser ligados à tela/fluxo real. |

## 7. Fluxo principal

1. Usuário informa equipamentos disponíveis.
2. App salva preferência no perfil.
3. Backend recebe `equipmentAvailable`.
4. Gerador de quest filtra exercícios compatíveis.
5. Quest retorna sem exercícios incompatíveis.

## 8. Impacto Flutter

- Tela/step de equipamento.
- Chips ou cards de seleção múltipla.
- i18n para opções.
- Revisão/edição futura em configurações.

## 9. Impacto Backend

- Persistir equipamento no perfil.
- Usar equipamento como filtro do catálogo.
- Validar enum/lista permitida.

## 10. Impacto DB

- Campo `equipmentAvailable` ou tabela relacional equivalente.
- Indexação se necessário para geração/consulta.

## 11. Critérios de aceite

### CA-001 — Equipamento salvo

Dado que o usuário escolhe peso corporal e pesos livres,
quando concluir a etapa,
então o perfil deve armazenar esses equipamentos.

### CA-002 — Geração compatível

Dado que o usuário não possui máquinas,
quando gerar quest,
então exercícios de máquinas não devem ser retornados.

## 12. Decisão registrada

> Equipamento disponível deixa de ser recurso órfão e passa a alimentar diretamente a geração de treino.
