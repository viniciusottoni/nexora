---
title: US-159 — Login de admin com senha complexa
sidebar_position: 159
---

# US-159 — Login de admin com senha complexa

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-159 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Admin e Segurança |
| Plataforma | Web Admin (React) + Backend .NET |
| Dependência | US-158, EPIC-002, EPIC-015, EPIC-018 US-180/US-181 |
| Status | Planejada |

## 2. História do usuário

Como **admin do AWAKEN**, quero **entrar no site admin usando credenciais fortes**, para **impedir acesso indevido a dados operacionais e sensíveis**.

## 3. Objetivo

Implementar autenticação administrativa separada do usuário comum, exigindo senha complexa, bloqueio de tentativas abusivas e sessão segura.

## 4. Escopo

### Entra nesta US

- Tela de login administrativo.
- Validação de senha complexa para criação/alteração de senha admin.
- Bloqueio de senhas fracas, comuns ou reutilizadas quando houver histórico.
- Endpoint de login admin separado do login do app.
- Rate limit e bloqueio temporário por tentativas inválidas.
- Registro de sucesso e falha de login em auditoria segura.
- Sessão curta, renovável de forma controlada.

### Fora desta US

- MFA obrigatório, tratado na US-160.
- Recuperação pública de senha.
- Gestão completa de usuários administradores.
- SSO corporativo.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Apenas identidade administrativa pode autenticar no site admin. |
| RN-002 | Senha admin deve cumprir política de complexidade vigente. |
| RN-003 | Senha fraca, vazada ou reutilizada deve ser recusada quando detectável. |
| RN-004 | Tentativas inválidas repetidas geram bloqueio temporário e auditoria. |
| RN-005 | Falhas de login não devem revelar se email, senha ou perfil está incorreto. |
| RN-006 | Login bem-sucedido ainda depende de MFA válido quando a US-160 estiver ativa. |

## 6. Fluxo principal

1. Admin informa email e senha.
2. Backend valida credenciais e perfil administrativo.
3. Backend aplica rate limit e política de bloqueio.
4. Credenciais válidas avançam para MFA ou criam sessão conforme configuração.
5. Evento de login é registrado com metadados sanitizados.

## 7. Impacto Frontend React

- Tela de login com estados de envio, erro genérico e bloqueio temporário.
- Tratamento de sessão expirada.
- Redirecionamento para MFA quando exigido.

## 8. Impacto Backend

- Serviço de autenticação admin.
- Hash seguro de senha.
- Política de senha complexa.
- Rate limiting específico para auth admin.
- AuditLog para login, falha e bloqueio.

## 9. Impacto DB

- Entidade/registro de admin.
- Campos de hash de senha, status, tentativas e bloqueio temporário.
- Histórico mínimo para evitar reutilização quando aplicável.

## 10. Critérios de aceite

### CA-001 — Login admin válido

Dado que o admin informa credenciais corretas,
quando fizer login,
então o backend deve reconhecer o perfil administrativo e prosseguir com a autenticação segura.

### CA-002 — Usuário comum bloqueado

Dado que um usuário comum possui conta no app,
quando tentar entrar no site admin,
então o acesso deve ser negado com mensagem genérica.

### CA-003 — Tentativas abusivas bloqueadas

Dado que há múltiplas falhas de login,
quando o limite for excedido,
então novas tentativas devem ser bloqueadas temporariamente e auditadas.

## 11. Decisão registrada

> A autenticação admin é separada da conta de jogador e exige senha forte, bloqueio de abuso e auditoria desde o MVP.
