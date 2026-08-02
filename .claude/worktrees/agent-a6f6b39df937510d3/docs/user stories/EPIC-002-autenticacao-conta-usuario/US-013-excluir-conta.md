---
title: US-013 — Excluir conta
sidebar_position: 13
---

# US-013 — Excluir conta

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-013 |
| Épico | EPIC-002 — Autenticação e Conta do Usuário |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário autenticado |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Conta do usuário e LGPD |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário autenticado**,

quero **excluir minha conta**,

para **exercer meu direito de remoção dos dados pessoais conforme regras de privacidade**.

---

## 3. Contexto

Exclusão de conta é uma exigência importante de privacidade. No AWAKEN, a ação deve ser clara, confirmada e segura, pois envolve dados pessoais, progresso, histórico e vínculo com assinatura.

---

## 4. Objetivo

Permitir que o usuário solicite exclusão da conta, com confirmação explícita, mensagens claras e tratamento adequado dos dados.

---

## 5. Escopo

### Entra nesta US

- Acesso à opção de excluir conta.
- Tela de explicação das consequências.
- Confirmação explícita.
- Solicitação de exclusão ao backend.
- Logout após solicitação concluída.
- Tratamento de assinatura ativa conforme regra da loja/plataforma.

### Fora desta US

- Cancelamento automático de assinatura fora das regras da loja.
- Exportação completa de dados.
- Suporte humano avançado.
- Exclusão física imediata de dados que precisem ser mantidos por obrigação legal.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Apenas usuário autenticado pode solicitar exclusão. |
| RN-002 | A exclusão deve exigir confirmação explícita. |
| RN-003 | O app deve explicar que progresso e dados associados poderão ser removidos ou anonimizados. |
| RN-004 | Exclusão de conta não substitui regras da loja para cancelamento de assinatura. |
| RN-005 | Após exclusão concluída, a sessão local deve ser encerrada. |
| RN-006 | O backend deve aplicar a política definida de remoção, anonimização ou retenção legal mínima. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode excluir conta. |
| Usuário em Trial | Pode solicitar exclusão. |
| Premium Mensal | Pode solicitar exclusão. |
| Premium Anual | Pode solicitar exclusão. |
| Trial expirado | Pode solicitar exclusão. |
| Assinatura expirada | Pode solicitar exclusão. |
| Admin interno | Fora do app mobile do MVP. |
| Suporte interno | Fora do app mobile do MVP. |

---

## 8. Fluxo principal

1. Usuário acessa configurações da conta.
2. Toca em excluir conta.
3. App exibe explicação das consequências.
4. Usuário confirma explicitamente.
5. Backend processa solicitação.
6. App encerra sessão local.
7. Usuário retorna para tela pública.

---

## 9. Fluxos alternativos

### 9.1. Usuário cancela

Se o usuário cancelar a confirmação, nenhuma alteração deve ser feita.

### 9.2. Assinatura ativa

Se houver assinatura ativa, o app deve informar que o cancelamento financeiro pode precisar ser feito pela loja/plataforma.

### 9.3. Falha de processamento

Se o backend não concluir a solicitação, o app deve exibir erro claro e permitir tentativa futura.

---

## 10. Estados de tela ou estados esperados

- inicial;
- explicando consequências;
- aguardando confirmação;
- processando;
- sucesso;
- erro de conexão;
- erro inesperado.

---

## 11. Impacto no Frontend Flutter

- Opção em configurações.
- Tela de confirmação.
- Mensagens localizadas.
- Alerta sobre assinatura ativa.
- Encerramento de sessão local após sucesso.

---

## 12. Impacto no Backend

- Endpoint de solicitação de exclusão.
- Política de remoção ou anonimização.
- Registro da solicitação.
- Bloqueio de novo acesso à conta excluída, conforme regra definida.

---

## 13. Impacto no Banco de Dados

Entidades principais:

- User;
- UserProfile;
- Subscription;
- QuestLog;
- HunterProgress.

Campos ou efeitos possíveis:

- deletedAt;
- anonymizedAt;
- accountStatus.

---

## 14. Impacto em Gamificação

- Progresso pode ser removido ou anonimizado conforme política.
- Não concede nem remove XP por si só.

---

## 15. Impacto em Monetização

- A exclusão não deve prometer cancelamento automático fora das regras da loja.
- Usuário deve ser orientado sobre assinatura ativa.
- RevenueCat pode precisar ser consultado para status comercial.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Textos de alerta e confirmação em português. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

### Endpoint

```txt
DELETE /api/account
```

### Request

```json
{
  "confirmation": true
}
```

### Response conceitual

```json
{
  "success": true,
  "accountStatus": "deleted"
}
```

### Erros esperados

```json
{
  "code": "ACCOUNT_DELETE_FAILED",
  "message": "Não foi possível excluir a conta agora.",
  "correlationId": "uuid"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| account_delete_started | Quando usuário abre o fluxo de exclusão. |
| account_delete_completed | Quando exclusão é concluída. |
| account_delete_failed | Quando exclusão falha. |

---

## 19. Critérios de aceite

### CA-001 — Exclusão confirmada

Dado que o usuário autenticado confirma exclusão,

Quando a solicitação for processada,

Então a conta deve seguir a política definida e a sessão local deve ser encerrada.

### CA-002 — Cancelamento pelo usuário

Dado que o usuário abriu o fluxo,

Quando cancelar,

Então nenhum dado deve ser alterado.

### CA-003 — Assinatura ativa

Dado que o usuário possui assinatura ativa,

Quando solicitar exclusão,

Então o app deve informar sobre regras de cancelamento da loja/plataforma.

---

## 20. Critérios de teste para QA

- exclusão com trial ativo;
- exclusão com assinatura ativa;
- exclusão com acesso expirado;
- cancelamento do fluxo;
- falha de conexão;
- encerramento de sessão após sucesso;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Exclusão de conta é P1, mas deve ser prevista para cumprir privacidade e confiança do usuário desde o MVP.
