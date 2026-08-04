# 10 — RLS, papéis de banco e índices

| | |
|---|---|
| **Ordem de execução** | 11 de 12 |
| **Depende de** | Todos os contextos anteriores |
| **ADRs** | [004](../ADRs/ADR-004-postgresql-rls-multitenancy.md), [006](../ADRs/ADR-006-event-sourcing-seletivo.md), [031](../ADRs/ADR-031-gestao-de-segredos.md) |

> Este documento contém o que impede o pior incidente possível do produto: um estabelecimento enxergar dados de outro.

---

## 1. Papéis de banco

```sql
-- aplicação: sujeito às políticas RLS
CREATE ROLE app_user_role NOLOGIN;

-- plataforma: ignora RLS, uso restrito e auditado (ADR-004)
CREATE ROLE platform_admin NOLOGIN BYPASSRLS;

-- somente leitura, para relatórios internos
CREATE ROLE app_readonly NOLOGIN;

-- usuários de conexão
CREATE USER app       WITH PASSWORD :'app_password'      IN ROLE app_user_role;
CREATE USER platform  WITH PASSWORD :'platform_password' IN ROLE platform_admin;
```

| Papel | Uso | Onde |
|---|---|---|
| `app_user_role` | Toda operação de negócio | API edge e cloud |
| `platform_admin` | Apenas `PlatformModule`, com auditoria obrigatória | API cloud |
| `app_readonly` | Consultas de suporte | Ferramenta interna |

---

## 2. Row Level Security

### Habilitação em massa

```sql
DO $$
DECLARE t text;
BEGIN
  FOREACH t IN ARRAY ARRAY[
    'tenant_config','store','edge_installation','app_user','role','user_role',
    'device','audit_log','tenant_secret',
    'station','category','product','product_variant','price',
    'modifier_group','modifier','product_modifier_group','media_asset',
    'area','dining_table','table_session','order','order_item',
    'order_item_fraction','order_item_modifier',
    'cash_session','cash_movement','payment','payment_allocation',
    'supplier','ingredient','recipe','recipe_item','stock_movement',
    'purchase','purchase_item','inventory_count','inventory_count_item',
    'customer','customer_address','delivery_zone','courier',
    'delivery_run','delivery_stop',
    'financial_account','expense_category','financial_entry',
    'employee','payroll','payroll_item',
    'metric_hourly','metric_daily','metric_product_daily',
    'metric_operator_daily','goal','alert','sync_conflict'
  ] LOOP
    EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', t);
    EXECUTE format('ALTER TABLE %I FORCE  ROW LEVEL SECURITY', t);
    EXECUTE format($f$
      CREATE POLICY tenant_isolation ON %I
        USING      (tenant_id = current_tenant_id())
        WITH CHECK (tenant_id = current_tenant_id())
    $f$, t);
  END LOOP;
END $$;
```

`USING` filtra a leitura; `WITH CHECK` impede gravar linha de outro tenant. **Ambos são obrigatórios** — só o primeiro permitiria inserir dado no tenant errado.

### Casos especiais

```sql
-- domain_event: particionada, política na tabela-mãe propaga às partições
ALTER TABLE domain_event ENABLE ROW LEVEL SECURITY;
ALTER TABLE domain_event FORCE  ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON domain_event
  USING      (tenant_id = current_tenant_id())
  WITH CHECK (tenant_id = current_tenant_id());

-- idempotency_key: mesma regra
ALTER TABLE idempotency_key ENABLE ROW LEVEL SECURITY;
ALTER TABLE idempotency_key FORCE  ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON idempotency_key
  USING      (tenant_id = current_tenant_id())
  WITH CHECK (tenant_id = current_tenant_id());

-- outbox e sync_cursor: apenas no edge, single-tenant; sem RLS
-- unit_of_measure e tenant: globais; sem RLS
```

### Definição do contexto na aplicação

```sql
-- SEMPRE local à transação (o terceiro parâmetro true)
SELECT set_config('app.tenant_id', $1, true);
```

Sem `true`, o contexto vaza entre requisições que reutilizam a mesma conexão do pool — o defeito mais perigoso possível neste sistema.

### Comportamento sem contexto

`current_tenant_id()` retorna `NULL`, a comparação resulta em `NULL` e **nenhuma linha é retornada**. Falha fechada.

---

## 3. Imutabilidade de auditoria e eventos

**Implementado (E-09/US-090)**, migration `PartitionAuditLogAndRestrictMutation` — imutabilidade
de `audit_log` só por revogação de permissão, sem trigger (mais simples e é exatamente o mecanismo
que a US-090 pede: "a revogação de permissão no banco é o que torna a imutabilidade real"):

```sql
REVOKE UPDATE, DELETE ON audit_log FROM app_user_role;
```

**Ainda não implementado** (gap pré-existente, fora do escopo de E-09 — `domain_event` não é tocado
por nenhuma migration de auditoria; `stock_movement`/`payment` dependem de casos de uso que ainda
não existem, ver `Nexora.IntegrationTests.AuditCoverageTests`):

```sql
-- domain_event: UPDATE seria bloqueado por grant (nunca trigger — a anonimização LGPD do
-- ADR-035 precisa reescrever payload por uma via controlada), DELETE por grant também.
REVOKE UPDATE, DELETE ON domain_event  FROM app_user_role;
REVOKE DELETE           ON stock_movement FROM app_user_role;
REVOKE DELETE           ON payment        FROM app_user_role;
```

> Auditoria que pode ser alterada não é auditoria (RF-AUD-04).

---

