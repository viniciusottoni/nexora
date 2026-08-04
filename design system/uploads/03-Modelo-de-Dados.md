# 03 — Modelo de Dados
## Ecossistema Dona Betinha

| | |
|---|---|
| **Projeto** | 004_DonaBetinha |
| **Documento** | Modelo de Dados |
| **Versão** | 1.0 |
| **Data** | 31/07/2026 |
| **Banco** | PostgreSQL 16 · ORM Prisma |
| **Depende de** | `02-Arquitetura-Tecnica.md` |

---

## 1. Convenções

| Convenção | Regra |
|---|---|
| Nomes | `snake_case` no banco, `camelCase` no código (mapeado pelo Prisma) |
| Chave primária | `id UUID` (v7, ordenável por tempo) |
| Multi-tenant | Toda tabela de negócio tem `tenant_id UUID NOT NULL` |
| Auditoria de linha | `created_at`, `updated_at`, `created_by`, `updated_by` |
| Exclusão | **Soft delete** (`deleted_at`) — nunca DELETE físico em dado de negócio |
| Dinheiro | `NUMERIC(12,2)`; **nunca** float |
| Quantidade de insumo | `NUMERIC(14,4)` — precisão de gramas e mililitros |
| Datas | `TIMESTAMPTZ`, sempre UTC; conversão na apresentação |
| Enums | Enum nativo do PostgreSQL |
| Configuração flexível | `JSONB` com schema validado por Zod na aplicação |
| Índices | Todo índice de tabela multi-tenant começa por `tenant_id` |

---

## 2. Mapa de domínios

```
┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│  PLATAFORMA  │  │   CATÁLOGO   │  │   OPERAÇÃO   │  │   ESTOQUE    │
│ tenant       │  │ category     │  │ area         │  │ ingredient   │
│ tenant_config│  │ product      │  │ dining_table │  │ supplier     │
│ store        │  │ variant      │  │ table_session│  │ recipe       │
│ user / role  │  │ modifier_*   │  │ order        │  │ recipe_item  │
│ device       │  │ price        │  │ order_item   │  │ stock_movement│
│ audit_log    │  │ station      │  │ payment      │  │ purchase     │
└──────────────┘  └──────────────┘  │ cash_session │  │ inv_count    │
                                    └──────────────┘  └──────────────┘
┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│   DELIVERY   │  │  FINANCEIRO  │  │   EVENTOS    │  │   MÉTRICAS   │
│ customer     │  │ fin_account  │  │ domain_event │  │ metric_daily │
│ address      │  │ fin_entry    │  │ outbox       │  │ metric_hourly│
│ courier      │  │ expense_cat  │  │ sync_cursor  │  │ goal         │
│ delivery_run │  │ payroll      │  │              │  │ alert        │
└──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘
```

---

## 3. Domínio: Plataforma e identidade

### 3.1 Entidades

| Entidade | Descrição | Campos-chave |
|---|---|---|
| `tenant` | Estabelecimento (unidade de isolamento) | `slug`, `name`, `document`, `status`, `plan`, `timezone`, `locale`, `currency` |
| `tenant_config` | Configuração operacional e de marca | `branding JSONB`, `operation JSONB`, `thresholds JSONB`, `fiscal JSONB` |
| `store` | Loja física do tenant (preparado para rede) | `tenant_id`, `name`, `address`, `is_default` |
| `edge_installation` | Servidor local registrado | `store_id`, `public_key`, `version`, `last_seen_at`, `last_synced_seq` |
| `user` | Usuário do sistema | `tenant_id`, `name`, `email`, `password_hash`, `pin_hash`, `status` |
| `role` | Papel com permissões | `tenant_id`, `code`, `name`, `permissions JSONB`, `is_system` |
| `user_role` | Vínculo | `user_id`, `role_id`, `store_id` |
| `device` | Terminal autorizado | `tenant_id`, `store_id`, `label`, `type`, `fingerprint`, `station_id` |
| `audit_log` | Trilha imutável | `tenant_id`, `actor_id`, `action`, `entity`, `entity_id`, `before JSONB`, `after JSONB`, `device_id`, `occurred_at` |

