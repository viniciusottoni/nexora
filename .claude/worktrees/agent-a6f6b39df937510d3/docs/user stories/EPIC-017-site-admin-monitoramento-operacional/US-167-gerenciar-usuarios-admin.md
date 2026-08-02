---
title: US-167 — Gerenciar usuários do admin com busca, filtros e exportação CSV
sidebar_position: 167
---

# US-167 — Gerenciar usuários do admin com busca, filtros e exportação CSV

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-167 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Suporte, Produto e Admin |
| Plataforma | Web Admin (React) |
| Dependência | US-158, US-166, EPIC-002, EPIC-003, EPIC-015 |
| Status | Planejada |

## 2. História do usuário

Como **suporte e produto**, quero **consultar usuários no site admin com busca, filtros e exportação segura**, para **investigar contas, entender base ativa e apoiar atendimento sem acessar dados desnecessários**.

## 3. Objetivo

Criar visão administrativa de usuários finais com busca, filtros por plano/status, detalhe seguro e exportação CSV minimizada.

## 4. Escopo

### Entra nesta US

- Listagem de usuários finais.
- Busca por nome, email mascarado/parcial, userId ou correlationId quando aplicável.
- Filtros por plano, status de acesso, trial, assinatura, atividade e data de criação.
- Detalhe seguro com dados de conta, status comercial e atividade resumida.
- Exportação CSV com campos permitidos.
- Auditoria de exportação e consultas sensíveis.

### Fora desta US

- Edição ampla de dados do usuário.
- Alteração manual de assinatura.
- Reset de senha de usuário final.
- Exportação de dados sensíveis completos.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A listagem deve minimizar dados pessoais. |
| RN-002 | Exportação CSV não pode incluir senha, token, dados físicos detalhados ou limitações. |
| RN-003 | Exportação deve ser auditada. |
| RN-004 | Filtros por plano e status devem refletir a regra comercial vigente. |
| RN-005 | Usuário comum não pode acessar a visão administrativa. |

## 6. Fluxo principal

1. Admin acessa a tela de usuários.
2. Sistema lista usuários com dados minimizados.
3. Admin aplica busca ou filtros.
4. Admin abre detalhe seguro quando necessário.
5. Admin exporta CSV autorizado, gerando auditoria.

## 7. Impacto Frontend React

- Página de usuários com tabela, busca, filtros e exportação.
- Detalhe lateral ou página de detalhe.
- Indicação clara de plano e status de acesso.

## 8. Impacto Backend

- Endpoint admin paginado de usuários.
- Endpoint de detalhe seguro.
- Endpoint de exportação CSV com auditoria.
- Projeções sem dados sensíveis.

## 9. Critérios de aceite

### CA-001 — Filtros por plano e status

Dado que há usuários em trial, mensal, anual e expirado,
quando admin filtrar por plano/status,
então a lista deve retornar apenas usuários correspondentes.

### CA-002 — CSV seguro

Dado que admin exporta usuários,
quando o CSV for gerado,
então não deve conter dados sensíveis proibidos e a exportação deve ser auditada.

### CA-003 — Detalhe minimizado

Dado que admin abre o detalhe de um usuário,
quando a tela carregar,
então deve exibir apenas dados necessários para suporte e operação.

## 10. Decisão registrada

> A visão de usuários no admin é operacional e minimizada: permite busca, filtro e exportação segura sem virar acesso irrestrito a dados pessoais.
