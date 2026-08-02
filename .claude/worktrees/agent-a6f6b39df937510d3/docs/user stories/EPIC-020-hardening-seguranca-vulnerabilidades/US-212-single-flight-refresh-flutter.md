---
title: US-212 — Implementar single-flight refresh no Flutter
sidebar_position: 212
---

# US-212 — Implementar single-flight refresh no Flutter

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-212 |
| Épico | EPIC-020 — Hardening de Segurança, Performance e Escalabilidade do MVP |
| Prioridade | P0 |
| Fase | Bloqueador do MVP em produção |
| Perfil principal | Usuário autenticado, Flutter e backend auth |
| Plano | Todos |
| Dependência principal | AuthInterceptor, SecureTokenStorage, AuthRemoteDataSource |
| Status | Planejada |

## 2. História do usuário

Como **usuário autenticado do AWAKEN**,

quero **que o app renove minha sessão de forma silenciosa e controlada**,

para **não ser deslogado nem gerar várias chamadas simultâneas quando o token expirar**.

## 3. Contexto

Quando várias requisições recebem 401 ao mesmo tempo, o interceptor pode tentar renovar a sessão várias vezes em paralelo. Isso aumenta carga no backend e pode causar conflitos de token. O app deve centralizar a renovação em uma única operação compartilhada.

## 4. Objetivo

Implementar padrão single-flight para refresh de sessão no Flutter, fazendo múltiplas requisições aguardarem a mesma renovação em andamento.

## 5. Escopo

### Entra nesta US

- Criar lock/future compartilhado para refresh em andamento.
- Garantir que apenas uma chamada de refresh rode por vez.
- Fazer requests concorrentes aguardarem o resultado.
- Repetir requests originais com novo access token.
- Limpar sessão se refresh falhar.
- Criar testes unitários do interceptor.

### Fora desta US

- Mudança no formato dos tokens.
- MFA.
- Login offline.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Várias respostas 401 simultâneas devem gerar no máximo uma chamada de refresh. |
| RN-002 | Requests pendentes devem aguardar a renovação em andamento. |
| RN-003 | Falha no refresh deve limpar sessão uma única vez. |
| RN-004 | Request de refresh não pode entrar em loop. |
| RN-005 | Token novo deve ser salvo antes de repetir requests. |

## 7. Fluxo principal

1. Request recebe 401.
2. Interceptor verifica se já existe refresh em andamento.
3. Se não existir, inicia refresh e guarda o Future.
4. Se existir, aguarda o Future atual.
5. Ao concluir, salva tokens e repete request original.
6. Ao falhar, limpa sessão e sinaliza expiração.

## 8. Impacto no Flutter

- Ajustar `AuthInterceptor`.
- Adicionar controle assíncrono para refresh.
- Criar testes com múltiplas requests simultâneas.
- Garantir que analytics de sessão expirada não duplique eventos excessivos.

## 9. Impacto no Backend

- Menos chamadas simultâneas ao endpoint de refresh.
- Menor risco de rate limit indevido por rajada do mesmo usuário.

## 10. Critérios de aceite

- Dez requests simultâneas com 401 geram uma única chamada de refresh.
- Todas as requests aguardam e são repetidas com token novo.
- Falha no refresh limpa sessão e não entra em loop.
- Refresh endpoint em si não tenta novo refresh.
- Testes automatizados cobrem concorrência.

## 11. Critérios de teste para QA

- expiração de token com uma request;
- expiração com várias requests simultâneas;
- refresh bem-sucedido;
- refresh inválido;
- app em rede instável;
- retorno à tela de login quando necessário.

## ✅ Decisão registrada

O app Flutter deve usar single-flight refresh para evitar rajadas simultâneas de renovação e reduzir carga desnecessária no backend.