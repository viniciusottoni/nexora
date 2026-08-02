---
title: US-012 — Recuperar senha
sidebar_position: 12
---

# US-012 — Recuperar senha

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-012 |
| Épico | EPIC-002 — Autenticação e Conta do Usuário |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Visitante com conta existente |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Serviço de recuperação de senha |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário que esqueceu a senha**,

quero **recuperar meu acesso**,

para **voltar a usar o AWAKEN sem perder meu progresso, trial ou assinatura**.

---

## 3. Contexto

Recuperação de senha reduz abandono de usuários que já têm conta. Embora seja P1, é importante para suporte básico e continuidade após trial ou assinatura.

---

## 4. Objetivo

Permitir que o usuário solicite recuperação de senha usando e-mail cadastrado e receba instruções para redefinir o acesso.

---

## 5. Escopo

### Entra nesta US

- Link “Esqueci minha senha” na tela de login.
- Tela de solicitação com e-mail.
- Validação de e-mail.
- Solicitação de recuperação ao backend.
- Mensagem genérica de confirmação.
- Fluxo de redefinição conforme implementação técnica definida.

### Fora desta US

- Suporte humano manual.
- Recuperação por telefone.
- MFA.
- Alteração de e-mail.
- Recuperação de conta excluída.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O usuário deve informar e-mail válido. |
| RN-002 | A resposta deve ser genérica para evitar revelar se o e-mail existe. |
| RN-003 | A solicitação deve gerar instrução de recuperação quando a conta existir. |
| RN-004 | Recuperar senha não deve alterar trial, assinatura ou progresso. |
| RN-005 | Link ou código de recuperação deve ter validade limitada. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Pode solicitar recuperação. |
| Usuário em Trial | Pode solicitar se estiver deslogado. |
| Premium Mensal | Pode solicitar se estiver deslogado. |
| Premium Anual | Pode solicitar se estiver deslogado. |
| Trial expirado | Pode recuperar acesso à conta e depois ver paywall. |
| Assinatura expirada | Pode recuperar acesso à conta e depois ver paywall. |
| Admin interno | Não aplicável no app mobile do MVP. |
| Suporte interno | Não aplicável no app mobile do MVP. |

---

## 8. Fluxo principal

1. Usuário toca em “Esqueci minha senha”.
2. Informa e-mail.
3. App valida formato do e-mail.
4. Backend processa solicitação.
5. App mostra mensagem de confirmação genérica.
6. Usuário segue instrução recebida para redefinir senha.

---

## 9. Fluxos alternativos

### 9.1. E-mail inválido

O app deve exibir validação local antes de enviar.

### 9.2. E-mail não encontrado

O app deve manter mensagem genérica para proteger privacidade.

### 9.3. Solicitação expirada

Se o usuário usar link ou código expirado, deve solicitar nova recuperação.

---

## 10. Estados de tela ou estados esperados

- inicial;
- enviando;
- confirmação;
- erro de validação;
- erro de conexão;
- solicitação expirada;
- erro inesperado.

---

## 11. Impacto no Frontend Flutter

- Link na tela de login.
- Tela de recuperação.
- Validação de e-mail.
- Mensagem de confirmação genérica.
- Tela ou deep link de redefinição, conforme decisão técnica.

---

## 12. Impacto no Backend

- Endpoint para solicitar recuperação.
- Geração de instrução de redefinição.
- Validade limitada da solicitação.
- Tratamento genérico para e-mail inexistente.

---

## 13. Impacto no Banco de Dados

Entidades ou campos possíveis:

- User;
- PasswordResetRequest.

Campos relevantes:

- userId;
- requestedAt;
- expiresAt;
- usedAt.

---

## 14. Impacto em Gamificação

- Não altera progresso.
- Permite recuperar acesso ao progresso existente.

---

## 15. Impacto em Monetização

- Não altera trial nem assinatura.
- Usuário com acesso expirado continua bloqueado após recuperar conta.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Textos e instruções em português. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

### Endpoint

```txt
POST /api/auth/forgot-password
```

### Request

```json
{
  "email": "usuario@email.com"
}
```

### Response conceitual

```json
{
  "success": true,
  "message": "Se existir uma conta com este e-mail, enviaremos instruções de recuperação."
}
```

### Erros esperados

```json
{
  "code": "PASSWORD_RESET_FAILED",
  "message": "Não foi possível processar a solicitação agora.",
  "correlationId": "uuid"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| password_reset_requested | Quando o usuário solicita recuperação. |
| password_reset_failed | Quando a solicitação falha. |

---

## 19. Critérios de aceite

### CA-001 — Solicitação válida

Dado que o usuário informa e-mail válido,

Quando solicita recuperação,

Então deve ver mensagem de confirmação genérica.

### CA-002 — E-mail inválido

Dado que o e-mail tem formato inválido,

Quando tentar enviar,

Então o app deve exibir validação.

### CA-003 — Sem revelar existência da conta

Dado que o e-mail pode ou não existir,

Quando a solicitação for processada,

Então a mensagem ao usuário deve ser genérica.

---

## 20. Critérios de teste para QA

- e-mail válido existente;
- e-mail válido inexistente;
- e-mail inválido;
- falha de conexão;
- link ou solicitação expirada;
- usuário com acesso expirado após recuperar conta;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Recuperação de senha é P1, mas deve ser prevista no MVP para reduzir perda de usuários que já iniciaram trial ou assinaram.
