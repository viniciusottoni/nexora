---
title: US-107 — Solicitar exclusão de conta
sidebar_position: 107
---

# US-107 — Solicitar exclusão de conta

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-107 |
| Épico | EPIC-015 — Segurança, Privacidade e LGPD |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário autenticado |
| Planos impactados | Trial, Mensal e Anual |
| Dependência principal | EPIC-002 — Conta e Autenticação |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário autenticado**,

quero **solicitar a exclusão da minha conta**,

para **ter controle sobre meus dados e encerrar meu vínculo com o AWAKEN quando desejar**.

---

## 3. Contexto

A exclusão de conta é P1 no EPIC-015, mas precisa estar prevista desde o MVP por privacidade, lojas mobile e confiança do usuário.

---

## 4. Objetivo

Permitir que o usuário solicite exclusão de conta, entenda impactos e tenha seus dados tratados conforme regra de privacidade definida.

---

## 5. Escopo

### Entra nesta US

- Tela/fluxo de solicitação de exclusão.
- Confirmação explícita antes de excluir.
- Informar impacto sobre progresso, histórico e acesso.
- Cancelar sessão após exclusão efetiva.
- Registrar `accountDeletedAt` ou status equivalente.
- Tratar assinatura conforme regra da loja/RevenueCat, sem prometer cancelamento automático indevido.

### Fora desta US

- Apagar dados de backups imediatamente.
- Painel jurídico interno.
- Fluxo avançado de exportação de dados.
- Cancelamento automático garantido da assinatura fora das regras da loja.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Usuário autenticado pode solicitar exclusão da própria conta. |
| RN-002 | Exclusão deve exigir confirmação explícita. |
| RN-003 | O app deve informar que assinatura pode precisar ser gerenciada na loja, conforme plataforma. |
| RN-004 | Após exclusão efetiva, sessão deve ser encerrada. |
| RN-005 | Dados devem seguir regra de exclusão, anonimização ou retenção mínima definida. |
| RN-006 | Conta excluída não deve conseguir acessar funcionalidades principais. |
| RN-007 | A ação deve ser auditável quando implementado AuditLog. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não possui conta para excluir. |
| Usuário em Trial | Pode solicitar exclusão. |
| Premium Mensal | Pode solicitar exclusão. |
| Premium Anual | Pode solicitar exclusão. |
| Trial expirado | Pode solicitar exclusão autenticado. |
| Assinatura expirada | Pode solicitar exclusão autenticado. |

---

## 8. Fluxo principal

1. Usuário acessa configurações da conta.
2. Toca em excluir conta.
3. App exibe impacto e aviso sobre assinatura/loja.
4. Usuário confirma explicitamente.
5. Backend processa solicitação.
6. Sistema marca conta como excluída ou inicia rotina definida.
7. App encerra sessão e redireciona para tela inicial.

---

## 9. Fluxos alternativos

### 9.1. Usuário cancela confirmação

Nenhuma alteração é feita.

### 9.2. Falha ao excluir

App exibe erro com correlationId e mantém conta ativa.

### 9.3. Assinatura ativa

App informa que o gerenciamento/cancelamento da assinatura pode depender da loja/plataforma.

---

## 10. Estados esperados

- exclusão disponível;
- confirmação pendente;
- excluindo;
- conta excluída;
- erro de exclusão;
- sessão encerrada.

---

## 11. Impacto Flutter

- Tela em configurações de conta.
- Modal de confirmação forte.
- Aviso sobre perda de progresso e assinatura.
- Limpeza de sessão local após sucesso.
- Textos PT-BR, EN e ES.

---

## 12. Impacto Backend

- Endpoint de exclusão/solicitação.
- Validação de usuário autenticado.
- Marcação de conta excluída.
- Rotina de anonimização/exclusão conforme regra definida.
- AuditLog quando aplicável.

---

## 13. Impacto DB

Campos/entidades:

- accountDeletedAt;
- deletionRequestedAt;
- deletionStatus;
- AuditLog.

---

## 14. Impacto Gamificação

- Conta excluída não acessa Perfil Hunter, XP, rank ou histórico.
- Progresso pode ser removido/anonimizado conforme regra definida.

---

## 15. Impacto Monetização

- Não prometer cancelamento automático fora das regras da loja.
- Usuário deve ser orientado sobre assinatura ativa quando aplicável.

---

## 16. Contrato API sugerido

```txt
POST /api/users/me/delete-account
```

Request conceitual:

```json
{
  "confirmation": "DELETE_MY_ACCOUNT"
}
```

---

## 17. Eventos Analytics

| Evento | Quando dispara |
|---|---|
| account_deletion_requested | Quando usuário solicita exclusão. |
| account_deletion_completed | Quando exclusão é concluída. |
| account_deletion_failed | Quando exclusão falha. |

Eventos não devem conter dados sensíveis.

---

## 18. Critérios de aceite

### CA-001 — Confirmação obrigatória

Dado que o usuário toca em excluir conta,
Quando o modal abrir,
Então deve exigir confirmação explícita antes de executar a ação.

### CA-002 — Sessão encerrada

Dado que a exclusão foi concluída,
Quando o backend confirmar sucesso,
Então o app deve limpar sessão local e impedir acesso às rotas protegidas.

---

## 19. Critérios de teste QA

- solicitar exclusão;
- cancelar confirmação;
- confirmar exclusão;
- falha de backend;
- usuário com assinatura ativa;
- sessão encerrada;
- conta excluída tentando acessar app;
- textos PT-BR, EN e ES.

---

## 20. Decisão registrada

> Exclusão de conta é P1, mas deve estar prevista com confirmação explícita, encerramento de sessão e orientação clara sobre assinatura.
