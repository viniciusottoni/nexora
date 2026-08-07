# 01 — Plataforma e identidade

| | |
|---|---|
| **Ordem de execução** | 2 de 12 |
| **Depende de** | `00-Convencoes-e-Tipos.md` |
| **ADRs** | [004](../ADRs/ADR-004-postgresql-rls-multitenancy.md), [013](../ADRs/ADR-013-proibicao-de-codigo-por-cliente.md), [014](../ADRs/ADR-014-autenticacao-por-pin.md), [023](../ADRs/ADR-023-modelo-de-autorizacao.md), [031](../ADRs/ADR-031-gestao-de-segredos.md), [032](../ADRs/ADR-032-configuracao-e-feature-flags.md) |

---

## ERD

```mermaid
erDiagram
    tenant ||--|| tenant_config : "configura"
    tenant ||--o{ store : "possui"
    tenant ||--o{ app_user : "possui"
    tenant ||--o{ role : "define"
    tenant ||--o{ tenant_secret : "guarda"
    tenant ||--o{ audit_log : "registra"
    store  ||--o{ device : "hospeda"
    app_user }o--o{ role : "user_role"
    device }o--|| store : "pertence"

    tenant {
        uuid id PK
        slug slug UK
        text name
        text document
        tenant_status status
        text timezone
        text locale
        char currency
    }
    tenant_config {
        uuid tenant_id PK_FK
        jsonb branding
        jsonb operation
        jsonb thresholds
        jsonb modules
        jsonb fiscal
        jsonb printers
        int catalog_version
        int config_version
        int branding_version
    }
    store {
        uuid id PK
        uuid tenant_id FK
        text name
        jsonb address
        bool is_default
    }
    app_user {
        uuid id PK
        uuid tenant_id FK
        text name
        email email UK
        text password_hash
        text pin_hash
        user_status status
        timestamptz pin_rotated_at
    }
    role {
        uuid id PK
        uuid tenant_id FK
        text code
        text name
        jsonb permissions
        bool is_system
    }
    device {
        uuid id PK
        uuid tenant_id FK
        uuid store_id FK
        text label
        device_type type
        text fingerprint UK
        uuid station_id FK
        timestamptz deleted_at
    }
    audit_log {
        uuid id PK
        uuid tenant_id FK
        uuid actor_id
        uuid authorized_by
        text action
        text entity
        uuid entity_id
        jsonb before
        jsonb after
        uuid domain_event_id
        varchar trace_id
        timestamptz occurred_at
    }
    tenant_secret {
        uuid tenant_id PK_FK
        text key PK
        bytea ciphertext
        int key_version
    }
```

---

## DDL

### tenant

```sql
CREATE TABLE tenant (
  id          UUID PRIMARY KEY,
  slug        slug NOT NULL,
  name        TEXT NOT NULL,
  legal_name  TEXT,
  document    VARCHAR(18),                    -- CNPJ
  status      tenant_status NOT NULL DEFAULT 'TRIAL',
  plan        VARCHAR(32) NOT NULL DEFAULT 'STANDARD',
  timezone    TEXT NOT NULL DEFAULT 'America/Sao_Paulo',
  locale      TEXT NOT NULL DEFAULT 'pt-BR',
  currency    CHAR(3) NOT NULL DEFAULT 'BRL',
  domain      TEXT,                           -- domínio próprio (ADR-010)
  created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
  deleted_at  TIMESTAMPTZ,

  CONSTRAINT uq_tenant_slug   UNIQUE (slug),
  CONSTRAINT uq_tenant_domain UNIQUE (domain)
);

COMMENT ON TABLE tenant IS 'Estabelecimento — unidade de isolamento (ADR-004). Sem tenant_id: é a raiz.';
```

> `tenant` é a única tabela de negócio **sem** `tenant_id` e **sem** RLS — ela é a raiz da hierarquia.

### tenant_config

Toda a diferença entre uma pizzaria e uma hamburgueria vive aqui (ADR-013, ADR-032).

