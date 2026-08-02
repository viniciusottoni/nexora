---
title: US-008 — Entrar com e-mail e senha
sidebar_position: 8
---

# US-008 — Entrar com e-mail e senha

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-008 |
| Épico | EPIC-002 — Autenticação e Conta do Usuário |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Visitante com conta existente |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Serviço de autenticação e entidade User |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com conta existente**,

quero **entrar com e-mail e senha**,

para **acessar meu perfil, meu trial, minha assinatura e meu progresso no AWAKEN**.

---

## 3. Contexto

O login por e-mail e senha é o caminho básico para retorno do usuário. Ele precisa identificar o status da conta e redirecionar corretamente para trial, onboarding, home, paywall ou tela bloqueada.

---

## 4. Objetivo

Permitir autenticação com e-mail e senha, mantendo o usuário dentro do fluxo correto conforme status comercial e onboarding.

---

## 5. Escopo

### Entra nesta US

- Tela de login com e-mail e senha.
- Validação local de campos.
- Envio de credenciais ao backend.
- Tratamento de credenciais inválidas.
- Tratamento de conta inexistente.
- Redirecionamento conforme status da conta.
- Link para recuperação de senha.
- Link para cadastro.

### Fora desta US

- Cadastro.
- Login Google.
- Recuperação de senha completa.
- MFA.
- Alteração de e-mail.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | E-mail e senha são obrigatórios. |
| RN-002 | Login válido deve criar sessão local. |
| RN-003 | Login inválido deve exibir mensagem clara. |
| RN-004 | Usuário com trial ativo deve seguir para onboarding ou home. |
| RN-005 | Usuário com assinatura ativa deve seguir para onboarding ou home. |
| RN-006 | Usuário com acesso expirado deve seguir para paywall ou estado bloqueado. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Pode tentar login. |
| Usuário em Trial | Pode manter ou renovar sessão. |
| Premium Mensal | Pode manter ou renovar sessão. |
| Premium Anual | Pode manter ou renovar sessão. |
| Trial expirado | Pode entrar, mas deve ser direcionado para paywall. |
| Assinatura expirada | Pode entrar, mas deve ser direcionado para paywall. |
| Admin interno | Não aplicável no app mobile do MVP. |
| Suporte interno | Não aplicável no app mobile do MVP. |

---

## 8. Fluxo principal

1. Usuário acessa tela de login.
2. Informa e-mail e senha.
3. App valida campos.
4. Backend autentica a conta.
5. App salva sessão local.
6. App consulta ou recebe status de acesso.
7. Usuário é redirecionado para a rota correta.

---

## 9. Fluxos alternativos

### 9.1. Credenciais inválidas

O app deve informar que e-mail ou senha estão incorretos sem revelar detalhes sensíveis.

### 9.2. Conta inexistente

O app deve orientar o usuário a criar conta, sem quebrar o fluxo.

### 9.3. Acesso expirado

Após login bem-sucedido, usuário com trial ou assinatura expirada deve ir para paywall.

---

## 10. Estados de tela ou estados esperados

- inicial;
- preenchendo;
- enviando;
- sucesso;
- erro de validação;
- credenciais inválidas;
- erro de conexão;
- acesso expirado.

---

## 11. Impacto no Frontend Flutter

- Tela de login.
- Formulário com validação.
- Estados de envio e erro.
- Links para cadastro e recuperação.
- Redirecionamento conforme status.
- Textos localizados.

---

## 12. Impacto no Backend

- Endpoint de login.
- Validação das credenciais.
- Retorno de dados básicos do usuário.
- Retorno ou consulta do status de acesso.
- Tratamento padronizado de erros.

---

## 13. Impacto no Banco de Dados

Entidades principais:

- User;
- Subscription;
- UserProfile.

Campos relevantes:

- email;
- authProvider;
- accessStatus;
- onboardingCompletedAt.

---

## 14. Impacto em Gamificação

- Não altera XP, rank, level ou streak.
- Permite recuperar acesso ao progresso existente.

---

## 15. Impacto em Monetização

- Login deve respeitar trial, assinatura ativa e expiração.
- Usuário expirado deve ser direcionado para assinatura.
- Não inicia novo trial automaticamente.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Labels e erros em português. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

### Endpoint

```txt
POST /api/auth/login
```

### Request

```json
{
  "email": "usuario@email.com",
  "password": "senha"
}
```

### Response conceitual

```json
{
  "userId": "uuid",
  "name": "Nome do Usuário",
  "email": "usuario@email.com",
  "accessStatus": "trial_active"
}
```

### Erros esperados

```json
{
  "code": "INVALID_CREDENTIALS",
  "message": "E-mail ou senha inválidos.",
  "correlationId": "uuid"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| login_started | Quando usuário inicia login. |
| login_completed | Quando login é concluído. |
| login_failed | Quando login falha. |

---

## 19. Critérios de aceite

### CA-001 — Login válido

Dado que o usuário informa e-mail e senha válidos,

Quando confirmar login,

Então deve entrar no app e ser direcionado conforme status da conta.

### CA-002 — Login inválido

Dado que as credenciais estão incorretas,

Quando tentar login,

Então deve ver mensagem clara de erro.

### CA-003 — Acesso expirado

Dado que o login é válido, mas o acesso expirou,

Quando entrar,

Então o usuário deve ser direcionado para paywall.

---

## 20. Critérios de teste para QA

- login válido;
- e-mail inválido;
- senha vazia;
- credenciais incorretas;
- conta inexistente;
- trial ativo;
- assinatura ativa;
- trial expirado;
- falha de conexão;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Login por e-mail e senha é P0 porque permite retorno do usuário e recuperação da jornada, respeitando o status comercial atual da conta.
