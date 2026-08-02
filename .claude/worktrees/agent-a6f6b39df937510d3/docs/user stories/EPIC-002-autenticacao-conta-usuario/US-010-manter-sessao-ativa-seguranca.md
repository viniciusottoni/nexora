---
title: US-010 — Manter sessão ativa com segurança
sidebar_position: 10
---

# US-010 — Manter sessão ativa com segurança

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-010 |
| Épico | EPIC-002 — Autenticação e Conta do Usuário |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário autenticado |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Sessão autenticada e status de acesso |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário autenticado**,

quero **manter minha sessão ativa com segurança**,

para **não precisar fazer login toda vez que abrir o AWAKEN e continuar minha jornada com fluidez**.

---

## 3. Contexto

O usuário deve voltar ao app rapidamente para manter hábito, streak e trial ativo. Ao mesmo tempo, a sessão precisa respeitar expiração, logout, troca de acesso e bloqueio comercial.

---

## 4. Objetivo

Manter a sessão do usuário de forma segura e redirecionar para a rota correta ao abrir o app.

---

## 5. Escopo

### Entra nesta US

- Persistência local da sessão.
- Verificação de sessão ao abrir o app.
- Renovação ou invalidação conforme regra técnica definida.
- Redirecionamento por status de acesso.
- Tratamento de sessão expirada.
- Limpeza local no logout.

### Fora desta US

- MFA.
- Gestão avançada de dispositivos.
- Painel de sessões ativas.
- Segurança corporativa avançada.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Sessão válida deve permitir entrada sem novo login. |
| RN-002 | Sessão inválida deve levar o usuário para login. |
| RN-003 | Sessão válida não deve liberar recurso protegido se o trial ou assinatura estiver expirado. |
| RN-004 | Logout deve limpar dados locais de sessão. |
| RN-005 | Falha ao validar sessão deve exibir erro controlado. |
| RN-006 | O app deve proteger dados locais sensíveis conforme prática recomendada da plataforma. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não possui sessão. |
| Usuário em Trial | Pode manter sessão. |
| Premium Mensal | Pode manter sessão. |
| Premium Anual | Pode manter sessão. |
| Trial expirado | Pode manter sessão, mas com acesso bloqueado. |
| Assinatura expirada | Pode manter sessão, mas com acesso bloqueado. |
| Admin interno | Não aplicável no app mobile do MVP. |
| Suporte interno | Não aplicável no app mobile do MVP. |

---

## 8. Fluxo principal

1. Usuário abre o app.
2. App verifica se existe sessão local.
3. App valida se a sessão ainda pode ser usada.
4. App identifica status de acesso.
5. Usuário é direcionado para onboarding, home ou paywall.

---

## 9. Fluxos alternativos

### 9.1. Sessão ausente

O app deve seguir para rota pública de entrada.

### 9.2. Sessão inválida

O app deve limpar a sessão local e direcionar para login.

### 9.3. Acesso expirado

O app mantém a conta identificada, mas direciona para paywall ou tela bloqueada.

---

## 10. Estados de tela ou estados esperados

- verificando sessão;
- sessão válida;
- sessão inválida;
- acesso ativo;
- acesso bloqueado;
- erro de conexão;
- erro inesperado.

---

## 11. Impacto no Frontend Flutter

- Guard de inicialização.
- Armazenamento seguro de dados de sessão.
- Redirecionamento por status.
- Estado de carregamento inicial.
- Limpeza local no logout.

---

## 12. Impacto no Backend

- Endpoint ou mecanismo de validação de sessão.
- Retorno de status de acesso.
- Tratamento de sessão inválida.
- Logs de falhas críticas de autenticação.

---

## 13. Impacto no Banco de Dados

Entidades principais:

- User;
- Subscription;
- UserProfile.

Campos relevantes:

- accessStatus;
- trialEndsAt;
- expiresAt;
- onboardingCompletedAt.

---

## 14. Impacto em Gamificação

- Permite continuidade da jornada.
- Não altera XP, rank, level ou streak.

---

## 15. Impacto em Monetização

- Sessão ativa não significa acesso comercial ativo.
- O app deve bloquear recursos se o trial ou assinatura expirar.
- O status comercial deve ser respeitado no redirecionamento.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de sessão e erro localizadas. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

### Endpoint

```txt
GET /api/auth/session
```

### Response conceitual

```json
{
  "userId": "uuid",
  "isAuthenticated": true,
  "accessStatus": "subscription_active",
  "onboardingCompleted": true
}
```

### Erros esperados

```json
{
  "code": "SESSION_INVALID",
  "message": "Sua sessão expirou. Entre novamente.",
  "correlationId": "uuid"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| session_restored | Quando sessão local é aceita. |
| session_expired | Quando sessão local não pode mais ser usada. |
| access_blocked | Quando sessão existe, mas acesso comercial está bloqueado. |

---

## 19. Critérios de aceite

### CA-001 — Sessão válida

Dado que o usuário possui sessão válida,

Quando abrir o app,

Então deve entrar sem precisar fazer login novamente.

### CA-002 — Sessão inválida

Dado que a sessão não é mais válida,

Quando o app abrir,

Então o usuário deve ser direcionado para login.

### CA-003 — Acesso expirado

Dado que a sessão existe, mas o trial expirou,

Quando o app abrir,

Então o usuário deve ser direcionado para paywall.

---

## 20. Critérios de teste para QA

- abrir app com sessão válida;
- abrir app sem sessão;
- abrir app com sessão inválida;
- abrir app com trial ativo;
- abrir app com assinatura ativa;
- abrir app com trial expirado;
- logout e reabertura;
- erro de conexão durante validação.

---

## ✅ Decisão registrada

> Sessão persistente é P0 para reduzir fricção de retorno, mas não pode ignorar status comercial nem liberar recursos após expiração de trial ou assinatura.