```sql
CREATE TABLE tenant_config (
  tenant_id        UUID PRIMARY KEY REFERENCES tenant(id) ON DELETE CASCADE,

  branding         JSONB NOT NULL DEFAULT '{}'::jsonb,
  operation        JSONB NOT NULL DEFAULT '{}'::jsonb,
  thresholds       JSONB NOT NULL DEFAULT '{}'::jsonb,
  modules          JSONB NOT NULL DEFAULT '{}'::jsonb,
  fiscal           JSONB NOT NULL DEFAULT '{}'::jsonb,   -- ADR-025
  printers         JSONB NOT NULL DEFAULT '[]'::jsonb,   -- ADR-026
  payments         JSONB NOT NULL DEFAULT '{}'::jsonb,   -- ADR-024
  maintenance      JSONB NOT NULL DEFAULT '{}'::jsonb,   -- janela de atualização (ADR-019)

  -- versões independentes para invalidação seletiva (ADR-028)
  catalog_version  INT NOT NULL DEFAULT 1,
  config_version   INT NOT NULL DEFAULT 1,
  branding_version INT NOT NULL DEFAULT 1,

  created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

Estrutura esperada de `operation` (validada por FluentValidation na aplicação):

```json
{
  "serviceFeePercent": 10,
  "serviceFeeOptional": true,
  "maxDiscountPercentWithoutApproval": 5,
  "halfAndHalfPricing": "HIGHEST",
  "maxFractions": 4,
  "stockDeductionMoment": "ITEM_READY",
  "businessDayStartHour": 5,
  "blockCloseWithPendingItems": true,
  "blockCashCloseWithOpenTables": true,
  "bottleneck": { "resource": "OVEN", "slots": 5, "avgCookMinutes": 7 }
}
```

Estrutura esperada de `thresholds` (E-08/US-080 — leitura tipada em
`iMenu.Application.Alerts.Support.AlertThresholds`; chave ausente usa o padrão do template de
negócio, US-080 §4 "Limiar padrão do modelo de negócio"):

```json
{
  "orderWarnMinutes": 12,
  "orderCriticalMinutes": 18,
  "itemInWindowMinutes": 2,
  "tableIdleMinutes": 10,
  "cashDivergenceAlert": 20.00,
  "cmvDivergencePercent": 5,
  "dineInPromiseMinutes": 10,
  "deliveryPromiseMinutes": 25,
  "avgTimeAboveTargetPercent": 20,
  "cancellationCountThreshold": 5,
  "cancellationWindowMinutes": 60,
  "discountAboveThresholdPercent": 15,
  "discountWindowMinutes": 60
}
```

Os cinco últimos campos (`avgTimeAboveTargetPercent` em diante) foram introduzidos pelo motor de
alertas (E-08/US-080 §2 "tempo médio acima da meta"/"cancelamento ou desconto acima do padrão") —
ausentes no seed original do template PIZZERIA, caem no padrão do próprio `AlertThresholds.Default`.

A matriz de direcionamento por tipo de alerta (US-082) e a janela de agrupamento por tipo (US-083)
vivem dentro de `operation.alertRouting` (não de `thresholds`) — ver Docs/Domain/09 §"alert" para o
racional de não terem ganhado coluna própria.

### store

```sql
CREATE TABLE store (
  id          UUID PRIMARY KEY,
  tenant_id   UUID NOT NULL REFERENCES tenant(id),
  name        TEXT NOT NULL,
  address     JSONB,
  phone       VARCHAR(20),
  is_default  BOOLEAN NOT NULL DEFAULT false,
  is_active   BOOLEAN NOT NULL DEFAULT true,
  created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
  deleted_at  TIMESTAMPTZ
);

-- exatamente uma loja padrão por tenant
CREATE UNIQUE INDEX uq_store_default ON store (tenant_id)
  WHERE is_default AND deleted_at IS NULL;
