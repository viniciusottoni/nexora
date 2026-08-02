---
title: US-009 — Entrar com Google
sidebar_position: 9
---

# US-009 — Entrar com Google

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-009 |
| Épico | EPIC-002 — Autenticação e Conta do Usuário |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Visitante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Google Sign-In e serviço de autenticação |
| Status | Planejada |

---

## 2. História do usuário

Como **visitante**,

quero **entrar com minha conta Google**,

para **acessar o AWAKEN rapidamente sem preencher cadastro manual**.

---

## 3. Contexto

Login Google reduz fricção no início do trial. Como o objetivo do MVP é fazer o usuário chegar rapidamente à primeira quest, o acesso social ajuda na ativação.

---

## 4. Objetivo

Permitir autenticação via Google, criando conta quando necessário ou reconhecendo conta existente.

---

## 5. Escopo

### Entra nesta US

- Botão de entrar com Google.
- Integração mobile com Google Sign-In.
- Criação de conta quando e-mail ainda não existir.
- Login em conta existente vinculada ao Google.
- Redirecionamento conforme status de acesso.
- Tratamento de cancelamento pelo usuário.

### Fora desta US

- Login Apple.
- Login Facebook.
- Vinculação avançada entre provedores.
- Migração de conta manual para Google.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Usuário pode iniciar autenticação com Google pela tela pública de acesso. |
| RN-002 | Se o e-mail Google ainda não existir, o sistema deve criar uma nova conta. |
| RN-003 | Se o e-mail já existir, o sistema deve autenticar a conta correspondente conforme regra definida. |
| RN-004 | Cancelamento pelo usuário não deve gerar erro crítico. |
| RN-005 | Após autenticação, o usuário deve seguir para trial, onboarding, home ou paywall conforme status. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Pode usar login Google. |
| Usuário em Trial | Pode manter acesso pela conta Google. |
| Premium Mensal | Pode manter acesso pela conta Google. |
| Premium Anual | Pode manter acesso pela conta Google. |
| Trial expirado | Pode entrar e ser direcionado ao paywall. |
| Assinatura expirada | Pode entrar e ser direcionado ao paywall. |
| Admin interno | Não aplicável no app mobile do MVP. |
| Suporte interno | Não aplicável no app mobile do MVP. |

---

## 8. Fluxo principal

1. Visitante toca em “Entrar com Google”.
2. App abre o fluxo de autenticação Google.
3. Usuário escolhe uma conta Google.
4. App recebe retorno de sucesso.
5. Backend cria ou reconhece o usuário.
6. App autentica a sessão.
7. Usuário é redirecionado conforme status de acesso.

---

## 9. Fluxos alternativos

### 9.1. Usuário cancela

Se o usuário cancelar o fluxo Google, o app deve voltar à tela anterior sem exibir erro crítico.

### 9.2. Falha do provedor

Se o Google não retornar sucesso, o app deve exibir mensagem clara e permitir nova tentativa.

### 9.3. Acesso expirado

Se a conta existir e o acesso estiver expirado, o usuário deve ser direcionado ao paywall.

---

## 10. Estados de tela ou estados esperados

- inicial;
- abrindo Google;
- aguardando retorno;
- sucesso;
- cancelado;
- erro do provedor;
- erro de conexão;
- acesso expirado.

---

## 11. Impacto no Frontend Flutter

- Adicionar botão Google.
- Integrar pacote Google Sign-In.
- Tratar cancelamento.
- Tratar loading.
- Tratar mensagens localizadas.
- Redirecionar conforme status retornado.

---

## 12. Impacto no Backend

- Endpoint para autenticação com provedor externo.
- Criação ou associação de usuário.
- Retorno de status de acesso.
- Tratamento padronizado de falhas.

---

## 13. Impacto no Banco de Dados

Entidades principais:

- User;
- AuthProvider;
- Subscription.

Campos relevantes:

- email;
- provider;
- providerUserId;
- createdAt.

---

## 14. Impacto em Gamificação

- Não concede XP.
- Permite acessar progresso vinculado à conta.

---

## 15. Impacto em Monetização

- Conta Google nova deve seguir para início do trial.
- Conta existente deve respeitar status de trial ou assinatura.
- Não deve reiniciar trial indevidamente.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Botão, mensagens e erros em português. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

### Endpoint

```txt
POST /api/auth/google
```

### Request

```json
{
  "provider": "google",
  "providerCredential": "valor-retornado-pelo-google"
}
```

### Response conceitual

```json
{
  "userId": "uuid",
  "email": "usuario@gmail.com",
  "accessStatus": "registered"
}
```

### Erros esperados

```json
{
  "code": "GOOGLE_AUTH_FAILED",
  "message": "Não foi possível entrar com Google.",
  "correlationId": "uuid"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| google_login_started | Quando usuário inicia login Google. |
| google_login_completed | Quando login Google conclui. |
| google_login_failed | Quando login Google falha. |

---

## 19. Critérios de aceite

### CA-001 — Login Google válido

Dado que o usuário escolhe uma conta Google válida,

Quando o fluxo concluir,

Então o usuário deve entrar no AWAKEN e seguir para a rota adequada.

### CA-002 — Cancelamento

Dado que o usuário cancela o fluxo Google,

Quando retornar ao app,

Então não deve ocorrer erro crítico.

### CA-003 — Conta existente

Dado que o e-mail Google já existe,

Quando o usuário entrar,

Então o sistema deve reconhecer a conta existente conforme regra definida.

---

## 20. Critérios de teste para QA

- login Google com conta nova;
- login Google com conta existente;
- cancelamento do fluxo;
- falha do provedor;
- acesso expirado;
- redirecionamento para trial;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Login Google é P0 porque reduz fricção na ativação e acelera o caminho até o trial e a primeira quest.
