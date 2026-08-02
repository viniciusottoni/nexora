---
title: EPIC-001 — Fundação Mobile e Experiência Base
sidebar_position: 1
---

# EPIC-001 — Fundação Mobile e Experiência Base

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | EPIC-001 |
| Fase | MVP Android Fitness Gamificado |
| Prioridade | P0 |
| Perfil principal | Visitante e usuário com acesso ativo |
| Planos impactados | Trial, Mensal e Anual |
| Plataformas | Android primeiro; iOS futuro |
| Status | Planejado |

## 2. Objetivo

Preparar a base do aplicativo Flutter para que o AWAKEN tenha uma experiência mobile estável, visualmente imersiva, navegável, internacionalizada e pronta para receber os fluxos de autenticação, trial, onboarding, quests, gamificação e assinatura.

## 3. Contexto de produto

O AWAKEN precisa transmitir desde o primeiro contato a sensação de um sistema de evolução pessoal com estética dark, épica e inspirada em anime, sem sacrificar clareza, performance ou acessibilidade. Este épico cria a fundação visual e estrutural do app.

## 4. Escopo

### Entra neste épico

- Splash screen inicial com identidade AWAKEN.
- Estrutura base de rotas e navegação.
- Tema dark e tokens visuais iniciais.
- Estados globais de tela: carregando, vazio, erro, sucesso e bloqueado.
- Internacionalização preparada para PT-BR, EN e ES.
- Compatibilidade mínima Android definida para o MVP.
- Base para animações leves de transição e feedback visual.

### Fora deste épico

- Login e cadastro.
- Onboarding.
- Assinatura e RevenueCat.
- Geração de treino.
- Sistema completo de gamificação.
- Design system final avançado.

## 5. User Stories relacionadas

| ID | Título | Prioridade | Documento |
|---|---|---|---|
| US-001 | Visualizar splash screen com identidade AWAKEN | P0 | [Abrir](./US-001-splash-screen-identidade-awaken.md) |
| US-002 | Navegar por estrutura base de telas | P0 | [Abrir](./US-002-estrutura-base-navegacao.md) |
| US-003 | Usar interface dark, legível e imersiva | P0 | [Abrir](./US-003-interface-dark-legivel-imersiva.md) |
| US-004 | Usar app em PT-BR com estrutura para EN e ES | P0 | [Abrir](./US-004-internacionalizacao-ptbr-en-es.md) |
| US-005 | Ver estados de carregamento, erro, vazio e sucesso | P0 | [Abrir](./US-005-estados-carregamento-erro-vazio-sucesso.md) |
| US-006 | Ter experiência estável em celulares Android mínimos | P0 | [Abrir](./US-006-estabilidade-android-minimo.md) |

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-EPIC-001-001 | O app deve iniciar em PT-BR quando não houver preferência salva. |
| RN-EPIC-001-002 | Nenhuma tela P0 deve depender de texto hardcoded fora da estrutura de internacionalização. |
| RN-EPIC-001-003 | O app deve apresentar estados de erro compreensíveis para o usuário final. |
| RN-EPIC-001-004 | A splash deve carregar rapidamente e não pode bloquear o usuário além do necessário. |
| RN-EPIC-001-005 | O tema visual deve ser dark, com contraste suficiente para leitura. |

## 7. Impactos técnicos

### Flutter

- Configuração inicial do projeto Flutter.
- Rotas base com go_router.
- Tema global, tokens de cor, tipografia e espaçamentos.
- Estrutura de internacionalização com arquivos ARB.
- Componentes comuns de estado: loading, empty, error, success e blocked.
- Base de responsividade para diferentes tamanhos de tela Android.

### Backend

- Sem endpoint obrigatório neste épico.
- Pode exigir endpoint futuro de health check ou remote config, se decidido.

### Banco de dados

- Sem entidade própria obrigatória.

### Analytics

- `app_opened`.
- `splash_viewed`.

### QA

- Testar abertura do app.
- Testar renderização em diferentes tamanhos de tela.
- Testar troca ou fallback de idioma.
- Testar estados comuns.
- Testar performance inicial sem travamentos perceptíveis.

## 8. Dependências

- Definição inicial da identidade visual.
- Configuração do projeto Flutter.
- Decisão de idiomas suportados no MVP.

## 9. Critérios de aceite do épico

- O app abre sem crash em dispositivo Android mínimo.
- A splash aparece com identidade AWAKEN.
- A navegação base permite avançar para os fluxos seguintes.
- O tema dark está aplicado de forma consistente.
- PT-BR funciona como idioma padrão.
- Estrutura para EN e ES está preparada.
- Estados comuns de UI estão disponíveis para reuso.

## 10. Decisão registrada

Este épico é a fundação visual, técnica e navegacional do aplicativo. Nenhuma funcionalidade de negócio crítica deve ser construída antes da base mínima de navegação, tema, estados e internacionalização estar funcional.