```

> Preparado para rede multi-unidade. Na Fase 1, todo tenant tem uma única loja.

> **`edge_installation` removida em 06/08/2026** ([ADR-040](../adrs/ADR-040-arquitetura-100-online-api-unica.md), [E-16/US-169](../user%20stories/E-16-iMenu-Online/US-169-Migracao-do-modelo-de-dados.md)). Sem servidor local por loja, não há mais instalação de edge a registrar.

### app_user

> Renomeada de `user` porque `user` é palavra reservada no PostgreSQL.

```sql
CREATE TABLE app_user (
  id              UUID PRIMARY KEY,
  tenant_id       UUID NOT NULL REFERENCES tenant(id),
  name            TEXT NOT NULL,
  email           email,
  password_hash   TEXT,                       -- Argon2id · gestor e administrativo
  pin_hash        TEXT,                       -- Argon2id · operação (ADR-014)
  pin_rotated_at  TIMESTAMPTZ,
  status          user_status NOT NULL DEFAULT 'ACTIVE',
  failed_attempts SMALLINT NOT NULL DEFAULT 0,
  blocked_until   TIMESTAMPTZ,
  last_login_at   TIMESTAMPTZ,
  created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
  deleted_at      TIMESTAMPTZ,

  -- INVITED (doc 12 §8, US-002): dono recém-provisionado ainda sem senha/PIN — só existe até o
  -- convite ser aceito (transição para ACTIVE via app_user.password_hash), daí a exceção abaixo.
  CONSTRAINT ck_app_user_credential
    CHECK (password_hash IS NOT NULL OR pin_hash IS NOT NULL OR status = 'INVITED')
);

CREATE UNIQUE INDEX uq_app_user_email ON app_user (tenant_id, email)
  WHERE email IS NOT NULL AND deleted_at IS NULL;

-- PIN não pode repetir entre usuários ativos do mesmo tenant (ADR-014)
CREATE UNIQUE INDEX uq_app_user_pin ON app_user (tenant_id, pin_hash)
  WHERE pin_hash IS NOT NULL AND status = 'ACTIVE' AND deleted_at IS NULL;
```

### role e user_role

```sql
CREATE TABLE role (
  id          UUID PRIMARY KEY,
  tenant_id   UUID NOT NULL REFERENCES tenant(id),
  code        VARCHAR(32) NOT NULL,
  name        TEXT NOT NULL,
  permissions JSONB NOT NULL DEFAULT '[]'::jsonb,   -- ["order:create", ...]
  is_system   BOOLEAN NOT NULL DEFAULT false,
  created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
  deleted_at  TIMESTAMPTZ,

  CONSTRAINT uq_role_code UNIQUE (tenant_id, code)
);

