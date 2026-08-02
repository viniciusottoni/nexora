---
title: US-007 — Criar conta com e-mail e senha
sidebar_position: 7
---

# US-007 — Criar conta com e-mail e senha

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-007 |
| Épico | EPIC-002 — Autenticação e Conta do Usuário |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Visitante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Serviço de autenticação e entidade User |
| Status | Planejada |

---

## 2. História do usuário

Como **visitante**,

quero **criar uma conta com e-mail e senha**,

para **salvar meu progresso, iniciar meu trial de 7 dias e continuar minha jornada no AWAKEN**.

---

## 3. Contexto

A conta é obrigatória para vincular trial, progresso, histórico, assinatura e status de acesso. O cadastro precisa ser simples e confiável, sem criar fricção excessiva antes do onboarding.

---

## 4. Objetivo

Permitir que o visitante crie uma conta válida usando nome, e-mail e senha, recebendo sessão autenticada e ficando apto a iniciar o trial.

---

## 5. Escopo

### Entra nesta US

- Tela de cadastro com nome, e-mail e senha.
- Validação de campos obrigatórios.
- Criação da conta no backend.
- Tratamento de e-mail já cadastrado.
- Mensagens de erro localizadas.
- Redirecionamento para fluxo de trial após cadastro.

### Fora desta US

- Login com Google.
- Recuperação de senha.
- Confirmação de e-mail obrigatória.
- MFA.
- Perfil físico e onboarding.
- Assinatura.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Nome, e-mail e senha são obrigatórios. |
| RN-002 | O e-mail deve ter formato válido. |
| RN-003 | E-mail já cadastrado não deve criar nova conta. |
| RN-004 | A senha deve respeitar política mínima definida pelo backend. |
| RN-005 | Após cadastro bem-sucedido, o usuário deve seguir para o fluxo de trial. |
| RN-006 | A criação da conta não deve iniciar onboarding automaticamente sem passar pela regra de trial. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Pode criar conta. |
| Usuário em Trial | Não precisa criar nova conta. |
| Premium Mensal | Não precisa criar nova conta. |
| Premium Anual | Não precisa criar nova conta. |
| Trial expirado | Não pode criar novo trial com a mesma conta. |
| Assinatura expirada | Não precisa criar nova conta. |
| Admin interno | Não aplicável no app mobile do MVP. |
| Suporte interno | Não aplicável no app mobile do MVP. |

---

## 8. Fluxo principal

1. Visitante acessa tela de cadastro.
2. Informa nome, e-mail e senha.
3. App valida campos localmente.
4. App envia solicitação de cadastro ao backend.
5. Backend cria a conta.
6. App recebe sucesso e autentica o usuário.
7. Usuário é direcionado para iniciar o trial.

---

## 9. Fluxos alternativos

### 9.1. E-mail já cadastrado

1. Usuário informa e-mail existente.
2. Backend retorna erro de conta já existente.
3. App informa que o e-mail já possui conta e oferece ir para login.

### 9.2. Senha inválida

1. Usuário informa senha fora da política mínima.
2. App exibe erro claro.
3. Usuário corrige e tenta novamente.

### 9.3. Falha de conexão

1. Solicitação não é concluída.
2. App exibe erro de conexão e ação para tentar novamente.

---

## 10. Estados de tela ou estados esperados

- inicial;
- preenchendo;
- enviando;
- sucesso;
- erro de validação;
- e-mail já cadastrado;
- erro de conexão;
- erro inesperado.

---

## 11. Impacto no Frontend Flutter

- Tela de cadastro.
- Formulário com validações.
- Estado de envio.
- Mensagens de erro localizadas.
- CTA para login quando e-mail já existir.
- Redirecionamento para fluxo de trial.

---

## 12. Impacto no Backend

- Endpoint de criação de usuário.
- Validação de e-mail único.
- Validação de senha mínima.
- Criação de sessão autenticada.
- Tratamento padronizado de erros.

---

## 13. Impacto no Banco de Dados

Entidade principal: User.

Campos relevantes:

- id;
- name;
- email;
- authProvider;
- createdAt;
- updatedAt.

Restrições esperadas:

- e-mail único;
- auditoria de criação;
- soft delete futuro, se aplicável.

---

## 14. Impacto em Gamificação

- Não concede XP.
- Não altera rank, level, atributos ou streak.
- É pré-requisito para salvar progresso futuro.

---

## 15. Impacto em Monetização

- Conta criada deve poder iniciar trial de 7 dias.
- Não deve assinar automaticamente.
- Não deve exibir paywall antes de explicar o trial.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Labels, validações e erros em português. |
| EN | Chaves preparadas em inglês. |
| ES | Chaves preparadas em espanhol. |

---

## 17. Contrato de API sugerido

### Endpoint

```txt
POST /api/auth/register
```

### Request

```json
{
  "name": "Nome do Usuário",
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
  "accessStatus": "registered"
}
```

### Erros esperados

```json
{
  "code": "EMAIL_ALREADY_EXISTS",
  "message": "Este e-mail já possui uma conta.",
  "correlationId": "uuid"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| sign_up_started | Quando o usuário abre ou inicia o cadastro. |
| sign_up_completed | Quando o cadastro é concluído. |
| sign_up_failed | Quando o cadastro falha. |

---

## 19. Critérios de aceite

### CA-001 — Cadastro válido

Dado que o visitante informa dados válidos,

Quando confirmar o cadastro,

Então a conta deve ser criada e o usuário deve seguir para o fluxo de trial.

### CA-002 — E-mail existente

Dado que o visitante informa e-mail já cadastrado,

Quando confirmar o cadastro,

Então deve ver mensagem clara e opção de ir para login.

### CA-003 — Campos inválidos

Dado que há campos obrigatórios vazios ou inválidos,

Quando o usuário tentar cadastrar,

Então o app deve exibir validações sem enviar dados inválidos.

---

## 20. Critérios de teste para QA

- cadastro válido;
- e-mail inválido;
- e-mail já cadastrado;
- senha inválida;
- campos vazios;
- falha de conexão;
- redirecionamento para trial;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Cadastro por e-mail e senha é P0 porque permite iniciar trial, salvar progresso e vincular assinatura futura ao usuário.