### 3.2 Estrutura de `tenant_config.branding`

```json
{
  "logo": { "light": "url", "dark": "url" },
  "colors": {
    "primary": "#C1121F", "secondary": "#669BBC",
    "surface": "#FDF0D5", "onPrimary": "#FFFFFF"
  },
  "typography": { "family": "Inter", "scale": 1.0 },
  "radius": 12,
  "pwa": { "name": "Dona Betinha", "shortName": "Betinha", "icon": "url", "themeColor": "#C1121F" },
  "texts": { "welcome": "...", "orderConfirmed": "...", "thanks": "..." }
}
```

### 3.3 Estrutura de `tenant_config.operation`

```json
{
  "serviceFeePercent": 10,
  "serviceFeeOptional": true,
  "maxDiscountPercentWithoutApproval": 5,
  "halfAndHalfPricing": "HIGHEST",
  "maxFractions": 4,
  "requireTableSessionToOrder": true,
  "blockCloseWithPendingItems": true,
  "stockDeductionMoment": "ITEM_READY",
  "bottleneck": { "resource": "OVEN", "slots": 5, "avgCookMinutes": 7 },
  "channels": { "dineIn": true, "delivery": false, "takeout": true }
}
```

### 3.4 Estrutura de `tenant_config.thresholds`

```json
{
  "orderWarnMinutes": 12,
  "orderCriticalMinutes": 18,
  "itemInWindowMinutes": 2,
  "tableIdleMinutes": 10,
  "cashDivergenceAlert": 20.00,
  "cmvDivergencePercent": 5,
  "syncDelayMinutes": 5,
  "deliveryPromiseMinutes": 25,
  "dineInPromiseMinutes": 10
}
```

> Toda a diferença entre uma pizzaria e uma hamburgueria vive nesses três blocos JSONB. É o que permite a diretriz "configuração, nunca código".

---

## 4. Domínio: Catálogo

| Entidade | Descrição | Campos-chave |
|---|---|---|
| `category` | Categoria do cardápio | `tenant_id`, `name`, `sort_order`, `is_active`, `available_schedule JSONB` |
| `product` | Produto | `tenant_id`, `category_id`, `name`, `description`, `image_url`, `station_id`, `is_active`, `is_available`, `allows_fractions`, `max_fractions` |
| `product_variant` | Tamanho/variação | `product_id`, `name`, `sku`, `prep_minutes`, `is_default` |
| `price` | Preço por variação e canal | `variant_id`, `channel`, `amount`, `valid_from`, `valid_to` |
| `modifier_group` | Grupo de modificadores | `tenant_id`, `name`, `min_select`, `max_select`, `is_required` |
| `modifier` | Opção | `group_id`, `name`, `price_delta`, `is_available` |
| `product_modifier_group` | Vínculo produto ↔ grupo | `product_id`, `group_id`, `sort_order` |
| `station` | Praça de produção | `tenant_id`, `name`, `type` (OVEN, ASSEMBLY, FRY, BAR, DESSERT), `capacity_slots` |

### 4.1 Enums

```sql
CREATE TYPE channel AS ENUM ('DINE_IN','DELIVERY','TAKEOUT','MARKETPLACE');
CREATE TYPE station_type AS ENUM ('ASSEMBLY','OVEN','GRILL','FRY','BAR','DESSERT','OTHER');
CREATE TYPE half_pricing AS ENUM ('HIGHEST','AVERAGE','PROPORTIONAL');
```

### 4.2 Como o meio a meio é modelado

Um item de pedido pode ter **frações**. A tabela `order_item_fraction` liga o item a N variações com peso proporcional:

```
order_item (1 pizza grande)
  ├─ fraction 1 → variant: Mussarela G   (peso 0.5)
  └─ fraction 2 → variant: Calabresa G   (peso 0.5)
```

O preço aplica `tenant_config.operation.halfAndHalfPricing`. A **baixa de estoque respeita o peso**: metade dos insumos de cada ficha técnica. Isso é essencial para o CMV ficar correto em pizzaria — e é onde a maioria dos sistemas erra.