## 4. Grants

```sql
GRANT USAGE ON SCHEMA public TO app_user_role, platform_admin, app_readonly;

GRANT SELECT, INSERT, UPDATE ON ALL TABLES    IN SCHEMA public TO app_user_role;
GRANT USAGE, SELECT          ON ALL SEQUENCES IN SCHEMA public TO app_user_role;
GRANT EXECUTE                ON ALL FUNCTIONS IN SCHEMA public TO app_user_role;

GRANT ALL    ON ALL TABLES IN SCHEMA public TO platform_admin;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO app_readonly;

-- segredos: apenas a plataforma
REVOKE ALL ON tenant_secret FROM app_user_role, app_readonly;
GRANT  SELECT, INSERT, UPDATE ON tenant_secret TO platform_admin;

-- delete físico é sempre proibido na aplicação (soft delete)
REVOKE DELETE ON ALL TABLES IN SCHEMA public FROM app_user_role;

ALTER DEFAULT PRIVILEGES IN SCHEMA public
  GRANT SELECT, INSERT, UPDATE ON TABLES TO app_user_role;
```

---

## 5. Índices consolidados

Resumo do que já foi declarado nos documentos anteriores, organizado por finalidade.

### Operação em tempo real — os mais críticos

```sql
idx_item_queue      order_item (tenant_id, station_id, status, placed_at)
                    WHERE status IN ('QUEUED','FIRED','IN_OVEN','OUT_OF_OVEN')
                    → fila do KDS, consultada dezenas de vezes por minuto

idx_order_active    "order" (tenant_id, store_id, status)
                    WHERE status IN ('PLACED','IN_PRODUCTION','READY')
                    → pedidos em andamento

idx_session_open    table_session (tenant_id, store_id, status)
                    WHERE status <> 'CLOSED'
                    → mapa de mesas

idx_item_ready      order_item (tenant_id, ready_at) WHERE status = 'READY'
                    → itens parados na janela de expedição

idx_item_oven       order_item (tenant_id, station_id) WHERE status = 'IN_OVEN'
                    → ocupação do gargalo

idx_order_late      "order" (tenant_id, promised_at)
                    WHERE status IN ('PLACED','IN_PRODUCTION')
                    → pedidos em risco de atraso
```

> Os índices parciais são deliberados. A fila da cozinha só interessa enquanto está aberta; indexar pedidos fechados de dois anos atrás desperdiça memória exatamente onde ela é mais necessária.

### Métrica e relatório

```sql
idx_order_day          "order" (tenant_id, business_day, channel)
idx_item_metrics       order_item (tenant_id, variant_id, placed_at)
idx_metric_hourly_day  metric_hourly (tenant_id, business_day)
idx_metric_product_day metric_product_daily (tenant_id, business_day)
idx_movement_day       stock_movement (tenant_id, business_day, type)
idx_entry_competence   financial_entry (tenant_id, competence_date, type)
```

### Evento e sincronização

```sql
-- por partição (documento 08)
(tenant_id, occurred_at DESC)
(tenant_id, aggregate_type, aggregate_id)
(tenant_id, type, occurred_at DESC)

idx_outbox_pending  outbox (device_seq) WHERE status IN ('PENDING','FAILED')
```

### Unicidade de negócio

```sql
uq_order_short_code      "order" (store_id, business_day, short_code)
uq_session_open          table_session (table_id) WHERE status <> 'CLOSED'
uq_cash_open             cash_session (store_id, operator_id) WHERE status <> 'CLOSED'
uq_price_current         price (variant_id, channel) WHERE valid_to IS NULL
uq_recipe_variant        recipe (variant_id) WHERE variant_id IS NOT NULL AND deleted_at IS NULL
uq_payment_provider_ref  payment (tenant_id, provider, provider_ref) WHERE provider_ref IS NOT NULL
uq_entry_reference       financial_entry (tenant_id, reference_type, reference_id)
uq_app_user_pin          app_user (tenant_id, pin_hash) WHERE status = 'ACTIVE'
uq_alert_group           alert (tenant_id, group_key) WHERE acknowledged_at IS NULL
```

---

## 6. Teste de isolamento — bloqueante em todo PR

```sql
-- deve retornar zero linhas para QUALQUER tabela de negócio
SELECT set_config('app.tenant_id', '<tenant-A>', true);
SELECT count(*) FROM "order" WHERE tenant_id <> '<tenant-A>'::uuid;   -- esperado: 0

-- sem contexto: nada retorna
SELECT set_config('app.tenant_id', '', true);
SELECT count(*) FROM "order";                                          -- esperado: 0

-- tentativa de gravar no tenant errado
SELECT set_config('app.tenant_id', '<tenant-A>', true);
INSERT INTO "order" (id, tenant_id, ...) VALUES (..., '<tenant-B>', ...);
-- esperado: ERROR - new row violates row-level security policy
```

Implementação em C# (`Nexora.IntegrationTests`, Testcontainers) no documento `Docs/10-Estrategia-de-Testes-e-Qualidade.md`, §8.1.

---

## 7. Verificação de cobertura de RLS

Consulta que denuncia tabela nova esquecida:

```sql
SELECT c.relname AS tabela_sem_rls
FROM pg_class c
JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE n.nspname = 'public'
  AND c.relkind = 'r'
  AND NOT c.relrowsecurity
  AND EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_name = c.relname AND column_name = 'tenant_id'
  );
```

Roda no CI. Resultado não vazio **falha o build** — nova tabela com `tenant_id` e sem RLS é defeito de segurança, não descuido.
