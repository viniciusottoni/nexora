---
title: US-227 — Garantir atomicidade entre Gold, pedido e inventário
sidebar_position: 227
---

# US-227 — Garantir atomicidade entre Gold, pedido e inventário

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-227 |
| Épico | EPIC-020 — Hardening de Segurança, Performance e Escalabilidade do MVP |
| Prioridade | P0 |
| Fase | Bloqueador do MVP em produção com loja Gold |
| Perfil principal | Usuário comprador, Backend, Banco, Loja, Inventário e QA |
| Plano | Trial, Mensal e Anual |
| Dependência principal | PurchaseWithGold, GoldWalletService, InventoryService, ShopOrder, UnitOfWork |
| Status | Planejada |

## 2. História do usuário

Como **usuário que usa Gold para comprar itens do jogo**,

quero **que débito de Gold, pedido e concessão do item aconteçam juntos ou não aconteçam**,

para **nunca perder Gold sem receber o item e nunca receber item sem pagar**.

## 3. Contexto

O fluxo atual de compra com Gold cria o pedido, salva, debita a carteira, salva, incrementa inventário, salva, e só depois marca o pedido como concedido. Esse desenho é rastreável, mas ainda precisa de uma transação atômica envolvendo débito, ledger, inventário e status final do pedido. Sem isso, uma falha intermediária pode deixar saldo debitado com pedido falho ou item concedido em estado inconsistente.

## 4. Objetivo

Garantir consistência transacional forte para toda compra com Gold.

## 5. Escopo

### Entra nesta US

- Executar débito de Gold, ledger, inventário e status do pedido em uma única transação de banco.
- Garantir que falha intermediária faça rollback do conjunto.
- Implementar retry seguro para conflito de concorrência controlado.
- Garantir idempotência por pedido/chave de compra.
- Impedir concessão dupla em reenvio de request.
- Adicionar token de concorrência ou operação atômica no inventário.
- Criar testes de concorrência com compras simultâneas.

### Fora desta US

- Transferência de Gold entre usuários.
- Marketplace entre jogadores.
- Reembolso manual avançado.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Nenhum item pode ser concedido sem débito confirmado na mesma transação. |
| RN-002 | Nenhum débito pode permanecer se a concessão do item falhar. |
| RN-003 | Repetição da mesma compra não pode conceder item duas vezes. |
| RN-004 | Compra simultânea não pode deixar saldo negativo. |
| RN-005 | Conflito de concorrência deve ser tratado com retry controlado ou erro amigável. |
| RN-006 | Inventário não pode perder incremento em compras simultâneas. |
| RN-007 | Pedido deve terminar em status coerente: granted ou failed, com evidência. |

## 7. Fluxo principal

1. Usuário solicita compra de item com Gold.
2. Backend valida produto ativo e preço server-side.
3. Backend abre transação de banco.
4. Backend cria ou recupera pedido idempotente.
5. Backend debita Gold e cria ledger.
6. Backend concede item no inventário.
7. Backend marca pedido como concedido.
8. Backend confirma transação.
9. Backend retorna pedido e saldo atualizado.

## 8. Fluxos alternativos

- Saldo insuficiente: pedido falha sem débito e sem item.
- Falha ao conceder item: rollback de débito, ledger e pedido.
- Conflito de concorrência: retry seguro ou erro controlado.
- Request repetida: retorna pedido existente sem nova concessão.

## 9. Impacto no Backend

- Ajustar `PurchaseWithGoldCommandHandler` para unidade transacional única.
- Evitar `SaveChanges` intermediários não protegidos por transação comum.
- Ajustar `GoldWalletService` e `InventoryService` para cooperarem com transação externa.
- Adicionar retry controlado para concorrência otimista.

## 10. Impacto no Banco

- Manter `GoldWallet` com controle de concorrência.
- Adicionar controle de concorrência ou incremento atômico no inventário.
- Avaliar índice/idempotency key para pedido Gold.

## 11. Impacto no Flutter

- Tratar resposta de conflito/compra em processamento de forma amigável.
- Evitar múltiplos taps no botão de compra.
- Sempre atualizar carteira e inventário a partir do backend.

## 12. Critérios de aceite

- Falha no inventário não deixa Gold debitado.
- Falha no débito não concede item.
- Compra simultânea não deixa saldo negativo.
- Compra repetida não duplica item.
- Conflito de concorrência retorna erro controlado ou retry bem-sucedido.
- Testes automatizados cobrem falhas intermediárias e concorrência.

## 13. Critérios de teste para QA

- compra normal;
- saldo insuficiente;
- falha simulada após débito;
- falha simulada após concessão;
- duas compras simultâneas do mesmo usuário;
- reenvio da mesma request;
- inventário já existente;
- inventário inexistente.

## ✅ Decisão registrada

Compra com Gold precisa ser atomicamente consistente: saldo, ledger, pedido e inventário devem confirmar juntos ou falhar juntos.