---

## 5. Domínio: Operação (núcleo)

### 5.1 Entidades

| Entidade | Descrição | Campos-chave |
|---|---|---|
| `area` | Ambiente do salão | `tenant_id`, `store_id`, `name` |
| `dining_table` | Mesa | `area_id`, `label`, `seats`, `qr_token`, `status` |
| `table_session` | Comanda / sessão de consumo | `table_id`, `opened_at`, `closed_at`, `status`, `guest_count`, `waiter_id`, `total_amount`, `service_fee_amount`, `rating` |
| `order` | Pedido | `tenant_id`, `store_id`, `session_id?`, `channel`, `code`, `status`, `customer_id?`, `address_id?`, `promised_at`, `subtotal`, `discount`, `delivery_fee`, `total`, timestamps T0–T5 |
| `order_item` | Item do pedido | `order_id`, `variant_id`, `quantity`, `unit_price`, `total_price`, `status`, `station_id`, `notes`, `fire_at`, timestamps T0–T5, `cancel_reason`, `refire_of_id` |
| `order_item_fraction` | Fração (meio a meio) | `order_item_id`, `variant_id`, `weight` |
| `order_item_modifier` | Modificador aplicado | `order_item_id`, `modifier_id`, `price_delta` |
| `payment` | Pagamento | `order_id?`, `session_id?`, `method`, `amount`, `status`, `provider`, `provider_ref`, `fee_amount`, `paid_at` |
| `cash_session` | Turno de caixa | `store_id`, `operator_id`, `opened_at`, `opening_amount`, `closed_at`, `counted_amount`, `expected_amount`, `divergence`, `status` |
| `cash_movement` | Sangria/suprimento | `cash_session_id`, `type`, `amount`, `reason`, `authorized_by` |

### 5.2 Enums de estado

```sql
CREATE TYPE order_status AS ENUM (
  'DRAFT','PLACED','IN_PRODUCTION','READY','DISPATCHED',
  'DELIVERED','CLOSED','CANCELLED'
);

CREATE TYPE order_item_status AS ENUM (
  'QUEUED','FIRED','IN_OVEN','OUT_OF_OVEN','READY',
  'SERVED','CANCELLED'
);

CREATE TYPE table_session_status AS ENUM (
  'OPEN','BILL_REQUESTED','PAID','CLOSED'
);

CREATE TYPE payment_method AS ENUM (
  'CASH','CREDIT','DEBIT','PIX','ONLINE','VOUCHER','OTHER'
);
```

### 5.3 Os carimbos de tempo (coração da métrica)

Presentes em `order_item` e agregados em `order`:

| Campo | Evento | Métrica derivada |
|---|---|---|
| `placed_at` (T0) | Pedido confirmado | Início do relógio |
| `fired_at` (T1) | Produção iniciada | Tempo de fila = T1 − T0 |
| `oven_in_at` (T2) | Entrada no gargalo | Tempo de montagem = T2 − T1 |
| `oven_out_at` (T3) | Saída do gargalo | Tempo de cocção = T3 − T2 |
| `ready_at` (T4) | Pronto para expedição | Tempo de finalização = T4 − T3 |
| `served_at` (T5) | Entregue / despachado | Tempo de expedição = T5 − T4 |

> `oven_in_at` e `oven_out_at` só se aplicam a itens roteados para estação do tipo gargalo. Para bebidas, T1 → T4 diretamente.

### 5.4 Índices críticos

```sql
CREATE INDEX idx_order_tenant_status   ON "order" (tenant_id, status)
  WHERE status IN ('PLACED','IN_PRODUCTION','READY');
CREATE INDEX idx_order_tenant_placed   ON "order" (tenant_id, placed_at DESC);
CREATE INDEX idx_item_station_status   ON order_item (tenant_id, station_id, status)
  WHERE status IN ('QUEUED','FIRED','IN_OVEN');
CREATE INDEX idx_session_open          ON table_session (tenant_id, status)
  WHERE status <> 'CLOSED';
CREATE INDEX idx_payment_session       ON payment (tenant_id, session_id);
```

