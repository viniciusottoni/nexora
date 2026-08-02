---
title: EPIC-002 — Autenticação e Conta do Usuário
sidebar_position: 2
---

# EPIC-002 — Autenticação e Conta do Usuário

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | EPIC-002 |
| Fase | MVP Android Fitness Gamificado |
| Prioridade | P0 |
| Perfil principal | Visitante e usuário autenticado |
| Planos impactados | Trial, Mensal e Anual |
| Status | Planejado |

## 2. Objetivo

Permitir que o usuário crie conta, entre no AWAKEN, mantenha acesso seguro e gerencie ações básicas da conta. A conta é necessária para iniciar trial, salvar progresso, controlar assinatura e manter consistência entre dispositivos.

## 3. Contexto de produto

Como o modelo comercial depende de um teste gratuito de 7 dias e assinatura obrigatória após expiração, a autenticação precisa ser confiável. O usuário não pode perder progresso e o sistema precisa reconhecer corretamente seu status de acesso.

## 4. Escopo

### Entra neste épico

- Cadastro com e-mail e senha.
- Login com e-mail e senha.
- Login com Google.
- Sessão persistente segura.
- Logout.
- Recuperação de senha.
- Exclusão de conta.

### Fora deste épico

- Login Apple, salvo preparação futura para iOS.
- MFA.
- Painel administrativo de usuários.
- Fluxo avançado de suporte.

## 5. User Stories relacionadas

| ID | Título | Prioridade | Documento |
|---|---|---|---|
| US-007 | Criar conta com e-mail e senha | P0 | [Abrir](./US-007-criar-conta-email-senha.md) |
| US-008 | Entrar com e-mail e senha | P0 | [Abrir](./US-008-login-email-senha.md) |
| US-009 | Entrar com Google | P0 | [Abrir](./US-009-login-google.md) |
| US-010 | Manter sessão ativa com segurança | P0 | [Abrir](./US-010-manter-sessao-ativa-seguranca.md) |
| US-011 | Sair da conta | P0 | [Abrir](./US-011-sair-da-conta.md) |
| US-012 | Recuperar senha | P1 | [Abrir](./US-012-recuperar-senha.md) |
| US-013 | Excluir conta | P1 | [Abrir](./US-013-excluir-conta.md) |

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-EPIC-002-001 | Todo usuário precisa estar autenticado para iniciar trial, onboarding e quests. |
| RN-EPIC-002-002 | O progresso deve estar vinculado ao usuário autenticado. |
| RN-EPIC-002-003 | Logout remove acesso local, mas não apaga dados remotos. |
| RN-EPIC-002-004 | Exclusão de conta deve respeitar LGPD. |
| RN-EPIC-002-005 | Erros de autenticação devem ser claros e localizados. |

## 7. Impactos técnicos

### Flutter

- Telas de cadastro, login, recuperação de senha, logout e exclusão.
- Validação de campos.
- Integração com Google Sign-In.
- Guards de rota para visitante, trial ativo, assinatura ativa e acesso bloqueado.

### Backend

- Endpoints de cadastro, login, recuperação e exclusão.
- Integração com provedor Google.
- Regras para vincular usuário ao trial e assinatura.
- Logs de ações sensíveis.

### Banco de dados

Entidades principais:

- User.
- AuthProvider.
- Subscription.
- AccessStatus.

### Analytics

- `login_started`.
- `login_completed`.
- `access_blocked`.

### QA

- Cadastro válido e inválido.
- Login válido e inválido.
- Login Google.
- Sessão persistente.
- Logout.
- Recuperação de senha.
- Exclusão de conta.
- Tentativa de acesso a rotas protegidas sem login.

## 8. Dependências

- EPIC-001 para navegação e estados base.
- EPIC-003 para início de trial e controle de acesso após autenticação.

## 9. Critérios de aceite do épico

- Usuário consegue criar conta.
- Usuário consegue entrar e sair.
- Sessão permanece ativa.
- Rotas protegidas exigem autenticação.
- Conta autenticada pode seguir para o fluxo de trial.
- Erros são claros e localizados.

## 10. Decisão registrada

Autenticação é pré-requisito para qualquer uso real do AWAKEN, pois o sistema precisa salvar progresso, controlar trial, reconhecer assinatura e aplicar o bloqueio de acesso quando necessário.
