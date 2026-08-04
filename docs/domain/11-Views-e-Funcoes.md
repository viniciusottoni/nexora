# 11 — Views, funções e triggers

| | |
|---|---|
| **Ordem de execução** | 12 de 12 |
| **Depende de** | Todos os anteriores |
| **ADRs** | [008](../ADRs/ADR-008-saldo-derivado-de-movimentos.md), [018](../ADRs/ADR-018-fuso-horario-e-dia-operacional.md), [028](../ADRs/ADR-028-cache-e-invalidacao-catalogo.md) |

---

## 1. Triggers de infraestrutura

### updated_at

```sql
DO $$
DECLARE t text;
BEGIN
  FOREACH t IN ARRAY ARRAY[
    'tenant','tenant_config','store','app_user','role','device',
    'category','product','product_variant','modifier_group','modifier','station',
    'area','dining_table','table_session','order','order_item',
    'cash_session','payment','ingredient','supplier','recipe',
    'customer','customer_address','delivery_zone','courier',
    'financial_account','financial_entry','employee','payroll'
  ] LOOP
    EXECUTE format(
      'CREATE TRIGGER trg_%s_updated BEFORE UPDATE ON %I
       FOR EACH ROW EXECUTE FUNCTION set_updated_at()', t, t);
  END LOOP;
END $$;
```

### Invalidação de catálogo (ADR-028)

```sql
CREATE OR REPLACE FUNCTION bump_catalog_version() RETURNS TRIGGER
LANGUAGE plpgsql AS $$
BEGIN
  -- disponibilidade NÃO incrementa a versão do catálogo (ADR-028)
  IF TG_TABLE_NAME = 'product'
     AND TG_OP = 'UPDATE'
     AND OLD.is_available IS DISTINCT FROM NEW.is_available
     AND OLD.* IS NOT DISTINCT FROM NEW.*  THEN
    RETURN NEW;
  END IF;

  UPDATE tenant_config
     SET catalog_version = catalog_version + 1
   WHERE tenant_id = COALESCE(NEW.tenant_id, OLD.tenant_id);
  RETURN COALESCE(NEW, OLD);
END;
$$;

CREATE TRIGGER trg_catalog_version
  AFTER INSERT OR UPDATE OR DELETE ON product
  FOR EACH ROW EXECUTE FUNCTION bump_catalog_version();

CREATE TRIGGER trg_catalog_version_variant
  AFTER INSERT OR UPDATE OR DELETE ON product_variant
  FOR EACH ROW EXECUTE FUNCTION bump_catalog_version();

CREATE TRIGGER trg_catalog_version_price
  AFTER INSERT OR UPDATE ON price
  FOR EACH ROW EXECUTE FUNCTION bump_catalog_version();
```

### Proteção do saldo materializado (ADR-008)

```sql
CREATE OR REPLACE FUNCTION guard_current_stock() RETURNS TRIGGER
LANGUAGE plpgsql AS $$
BEGIN
  IF OLD.current_stock IS DISTINCT FROM NEW.current_stock
     AND current_setting('app.allow_stock_sync', true) IS DISTINCT FROM 'on' THEN
    RAISE EXCEPTION
      'current_stock é materializado e só pode ser atualizado pelo recálculo (ADR-008)';
  END IF;
  RETURN NEW;
END;
$$;

CREATE TRIGGER trg_ingredient_stock_guard
  BEFORE UPDATE ON ingredient
  FOR EACH ROW EXECUTE FUNCTION guard_current_stock();
```

O job de recálculo abre a exceção explicitamente:

```sql
SELECT set_config('app.allow_stock_sync', 'on', true);
```

---

## 2. Funções de negócio

### Recalcular saldo de estoque

```sql
CREATE OR REPLACE FUNCTION recalc_ingredient_stock(p_tenant UUID, p_ingredient UUID DEFAULT NULL)
RETURNS INT LANGUAGE plpgsql AS $$
DECLARE afetados INT;
BEGIN
  PERFORM set_config('app.allow_stock_sync', 'on', true);

  UPDATE ingredient i
     SET current_stock = COALESCE(s.saldo, 0),
         stock_synced_at = now()
    FROM (
      SELECT ingredient_id, SUM(quantity) AS saldo
      FROM stock_movement
      WHERE tenant_id = p_tenant
        AND (p_ingredient IS NULL OR ingredient_id = p_ingredient)
      GROUP BY ingredient_id
    ) s
   WHERE i.id = s.ingredient_id AND i.tenant_id = p_tenant;

  GET DIAGNOSTICS afetados = ROW_COUNT;
  PERFORM set_config('app.allow_stock_sync', 'off', true);
  RETURN afetados;
END;
$$;
```

### Custo de uma variação (recursivo)