Os índices parciais são deliberados: a fila da cozinha é consultada dezenas de vezes por minuto e só interessa o que está aberto.

---

## 6. Domínio: Estoque e ficha técnica

| Entidade | Descrição | Campos-chave |
|---|---|---|
| `unit_of_measure` | Unidade | `code` (KG, G, L, ML, UN), `base_code`, `factor` |
| `supplier` | Fornecedor | `tenant_id`, `name`, `document`, `contact`, `lead_time_days` |
| `ingredient` | Insumo | `tenant_id`, `name`, `uom_code`, `avg_cost`, `min_stock`, `current_stock`, `is_perishable`, `shelf_life_days` |
| `recipe` | Ficha técnica | `tenant_id`, `variant_id?`, `sub_recipe_of?`, `yield_qty`, `yield_uom` |
| `recipe_item` | Componente | `recipe_id`, `ingredient_id?`, `sub_recipe_id?`, `quantity`, `uom_code`, `waste_percent` |
| `stock_movement` | Movimento | `tenant_id`, `ingredient_id`, `type`, `quantity`, `unit_cost`, `reference_type`, `reference_id`, `occurred_at`, `reason` |
| `purchase` | Compra | `tenant_id`, `supplier_id`, `document`, `total`, `purchased_at` |
| `purchase_item` | Item da compra | `purchase_id`, `ingredient_id`, `quantity`, `unit_cost`, `expires_at` |
| `inventory_count` | Contagem cíclica | `tenant_id`, `counted_at`, `counted_by`, `status` |
| `inventory_count_item` | Item contado | `count_id`, `ingredient_id`, `expected_qty`, `counted_qty`, `divergence_qty`, `divergence_cost` |

```sql
CREATE TYPE stock_movement_type AS ENUM (
  'PURCHASE','PRODUCTION','WASTE','ADJUSTMENT','TRANSFER','RETURN','COUNT'
);

CREATE TYPE waste_reason AS ENUM (
  'BREAKAGE','EXPIRATION','PRODUCTION_ERROR','COURTESY','THEFT','OTHER'
);
```

### 6.1 Regra central: saldo é derivado, nunca escrito

`ingredient.current_stock` é um **campo materializado por conveniência de leitura**, recalculado a partir de `stock_movement`. Isso é o que elimina o conflito de sincronização (arquitetura, seção 6.4) e permite auditar qualquer divergência.

```sql
-- saldo real de um insumo
SELECT COALESCE(SUM(quantity), 0)
FROM stock_movement
WHERE tenant_id = $1 AND ingredient_id = $2;
```

### 6.2 Cálculo de custo do produto

```
custo(variant) = Σ  recipe_item.quantity
                  × (1 + waste_percent)
                  × ingredient.avg_cost
                  [+ custo recursivo das sub-receitas]
```

Para item meio a meio: `Σ (custo(variant_i) × weight_i)`.

### 6.3 CMV teórico × real

```
CMV teórico = Σ (custo do item produzido no período)      -- da ficha técnica
CMV real    = estoque inicial + compras − estoque final    -- da contagem
divergência = (real − teórico) ÷ teórico
```

Divergência acima do limiar dispara alerta ao gestor. **É a métrica mais reveladora do negócio.**

---

## 7. Domínio: Delivery

| Entidade | Descrição | Campos-chave |
|---|---|---|
| `customer` | Cliente final | `tenant_id`, `name`, `phone`, `email?`, `anonymized_at?` |
| `customer_address` | Endereço | `customer_id`, `label`, `street`, `number`, `complement`, `district`, `city`, `zip`, `lat`, `lng`, `reference` |
| `delivery_zone` | Região de entrega | `tenant_id`, `name`, `geometry JSONB`, `fee`, `avg_minutes`, `is_active` |
| `courier` | Entregador | `tenant_id`, `user_id?`, `name`, `phone`, `vehicle`, `is_own` |
| `delivery_run` | Rota (agrupamento) | `tenant_id`, `courier_id`, `dispatched_at`, `returned_at` |
| `delivery_stop` | Parada da rota | `run_id`, `order_id`, `sequence`, `delivered_at`, `outcome`, `outcome_reason` |

