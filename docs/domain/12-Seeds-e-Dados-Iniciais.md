# 12 — Seeds e dados iniciais

| | |
|---|---|
| **Ordem de execução** | Após todo o DDL |
| **ADRs** | [013](../ADRs/ADR-013-proibicao-de-codigo-por-cliente.md), [023](../ADRs/ADR-023-modelo-de-autorizacao.md), [032](../ADRs/ADR-032-configuracao-e-feature-flags.md) |

---

## 1. Unidades de medida (global)

```sql
INSERT INTO unit_of_measure (code, name, base_code, factor) VALUES
  ('KG', 'Quilograma',  NULL, 1),
  ('G',  'Grama',       'KG', 0.001),
  ('L',  'Litro',       NULL, 1),
  ('ML', 'Mililitro',   'L',  0.001),
  ('UN', 'Unidade',     NULL, 1),
  ('DZ', 'Dúzia',       'UN', 12),
  ('PC', 'Pacote',      NULL, 1),
  ('CX', 'Caixa',       NULL, 1)
ON CONFLICT (code) DO NOTHING;
```

---

## 2. Papéis de sistema

Criados em toda instalação nova. São **modelos ajustáveis**, não imutáveis — mas o conjunto de permissões existentes é produto (ADR-013).

```sql
-- executar por tenant, no provisionamento
INSERT INTO role (id, tenant_id, code, name, permissions, is_system) VALUES

(uuid7(), :tenant, 'OWNER', 'Proprietário',
 '["*"]'::jsonb, true),

(uuid7(), :tenant, 'MANAGER', 'Gerente',
 '["order:*","table:*","kds:*","cash:*","stock:*","report:read",
   "order:cancel_started","cash:discount_any","cash:close_divergent",
   "stock:adjust","payment:refund","order:close_with_pending",
   "user:read","catalog:read","catalog:write"]'::jsonb, true),

(uuid7(), :tenant, 'CASHIER', 'Caixa',
 '["order:read","order:create","table:read","table:close_request",
   "cash:open","cash:close","cash:movement","cash:discount_limited",
   "payment:register","report:read_own"]'::jsonb, true),

(uuid7(), :tenant, 'WAITER', 'Garçom',
 '["table:open","table:read","table:transfer","table:close_request",
   "order:create","order:read","order:add_item","order:cancel_queued",
   "kds:read","report:read_own"]'::jsonb, true),

(uuid7(), :tenant, 'KITCHEN', 'Cozinha',
 '["kds:read","kds:advance","kds:refire","catalog:set_unavailable",
   "order:read"]'::jsonb, true),

(uuid7(), :tenant, 'STOCK', 'Estoque',
 '["stock:read","stock:purchase","stock:waste","stock:count",
   "recipe:read","recipe:write","supplier:*"]'::jsonb, true),

(uuid7(), :tenant, 'COURIER', 'Entregador',
 '["delivery:read_own","delivery:advance"]'::jsonb, true);
```

### Catálogo de permissões

Convenção `<recurso>:<ação>[_<qualificador>]` (ADR-023). O qualificador é o que gradua poder sem multiplicar papéis.

| Recurso | Ações |
|---|---|
| `table` | `open`, `read`, `transfer`, `close_request`, `close` |
| `order` | `create`, `read`, `add_item`, `cancel_queued`, **`cancel_started`**, `override_price`, **`close_with_pending`** |
| `kds` | `read`, `advance`, `refire` |
| `cash` | `open`, `close`, `movement`, `discount_limited`, **`discount_any`**, **`close_divergent`** |
| `payment` | `register`, **`refund`** |
| `stock` | `read`, `purchase`, `waste`, `count`, **`adjust`** |
| `recipe` | `read`, `write` |
| `catalog` | `read`, `write`, `set_unavailable` |
| `report` | `read`, `read_own` |
| `finance` | `read`, `write` |
| `user` | `read`, `write` |
| `config` | `read`, `write` |

Em **negrito**: ações sensíveis, elegíveis a elevação pontual (ADR-023).

---

## 3. Configuração padrão — modelo Pizzaria

