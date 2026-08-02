---
title: US-158 — Criar base do site admin em React
sidebar_position: 158
---

# US-158 — Criar base do site admin em React

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-158 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Admin, Engenharia e Produto |
| Plataforma | Web Admin (React) |
| Dependência | EPIC-002, EPIC-014, EPIC-015, EPIC-018 US-180 |
| Status | Planejada |

## 2. História do usuário

Como **admin do AWAKEN**, quero **acessar uma base web administrativa segura e organizada**, para **operar usuários, suporte, segurança e indicadores antes do release Android**.

## 3. Objetivo

Criar o app React do site admin com shell autenticado, rotas protegidas, layout operacional e estados básicos para sustentar as telas do EPIC-017.

## 4. Escopo

### Entra nesta US

- Novo app web admin em React.
- Shell com navegação lateral, barra superior, área de conteúdo e identificação do admin logado.
- Roteamento protegido para dashboard, usuários, tickets, bugs, segurança, audit log, eventos, engajamento e relatórios.
- Componentes base de tabela, filtros, cards, chips de status, loading, erro e vazio.
- Layout desktop-first com suporte emergencial a telas menores.
- Configuração de ambiente para consumir APIs internas.

### Fora desta US

- Implementar métricas reais de cada tela.
- Login completo, MFA e gestão de sessão.
- Permissões granulares por módulo.
- Site público ou marketing.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O site admin não pode ser acessível como experiência pública do usuário final. |
| RN-002 | Toda rota administrativa deve estar preparada para exigir autenticação e perfil admin. |
| RN-003 | Navegação deve refletir apenas módulos previstos no EPIC-017. |
| RN-004 | Estados de loading, erro e vazio devem ser explícitos em telas de dados. |
| RN-005 | O app deve consumir configuração por ambiente, sem endpoint hardcoded de produção. |

## 6. Fluxo principal

1. Admin acessa a URL interna do site admin.
2. Sistema valida se existe sessão administrativa.
3. Sem sessão, admin é direcionado ao login.
4. Com sessão válida, admin vê o shell e a rota inicial do dashboard.
5. Admin navega entre módulos sem perder contexto de autenticação.

## 7. Impacto Frontend React

- Estrutura de projeto do admin.
- Roteador, layout base e proteção de rotas.
- Design system mínimo para tabelas, filtros e indicadores.
- Cliente HTTP com interceptação de erro e correlationId quando disponível.

## 8. Impacto Backend

- Nenhum endpoint de domínio obrigatório nesta US.
- Contrato esperado para health/configuração do admin, se necessário.

## 9. Critérios de aceite

### CA-001 — Shell criado

Dado que o admin tem sessão válida,
quando acessar o site admin,
então deve visualizar navegação, barra superior e área de conteúdo.

### CA-002 — Rota protegida

Dado que não existe sessão administrativa,
quando acessar uma rota interna,
então o usuário deve ser redirecionado ao login.

### CA-003 — Estados básicos

Dado que uma tela de dados ainda não tem conteúdo,
quando for renderizada,
então deve exibir estado vazio ou loading sem quebrar a navegação.

## 10. Decisão registrada

> O site admin nasce como ferramenta operacional interna em React, com shell seguro e preparado para todas as visões críticas do EPIC-017.
