---
title: US-203 — Serializar metadados de auditoria com JSON seguro
sidebar_position: 203
---

# US-203 — Serializar metadados de auditoria com JSON seguro

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-203 |
| Épico | EPIC-020 — Hardening de Segurança e Fechamento de Vulnerabilidades |
| Prioridade | P1 |
| Fase | Pré-teste aberto |
| Perfil principal | Backend, auditoria e segurança |
| Plano | Todos |
| Idiomas impactados | Não aplicável ao usuário final |
| Dependência principal | AuditLogService, handlers de loja, economia e assinatura |
| Status | Planejada |

## 2. História do usuário

Como **responsável por auditoria e suporte**,

quero **que metadados de auditoria sejam serializados de forma segura e padronizada**,

para **evitar JSON quebrado, poluição de logs e inconsistência nos rastros operacionais**.

## 3. Contexto

Alguns handlers montam metadados de auditoria manualmente por interpolação de string. O padrão deve ser substituído por serialização segura e centralizada.

## 4. Objetivo

Padronizar criação de metadados de auditoria usando serialização JSON, com allowlist de campos seguros.

## 5. Escopo

### Entra nesta US

- Criar helper/factory para metadados seguros de auditoria.
- Substituir interpolações manuais em loja, IAP, assinatura e Gold.
- Garantir que campos sensíveis não entrem nos metadados.
- Criar testes com valores contendo aspas, quebras e caracteres especiais.
- Documentar padrão para novos handlers.

### Fora desta US

- Dashboard de auditoria.
- Retenção avançada de logs.
- Criptografia campo a campo.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Metadados de auditoria devem ser serializados por biblioteca JSON. |
| RN-002 | Metadados devem usar allowlist de campos seguros. |
| RN-003 | Dados de pagamento, tokens, recibos e credenciais não podem entrar na auditoria. |
| RN-004 | Falha de auditoria não deve cancelar transação principal, mas deve ser observável. |
| RN-005 | O JSON persistido deve ser válido. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário final | Não acessa auditoria interna. |
| Admin interno | Pode consultar auditoria conforme RBAC futuro. |
| Sistema | Pode registrar auditoria segura. |

## 8. Fluxo principal

1. Handler executa ação sensível.
2. Handler monta objeto de metadados seguro.
3. Helper serializa o objeto.
4. AuditLogService persiste metadado válido.
5. Falha de auditoria gera warning sem expor dado sensível.

## 9. Fluxos alternativos

- Metadado nulo: auditoria registra apenas ação/recurso.
- Campo não permitido: helper ignora ou rejeita.
- Falha ao serializar: registra auditoria sem metadado e gera warning.

## 10. Estados esperados

- auditoria com metadado válido;
- auditoria sem metadado;
- campo sensível bloqueado;
- falha observável;
- transação principal preservada.

## 11. Impacto no Frontend Flutter

Sem impacto direto.

## 12. Impacto no Backend

- Criar helper de serialização.
- Refatorar handlers de `ProcessIapPurchase`, `PurchaseWithGold`, `GoldWalletService` e `SyncEntitlement`.
- Adicionar testes unitários de serialização.

## 13. Impacto no Banco de Dados

- Mantém campo atual de metadados.
- Garante JSON válido persistido.
- Pode facilitar índice/consulta futura.

## 14. Impacto em Gamificação

Sem impacto direto.

## 15. Impacto em Monetização

Melhora rastreabilidade segura de compras, Gold, IAP e assinatura.

## 16. Impacto em Internacionalização

Não aplicável.

## 17. Contrato técnico sugerido

```txt
AuditMetadata.Safe(new { productKey, channel, reason })
```

## 18. Eventos de Analytics

Não aplicável.

## 19. Critérios de aceite

- Nenhum handler novo usa JSON manual por interpolação.
- Valores com caracteres especiais geram JSON válido.
- Campos sensíveis não são serializados.
- Falha de auditoria não cancela compra/assinatura.
- Testes cobrem loja, IAP, Gold e assinatura.

## 20. Critérios de teste para QA

- compra com produto contendo caracteres especiais em ambiente controlado;
- auditoria de falha;
- auditoria de assinatura;
- auditoria sem metadado;
- verificação de ausência de campos sensíveis.

## ✅ Decisão registrada

Metadados de auditoria devem ser serializados por helper seguro, nunca por montagem manual de JSON.