---

## 8. Domínio: Financeiro

| Entidade | Descrição | Campos-chave |
|---|---|---|
| `financial_account` | Conta (caixa, banco, adquirente) | `tenant_id`, `name`, `type`, `balance` |
| `expense_category` | Categoria de despesa | `tenant_id`, `name`, `group` (FIXED, VARIABLE, PAYROLL, TAX) |
| `financial_entry` | Lançamento | `tenant_id`, `account_id`, `category_id?`, `type` (REVENUE/EXPENSE), `amount`, `competence_date`, `due_date`, `paid_at`, `reference_type`, `reference_id`, `is_recurring`, `recurrence JSONB` |
| `employee` | Funcionário | `tenant_id`, `user_id?`, `name`, `role_title`, `salary`, `hired_at`, `terminated_at?` |
| `payroll` | Folha do período | `tenant_id`, `period`, `total_gross`, `total_charges`, `total_net`, `status` |
| `payroll_item` | Item da folha | `payroll_id`, `employee_id`, `gross`, `charges`, `net` |

### 8.1 Ligação automática venda → financeiro

Todo `payment` confirmado gera um `financial_entry` de receita, com `reference_type = 'PAYMENT'`. Toda `purchase` gera despesa. Todo `payroll` gera despesa. **O financeiro não é digitado — é derivado.** Isso cumpre o princípio "o dado nasce do trabalho".

---

## 9. Domínio: Eventos e sincronização

| Entidade | Descrição | Campos-chave |
|---|---|---|
| `domain_event` | Log append-only | `id`, `tenant_id`, `store_id`, `type`, `version`, `aggregate_type`, `aggregate_id`, `payload JSONB`, `actor_id`, `device_id`, `origin` (EDGE/CLOUD), `device_seq`, `occurred_at`, `recorded_at` |
| `outbox` | Fila de saída (edge) | `event_id`, `status`, `attempts`, `last_error`, `synced_at` |
| `sync_cursor` | Posição de leitura | `installation_id`, `direction`, `last_seq`, `updated_at` |
| `sync_conflict` | Conflito registrado | `tenant_id`, `event_id`, `reason`, `resolution`, `reviewed_by`, `reviewed_at` |

```sql
CREATE TABLE domain_event (
  id            UUID PRIMARY KEY,
  tenant_id     UUID NOT NULL,
  store_id      UUID,
  type          TEXT NOT NULL,
  version       SMALLINT NOT NULL DEFAULT 1,
  aggregate_type TEXT NOT NULL,
  aggregate_id  UUID NOT NULL,
  payload       JSONB NOT NULL,
  actor_id      UUID,
  device_id     UUID,
  origin        TEXT NOT NULL,
  device_seq    BIGINT,
  occurred_at   TIMESTAMPTZ NOT NULL,
  recorded_at   TIMESTAMPTZ NOT NULL DEFAULT now()
) PARTITION BY RANGE (occurred_at);

CREATE INDEX idx_event_tenant_time ON domain_event (tenant_id, occurred_at DESC);
CREATE INDEX idx_event_aggregate   ON domain_event (tenant_id, aggregate_type, aggregate_id);
CREATE UNIQUE INDEX idx_event_dedup ON domain_event (id);
```

**Particionamento mensal** por `occurred_at` — o volume esperado (uma pizzaria média gera 3 a 8 mil eventos/dia) torna isso necessário a partir do segundo ano.

> **Regra inviolável:** `occurred_at` é o horário do fato, na origem. `recorded_at` é quando chegou. Toda métrica de horário usa `occurred_at` — senão a instabilidade de internet corrompe todo o indicador de pico (RN-020).

---

## 10. Domínio: Métricas e alertas

