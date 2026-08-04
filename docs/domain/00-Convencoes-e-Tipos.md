# 00 — Convenções, tipos e funções base

| | |
|---|---|
| **Ordem de execução** | 1 de 12 |
| **Depende de** | — |
| **ADRs** | [016](../ADRs/ADR-016-identificadores-e-codigos.md), [017](../ADRs/ADR-017-representacao-monetaria.md), [018](../ADRs/ADR-018-fuso-horario-e-dia-operacional.md) |

---

## 1. Extensões

```sql
CREATE EXTENSION IF NOT EXISTS "pgcrypto";     -- gen_random_uuid, digest
CREATE EXTENSION IF NOT EXISTS "citext";       -- e-mail case-insensitive
CREATE EXTENSION IF NOT EXISTS "btree_gist";   -- constraint de exclusão (mesa/sessão)
CREATE EXTENSION IF NOT EXISTS "pg_trgm";      -- busca por similaridade em cardápio
```

> **UUIDv7** é gerado pela aplicação (ADR-016), não pelo banco. `gen_random_uuid()` (v4) é usado apenas como default defensivo em tabelas de infraestrutura, nunca em entidade de negócio.

---

## 2. Domínios de tipo

Domínios em vez de tipos crus tornam a intenção explícita e permitem alterar precisão em um único lugar.

```sql
-- Dinheiro: 2 casas, sempre. NUNCA float. (ADR-017)
CREATE DOMAIN money_amount AS NUMERIC(12,2);

-- Quantidade de insumo: 4 casas (grama, mililitro). (ADR-017)
CREATE DOMAIN qty_amount AS NUMERIC(14,4);

-- Percentual: 3 casas, 0 a 100
CREATE DOMAIN percent_amount AS NUMERIC(6,3)
  CHECK (VALUE >= 0 AND VALUE <= 100);

-- Peso de fração de produto (meio a meio): 0 a 1
CREATE DOMAIN fraction_weight AS NUMERIC(5,4)
  CHECK (VALUE > 0 AND VALUE <= 1);

-- Código curto legível
CREATE DOMAIN short_code AS VARCHAR(8);

-- Slug de URL
CREATE DOMAIN slug AS VARCHAR(64)
  CHECK (VALUE ~ '^[a-z0-9]+(-[a-z0-9]+)*$');

-- E-mail
CREATE DOMAIN email AS CITEXT
  CHECK (VALUE ~ '^[^@\s]+@[^@\s]+\.[^@\s]+$');
```

---

## 3. Enums globais

```sql
-- Canal de venda
CREATE TYPE channel AS ENUM ('DINE_IN','DELIVERY','TAKEOUT','MARKETPLACE');

-- Ciclo de vida do pedido (ADR-006, doc. 04 §4.1)
CREATE TYPE order_status AS ENUM (
  'DRAFT','PLACED','IN_PRODUCTION','READY',
  'DISPATCHED','DELIVERED','CLOSED','CANCELLED'
);

-- Ciclo de vida do item — onde nasce a métrica (doc. 04 §4.2)
CREATE TYPE order_item_status AS ENUM (
  'QUEUED','FIRED','IN_OVEN','OUT_OF_OVEN','READY','SERVED','CANCELLED'
);

-- Sessão de mesa
CREATE TYPE table_session_status AS ENUM ('OPEN','BILL_REQUESTED','PAID','CLOSED');

-- Estado da mesa
CREATE TYPE table_status AS ENUM ('FREE','OCCUPIED','RESERVED','BLOCKED');

-- Praça de produção
CREATE TYPE station_type AS ENUM ('ASSEMBLY','OVEN','GRILL','FRY','BAR','DESSERT','OTHER');

-- Regra de preço do meio a meio (RF-CAT-05)
CREATE TYPE half_pricing_rule AS ENUM ('HIGHEST','AVERAGE','PROPORTIONAL');

-- Pagamento
CREATE TYPE payment_method AS ENUM ('CASH','CREDIT','DEBIT','PIX','ONLINE','VOUCHER','OTHER');
CREATE TYPE payment_status AS ENUM ('PENDING','AUTHORIZED','PAID','REFUNDED','FAILED','CANCELLED');

-- Caixa
CREATE TYPE cash_session_status AS ENUM ('OPEN','CLOSING','CLOSED');
CREATE TYPE cash_movement_type  AS ENUM ('WITHDRAWAL','SUPPLY');

-- Estoque (ADR-008)
CREATE TYPE stock_movement_type AS ENUM (
  'PURCHASE','PRODUCTION','WASTE','ADJUSTMENT','TRANSFER','RETURN','COUNT'
);
CREATE TYPE waste_reason AS ENUM (
  'BREAKAGE','EXPIRATION','PRODUCTION_ERROR','COURTESY','THEFT','OTHER'
);

-- Financeiro
CREATE TYPE financial_entry_type AS ENUM ('REVENUE','EXPENSE');
CREATE TYPE expense_group        AS ENUM ('FIXED','VARIABLE','PAYROLL','TAX','OTHER');

-- Delivery
CREATE TYPE delivery_stop_status AS ENUM ('PENDING','ASSIGNED','IN_TRANSIT','DELIVERED','FAILED');
CREATE TYPE delivery_outcome     AS ENUM ('DELIVERED','CUSTOMER_ABSENT','WRONG_ADDRESS','REFUSED','OTHER');

-- Plataforma
CREATE TYPE tenant_status   AS ENUM ('TRIAL','ACTIVE','SUSPENDED','CANCELLED');
CREATE TYPE user_status     AS ENUM ('ACTIVE','INACTIVE','BLOCKED','INVITED'); -- INVITED: doc 12 §8 (US-002)
CREATE TYPE device_type     AS ENUM ('POS','KDS','WAITER','TABLET','PRINTER_HOST','OTHER');
CREATE TYPE event_origin    AS ENUM ('EDGE','CLOUD');
CREATE TYPE alert_severity  AS ENUM ('INFO','WARNING','HIGH','CRITICAL');
CREATE TYPE fiscal_status   AS ENUM ('NONE','PENDING','ISSUED','CANCELLED','REJECTED');
```

