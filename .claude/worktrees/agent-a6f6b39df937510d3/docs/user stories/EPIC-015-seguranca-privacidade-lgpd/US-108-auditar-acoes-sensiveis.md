---
title: US-108 — Auditar ações sensíveis
sidebar_position: 108
---

# US-108 — Auditar ações sensíveis

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-108 |
| Épico | EPIC-015 — Segurança, Privacidade e LGPD |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Engenharia, Suporte e Produto |
| Planos impactados | Trial, Mensal e Anual |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema do AWAKEN**,

quero **auditar ações sensíveis**,

para **permitir rastreabilidade de operações importantes sem expor dados pessoais desnecessários**.

---

## 3. Contexto

Algumas ações têm impacto relevante em privacidade, segurança, assinatura e progresso do usuário. O AWAKEN deve registrar o que aconteceu, quando aconteceu e quem executou, com cuidado para não transformar auditoria em vazamento de dados.

---

## 4. Objetivo

Criar estrutura P1 de AuditLog para ações sensíveis como aceite legal, alteração de dados de perfil, exclusão de conta, alterações comerciais e eventos críticos de segurança.

---

## 5. Escopo

### Entra nesta US

- Auditar aceite de termos/política.
- Auditar alteração de dados de perfil sensíveis.
- Auditar solicitação/exclusão de conta.
- Auditar mudança relevante de assinatura/status comercial.
- Auditar ações administrativas futuras quando existirem.
- Registrar correlationId quando aplicável.

### Fora desta US

- Painel jurídico interno.
- SIEM avançado.
- Tracing distribuído completo.
- Armazenar payload completo com dados sensíveis.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Ações sensíveis devem ser rastreáveis quando aplicável. |
| RN-002 | AuditLog não deve armazenar dados físicos detalhados ou limitações em texto aberto. |
| RN-003 | Deve registrar ação, data/hora, usuário, origem e correlationId quando possível. |
| RN-004 | Logs devem ajudar investigação sem expor dados pessoais desnecessários. |
| RN-005 | A auditoria deve ser protegida contra alteração por usuário comum. |
| RN-006 | Eventos administrativos futuros devem ser diferenciados de ações do próprio usuário. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário comum | Gera eventos auditáveis ao executar ações sensíveis. |
| Admin | Pode gerar ações auditáveis futuras. |
| Sistema/Worker | Pode gerar eventos auditáveis automáticos. |
| Visitante | Não gera auditoria de conta autenticada. |

---

## 8. Fluxo principal

1. Usuário ou sistema executa ação sensível.
2. Backend processa a ação.
3. Backend cria entrada de auditoria com metadados mínimos.
4. Auditoria salva ação, ator, data/hora, origem e correlationId.
5. Dados sensíveis detalhados são omitidos ou mascarados.

---

## 9. Fluxos alternativos

### 9.1. Falha ao registrar auditoria

Para ações críticas, backend deve tratar com transação ou registrar erro operacional conforme severidade definida.

### 9.2. Ação sem usuário autenticado

Registrar ator como sistema/anonimizado quando aplicável, sem inventar userId.

---

## 10. Estados esperados

- ação auditada;
- auditoria ignorada por não sensível;
- auditoria falhou;
- dados mascarados;
- correlationId associado.

---

## 11. Impacto Flutter

- Sem tela obrigatória no MVP.
- Pode exibir correlationId em erros críticos.
- Não envia dados sensíveis para analytics.

---

## 12. Impacto Backend

- Serviço de AuditLog.
- Máscara/sanitização de metadados.
- Integração com correlationId.
- Proteção para leitura/escrita de auditoria.

---

## 13. Impacto DB

Entidade sugerida: AuditLog.

Campos:

- id;
- actorUserId;
- actorType;
- action;
- resourceType;
- resourceId;
- metadataSafe;
- correlationId;
- createdAt.

---

## 14. Impacto Gamificação

- Pode auditar alterações críticas de progresso futuramente.
- Não concede XP.

---

## 15. Impacto Monetização

- Pode auditar mudanças de assinatura, restauração e bloqueio de acesso.
- Ajuda investigação de problemas comerciais sem expor dados de pagamento.

---

## 16. Contrato interno sugerido

```txt
AuditLogService.Record(action, actor, resource, metadataSafe, correlationId)
```

---

## 17. Eventos Analytics

AuditLog não substitui analytics.

Não enviar conteúdo sensível de auditoria para Firebase Analytics.

---

## 18. Critérios de aceite

### CA-001 — Ação sensível auditada

Dado que o usuário solicita exclusão de conta,
Quando o backend processar a ação,
Então deve ser criada entrada de AuditLog com ação, ator, data e correlationId quando disponível.

### CA-002 — Dados mascarados

Dado que uma alteração de perfil é auditada,
Quando o log for salvo,
Então não deve armazenar valores físicos detalhados ou limitações em texto aberto.

---

## 19. Critérios de teste QA

- auditar aceite legal;
- auditar exclusão de conta;
- auditar alteração de perfil;
- auditar mudança comercial;
- validar metadados mascarados;
- validar correlationId;
- validar que usuário comum não acessa AuditLog.

---

## 20. Decisão registrada

> Auditoria é P1 e deve priorizar rastreabilidade segura, não armazenamento indiscriminado de dados sensíveis.