```sql
CREATE OR REPLACE FUNCTION variant_cost(p_variant UUID)
RETURNS NUMERIC(14,4) LANGUAGE sql STABLE AS $$
  WITH RECURSIVE c AS (
    SELECT ri.recipe_id,
           ri.quantity * (1 + ri.waste_percent/100) * i.avg_cost AS amount
    FROM recipe_item ri
    JOIN ingredient i ON i.id = ri.ingredient_id
    WHERE ri.ingredient_id IS NOT NULL

    UNION ALL

    SELECT ri.recipe_id,
           ri.quantity * (1 + ri.waste_percent/100) * c.amount / NULLIF(r.yield_qty, 0)
    FROM recipe_item ri
    JOIN recipe r ON r.id = ri.sub_recipe_id
    JOIN c        ON c.recipe_id = r.id
    WHERE ri.sub_recipe_id IS NOT NULL
  )
  SELECT COALESCE(ROUND(SUM(c.amount), 4), 0)
  FROM c
  JOIN recipe r ON r.id = c.recipe_id
  WHERE r.variant_id = p_variant;
$$;
```

### Próximo código curto do pedido (ADR-016)

```sql
CREATE OR REPLACE FUNCTION next_short_code(p_store UUID, p_business_day DATE)
RETURNS short_code LANGUAGE plpgsql AS $$
DECLARE
  prefixo CHAR(1);
  seq INT;
BEGIN
  prefixo := chr(65 + (EXTRACT(DOY FROM p_business_day)::int % 26));   -- A..Z

  SELECT COALESCE(MAX(SUBSTRING(short_code FROM 2)::int), 0) + 1
    INTO seq
    FROM "order"
   WHERE store_id = p_store AND business_day = p_business_day;

  IF seq > 999 THEN
    RAISE EXCEPTION 'Sequência de código curto esgotada para o dia %', p_business_day;
  END IF;

  RETURN prefixo || seq::text;
END;
$$;
```

---

## 3. Views de leitura

### v_kds_queue — a fila da cozinha

```sql
CREATE VIEW v_kds_queue AS
SELECT
  oi.id                AS order_item_id,
  oi.tenant_id,
  o.store_id,
  o.short_code,
  o.channel,
  dt.label             AS table_label,
  oi.station_id,
  st.name              AS station_name,
  p.name               AS product_name,
  pv.name              AS variant_name,
  oi.quantity,
  oi.notes,
  oi.status,
  oi.placed_at,
  oi.fire_at,
  oi.fired_at,
  oi.oven_in_at,
  EXTRACT(EPOCH FROM (now() - oi.placed_at))::int AS elapsed_seconds,
  o.promised_at,
  EXTRACT(EPOCH FROM (o.promised_at - now()))::int AS seconds_to_promise,
  oi.priority_score,
  -- sabores do meio a meio, concatenados
  (SELECT string_agg(pf.name, ' / ' ORDER BY f.sort_order)
     FROM order_item_fraction f
     JOIN product_variant pvf ON pvf.id = f.variant_id
     JOIN product pf ON pf.id = pvf.product_id
    WHERE f.order_item_id = oi.id) AS fractions,
  -- modificadores
  (SELECT string_agg(m.name_snapshot, ', ')
     FROM order_item_modifier m
    WHERE m.order_item_id = oi.id) AS modifiers
FROM order_item oi
JOIN "order" o        ON o.id  = oi.order_id
JOIN product_variant pv ON pv.id = oi.variant_id
JOIN product p        ON p.id  = pv.product_id
LEFT JOIN station st  ON st.id = oi.station_id
LEFT JOIN table_session ts ON ts.id = o.session_id
LEFT JOIN dining_table dt  ON dt.id = ts.table_id
WHERE oi.status IN ('QUEUED','FIRED','IN_OVEN','OUT_OF_OVEN');
```

### v_table_map — mapa de mesas

```sql
CREATE VIEW v_table_map AS
SELECT
  dt.id            AS table_id,
  dt.tenant_id,
  dt.store_id,
  a.name           AS area_name,
  dt.label,
  dt.seats,
  dt.status,
  ts.id            AS session_id,
  ts.opened_at,
  EXTRACT(EPOCH FROM (now() - ts.opened_at))::int AS open_seconds,
  ts.guest_count,
  ts.total_amount,
  ts.status        AS session_status,
  u.name           AS waiter_name,
  -- itens prontos aguardando retirada
  (SELECT count(*) FROM order_item oi
     JOIN "order" o ON o.id = oi.order_id
    WHERE o.session_id = ts.id AND oi.status = 'READY') AS items_waiting,
  -- itens ainda em produção
  (SELECT count(*) FROM order_item oi
     JOIN "order" o ON o.id = oi.order_id
    WHERE o.session_id = ts.id
      AND oi.status IN ('QUEUED','FIRED','IN_OVEN','OUT_OF_OVEN')) AS items_producing
FROM dining_table dt
JOIN area a ON a.id = dt.area_id
LEFT JOIN table_session ts ON ts.table_id = dt.id AND ts.status <> 'CLOSED'
LEFT JOIN app_user u ON u.id = ts.waiter_id
WHERE dt.deleted_at IS NULL AND dt.is_active;
```