CREATE TABLE user_role (
  user_id   UUID NOT NULL REFERENCES app_user(id) ON DELETE CASCADE,
  role_id   UUID NOT NULL REFERENCES role(id)     ON DELETE CASCADE,
  store_id  UUID REFERENCES store(id),            -- NULL = todas as lojas
  tenant_id UUID NOT NULL REFERENCES tenant(id),
  PRIMARY KEY (user_id, role_id, COALESCE(store_id, '00000000-0000-0000-0000-000000000000'::uuid))
);
```

### device

```sql
CREATE TABLE device (
  id           UUID PRIMARY KEY,
  tenant_id    UUID NOT NULL REFERENCES tenant(id),
  store_id     UUID NOT NULL REFERENCES store(id),
  label        TEXT NOT NULL,
  type         device_type NOT NULL,
  fingerprint  TEXT NOT NULL,
  station_id   UUID,                        -- FK adicionada no doc. 02
  is_active    BOOLEAN NOT NULL DEFAULT true,
  last_seen_at TIMESTAMPTZ,
  created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
  deleted_at   TIMESTAMPTZ,                -- soft delete de dispositivo já revogado; some da
                                            -- listagem, mas o fingerprint continua reservado (a
                                            -- constraint abaixo NÃO filtra deleted_at de propósito:
                                            -- reautorizar reaproveita a mesma linha e limpa este
                                            -- campo, nunca cria outro registro para a instalação)

  CONSTRAINT uq_device_fingerprint UNIQUE (tenant_id, fingerprint)
);
```

### audit_log

Append-only — imutabilidade real por **revogação de permissão** (`REVOKE UPDATE, DELETE ON audit_log FROM app_user_role`, migration `PartitionAuditLogAndRestrictMutation`, E-09/US-090), não por trigger. Particionada por `RANGE (occurred_at)`, mensal (ADR-035) — a chave de partição entra na `PRIMARY KEY` (mesma técnica de `domain_event`, ver ADR-035; `domain_event` em si ainda não está particionada nesta base — gap operacional registrado à parte). Retenção: 5 anos, arquivamento frio (ADR-035, tabela de retenção).

```sql
CREATE TABLE audit_log (
  id              UUID NOT NULL,
  tenant_id       UUID NOT NULL REFERENCES tenant(id),
  store_id        UUID,
  actor_id        UUID,                       -- quem executou
  authorized_by   UUID,                       -- quem autorizou (ADR-023)
  device_id       UUID,
  action          TEXT NOT NULL,              -- 'ORDER_ITEM_CANCELLED'
  entity          TEXT NOT NULL,              -- 'order_item'
  entity_id       UUID,
  before          JSONB,
  after           JSONB,
  reason          TEXT,
  ip              INET,
  domain_event_id UUID,                       -- correlação com domain_event.id (E-09/US-090)
  trace_id        VARCHAR(32),                -- W3C Trace Context (ADR-022)
  occurred_at     TIMESTAMPTZ NOT NULL,       -- ADR-034 — também a chave de partição
  recorded_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
  PRIMARY KEY (id, occurred_at)
) PARTITION BY RANGE (occurred_at);

CREATE INDEX idx_audit_tenant_time   ON audit_log (tenant_id, occurred_at DESC);
CREATE INDEX idx_audit_entity        ON audit_log (tenant_id, entity, entity_id);
CREATE INDEX idx_audit_actor         ON audit_log (tenant_id, actor_id, occurred_at DESC);
CREATE INDEX idx_audit_action        ON audit_log (tenant_id, action, occurred_at DESC);

REVOKE UPDATE, DELETE ON audit_log FROM app_user_role;
```

> Cobertura das ações sensíveis do RF-AUD-02: cancelamento, desconto (via autorização elevada),
> alteração de preço e alteração de permissão já emitem registro. Movimentação de estoque, ajuste
> financeiro e abertura/fechamento de caixa **não têm caso de uso de Application implementado
> ainda** (só entidade de domínio) — pendência conhecida e documentada em
> `iMenu.IntegrationTests.AuditCoverageTests`, não esquecida; depende de `Docs/User Stories/`
> próprias para Caixa/Estoque/Financeiro, fora do escopo de E-09.

### tenant_secret

Credenciais criptografadas em repouso (ADR-031).

```sql
CREATE TABLE tenant_secret (
  tenant_id   UUID NOT NULL REFERENCES tenant(id) ON DELETE CASCADE,
  key         TEXT NOT NULL,                 -- 'mercadopago.accessToken'
  ciphertext  BYTEA NOT NULL,                -- AES-256-GCM
  key_version INT  NOT NULL,
  created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
  PRIMARY KEY (tenant_id, key)
);

COMMENT ON TABLE tenant_secret IS
  'Nunca retorna pela API, nem para OWNER. Nunca trafega para o edge. (ADR-031)';
```

---

## Regras de integridade

| # | Regra | Onde |
|---|---|---|
| 1 | Usuário precisa ter senha **ou** PIN | `ck_app_user_credential` |
| 2 | PIN único entre usuários ativos do tenant | `uq_app_user_pin` |
| 3 | Uma única loja padrão por tenant | `uq_store_default` |
| 4 | Auditoria não aceita UPDATE nem DELETE | Documento 10 |
| 5 | Papel de sistema não pode ser excluído | Aplicação + `is_system` |