```sql
INSERT INTO tenant_config (tenant_id, operation, thresholds, modules) VALUES (:tenant,
'{
  "serviceFeePercent": 10,
  "serviceFeeOptional": true,
  "maxDiscountPercentWithoutApproval": 5,
  "halfAndHalfPricing": "HIGHEST",
  "maxFractions": 2,
  "stockDeductionMoment": "ITEM_READY",
  "businessDayStartHour": 5,
  "blockCloseWithPendingItems": true,
  "blockCashCloseWithOpenTables": true,
  "bottleneck": { "resource": "OVEN", "slots": 5, "avgCookMinutes": 7 }
}'::jsonb,
'{
  "orderWarnMinutes": 12,
  "orderCriticalMinutes": 18,
  "itemInWindowMinutes": 2,
  "tableIdleMinutes": 10,
  "cashDivergenceAlert": 20.00,
  "cmvDivergencePercent": 5,
  "syncDelayMinutes": 5,
  "dineInPromiseMinutes": 10,
  "deliveryPromiseMinutes": 25
}'::jsonb,
'{
  "dineIn": true, "kds": true, "cash": true,
  "delivery": false, "stock": false, "finance": false
}'::jsonb);
```

### Outros modelos de negócio

O mesmo produto, com configuração diferente (ADR-013):

| Parâmetro | Pizzaria | Hamburgueria | Restaurante |
|---|---|---|---|
| `halfAndHalfPricing` | `HIGHEST` | — | — |
| `maxFractions` | 2 | 1 | 1 |
| `businessDayStartHour` | 5 | 5 | 4 |
| `bottleneck.resource` | `OVEN` | `GRILL` | `ASSEMBLY` |
| `bottleneck.slots` | 5 | 8 | — |
| `dineInPromiseMinutes` | 10 | 8 | 20 |
| `serviceFeePercent` | 10 | 10 | 10 |

> Nada disso é código. É a mesma base servindo negócios diferentes — que é exatamente a diretriz de produto.

---

## 4. Praças de produção padrão (pizzaria)

```sql
INSERT INTO station (id, tenant_id, store_id, name, type, capacity_slots, avg_cook_seconds, sort_order) VALUES
  (uuid7(), :tenant, :store, 'Montagem', 'ASSEMBLY', NULL, NULL, 1),
  (uuid7(), :tenant, :store, 'Forno',    'OVEN',     5,    420,  2),
  (uuid7(), :tenant, :store, 'Fritura',  'FRY',      2,    300,  3),
  (uuid7(), :tenant, :store, 'Bar',      'BAR',      NULL, 60,   4),
  (uuid7(), :tenant, :store, 'Sobremesa','DESSERT',  NULL, 180,  5);
```

---

## 5. Categorias de despesa padrão

```sql
INSERT INTO expense_category (id, tenant_id, name, "group", is_cmv) VALUES
  (uuid7(), :tenant, 'Insumos e mercadorias', 'VARIABLE', true),
  (uuid7(), :tenant, 'Embalagens',            'VARIABLE', true),
  (uuid7(), :tenant, 'Salários',              'PAYROLL',  false),
  (uuid7(), :tenant, 'Encargos trabalhistas', 'PAYROLL',  false),
  (uuid7(), :tenant, 'Aluguel',               'FIXED',    false),
  (uuid7(), :tenant, 'Energia elétrica',      'FIXED',    false),
  (uuid7(), :tenant, 'Água',                  'FIXED',    false),
  (uuid7(), :tenant, 'Gás',                   'VARIABLE', false),
  (uuid7(), :tenant, 'Internet e telefonia',  'FIXED',    false),
  (uuid7(), :tenant, 'Impostos',              'TAX',      false),
  (uuid7(), :tenant, 'Taxas de cartão',       'VARIABLE', false),
  (uuid7(), :tenant, 'Manutenção',            'VARIABLE', false),
  (uuid7(), :tenant, 'Marketing',             'VARIABLE', false),
  (uuid7(), :tenant, 'Contabilidade',         'FIXED',    false),
  (uuid7(), :tenant, 'Outras despesas',       'OTHER',    false);
```

`is_cmv` é o que separa o que entra no cálculo do CMV do que é despesa operacional — sem essa marcação, o indicador mais importante do dono sai errado.

---

## 6. Contas financeiras padrão