### v_order_timings — os tempos de cada pedido

```sql
CREATE VIEW v_order_timings AS
SELECT
  o.id AS order_id,
  o.tenant_id, o.store_id, o.business_day, o.channel, o.short_code,
  o.placed_at, o.served_at, o.promised_at,
  EXTRACT(EPOCH FROM (o.first_fired_at - o.placed_at))::int AS queue_seconds,
  EXTRACT(EPOCH FROM (o.ready_at       - o.first_fired_at))::int AS prep_seconds,
  EXTRACT(EPOCH FROM (o.served_at      - o.ready_at))::int AS expedite_seconds,
  EXTRACT(EPOCH FROM (o.served_at      - o.placed_at))::int AS total_seconds,
  (o.served_at <= o.promised_at)                            AS on_time,
  -- dessincronização entre itens da mesma mesa
  (SELECT EXTRACT(EPOCH FROM (max(ready_at) - min(ready_at)))::int
     FROM order_item WHERE order_id = o.id AND ready_at IS NOT NULL) AS sync_gap_seconds
FROM "order" o
WHERE o.served_at IS NOT NULL;
```

### v_stock_position — posição de estoque

```sql
CREATE VIEW v_stock_position AS
SELECT
  i.id AS ingredient_id,
  i.tenant_id,
  i.name,
  i.uom_code,
  i.current_stock,
  i.min_stock,
  i.avg_cost,
  ROUND(i.current_stock * i.avg_cost, 2) AS stock_value,
  (i.current_stock <= i.min_stock)       AS below_minimum,
  -- consumo diário médio dos últimos 30 dias
  COALESCE((
    SELECT ABS(SUM(quantity)) / 30.0
    FROM stock_movement
    WHERE ingredient_id = i.id AND type = 'PRODUCTION'
      AND occurred_at > now() - interval '30 days'
  ), 0) AS avg_daily_usage,
  -- cobertura em dias
  CASE WHEN COALESCE((
    SELECT ABS(SUM(quantity)) / 30.0 FROM stock_movement
    WHERE ingredient_id = i.id AND type = 'PRODUCTION'
      AND occurred_at > now() - interval '30 days'), 0) > 0
  THEN ROUND(i.current_stock / ((
    SELECT ABS(SUM(quantity)) / 30.0 FROM stock_movement
    WHERE ingredient_id = i.id AND type = 'PRODUCTION'
      AND occurred_at > now() - interval '30 days')), 1)
  END AS coverage_days
FROM ingredient i
WHERE i.deleted_at IS NULL AND i.is_active;
```

### v_product_margin — custo e margem

```sql
CREATE VIEW v_product_margin AS
SELECT
  pv.id AS variant_id,
  pv.tenant_id,
  p.name || ' ' || pv.name AS full_name,
  pr.channel,
  pr.amount                     AS price,
  variant_cost(pv.id)           AS cost,
  pr.amount - variant_cost(pv.id) AS margin,
  CASE WHEN pr.amount > 0
       THEN ROUND((pr.amount - variant_cost(pv.id)) / pr.amount * 100, 2)
  END AS margin_percent
FROM product_variant pv
JOIN product p ON p.id = pv.product_id
LEFT JOIN price pr ON pr.variant_id = pv.id AND pr.valid_to IS NULL
WHERE pv.deleted_at IS NULL AND pv.is_active;
```

---

## 4. Views de integridade

Consultadas pelo job diário. Resultado não vazio é incidente.

```sql
CREATE VIEW v_integrity_checks AS
  SELECT 'saldo_divergente' AS check_name,
         count(*) AS issues
  FROM (
    SELECT i.id
    FROM ingredient i
    LEFT JOIN stock_movement m ON m.ingredient_id = i.id
    GROUP BY i.id, i.current_stock
    HAVING i.current_stock <> COALESCE(SUM(m.quantity), 0)
  ) x
UNION ALL
  SELECT 'pedido_sem_evento', count(*)
  FROM "order" o
  LEFT JOIN domain_event e ON e.aggregate_id = o.id AND e.type = 'order.placed'
  WHERE o.status <> 'DRAFT' AND e.id IS NULL
UNION ALL
  SELECT 'outbox_travado', count(*)
  FROM outbox
  WHERE status IN ('PENDING','FAILED') AND created_at < now() - interval '10 minutes'
UNION ALL
  SELECT 'item_tempo_negativo', count(*)
  FROM order_item
  WHERE served_at IS NOT NULL AND served_at < placed_at
UNION ALL
  SELECT 'pagamento_sem_lancamento', count(*)
  FROM payment p
  LEFT JOIN financial_entry fe
    ON fe.reference_type = 'payment' AND fe.reference_id = p.id
  WHERE p.status = 'PAID' AND fe.id IS NULL;
```
