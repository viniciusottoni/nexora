---
title: US-199 — Controle de volume e proteção de autenticação
sidebar_position: 199
---

# US-199 — Controle de volume e proteção de autenticação

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-199 |
| Épico | EPIC-020 — Hardening de Segurança e Fechamento de Vulnerabilidades |
| Prioridade | P0 |
| Fase | Bloqueador pré-produção / pré-teste aberto |
| Perfil principal | Visitante, usuário autenticado, backend e segurança |
| Plano | Todos |
| Idiomas impactados | PT-BR / EN / ES / FR |
| Dependência principal | Autenticação, sessão e limitação de chamadas |
| Status | Planejada |

## 2. História do usuário

Como **usuário legítimo do AWAKEN**,

quero **que chamadas excessivas de autenticação sejam controladas sem afetar todo o sistema**,

para **manter minha conta protegida e o app disponível**.

## 3. Contexto

A proteção atual precisa separar limites por origem e por identificador funcional. Um controle único pode prejudicar usuários legítimos. A renovação de sessão também precisa de controle próprio.

## 4. Objetivo

Implantar controle de volume por origem/usuário, bloqueio temporário em caso de falhas repetidas e proteção da renovação de sessão.

## 5. Escopo

### Entra nesta US

- Separar limites por origem e e-mail normalizado.
- Proteger login, registro, recuperação de senha, login social e renovação de sessão.
- Aplicar bloqueio temporário por conta após falhas consecutivas.
- Retornar mensagens genéricas e localizadas.
- Criar testes de isolamento entre usuários e origens.

### Fora desta US

- CAPTCHA.
- MFA.
- Antifraude avançado.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O controle deve ser particionado por origem e identificador funcional. |
| RN-002 | Excesso de chamadas de uma origem não pode bloquear todo o sistema. |
| RN-003 | Falhas repetidas devem gerar bloqueio temporário da conta. |
| RN-004 | Renovação de sessão deve ter controle específico. |
| RN-005 | Mensagens não devem confirmar existência de conta. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Pode autenticar dentro dos limites. |
| Usuário autenticado | Pode renovar sessão dentro dos limites. |
| Conta bloqueada temporariamente | Deve aguardar o período definido. |
| Admin interno | Também sujeito às proteções. |

## 8. Fluxo principal

1. Usuário realiza ação de autenticação.
2. Backend identifica origem e identificador funcional.
3. Backend valida o limite aplicável.
4. Se permitido, processa a ação.
5. Falhas consecutivas atualizam controle temporário.
6. Sucesso limpa controles relevantes.

## 9. Fluxos alternativos

- Limite excedido: retorna erro 429 com mensagem genérica.
- Conta temporariamente bloqueada: retorna mensagem genérica.
- Sessão inválida: app limpa sessão e solicita novo login.

## 10. Estados esperados

- ação permitida;
- limite excedido;
- conta temporariamente bloqueada;
- sessão renovada;
- sessão inválida;
- erro inesperado com correlationId.

## 11. Impacto no Frontend Flutter

- Exibir mensagem amigável de muitas tentativas.
- Não repetir chamadas automaticamente em loop.
- Encerrar sessão quando backend indicar sessão inválida.

## 12. Impacto no Backend

- Ajustar política de limitação de chamadas.
- Proteger renovação de sessão.
- Persistir ou cachear controles temporários.
- Criar testes de integração.

## 13. Impacto no Banco de Dados

Pode usar cache Redis ou campos de controle temporário por usuário.

## 14. Impacto em Gamificação

Sem impacto direto.

## 15. Impacto em Monetização

Protege contas pagantes e reduz indisponibilidade por excesso de chamadas.

## 16. Impacto em Internacionalização

Mensagens de limite e bloqueio temporário devem existir em PT-BR, EN, ES e FR.

## 17. Contrato de API sugerido

Impacta endpoints de autenticação, recuperação de senha, login social e renovação de sessão.

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| auth_limited | Chamada bloqueada por limite. |
| auth_temporary_lock_started | Conta entra em bloqueio temporário. |
| session_invalidated | Sessão deixa de ser aceita. |

## 19. Critérios de aceite

- Exceder limite para um e-mail não bloqueia e-mails diferentes.
- Exceder limite em uma origem não derruba todo o sistema.
- Renovação de sessão tem proteção própria.
- Conta com falhas consecutivas entra em bloqueio temporário.
- Mensagens não revelam existência de conta.

## 20. Critérios de teste para QA

- chamadas repetidas para o mesmo e-mail;
- chamadas para e-mails diferentes;
- origens diferentes;
- renovação de sessão repetida;
- bloqueio temporário e expiração;
- mensagens localizadas.

## ✅ Decisão registrada

Autenticação e sessão devem ter controle particionado e bloqueio temporário, sem controle global que prejudique usuários legítimos.