```sql
INSERT INTO financial_account (id, tenant_id, name, type) VALUES
  (uuid7(), :tenant, 'Caixa da loja',     'CASH'),
  (uuid7(), :tenant, 'Conta bancária',    'BANK'),
  (uuid7(), :tenant, 'Cielo',             'ACQUIRER'),
  (uuid7(), :tenant, 'Mercado Pago',      'ACQUIRER');
```

---

## 7. Seed de desenvolvimento — pizzaria completa

Usado em ambiente local e nos testes E2E. Não vai para produção.

```sql
-- insumos
INSERT INTO ingredient (id, tenant_id, name, uom_code, avg_cost, min_stock) VALUES
  (:ing_massa,     :tenant, 'Farinha de trigo', 'KG', 4.20,  20),
  (:ing_mussarela, :tenant, 'Mussarela',        'KG', 42.35, 10),
  (:ing_molho,     :tenant, 'Molho de tomate',  'KG', 8.90,   5),
  (:ing_calabresa, :tenant, 'Calabresa',        'KG', 28.50,  5),
  (:ing_catupiry,  :tenant, 'Catupiry',         'KG', 38.00,  3),
  (:ing_oregano,   :tenant, 'Orégano',          'KG', 62.00,  1);

-- produto com duas variações
INSERT INTO product (id, tenant_id, category_id, station_id, name,
                     allows_fractions, max_fractions, fraction_group)
VALUES (:prod_muss, :tenant, :cat_pizzas, :station_forno, 'Pizza Mussarela',
        true, 2, 'PIZZA');

INSERT INTO product_variant (id, tenant_id, product_id, name, size_code, prep_minutes, is_default)
VALUES (:var_muss_g, :tenant, :prod_muss, 'Grande', 'G', 12, true),
       (:var_muss_b, :tenant, :prod_muss, 'Broto',  'B',  9, false);

INSERT INTO price (id, tenant_id, variant_id, channel, amount) VALUES
  (uuid7(), :tenant, :var_muss_g, 'DINE_IN',  52.00),
  (uuid7(), :tenant, :var_muss_g, 'DELIVERY', 56.00),
  (uuid7(), :tenant, :var_muss_b, 'DINE_IN',  32.00);

-- ficha técnica da mussarela grande
INSERT INTO recipe (id, tenant_id, variant_id, yield_qty, yield_uom)
VALUES (:rec_muss_g, :tenant, :var_muss_g, 1, 'UN');

INSERT INTO recipe_item (id, tenant_id, recipe_id, ingredient_id, quantity, uom_code, waste_percent) VALUES
  (uuid7(), :tenant, :rec_muss_g, :ing_massa,     0.2500, 'KG', 3),
  (uuid7(), :tenant, :rec_muss_g, :ing_molho,     0.1200, 'KG', 2),
  (uuid7(), :tenant, :rec_muss_g, :ing_mussarela, 0.1800, 'KG', 2),
  (uuid7(), :tenant, :rec_muss_g, :ing_oregano,   0.0020, 'KG', 0);
```

Custo resultante:

```
massa      0,2500 × 1,03 × 4,20  = 1,0815
molho      0,1200 × 1,02 × 8,90  = 1,0894
mussarela  0,1800 × 1,02 × 42,35 = 7,7754
orégano    0,0020 × 1,00 × 62,00 = 0,1240
                            custo = R$ 10,07
preço salão                       = R$ 52,00
margem                            = R$ 41,93  (80,6%)
```

> A mussarela responde por 77% do custo. É esse número que explica por que 15 gramas a mais por pizza somem no fim do mês sem ninguém perceber — e por que a ficha técnica é onde o sistema se paga.

---

## 8. Ordem de execução do seed

```
1. unit_of_measure                    (global, uma vez)
2. tenant + tenant_config             (por cliente)
3. store                              (por cliente)
4. role (7 papéis de sistema)         (por cliente)
5. app_user OWNER                     (por cliente)
6. station                            (por cliente, conforme modelo)
7. expense_category                   (por cliente)
8. financial_account                  (por cliente)
9. area + dining_table                (por cliente, conforme a loja)
10. category + product + variant + price + recipe   (carga do cardápio)
```

Os passos 1 a 8 são automáticos no provisionamento (RF-PLT-05). Os passos 9 e 10 dependem de dados do cliente — e o passo 10 é o que mais atrasa implantação (doc. 09, §8.2).
