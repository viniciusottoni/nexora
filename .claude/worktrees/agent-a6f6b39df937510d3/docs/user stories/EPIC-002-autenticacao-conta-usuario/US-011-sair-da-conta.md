---
title: US-011 — Sair da conta
sidebar_position: 11
---

# US-011 — Sair da conta

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-011 |
| Épico | EPIC-002 — Autenticação e Conta do Usuário |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário autenticado |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Sessão autenticada |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário autenticado**,

quero **sair da minha conta**,

para **proteger meu acesso em aparelhos compartilhados ou quando eu não quiser manter a sessão ativa**.

---

## 3. Contexto

Logout é uma ação básica de conta e segurança. O app deve permitir sair sem apagar progresso, assinatura ou dados remotos.

---

## 4. Objetivo

Permitir encerramento local da sessão e retorno para a experiência pública de entrada.

---

## 5. Escopo

### Entra nesta US

- Ação de sair da conta.
- Confirmação antes de sair, se necessário.
- Limpeza da sessão local.
- Redirecionamento para rota pública.
- Mensagem de sucesso ou estado final claro.

### Fora desta US

- Exclusão de conta.
- Cancelamento de assinatura.
- Apagar progresso.
- Logout remoto de todos os dispositivos.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Logout não deve apagar progresso remoto. |
| RN-002 | Logout não deve cancelar trial ou assinatura. |
| RN-003 | Após logout, rotas protegidas não devem ser acessíveis. |
| RN-004 | Após logout, usuário deve ir para tela pública. |
| RN-005 | Se ocorrer falha no servidor, o app ainda deve limpar sessão local quando seguro. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não aplicável. |
| Usuário em Trial | Pode sair. |
| Premium Mensal | Pode sair. |
| Premium Anual | Pode sair. |
| Trial expirado | Pode sair. |
| Assinatura expirada | Pode sair. |
| Admin interno | Não aplicável no app mobile do MVP. |
| Suporte interno | Não aplicável no app mobile do MVP. |

---

## 8. Fluxo principal

1. Usuário acessa área de conta ou configurações.
2. Toca em sair da conta.
3. App solicita confirmação, se definido pelo UX.
4. Usuário confirma.
5. App encerra sessão local.
6. Usuário é redirecionado para tela pública.

---

## 9. Fluxos alternativos

### 9.1. Usuário cancela

Se o usuário cancelar a confirmação, permanece logado.

### 9.2. Falha de conexão

Se não for possível notificar o backend, o app deve tratar a falha e priorizar encerramento local quando seguro.

---

## 10. Estados de tela ou estados esperados

- inicial;
- confirmando;
- saindo;
- sucesso;
- erro de conexão;
- erro inesperado.

---

## 11. Impacto no Frontend Flutter

- Ação de logout em configurações ou perfil.
- Modal de confirmação, se definido.
- Limpeza de sessão local.
- Redirecionamento para rota pública.
- Mensagens localizadas.

---

## 12. Impacto no Backend

- Endpoint de logout pode ser usado para registrar encerramento de sessão.
- Logout local não deve depender totalmente do backend para proteger o aparelho.

---

## 13. Impacto no Banco de Dados

Sem impacto obrigatório em banco no MVP.

Pode haver registro de auditoria futuro.

---

## 14. Impacto em Gamificação

- Não altera XP, rank, level, atributos ou streak.
- Progresso permanece salvo.

---

## 15. Impacto em Monetização

- Logout não cancela trial nem assinatura.
- Status comercial permanece vinculado à conta.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de logout em português. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

### Endpoint

```txt
POST /api/auth/logout
```

### Request

```json
{}
```

### Response conceitual

```json
{
  "success": true
}
```

### Erros esperados

```json
{
  "code": "LOGOUT_FAILED",
  "message": "Não foi possível concluir a saída agora.",
  "correlationId": "uuid"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| logout_started | Quando usuário toca em sair. |
| logout_completed | Quando sessão local é encerrada. |

---

## 19. Critérios de aceite

### CA-001 — Logout concluído

Dado que o usuário está autenticado,

Quando confirmar saída,

Então a sessão local deve ser encerrada e o usuário deve ir para tela pública.

### CA-002 — Progresso preservado

Dado que o usuário saiu da conta,

Quando entrar novamente,

Então seu progresso remoto deve continuar disponível.

### CA-003 — Rotas protegidas bloqueadas

Dado que o usuário saiu,

Quando tentar acessar rota protegida,

Então deve ser direcionado para login ou entrada pública.

---

## 20. Critérios de teste para QA

- logout com trial ativo;
- logout com assinatura ativa;
- logout com acesso expirado;
- cancelar confirmação;
- tentar acessar rota protegida após logout;
- entrar novamente e validar progresso;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Logout deve encerrar o acesso local ao app, mas nunca apagar progresso, trial ou assinatura vinculados à conta.
