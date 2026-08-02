---
title: US-105 — Proteger sessão do usuário
sidebar_position: 105
---

# US-105 — Proteger sessão do usuário

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-105 |
| Épico | EPIC-015 — Segurança, Privacidade e LGPD |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário autenticado |
| Planos impactados | Trial, Mensal e Anual |
| Dependência principal | EPIC-002 — Autenticação e Conta |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário autenticado**,

quero **ter minha sessão protegida**,

para **usar o AWAKEN sem que meus dados e progresso fiquem expostos indevidamente**.

---

## 3. Contexto

O AWAKEN armazena progresso, perfil físico, limitações e assinatura. A sessão precisa ser persistente o suficiente para boa experiência, mas segura contra uso indevido.

---

## 4. Objetivo

Garantir armazenamento seguro de tokens, renovação controlada de sessão, logout efetivo e bloqueio de acesso quando credenciais forem inválidas.

---

## 5. Escopo

### Entra nesta US

- Armazenar tokens em storage seguro no app.
- Renovar sessão quando permitido.
- Invalidar sessão no logout.
- Bloquear acesso quando token expirar ou for inválido.
- Evitar exposição de tokens em logs.
- Tratar sessão expirada com mensagem clara.

### Fora desta US

- MFA no MVP.
- Gestão avançada de dispositivos.
- Detecção antifraude complexa.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Tokens não devem ser armazenados em texto puro. |
| RN-002 | Logout deve remover tokens locais. |
| RN-003 | API deve rejeitar token expirado ou inválido. |
| RN-004 | Sessão expirada deve direcionar usuário para login. |
| RN-005 | Logs não devem conter token, refresh token ou credenciais. |
| RN-006 | Rotas protegidas exigem usuário autenticado. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Sem sessão autenticada. |
| Usuário autenticado | Pode manter sessão conforme validade. |
| Trial expirado | Pode manter login, mas acesso funcional segue EPIC-003. |
| Assinatura expirada | Pode manter login, mas acesso funcional segue EPIC-003. |

---

## 8. Fluxo principal

1. Usuário faz login.
2. App recebe tokens de sessão.
3. App armazena tokens em storage seguro.
4. App usa token nas chamadas protegidas.
5. Backend valida token em cada rota protegida.
6. Se token expirar, app tenta renovar ou envia usuário para login.

---

## 9. Fluxos alternativos

### 9.1. Token expirado

App tenta renovar. Se não for possível, limpa sessão e direciona para login.

### 9.2. Logout

App remove tokens locais e backend pode invalidar refresh token quando aplicável.

### 9.3. Erro 401

App deve exibir estado de sessão expirada, não erro genérico assustador.

---

## 10. Estados esperados

- autenticado;
- renovando sessão;
- sessão expirada;
- logout em andamento;
- logout concluído;
- acesso negado.

---

## 11. Impacto Flutter

- Secure storage para tokens.
- Interceptor HTTP para token/refresh.
- Guard de rotas protegidas.
- Limpeza de sessão no logout.
- Tratamento padronizado de 401.

---

## 12. Impacto Backend

- Validação de JWT.
- Expiração de tokens.
- Refresh token quando definido.
- Rejeição de credenciais inválidas.
- Logs sem credenciais.

---

## 13. Impacto DB

Entidades/campos possíveis:

- UserSession;
- refreshTokenHash;
- revokedAt;
- lastUsedAt.

---

## 14. Impacto Gamificação

- Protege progresso e perfil Hunter.
- Não concede XP.

---

## 15. Impacto Monetização

- Usuário com sessão válida ainda pode ser bloqueado por acesso expirado conforme EPIC-003.
- Sessão autenticada não significa assinatura ativa.

---

## 16. Contratos API sugeridos

```txt
POST /api/auth/refresh
POST /api/auth/logout
```

---

## 17. Eventos Analytics

| Evento | Quando dispara |
|---|---|
| session_expired | Quando sessão expira. |
| logout_completed | Quando logout conclui. |

Eventos não devem conter tokens.

---

## 18. Critérios de aceite

### CA-001 — Token seguro

Dado que o usuário fez login,
Quando o app persistir sessão,
Então tokens devem ser armazenados em storage seguro.

### CA-002 — Logout limpa sessão

Dado que o usuário toca em sair,
Quando logout concluir,
Então tokens locais devem ser removidos e rotas protegidas bloqueadas.

---

## 19. Critérios de teste QA

- login e sessão persistida;
- refresh de sessão;
- token expirado;
- 401 em rota protegida;
- logout;
- logs sem token;
- textos PT-BR, EN e ES.

---

## 20. Decisão registrada

> Sessão autenticada deve ser segura e separada do status comercial; login válido não libera funcionalidades se trial ou assinatura estiverem expirados.
