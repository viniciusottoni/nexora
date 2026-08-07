# Domínio — ERDs e DDLs
## Ecossistema Nexora · Projeto 004_DonaBetinha

| | |
|---|---|
| **Projeto** | 004_DonaBetinha |
| **Documento** | Modelo físico de dados — ERDs e DDLs |
| **Versão** | 1.0 |
| **Data** | 31/07/2026 |
| **Banco** | PostgreSQL 16 |
| **ORM** | Entity Framework Core (Npgsql) (mapeamento no documento 13) |
| **Depende de** | `Docs/03-Modelo-de-Dados.md`, `Docs/ADRs/` |

---

## O que é este conjunto

Estes documentos são o **modelo físico executável** do sistema. Contêm o DDL real, na ordem correta de execução, com ERDs por contexto delimitado.

Diferença em relação ao `Docs/03-Modelo-de-Dados.md`: aquele é a **visão conceitual** (que entidades existem e por quê); este é o **contrato de implementação** (como elas existem no banco, com tipos, constraints, índices e políticas).

Em caso de divergência entre os dois, **prevalece este conjunto**.

---

## Documentos

| # | Documento | Conteúdo |
|---|---|---|
| **00** | [Convenções e tipos](00-Convencoes-e-Tipos.md) | Extensões, domínios, enums globais, funções utilitárias, convenções |
| **01** | [Plataforma e identidade](01-Plataforma-e-Identidade.md) | `tenant`, `tenant_config`, `store`, `edge_installation`, `app_user`, `role`, `device`, `audit_log`, `tenant_secret` |
| **02** | [Catálogo](02-Catalogo.md) | `category`, `product`, `product_variant`, `price`, `modifier_group`, `modifier`, `station`, `media_asset` |
| **03** | [Operação](03-Operacao.md) | `area`, `dining_table`, `table_session`, `order`, `order_item`, `order_item_fraction`, `order_item_modifier` |
| **04** | [Caixa e pagamento](04-Caixa-e-Pagamento.md) | `cash_session`, `cash_movement`, `payment`, `payment_allocation` |
| **05** | [Estoque e ficha técnica](05-Estoque-e-Ficha-Tecnica.md) | `ingredient`, `supplier`, `recipe`, `recipe_item`, `stock_movement`, `purchase`, `inventory_count` |
| **06** | [Delivery](06-Delivery.md) | `customer`, `customer_address`, `delivery_zone`, `courier`, `delivery_run`, `delivery_stop` |
| **07** | [Financeiro](07-Financeiro.md) | `financial_account`, `expense_category`, `financial_entry`, `employee`, `payroll` |
| **08** | [Eventos e sincronização](08-Eventos-e-Sincronizacao.md) | `domain_event` (particionada), `outbox`, `sync_cursor`, `sync_conflict`, `idempotency_key` |
| **09** | [Métricas e alertas](09-Metricas-e-Alertas.md) | `metric_hourly`, `metric_daily`, `metric_product_daily`, `metric_operator_daily`, `goal`, `alert` |
| **10** | [RLS, papéis e índices](10-RLS-Papeis-e-Indices.md) | Políticas de isolamento, papéis de banco, grants, índices consolidados |
| **11** | [Views e funções](11-Views-e-Funcoes.md) | Funções de negócio, views de leitura, triggers |
| **12** | [Seeds e dados iniciais](12-Seeds-e-Dados-Iniciais.md) | Unidades, papéis padrão, modelo de pizzaria |
| **13** | [Mapeamento EF Core](13-Mapeamento-EFCore.md) | `DbContext`, `IEntityTypeConfiguration<T>` equivalentes e notas de integração |
| **14** | [Plataforma em escala](14-Plataforma-em-Escala.md) | `installation_incident`, `tenant_domain`, `support_access`, `onboarding_step`, `release`, `business_template` (E-14) |
| **15** | [Gestão geral da plataforma](15-Gestao-Geral-da-Plataforma.md) | `tenant.owner_email`, `tenant.template_code` — diretório de estabelecimentos (E-15/US-151) |
| **ERD** | [ERD consolidado](ERD-Consolidado.md) | Visão geral e por contexto |

---

## Ordem de execução do DDL

A ordem importa por causa das chaves estrangeiras:

```
00  extensões, domínios, enums, funções
01  plataforma e identidade
02  catálogo
03  operação
04  caixa e pagamento
05  estoque
06  delivery
07  financeiro
08  eventos e sincronização
09  métricas e alertas
10  RLS, papéis, grants, índices
11  views, funções de negócio, triggers
12  seeds
14  plataforma em escala (E-14 — instalações/saúde, domínio próprio, suporte, onboarding, releases, modelos)
15  gestão geral da plataforma (E-15 — diretório de estabelecimentos: owner_email/template_code em tenant)
```

No repositório, isso vira migrations do EF Core (`dotnet ef migrations add`) na mesma sequência (ADR-019).

---

## Convenções resumidas

| Item | Regra | ADR |
|---|---|---|
| Chave primária | `UUID` v7, gerado na origem | [016](../ADRs/ADR-016-identificadores-e-codigos.md) |
| Multi-tenant | `tenant_id UUID NOT NULL` em toda tabela de negócio + RLS | [004](../ADRs/ADR-004-postgresql-rls-multitenancy.md) |
| Dinheiro | `money_amount` = `NUMERIC(12,2)` | [017](../ADRs/ADR-017-representacao-monetaria.md) |
| Quantidade de insumo | `qty_amount` = `NUMERIC(14,4)` | [017](../ADRs/ADR-017-representacao-monetaria.md) |
| Data e hora | `TIMESTAMPTZ`, sempre UTC | [018](../ADRs/ADR-018-fuso-horario-e-dia-operacional.md) |
| Agregação de negócio | `business_day DATE` materializado | [018](../ADRs/ADR-018-fuso-horario-e-dia-operacional.md) |
| Exclusão | Soft delete (`deleted_at`) — nunca `DELETE` físico | — |
| Nomes | `snake_case`, singular | — |
| Enum | Tipo nativo do PostgreSQL | — |
| Configuração flexível | `JSONB` validado por FluentValidation na aplicação | [032](../ADRs/ADR-032-configuracao-e-feature-flags.md) |
| Índice multi-tenant | Sempre começa por `tenant_id` | — |

### Duas correções em relação ao `Docs/03-Modelo-de-Dados.md`

| Item | Motivo |
|---|---|
| `user` → **`app_user`** | `user` é palavra reservada no PostgreSQL; exigiria aspas em toda consulta |
| `price` ganhou `channel` na chave única | Necessário para preço por canal sem ambiguidade (RF-CAT-06) |

---

## Onde cada requisito vive

| Requisito | Tabelas |
|---|---|
| Pedido chega à cozinha | `order`, `order_item`, `station` |
| Métrica de tempo | `order_item` (T0–T5), `domain_event`, `metric_*` |
| Ficha técnica e CMV | `recipe`, `recipe_item`, `stock_movement`, `ingredient` |
| Saúde financeira | `financial_entry`, `payroll`, `metric_daily` |
| Offline e sincronização | `domain_event`, `outbox`, `sync_cursor` |
| Auditoria | `audit_log` |
| Multi-estabelecimento | `tenant`, `tenant_config`, RLS |

---

*Replay Studio — Projeto 004_DonaBetinha.*
