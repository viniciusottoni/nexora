# ADR-023 — Fundação de economia: carteira de Gold, rastreamento unificado de compras e auditoria

Status: Aceito

## Contexto

A ADR-022 implementou o menor recorte possível de inventário e loja para sustentar o fluxo do "Pergaminho da Reforja" (US-048): `InventoryItem` genérico, `ShopCatalog` estático em código com preço em "Gold", e uma compra **mock** (`POST /api/shop/items/{itemKey}/purchase`) que apenas incrementa o inventário, sem dedução de moeda nem pagamento real. A própria ADR-022 deferiu explicitamente para "uma US de economia futura":

- moeda real/IAP para consumíveis (saldo de Gold real e sua emissão);
- catálogo completo de itens, equipáveis e raridades com efeito de jogo;
- inventário com dados reais do backend nas telas Flutter.

O EPIC-018 antecipou partes disso como hardening pós-MVP (US-178 catálogo, US-179 IAP, US-180 RBAC), mas sem uma fundação coesa: não existe carteira/saldo de Gold, não há ledger de movimentação de moeda, as compras não passam por auditoria (`AuditLog`/`IAuditLogService` hoje cobre trial, entitlement, legal e exclusão de conta — não economia), e não há rastreamento unificado de compras para suporte e finanças.

O EPIC-019 cria essa fundação. Esta ADR registra as decisões estruturais que sustentam toda a economia futura **sem** introduzir itens concretos nem regras de itens (efeitos, emissão, sinks, balanceamento), que ficam para um épico posterior de catálogo/itens.

## Decisão

1. **Carteira de Gold como contêiner de saldo, separada da emissão.**
   Criar `GoldWallet` (`UserId`, `Balance`, concorrência otimista) e `GoldLedgerEntry` (lançamento imutável: `WalletId`, `Direction` credit/debit, `Amount`, `Reason`, `ReferenceType`, `ReferenceId`, `BalanceAfter`, `CorrelationId`, `CreatedAtUtc`). O saldo é sempre derivável/reconciliável pelo ledger. **Esta ADR não define como o Gold é emitido** (ganho por quest/streak) nem o que ele compra — apenas a estrutura que guarda e movimenta saldo. O backend é a única autoridade do saldo (coerente com ADR-009).

2. **Rastreamento de compras unificado.**
   Toda compra na loja — em Gold (interna) ou via IAP/RevenueCat (dinheiro real) — gera um registro em uma trilha de pedido (`ShopOrder`: `UserId`, `Channel` gold/iap, `ProductKey`, `Status` pending/granted/failed/refunded, `ExternalTransactionId?`, `CorrelationId`, timestamps). O `iap_transaction_ledger` existente (ADR-022/EPIC-018) permanece como detalhe específico de IAP e é referenciado pelo pedido; o débito de Gold gera um `GoldLedgerEntry`. A idempotência segue ADR-010: chave por `ExternalTransactionId` (IAP) ou por chave de pedido (Gold).

3. **Auditoria obrigatória de toda mutação de economia.**
   Compra iniciada, concessão de benefício, débito/crédito de Gold, e consumo de item são registrados via `IAuditLogService.RecordAsync` com `ResourceType` dedicado (`ShopOrder`, `GoldWallet`, `InventoryItem`), `Action` estável, `ActorType` (User/System/Admin) e `MetadataSafe` sem dados sensíveis (ADR-015). Auditoria e rastreamento são requisito de aceite, não follow-up.

4. **Catálogo orientado a dados, vazio na fundação.**
   O `ShopCatalog` estático em código (ADR-022) é substituído por catálogo carregado de dados (tabela `shop_products` já existente + preço/Gold quando aplicável). Na entrega do EPIC-019 o catálogo nasce **vazio ou apenas com os itens já existentes da ADR-022 marcados como legados**; adicionar itens reais é só dado/configuração, sem mudança de schema.

5. **UI estrutural, sem itens.**
   As telas Flutter de loja, inventário, carteira e extrato passam a ler do backend e a tratar loading/erro/vazio de verdade. Com catálogo vazio, exibem empty state honesto (sem mocks enganosos). Nenhum item decorativo falso permanece.

## Fora de escopo (deferido para épico de itens)

- Itens concretos, equipáveis, cosméticos e suas raridades com efeito real.
- Regras de efeito/consumo de cada item e regras de emissão de Gold (quanto se ganha por quest/streak).
- Balanceamento de economia, temporadas/passe e promoções.
- Marketplace entre usuários.

## Consequências

A economia ganha uma fundação reconciliável e auditável: todo Gold movimentado tem lançamento, toda compra tem pedido rastreável com status, e toda mutação é auditada — pré-requisito para suporte, finanças e LGPD. Itens reais e suas regras entram depois apenas como dados + handlers de efeito, sem tocar a estrutura. O custo é a antecipação de modelagem (carteira, ledger, pedido) antes de existir item monetizável, aceito por desbloquear todo o resto e por evitar retrabalho na compra mock.

## Critérios de aceite

- Existem `gold_wallets` (único por `UserId`) e `gold_ledger_entries` (append-only, com `BalanceAfter`).
- Existe `shop_orders` rastreando canal, produto, status e correlação; idempotente por transação/pedido.
- Débito de Gold nunca deixa saldo negativo e sempre gera lançamento no ledger.
- Toda compra, concessão, movimentação de saldo e consumo de item gera `AuditLog` com `MetadataSafe` sanitizado.
- O catálogo é carregado de dados; a entrega não contém itens fictícios nem preço Gold hardcoded em código de domínio.
- As telas de loja/inventário/carteira/extrato leem do backend e exibem empty state quando não há itens.
