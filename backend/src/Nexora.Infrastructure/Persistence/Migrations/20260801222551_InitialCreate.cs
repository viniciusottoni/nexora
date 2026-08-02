using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            // ---------------------------------------------------------------------------------
            // Extensões e domínios de tipo (Docs/Domain/00-Convencoes-e-Tipos.md §1/§2) — o EF
            // Core só gera CREATE TABLE/CREATE INDEX a partir do modelo; domínios (money_amount,
            // qty_amount, percent_amount, fraction_weight, slug) e extensões precisam existir
            // ANTES das tabelas que os referenciam via HasColumnType(...), por isso entram como
            // SQL manual no topo desta mesma migration (ver Docs/Domain/13-Mapeamento-EFCore.md
            // §6 — aqui juntos por serem só pré-requisito de US-001, não o modelo de dados
            // completo). "citext" já é criada por CreateExtensionIfNotExists via annotation
            // acima; as demais entram explicitamente porque ainda não têm coluna que force o
            // provider Npgsql a inferi-las.
            // ---------------------------------------------------------------------------------
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"pgcrypto\";");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"pg_trgm\";");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"btree_gist\";");

            migrationBuilder.Sql("CREATE DOMAIN money_amount AS NUMERIC(12,2);");
            migrationBuilder.Sql("CREATE DOMAIN qty_amount AS NUMERIC(14,4);");
            migrationBuilder.Sql("CREATE DOMAIN percent_amount AS NUMERIC(6,3) CHECK (VALUE >= 0 AND VALUE <= 100);");
            migrationBuilder.Sql("CREATE DOMAIN fraction_weight AS NUMERIC(5,4) CHECK (VALUE > 0 AND VALUE <= 1);");
            migrationBuilder.Sql("CREATE DOMAIN slug AS VARCHAR(64) CHECK (VALUE ~ '^[a-z0-9]+(-[a-z0-9]+)*$');");

            // Função auxiliar do contexto de tenant (Docs/Domain/00 §4.3, ADR-004) — usada pelas
            // políticas RLS criadas na migration EnableRowLevelSecurity, definida aqui porque
            // pertence à mesma seção de pré-requisitos que os domínios acima.
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION current_tenant_id() RETURNS uuid
                LANGUAGE sql STABLE AS $$
                  SELECT NULLIF(current_setting('app.tenant_id', true), '')::uuid;
                $$;
                """);

            migrationBuilder.CreateTable(
                name: "alert",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    severity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    entity_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_roles = table.Column<string[]>(type: "text[]", nullable: false),
                    target_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    message = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    raised_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    acknowledged_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    acknowledged_by = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    group_key = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alert", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "area",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_area", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cash_session",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    business_day = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    opening_amount = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    expected_amount = table.Column<decimal>(type: "money_amount", nullable: true),
                    counted_amount = table.Column<decimal>(type: "money_amount", nullable: true),
                    divergence = table.Column<decimal>(type: "money_amount", nullable: true),
                    opened_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    closed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    authorized_by = table.Column<Guid>(type: "uuid", nullable: true),
                    justification = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_session", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "category",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    available_schedule = table.Column<string>(type: "jsonb", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "courier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    vehicle = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    plate = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    is_own = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_courier", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customer",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "citext", nullable: true),
                    document = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    anonymized_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    last_order_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    orders_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_spent = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customer_address",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    zone_id = table.Column<Guid>(type: "uuid", nullable: true),
                    label = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    street = table.Column<string>(type: "text", nullable: false),
                    number = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    complement = table.Column<string>(type: "text", nullable: true),
                    district = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<string>(type: "char(2)", nullable: true),
                    zip = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    reference = table.Column<string>(type: "text", nullable: true),
                    lat = table.Column<decimal>(type: "numeric(10,7)", nullable: true),
                    lng = table.Column<decimal>(type: "numeric(10,7)", nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_address", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "delivery_run",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    courier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_day = table.Column<DateOnly>(type: "date", nullable: false),
                    arrived_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    dispatched_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    returned_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    stops_count = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    distance_km = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_run", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "delivery_stop",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sequence = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    delivered_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    outcome = table.Column<int>(type: "integer", nullable: true),
                    outcome_reason = table.Column<string>(type: "text", nullable: true),
                    received_by = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_stop", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "delivery_zone",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    geometry = table.Column<string>(type: "jsonb", nullable: true),
                    districts = table.Column<string[]>(type: "text[]", nullable: false),
                    fee = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    min_order = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    avg_minutes = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)20),
                    max_distance_km = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_zone", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "domain_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    aggregate_type = table.Column<string>(type: "text", nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    authorized_by = table.Column<Guid>(type: "uuid", nullable: true),
                    device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    origin = table.Column<string>(type: "text", nullable: false),
                    device_seq = table.Column<long>(type: "bigint", nullable: true),
                    installation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    trace_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    clock_suspect = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_domain_event", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient = table.Column<string>(type: "citext", nullable: false),
                    template = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    payload_encrypted = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "PENDING"),
                    attempts = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_outbox", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    role_title = table.Column<string>(type: "text", nullable: true),
                    employment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    salary = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    hired_at = table.Column<DateOnly>(type: "date", nullable: true),
                    terminated_at = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "expense_category",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    group = table.Column<int>(type: "integer", nullable: false),
                    is_cmv = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expense_category", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "financial_account",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    bank_info = table.Column<string>(type: "jsonb", nullable: true),
                    balance = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_financial_account", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "financial_entry",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: true),
                    account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "money_amount", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    competence_date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    reference_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_recurring = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    recurrence = table.Column<string>(type: "jsonb", nullable: true),
                    parent_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_financial_entry", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "goal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metric_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    target_value = table.Column<decimal>(type: "numeric(14,4)", nullable: false),
                    comparison = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false, defaultValue: "LTE"),
                    period = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_goal", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_key",
                columns: table => new
                {
                    key = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    endpoint = table.Column<string>(type: "text", nullable: false),
                    request_hash = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    response_status = table.Column<int>(type: "integer", nullable: true),
                    response_body = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_idempotency_key", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "installation_nonce",
                columns: table => new
                {
                    installation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nonce = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_installation_nonce", x => new { x.installation_id, x.nonce });
                });

            migrationBuilder.CreateTable(
                name: "inventory_count",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_day = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "OPEN"),
                    counted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    counted_by = table.Column<Guid>(type: "uuid", nullable: false),
                    applied_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    total_divergence_cost = table.Column<decimal>(type: "money_amount", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_count", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "media_asset",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    variant = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    bytes = table.Column<int>(type: "integer", nullable: true),
                    mime_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    blur_data = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_media_asset", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "metric_daily",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_day = table.Column<DateOnly>(type: "date", nullable: false),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    orders = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    orders_cancelled = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    items = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    revenue = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    discounts = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    service_fee = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    avg_ticket = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    covers = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    sessions = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    table_turns = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    avg_stay_seconds = table.Column<int>(type: "integer", nullable: true),
                    avg_total_seconds = table.Column<int>(type: "integer", nullable: true),
                    p90_total_seconds = table.Column<int>(type: "integer", nullable: true),
                    on_time_rate = table.Column<decimal>(type: "numeric(5,4)", nullable: true),
                    cmv_theoretical = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    labor_cost = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    card_fees = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    computed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_metric_daily", x => new { x.tenant_id, x.store_id, x.business_day, x.channel });
                });

            migrationBuilder.CreateTable(
                name: "metric_hourly",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hour = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    business_day = table.Column<DateOnly>(type: "date", nullable: false),
                    orders = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    orders_cancelled = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    items = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    items_refired = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    revenue = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    avg_queue_seconds = table.Column<int>(type: "integer", nullable: true),
                    avg_prep_seconds = table.Column<int>(type: "integer", nullable: true),
                    avg_cook_seconds = table.Column<int>(type: "integer", nullable: true),
                    avg_expedite_seconds = table.Column<int>(type: "integer", nullable: true),
                    avg_total_seconds = table.Column<int>(type: "integer", nullable: true),
                    p90_total_seconds = table.Column<int>(type: "integer", nullable: true),
                    max_total_seconds = table.Column<int>(type: "integer", nullable: true),
                    on_time_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    late_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    oven_busy_seconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    oven_idle_with_queue_seconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    computed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_metric_hourly", x => new { x.tenant_id, x.store_id, x.hour, x.channel });
                });

            migrationBuilder.CreateTable(
                name: "metric_operator_daily",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_day = table.Column<DateOnly>(type: "date", nullable: false),
                    role_context = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    orders = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    items = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    revenue = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    avg_ticket = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    sessions = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    avg_serve_seconds = table.Column<int>(type: "integer", nullable: true),
                    upsell_offered = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    upsell_accepted = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    cancellations = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    discounts_given = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    computed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_metric_operator_daily", x => new { x.tenant_id, x.store_id, x.user_id, x.business_day, x.role_context });
                });

            migrationBuilder.CreateTable(
                name: "metric_product_daily",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_day = table.Column<DateOnly>(type: "date", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    fraction_quantity = table.Column<decimal>(type: "numeric(10,4)", nullable: false, defaultValue: 0m),
                    revenue = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    cost = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    margin = table.Column<decimal>(type: "money_amount", nullable: false),
                    avg_prep_seconds = table.Column<int>(type: "integer", nullable: true),
                    cancelled = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    refired = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    computed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_metric_product_daily", x => new { x.tenant_id, x.store_id, x.variant_id, x.business_day });
                });

            migrationBuilder.CreateTable(
                name: "modifier_group",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    min_select = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    max_select = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    is_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_modifier_group", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_seq = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "PENDING"),
                    attempts = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    next_retry_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    synced_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox", x => new { x.event_id, x.occurred_at });
                });

            migrationBuilder.CreateTable(
                name: "pairing_code",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    attempts = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pairing_code", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cash_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    business_day = table.Column<DateOnly>(type: "date", nullable: false),
                    method = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    amount = table.Column<decimal>(type: "money_amount", nullable: false),
                    fee_amount = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    net_amount = table.Column<decimal>(type: "money_amount", nullable: false),
                    tip_amount = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    change_amount = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    provider_ref = table.Column<string>(type: "text", nullable: true),
                    provider_payload = table.Column<string>(type: "jsonb", nullable: true),
                    installments = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    card_brand = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    authorization_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    refunded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    refund_amount = table.Column<decimal>(type: "money_amount", nullable: true),
                    refund_reason = table.Column<string>(type: "text", nullable: true),
                    authorized_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payroll",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: true),
                    period = table.Column<string>(type: "char(7)", nullable: false),
                    total_gross = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    total_charges = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    total_net = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "DRAFT"),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payroll", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payroll_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payroll_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gross = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    charges = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    benefits = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    deductions = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    net = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payroll_item", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "purchase",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    document = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    total = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    purchased_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    business_day = table.Column<DateOnly>(type: "date", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recipe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "text", nullable: true),
                    is_sub_recipe = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    yield_qty = table.Column<decimal>(type: "qty_amount", nullable: false, defaultValue: 1m),
                    yield_uom = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipe", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stock_movement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_day = table.Column<DateOnly>(type: "date", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "qty_amount", nullable: false),
                    uom_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    unit_cost = table.Column<decimal>(type: "money_amount", nullable: true),
                    total_cost = table.Column<decimal>(type: "money_amount", nullable: true),
                    reference_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    waste_reason = table.Column<int>(type: "integer", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    authorized_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movement", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "supplier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    document = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: true),
                    contact = table.Column<string>(type: "jsonb", nullable: true),
                    lead_time_days = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sync_conflict",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    installation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    resolution = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: true),
                    detected_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_conflict", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sync_cursor",
                columns: table => new
                {
                    installation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction = table.Column<string>(type: "varchar(8)", nullable: false),
                    last_seq = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    last_success_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_cursor", x => new { x.installation_id, x.direction });
                });

            migrationBuilder.CreateTable(
                name: "tenant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "slug", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    legal_name = table.Column<string>(type: "text", nullable: true),
                    document = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    plan = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "STANDARD"),
                    timezone = table.Column<string>(type: "text", nullable: false, defaultValue: "America/Sao_Paulo"),
                    locale = table.Column<string>(type: "text", nullable: false, defaultValue: "pt-BR"),
                    currency = table.Column<string>(type: "char(3)", nullable: false, defaultValue: "BRL"),
                    domain = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_secret",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "text", nullable: false),
                    ciphertext = table.Column<byte[]>(type: "bytea", nullable: false),
                    key_version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_secret", x => new { x.tenant_id, x.key });
                });

            migrationBuilder.CreateTable(
                name: "unit_of_measure",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    base_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    factor = table.Column<decimal>(type: "numeric(18,9)", nullable: false, defaultValue: 1m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_units_of_measure_code", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "dining_table",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    seats = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)4),
                    qr_token = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dining_table", x => x.id);
                    table.ForeignKey(
                        name: "fk_dining_table_area_area_id",
                        column: x => x.area_id,
                        principalTable: "area",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cash_movement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cash_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "money_amount", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    authorized_by = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_movement", x => x.id);
                    table.ForeignKey(
                        name: "fk_cash_movement_cash_sessions_cash_session_id",
                        column: x => x.cash_session_id,
                        principalTable: "cash_session",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    ingredients_text = table.Column<string>(type: "text", nullable: true),
                    allergens = table.Column<string[]>(type: "text[]", nullable: false),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_available = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    unavailable_reason = table.Column<string>(type: "text", nullable: true),
                    unavailable_since = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    allows_fractions = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    max_fractions = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    fraction_group = table.Column<string>(type: "text", nullable: true),
                    ncm = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    cest = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    cfop = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    origin_code = table.Column<short>(type: "smallint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_category_category_id",
                        column: x => x.category_id,
                        principalTable: "category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_count_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    count_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expected_qty = table.Column<decimal>(type: "qty_amount", nullable: false),
                    counted_qty = table.Column<decimal>(type: "qty_amount", nullable: false),
                    divergence_qty = table.Column<decimal>(type: "qty_amount", nullable: false),
                    unit_cost = table.Column<decimal>(type: "money_amount", nullable: false),
                    divergence_cost = table.Column<decimal>(type: "money_amount", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_count_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_count_item_inventory_count_count_id",
                        column: x => x.count_id,
                        principalTable: "inventory_count",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "modifier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    price_delta = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<decimal>(type: "qty_amount", nullable: true),
                    is_available = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_modifier", x => x.id);
                    table.ForeignKey(
                        name: "fk_modifier_modifier_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "modifier_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_allocation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "money_amount", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_allocation", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_allocation_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "qty_amount", nullable: false),
                    uom_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    unit_cost = table.Column<decimal>(type: "money_amount", nullable: false),
                    total_cost = table.Column<decimal>(type: "money_amount", nullable: false),
                    expires_at = table.Column<DateOnly>(type: "date", nullable: true),
                    lot_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_item_purchase_purchase_id",
                        column: x => x.purchase_id,
                        principalTable: "purchase",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recipe_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sub_recipe_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<decimal>(type: "qty_amount", nullable: false),
                    uom_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    waste_percent = table.Column<decimal>(type: "percent_amount", nullable: false, defaultValue: 0m),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipe_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_recipe_item_recipe_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipe",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_recipe_item_recipe_sub_recipe_id",
                        column: x => x.sub_recipe_id,
                        principalTable: "recipe",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "app_user",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "citext", nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    pin_hash = table.Column<string>(type: "text", nullable: true),
                    mfa_secret_encrypted = table.Column<string>(type: "text", nullable: true),
                    pin_lookup = table.Column<string>(type: "text", nullable: true),
                    pin_rotated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    failed_attempts = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    blocked_until = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_user", x => x.id);
                    table.CheckConstraint("ck_app_user_credential", "password_hash IS NOT NULL OR pin_hash IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_app_user_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    authorized_by = table.Column<Guid>(type: "uuid", nullable: true),
                    device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "text", nullable: false),
                    entity = table.Column<string>(type: "text", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    before = table.Column<string>(type: "jsonb", nullable: true),
                    after = table.Column<string>(type: "jsonb", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    ip = table.Column<IPAddress>(type: "inet", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_log", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_log_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    permissions = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "store",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    timezone = table.Column<string>(type: "text", nullable: false, defaultValue: "America/Sao_Paulo"),
                    address = table.Column<string>(type: "jsonb", nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_store", x => x.id);
                    table.ForeignKey(
                        name: "fk_store_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_config",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branding = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    operation = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    thresholds = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    modules = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    fiscal = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    printers = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    payments = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    maintenance = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    catalog_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    config_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    branding_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_config", x => x.tenant_id);
                    table.ForeignKey(
                        name: "fk_tenant_config_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ingredient",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    uom_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    avg_cost = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    last_cost = table.Column<decimal>(type: "money_amount", nullable: true),
                    current_stock = table.Column<decimal>(type: "qty_amount", nullable: false, defaultValue: 0m),
                    stock_synced_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    min_stock = table.Column<decimal>(type: "qty_amount", nullable: false, defaultValue: 0m),
                    is_perishable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    shelf_life_days = table.Column<short>(type: "smallint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ingredient", x => x.id);
                    table.ForeignKey(
                        name: "fk_ingredient_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "supplier",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_ingredient_units_of_measure_uom_code",
                        column: x => x.uom_code,
                        principalTable: "unit_of_measure",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "table_session",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    table_id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_day = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    guest_count = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    waiter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    opened_by = table.Column<Guid>(type: "uuid", nullable: true),
                    opened_source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "WAITER"),
                    opened_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    bill_requested_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    released_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    subtotal = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    discount_amount = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    service_fee_amount = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    total_amount = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    rating = table.Column<short>(type: "smallint", nullable: true),
                    rating_comment = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_table_session", x => x.id);
                    table.ForeignKey(
                        name: "fk_table_session_dining_table_table_id",
                        column: x => x.table_id,
                        principalTable: "dining_table",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_modifier_group",
                columns: table => new
                {
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_modifier_group", x => new { x.product_id, x.group_id });
                    table.ForeignKey(
                        name: "fk_product_modifier_group_modifier_group_group_id",
                        column: x => x.group_id,
                        principalTable: "modifier_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_modifier_group_product_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_variant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    sku = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    size_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    prep_minutes = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)10),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    fiscal_rates = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_variant", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_variant_product_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "owner_invite",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "citext", nullable: false),
                    secret_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_owner_invite", x => x.id);
                    table.ForeignKey(
                        name: "fk_owner_invite_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_role",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_role", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_role_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_role_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "device",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    fingerprint = table.Column<string>(type: "text", nullable: false),
                    secret_hash = table.Column<string>(type: "text", nullable: true),
                    station_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_device", x => x.id);
                    table.ForeignKey(
                        name: "fk_device_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_device_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "edge_installation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    public_key = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    last_synced_seq = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    clock_offset_ms = table.Column<int>(type: "integer", nullable: true),
                    health = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    install_token_hash = table.Column<string>(type: "text", nullable: true),
                    token_expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    token_consumed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    installed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_edge_installation", x => x.id);
                    table.ForeignKey(
                        name: "fk_edge_installation_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_edge_installation_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "station",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    capacity_slots = table.Column<short>(type: "smallint", nullable: true),
                    avg_cook_seconds = table.Column<int>(type: "integer", nullable: true),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_station", x => x.id);
                    table.ForeignKey(
                        name: "fk_station_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    short_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    business_day = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    address_id = table.Column<Guid>(type: "uuid", nullable: true),
                    courier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    placed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    first_fired_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    ready_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    dispatched_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    served_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    promised_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    subtotal = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    discount_amount = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    delivery_fee = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    service_fee_amount = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    total = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    notes = table.Column<string>(type: "text", nullable: true),
                    cancel_reason = table.Column<string>(type: "text", nullable: true),
                    cancelled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    authorized_by = table.Column<Guid>(type: "uuid", nullable: true),
                    fiscal_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    fiscal_key = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: true),
                    fiscal_number = table.Column<int>(type: "integer", nullable: true),
                    fiscal_series = table.Column<short>(type: "smallint", nullable: true),
                    fiscal_protocol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    device_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_table_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "table_session",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "price",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "money_amount", nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    valid_to = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price", x => x.id);
                    table.ForeignKey(
                        name: "fk_price_product_variants_variant_id",
                        column: x => x.variant_id,
                        principalTable: "product_variant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auth_attempt",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    failed_attempts = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    blocked_until = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auth_attempt", x => x.id);
                    table.ForeignKey(
                        name: "fk_auth_attempt_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "device",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auth_session",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    refresh_hash = table.Column<string>(type: "text", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    last_active_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auth_session", x => x.id);
                    table.ForeignKey(
                        name: "fk_auth_session_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_auth_session_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "device",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "order_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    unit_price = table.Column<decimal>(type: "money_amount", nullable: false),
                    modifiers_total = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    total_price = table.Column<decimal>(type: "money_amount", nullable: false),
                    unit_cost = table.Column<decimal>(type: "money_amount", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    notes = table.Column<string>(type: "text", nullable: true),
                    placed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    fire_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    fired_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    oven_in_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    oven_out_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    ready_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    served_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    oven_slot = table.Column<short>(type: "smallint", nullable: true),
                    priority_score = table.Column<int>(type: "integer", nullable: true),
                    cancel_reason = table.Column<string>(type: "text", nullable: true),
                    cancelled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    authorized_by = table.Column<Guid>(type: "uuid", nullable: true),
                    refire_of_id = table.Column<Guid>(type: "uuid", nullable: true),
                    refire_reason = table.Column<string>(type: "text", nullable: true),
                    fired_by = table.Column<Guid>(type: "uuid", nullable: true),
                    ready_by = table.Column<Guid>(type: "uuid", nullable: true),
                    served_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_item_order_item_refire_of_id",
                        column: x => x.refire_of_id,
                        principalTable: "order_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_item_order_order_id",
                        column: x => x.order_id,
                        principalTable: "order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_order_item_product_variant_variant_id",
                        column: x => x.variant_id,
                        principalTable: "product_variant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_item_fraction",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    weight = table.Column<decimal>(type: "fraction_weight", nullable: false),
                    unit_price = table.Column<decimal>(type: "money_amount", nullable: false),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_item_fraction", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_item_fraction_order_item_order_item_id",
                        column: x => x.order_item_id,
                        principalTable: "order_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_order_item_fraction_product_variant_variant_id",
                        column: x => x.variant_id,
                        principalTable: "product_variant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_item_modifier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    modifier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    price_delta = table.Column<decimal>(type: "money_amount", nullable: false, defaultValue: 0m),
                    name_snapshot = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_item_modifier", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_item_modifier_order_item_order_item_id",
                        column: x => x.order_item_id,
                        principalTable: "order_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_alert_tenant_id_entity_type_entity_id",
                table: "alert",
                columns: new[] { "tenant_id", "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "idx_app_user_tenant",
                table: "app_user",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "uq_app_user_email",
                table: "app_user",
                columns: new[] { "tenant_id", "email" },
                unique: true,
                filter: "email IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_app_user_pin",
                table: "app_user",
                columns: new[] { "tenant_id", "pin_hash" },
                unique: true,
                filter: "pin_hash IS NOT NULL AND status = 0 AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_audit_action",
                table: "audit_log",
                columns: new[] { "tenant_id", "action", "occurred_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "idx_audit_actor",
                table: "audit_log",
                columns: new[] { "tenant_id", "actor_id", "occurred_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "idx_audit_entity",
                table: "audit_log",
                columns: new[] { "tenant_id", "entity", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "idx_audit_tenant_time",
                table: "audit_log",
                columns: new[] { "tenant_id", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_auth_attempt_tenant_blocked",
                table: "auth_attempt",
                columns: new[] { "tenant_id", "blocked_until" });

            migrationBuilder.CreateIndex(
                name: "uq_auth_attempt_device",
                table: "auth_attempt",
                column: "device_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_auth_session_tenant_user",
                table: "auth_session",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_auth_session_device_id",
                table: "auth_session",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "ix_auth_session_user_id",
                table: "auth_session",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_cash_movement_cash_session_id",
                table: "cash_movement",
                column: "cash_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_cash_movement_tenant_id_cash_session_id_occurred_at",
                table: "cash_movement",
                columns: new[] { "tenant_id", "cash_session_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_cash_session_tenant_id_business_day",
                table: "cash_session",
                columns: new[] { "tenant_id", "business_day" });

            migrationBuilder.CreateIndex(
                name: "idx_category_tenant_sort",
                table: "category",
                columns: new[] { "tenant_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_address_tenant_id_customer_id",
                table: "customer_address",
                columns: new[] { "tenant_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_delivery_stop_order_id",
                table: "delivery_stop",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_delivery_stop_tenant_id_run_id_sequence",
                table: "delivery_stop",
                columns: new[] { "tenant_id", "run_id", "sequence" });

            migrationBuilder.CreateIndex(
                name: "idx_device_tenant",
                table: "device",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_device_store_id",
                table: "device",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "uq_device_fingerprint",
                table: "device",
                columns: new[] { "tenant_id", "fingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dining_table_area_id",
                table: "dining_table",
                column: "area_id");

            migrationBuilder.CreateIndex(
                name: "uq_dining_table_qr_token",
                table: "dining_table",
                column: "qr_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_dining_table_store_label",
                table: "dining_table",
                columns: new[] { "store_id", "label" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_domain_event_tenant_time",
                table: "domain_event",
                columns: new[] { "tenant_id", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_edge_last_seen",
                table: "edge_installation",
                column: "last_seen_at",
                filter: "last_seen_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_edge_tenant",
                table: "edge_installation",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "uq_edge_store",
                table: "edge_installation",
                column: "store_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_email_outbox_pending",
                table: "email_outbox",
                columns: new[] { "tenant_id", "status", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "uq_expense_category_tenant_name",
                table: "expense_category",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_financial_entry_tenant_id_competence_date_type",
                table: "financial_entry",
                columns: new[] { "tenant_id", "competence_date", "type" });

            migrationBuilder.CreateIndex(
                name: "uq_goal_tenant_store_metric_period_valid_from",
                table: "goal",
                columns: new[] { "tenant_id", "store_id", "metric_code", "period", "valid_from" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_idempotency_key_tenant_expires",
                table: "idempotency_key",
                columns: new[] { "tenant_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ingredient_supplier_id",
                table: "ingredient",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_ingredient_uom_code",
                table: "ingredient",
                column: "uom_code");

            migrationBuilder.CreateIndex(
                name: "idx_installation_nonce_tenant_expires",
                table: "installation_nonce",
                columns: new[] { "tenant_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "uq_inventory_count_item_count_ingredient",
                table: "inventory_count_item",
                columns: new[] { "count_id", "ingredient_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_media_asset_owner",
                table: "media_asset",
                columns: new[] { "tenant_id", "owner_type", "owner_id" });

            migrationBuilder.CreateIndex(
                name: "uq_media_asset",
                table: "media_asset",
                columns: new[] { "tenant_id", "owner_type", "owner_id", "variant", "content_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_metric_hourly_tenant_id_business_day",
                table: "metric_hourly",
                columns: new[] { "tenant_id", "business_day" });

            migrationBuilder.CreateIndex(
                name: "ix_metric_product_daily_tenant_id_business_day",
                table: "metric_product_daily",
                columns: new[] { "tenant_id", "business_day" });

            migrationBuilder.CreateIndex(
                name: "ix_modifier_group_id",
                table: "modifier",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "idx_order_placed_desc",
                table: "order",
                columns: new[] { "tenant_id", "placed_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_order_session_id",
                table: "order",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_tenant_id_business_day_channel",
                table: "order",
                columns: new[] { "tenant_id", "business_day", "channel" });

            migrationBuilder.CreateIndex(
                name: "uq_order_short_code",
                table: "order",
                columns: new[] { "store_id", "business_day", "short_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_item_queue",
                table: "order_item",
                columns: new[] { "tenant_id", "station_id", "status", "placed_at" },
                filter: "status IN (0,1,2,3)");

            migrationBuilder.CreateIndex(
                name: "ix_order_item_order_id",
                table: "order_item",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_item_refire_of_id",
                table: "order_item",
                column: "refire_of_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_item_tenant_id_variant_id_placed_at",
                table: "order_item",
                columns: new[] { "tenant_id", "variant_id", "placed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_order_item_variant_id",
                table: "order_item",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "idx_order_item_fraction_tenant_item",
                table: "order_item_fraction",
                columns: new[] { "tenant_id", "order_item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_order_item_fraction_order_item_id",
                table: "order_item_fraction",
                column: "order_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_item_fraction_variant_id",
                table: "order_item_fraction",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "idx_order_item_modifier_tenant_item",
                table: "order_item_modifier",
                columns: new[] { "tenant_id", "order_item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_order_item_modifier_order_item_id",
                table: "order_item_modifier",
                column: "order_item_id");

            migrationBuilder.CreateIndex(
                name: "idx_owner_invite_tenant_expires",
                table: "owner_invite",
                columns: new[] { "tenant_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_owner_invite_user_id",
                table: "owner_invite",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_pairing_code_tenant_expires",
                table: "pairing_code",
                columns: new[] { "tenant_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_tenant_id_business_day_method",
                table: "payment",
                columns: new[] { "tenant_id", "business_day", "method" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_allocation_tenant_id_order_id",
                table: "payment_allocation",
                columns: new[] { "tenant_id", "order_id" });

            migrationBuilder.CreateIndex(
                name: "uq_payment_allocation_payment_order",
                table: "payment_allocation",
                columns: new[] { "payment_id", "order_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_payroll_tenant_store_period",
                table: "payroll",
                columns: new[] { "tenant_id", "store_id", "period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_payroll_item_payroll_employee",
                table: "payroll_item",
                columns: new[] { "payroll_id", "employee_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_price_tenant_variant_channel_valid_from",
                table: "price",
                columns: new[] { "tenant_id", "variant_id", "channel", "valid_from" },
                descending: new[] { false, false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_price_variant_id",
                table: "price",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "idx_product_tenant_category_sort",
                table: "product",
                columns: new[] { "tenant_id", "category_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_product_category_id",
                table: "product",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_modifier_group_group_id",
                table: "product_modifier_group",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "idx_product_variant_tenant_product",
                table: "product_variant",
                columns: new[] { "tenant_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "ix_product_variant_product_id",
                table: "product_variant",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "uq_purchase_tenant_supplier_document",
                table: "purchase",
                columns: new[] { "tenant_id", "supplier_id", "document" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_item_purchase_id",
                table: "purchase_item",
                column: "purchase_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_item_tenant_id_purchase_id",
                table: "purchase_item",
                columns: new[] { "tenant_id", "purchase_id" });

            migrationBuilder.CreateIndex(
                name: "ix_recipe_item_recipe_id",
                table: "recipe_item",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_item_sub_recipe_id",
                table: "recipe_item",
                column: "sub_recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_item_tenant_id_recipe_id",
                table: "recipe_item",
                columns: new[] { "tenant_id", "recipe_id" });

            migrationBuilder.CreateIndex(
                name: "uq_role_code",
                table: "role",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_station_tenant_store",
                table: "station",
                columns: new[] { "tenant_id", "store_id" });

            migrationBuilder.CreateIndex(
                name: "ix_station_store_id",
                table: "station",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "uq_station_code",
                table: "station",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_stock_movement_ingredient",
                table: "stock_movement",
                columns: new[] { "tenant_id", "ingredient_id", "occurred_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_stock_movement_tenant_id_business_day_type",
                table: "stock_movement",
                columns: new[] { "tenant_id", "business_day", "type" });

            migrationBuilder.CreateIndex(
                name: "uq_store_default",
                table: "store",
                column: "tenant_id",
                unique: true,
                filter: "is_default AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_sync_conflict_tenant_detected_desc",
                table: "sync_conflict",
                columns: new[] { "tenant_id", "detected_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_table_session_tenant_business_day",
                table: "table_session",
                columns: new[] { "tenant_id", "business_day" });

            migrationBuilder.CreateIndex(
                name: "ix_table_session_table_id",
                table: "table_session",
                column: "table_id");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_domain",
                table: "tenant",
                column: "domain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_tenant_slug",
                table: "tenant",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_role_tenant",
                table: "user_role",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_role_role_id",
                table: "user_role",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "uq_user_role",
                table: "user_role",
                columns: new[] { "user_id", "role_id", "store_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alert");

            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "auth_attempt");

            migrationBuilder.DropTable(
                name: "auth_session");

            migrationBuilder.DropTable(
                name: "cash_movement");

            migrationBuilder.DropTable(
                name: "courier");

            migrationBuilder.DropTable(
                name: "customer");

            migrationBuilder.DropTable(
                name: "customer_address");

            migrationBuilder.DropTable(
                name: "delivery_run");

            migrationBuilder.DropTable(
                name: "delivery_stop");

            migrationBuilder.DropTable(
                name: "delivery_zone");

            migrationBuilder.DropTable(
                name: "domain_event");

            migrationBuilder.DropTable(
                name: "edge_installation");

            migrationBuilder.DropTable(
                name: "email_outbox");

            migrationBuilder.DropTable(
                name: "employee");

            migrationBuilder.DropTable(
                name: "expense_category");

            migrationBuilder.DropTable(
                name: "financial_account");

            migrationBuilder.DropTable(
                name: "financial_entry");

            migrationBuilder.DropTable(
                name: "goal");

            migrationBuilder.DropTable(
                name: "idempotency_key");

            migrationBuilder.DropTable(
                name: "ingredient");

            migrationBuilder.DropTable(
                name: "installation_nonce");

            migrationBuilder.DropTable(
                name: "inventory_count_item");

            migrationBuilder.DropTable(
                name: "media_asset");

            migrationBuilder.DropTable(
                name: "metric_daily");

            migrationBuilder.DropTable(
                name: "metric_hourly");

            migrationBuilder.DropTable(
                name: "metric_operator_daily");

            migrationBuilder.DropTable(
                name: "metric_product_daily");

            migrationBuilder.DropTable(
                name: "modifier");

            migrationBuilder.DropTable(
                name: "order_item_fraction");

            migrationBuilder.DropTable(
                name: "order_item_modifier");

            migrationBuilder.DropTable(
                name: "outbox");

            migrationBuilder.DropTable(
                name: "owner_invite");

            migrationBuilder.DropTable(
                name: "pairing_code");

            migrationBuilder.DropTable(
                name: "payment_allocation");

            migrationBuilder.DropTable(
                name: "payroll");

            migrationBuilder.DropTable(
                name: "payroll_item");

            migrationBuilder.DropTable(
                name: "price");

            migrationBuilder.DropTable(
                name: "product_modifier_group");

            migrationBuilder.DropTable(
                name: "purchase_item");

            migrationBuilder.DropTable(
                name: "recipe_item");

            migrationBuilder.DropTable(
                name: "station");

            migrationBuilder.DropTable(
                name: "stock_movement");

            migrationBuilder.DropTable(
                name: "sync_conflict");

            migrationBuilder.DropTable(
                name: "sync_cursor");

            migrationBuilder.DropTable(
                name: "tenant_config");

            migrationBuilder.DropTable(
                name: "tenant_secret");

            migrationBuilder.DropTable(
                name: "user_role");

            migrationBuilder.DropTable(
                name: "device");

            migrationBuilder.DropTable(
                name: "cash_session");

            migrationBuilder.DropTable(
                name: "supplier");

            migrationBuilder.DropTable(
                name: "unit_of_measure");

            migrationBuilder.DropTable(
                name: "inventory_count");

            migrationBuilder.DropTable(
                name: "order_item");

            migrationBuilder.DropTable(
                name: "payment");

            migrationBuilder.DropTable(
                name: "modifier_group");

            migrationBuilder.DropTable(
                name: "purchase");

            migrationBuilder.DropTable(
                name: "recipe");

            migrationBuilder.DropTable(
                name: "app_user");

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropTable(
                name: "store");

            migrationBuilder.DropTable(
                name: "order");

            migrationBuilder.DropTable(
                name: "product_variant");

            migrationBuilder.DropTable(
                name: "tenant");

            migrationBuilder.DropTable(
                name: "table_session");

            migrationBuilder.DropTable(
                name: "product");

            migrationBuilder.DropTable(
                name: "dining_table");

            migrationBuilder.DropTable(
                name: "category");

            migrationBuilder.DropTable(
                name: "area");

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS current_tenant_id();");
            migrationBuilder.Sql("DROP DOMAIN IF EXISTS slug;");
            migrationBuilder.Sql("DROP DOMAIN IF EXISTS fraction_weight;");
            migrationBuilder.Sql("DROP DOMAIN IF EXISTS percent_amount;");
            migrationBuilder.Sql("DROP DOMAIN IF EXISTS qty_amount;");
            migrationBuilder.Sql("DROP DOMAIN IF EXISTS money_amount;");
        }
    }
}