| Entidade | Descrição | Campos-chave |
|---|---|---|
| `metric_hourly` | Agregado por hora | `tenant_id`, `store_id`, `hour`, `channel`, `orders`, `revenue`, `items`, `avg_total_seconds`, `p90_total_seconds`, `on_time_count`, `late_count` |
| `metric_daily` | Agregado por dia | idem + `covers`, `table_turns`, `avg_ticket`, `cmv_theoretical`, `labor_cost` |
| `metric_product_daily` | Por produto/dia | `variant_id`, `qty`, `revenue`, `cost`, `margin`, `avg_prep_seconds` |
| `metric_operator_daily` | Por operador/dia | `user_id`, `orders`, `revenue`, `avg_serve_seconds`, `upsell_accepted` |
| `goal` | Meta definida | `tenant_id`, `metric_code`, `target_value`, `period`, `valid_from`, `valid_to` |
| `alert` | Alerta gerado | `tenant_id`, `type`, `severity`, `entity_type`, `entity_id`, `payload JSONB`, `raised_at`, `acknowledged_at`, `acknowledged_by` |

### 10.1 Como os agregados são mantidos

Worker consome `domain_event` e atualiza incrementalmente `metric_hourly`. Um job noturno recalcula o dia fechado (garante correção após eventos sincronizados com atraso). Os agregados são **recalculáveis a partir do zero** — se um bug corromper um número, reprocessa-se o event store.

---

## 11. Row Level Security

Aplicado a **todas** as tabelas com `tenant_id`:

```sql
ALTER TABLE "order" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "order" FORCE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation ON "order"
  USING (tenant_id = current_setting('app.tenant_id', true)::uuid)
  WITH CHECK (tenant_id = current_setting('app.tenant_id', true)::uuid);
```

Middleware da aplicação, antes de qualquer query:

```ts
await prisma.$executeRaw`SELECT set_config('app.tenant_id', ${tenantId}, true)`;
```

O papel de plataforma usa role de banco com `BYPASSRLS`, **exclusivamente** em rotas de administração e sempre com registro em `audit_log`.

### 11.1 Proteção da auditoria

```sql
REVOKE UPDATE, DELETE ON audit_log FROM app_user;
REVOKE UPDATE, DELETE ON domain_event FROM app_user;
```

Auditoria que pode ser alterada não é auditoria (RF-AUD-04).

---

## 12. Estratégia de retenção

| Dado | Retenção quente | Depois |
|---|---|---|
| `domain_event` | 24 meses | Arquivamento em object storage (Parquet) |
| `metric_*` | Indefinida | Mantida (volume baixo) |
| `audit_log` | 5 anos | Arquivamento frio |
| Dados de cliente final | 24 meses sem pedido | Anonimização (LGPD) |
| Outbox sincronizado (edge) | 30 dias | Purga |
| Backup de banco | 30 dias PITR | Snapshot mensal por 12 meses |

---

## 13. Migrations e evolução

| Regra | Motivo |
|---|---|
| Toda alteração de schema é uma migration Prisma versionada | Reprodutibilidade em N lojas |
| Migrations devem ser compatíveis para trás por uma versão | Edge pode estar desatualizado |
| Adicionar coluna: sempre `NULLABLE` ou com default | Evita lock em tabela grande |
| Remover coluna: em duas versões (parar de usar → remover) | Zero downtime |
| Migration de dados roda como job, não em migration de schema | Evita travar deploy |
| Toda migration testada contra dump de produção antes do parque | Segurança do parque instalado |

---

## 14. Volumetria estimada (pizzaria média)

| Métrica | Estimativa |
|---|---|
| Pedidos/dia | 120 – 250 |
| Itens/dia | 300 – 700 |
| Eventos/dia | 3.000 – 8.000 |
| Eventos/ano | ~2 milhões |
| Banco local após 12 meses | < 5 GB |
| Banco de nuvem, 50 lojas, 24 meses | ~200 GB |

> Volume perfeitamente confortável para PostgreSQL. A complexidade do projeto está na sincronização e na correção do dado, não em escala.

---

*Documento 03 do pacote 004_DonaBetinha. Replay Studio.*
