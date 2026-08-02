---
title: US-228 — Reconciliar Gold, ledger, pedidos e inventário com alertas antifraude
sidebar_position: 228
---

# US-228 — Reconciliar Gold, ledger, pedidos e inventário com alertas antifraude

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-228 |
| Épico | EPIC-020 — Hardening de Segurança, Performance e Escalabilidade do MVP |
| Prioridade | P0 |
| Fase | Bloqueador do MVP em produção com economia Gold |
| Perfil principal | Backend, Segurança, Admin, Economia, QA e DevOps |
| Plano | Trial, Mensal e Anual |
| Dependência principal | GoldWallet, GoldLedgerEntry, ShopOrder, InventoryItem, AuditLog, Admin |
| Status | Planejada |

## 2. História do usuário

Como **responsável pela economia do AWAKEN**,

quero **reconciliar saldos, lançamentos, pedidos e inventário com alertas de anomalia**,

para **detectar bugs, duplicidades, fraude, inconsistências e abuso antes que a economia seja comprometida**.

## 3. Contexto

O Gold será usado para comprar itens dentro do app. Mesmo com validação server-side e transações atômicas, é necessário ter reconciliação periódica e alertas preventivos. A economia precisa ser auditável: saldo atual deve bater com ledger, pedidos concedidos devem ter débito/crédito correspondente, e inventário concedido deve ter origem rastreável.

## 4. Objetivo

Criar reconciliação automática e alertas operacionais para proteger a economia Gold.

## 5. Escopo

### Entra nesta US

- Job/rotina de reconciliação entre GoldWallet e GoldLedgerEntry.
- Verificação entre ShopOrder granted e movimentação correspondente de Gold.
- Verificação entre item concedido e origem rastreável.
- Alertas para saldo negativo, saldo divergente, crédito incomum, débito incomum e duplicidade.
- Alertas para volume anormal de compras com Gold por usuário/período.
- Exposição segura dos alertas no Admin.
- Relatório exportável de divergências sem dados sensíveis.

### Fora desta US

- Bloqueio automático permanente de usuário.
- Machine learning antifraude.
- Marketplace entre usuários.
- Economia dinâmica avançada.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Saldo da carteira deve ser reconciliável pelo ledger. |
| RN-002 | Pedido granted em Gold deve possuir débito correspondente. |
| RN-003 | Crédito de Gold comprado deve possuir compra validada correspondente. |
| RN-004 | Item concedido deve possuir origem rastreável. |
| RN-005 | Divergência deve gerar alerta operacional. |
| RN-006 | Alerta não deve expor dados sensíveis de pagamento ou provider. |
| RN-007 | Admin pode marcar alerta como analisado, mas não apagar evidência. |

## 7. Tipos de alerta mínimos

- saldo divergente;
- saldo negativo;
- ledger ausente;
- pedido concedido sem débito;
- crédito sem validação;
- item concedido sem origem;
- compra repetida;
- volume anormal por usuário;
- muitas falhas de compra em curto período.

## 8. Fluxo principal

1. Rotina de reconciliação inicia por período ou lote.
2. Sistema compara carteira, ledger, pedidos e inventário.
3. Sistema registra divergências como alertas operacionais.
4. Admin visualiza alertas no painel.
5. Admin marca como analisado e registra nota.

## 9. Impacto no Backend

- Criar rotina de reconciliação em lote.
- Criar serviço de detecção de anomalia simples por regras.
- Integrar com alertas de segurança/admin.
- Garantir queries paginadas e seguras.

## 10. Impacto no Banco

- Índices para ledger por wallet/data/referência.
- Índices para pedidos por usuário/status/canal/data.
- Possível tabela de alertas de economia.

## 11. Impacto no Admin

- Exibir alertas de economia Gold.
- Filtros por tipo, severidade, usuário, período e status.
- Detalhe seguro com evidências e trilha.

## 12. Critérios de aceite

- Divergência entre saldo e ledger gera alerta.
- Pedido Gold concedido sem débito gera alerta.
- Crédito de Gold sem compra validada gera alerta.
- Item sem origem rastreável gera alerta.
- Volume anormal gera alerta.
- Admin consegue analisar alerta sem apagar evidência.
- Relatório não expõe dados sensíveis.

## 13. Critérios de teste para QA

- saldo consistente;
- saldo divergente;
- pedido sem débito;
- crédito sem origem;
- item sem origem;
- volume anormal;
- alerta analisado;
- relatório exportado.

## ✅ Decisão registrada

A economia Gold precisa de reconciliação e alertas preventivos para detectar inconsistência ou abuso mesmo quando as validações principais estiverem implementadas.