---

## 4. Funções utilitárias

### 4.1 Dia operacional (ADR-018)

A função mais importante deste conjunto. Uma pizzaria que fecha às 2h precisa que o pedido de 1h30 de sábado conte como sexta.

```sql
CREATE OR REPLACE FUNCTION business_day(
  p_occurred_at TIMESTAMPTZ,
  p_timezone    TEXT,
  p_start_hour  INT DEFAULT 5
) RETURNS DATE
LANGUAGE sql IMMUTABLE AS $$
  SELECT ((p_occurred_at AT TIME ZONE p_timezone) - make_interval(hours => p_start_hour))::date;
$$;

COMMENT ON FUNCTION business_day IS
  'Dia operacional conforme ADR-018. Pedido às 01h30 com virada às 5h pertence ao dia anterior.';
```

Verificação rápida:

```sql
-- pizzaria (virada 5h), pedido à 01h30 de sábado 01/08
SELECT business_day('2026-08-01 04:30:00+00', 'America/Sao_Paulo', 5);
-- → 2026-07-31  (sexta-feira) ✔
```

### 4.2 Atualização de `updated_at`

```sql
CREATE OR REPLACE FUNCTION set_updated_at() RETURNS TRIGGER
LANGUAGE plpgsql AS $$
BEGIN
  NEW.updated_at := now();
  RETURN NEW;
END;
$$;
```

### 4.3 Contexto do tenant (ADR-004)

```sql
CREATE OR REPLACE FUNCTION current_tenant_id() RETURNS UUID
LANGUAGE sql STABLE AS $$
  SELECT NULLIF(current_setting('app.tenant_id', true), '')::uuid;
$$;
```

Retorna `NULL` quando não há contexto — e a política RLS, ao comparar com `NULL`, não retorna nenhuma linha. **Falha fechada**, que é o comportamento seguro.

### 4.4 Bloqueio de alteração (auditoria e eventos)

```sql
CREATE OR REPLACE FUNCTION prevent_mutation() RETURNS TRIGGER
LANGUAGE plpgsql AS $$
BEGIN
  RAISE EXCEPTION 'Tabela % é append-only (%). Correção se faz com registro compensatório.',
    TG_TABLE_NAME, TG_OP;
END;
$$;
```

---

## 5. Colunas padrão

Todas as tabelas de negócio carregam:

```sql
  id          UUID        PRIMARY KEY,                    -- UUIDv7 da aplicação
  tenant_id   UUID        NOT NULL REFERENCES tenant(id),
  created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
  created_by  UUID,                                        -- app_user.id
  updated_by  UUID,
  deleted_at  TIMESTAMPTZ                                  -- soft delete
```

Regras:

| Regra | Motivo |
|---|---|
| `deleted_at IS NULL` em toda consulta de negócio | Soft delete |
| Índice único parcial com `WHERE deleted_at IS NULL` | Permite reaproveitar nome de registro excluído |
| `created_by`/`updated_by` sem FK obrigatória | Evita bloqueio ao inativar usuário |
| Trigger `set_updated_at` em toda tabela mutável | Consistência |

---

## 6. Convenção de nomes

| Objeto | Padrão | Exemplo |
|---|---|---|
| Tabela | `snake_case`, singular | `order_item` |
| Chave primária | `id` | — |
| Chave estrangeira | `<tabela>_id` | `product_id` |
| Índice | `idx_<tabela>_<colunas>` | `idx_order_tenant_placed` |
| Único | `uq_<tabela>_<colunas>` | `uq_order_short_code` |
| Check | `ck_<tabela>_<regra>` | `ck_order_total_positive` |
| FK | `fk_<tabela>_<destino>` | `fk_order_item_order` |
| Política RLS | `tenant_isolation` | — |
| Trigger | `trg_<tabela>_<ação>` | `trg_order_set_updated_at` |

Palavras reservadas em uso: `order` (aspas obrigatórias), `user` → renomeado para `app_user`.
