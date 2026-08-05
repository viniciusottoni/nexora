import { useEffect, useId, useState } from 'react';
import {
  AlertBanner,
  Badge,
  Button,
  Card,
  DataTable,
  EmptyState,
  Field,
  Icon,
  Input,
  Select,
  type DataTableColumn,
} from '@nexora/ui';
import type { AuditLogEntry, AuditLogFilters } from '@nexora/contracts';
import { AuditApi } from './audit-api.js';
import './audit-log.css';

/**
 * US-091 (Consulta e filtro da trilha) — tela de consulta/filtro da trilha de auditoria (RF-AUD-03).
 * Casca padrão `db-page` (CLAUDE.md § Layout das telas de gestão), tabela via `DataTable`
 * (`packages/ui/src/components/data-table.tsx`, cuja própria docstring já reserva o componente
 * para "caixa, estoque, financeiro e auditoria"). Cada linha mostra `summary` — a frase pronta em
 * português que o backend já monta — nunca `before`/`after` cru; expandir uma linha mostra
 * autor/autorizador/dispositivo/motivo/origem e o antes/depois como lista de chave→valor (Gherkin
 * da US: "não deve exibir JSON bruto ao gestor").
 *
 * Paginação por cursor em modo APPEND: "Carregar mais" busca a próxima página com
 * `meta.nextCursor` e concatena ao final da lista já carregada (não substitui a página inteira) —
 * ver `loadMore()` abaixo.
 *
 * Sem navegação real para pedido/mesa/produto (esses apps ainda não existem neste painel): tocar
 * na origem (`target.label`) só copia o id para a área de transferência, nunca inventa uma rota.
 *
 * Auto-suficiente de propósito (busca os próprios dados via `AuditApi`, não recebe lista pronta em
 * prop) — mesmo padrão de `UnavailableListPage`/`AvailabilityApi`, porque a trilha não é uma lista
 * pequena e completa como áreas/categorias: ela é buscada sob demanda (na montagem e a cada filtro
 * novo), nunca pré-carregada no boot do `CloudAdmin`.
 */

const ACTION_OPTIONS: ReadonlyArray<{ value: string; label: string }> = [
  { value: '', label: 'Todas as ações' },
  { value: 'ORDER_CANCELLED', label: 'Pedido cancelado' },
  { value: 'ORDER_ITEM_CANCELLED', label: 'Item de pedido cancelado' },
  { value: 'ORDER_CANCEL_DENIED', label: 'Cancelamento negado' },
  { value: 'DISCOUNT_APPLIED', label: 'Desconto aplicado' },
  { value: 'PRICE_CHANGED', label: 'Preço alterado' },
  { value: 'VARIANT_PRICE_CHANGED', label: 'Preço de variação alterado' },
  { value: 'PRICE_BULK_ADJUSTED', label: 'Reajuste de preço em massa' },
  { value: 'PERMISSION_CHANGED', label: 'Permissão alterada' },
  { value: 'SUPPORT_ACCESS_GRANTED', label: 'Acesso de suporte concedido' },
  {
    value: 'tenant.cross_tenant_access_attempt',
    label: 'Tentativa de acesso entre estabelecimentos',
  },
];

const ENTITY_OPTIONS: ReadonlyArray<{ value: string; label: string }> = [
  { value: '', label: 'Todas as entidades' },
  { value: 'order', label: 'Pedido' },
  { value: 'order_item', label: 'Item de pedido' },
  { value: 'role', label: 'Papel' },
  { value: 'price', label: 'Preço' },
  { value: 'tenant', label: 'Estabelecimento' },
  { value: 'authorization', label: 'Autorização' },
];

const ACTION_LABEL_BY_VALUE = new Map(
  ACTION_OPTIONS.filter((option) => option.value).map((option) => [option.value, option.label]),
);

interface FilterFormState {
  readonly from: string;
  readonly to: string;
  readonly actorId: string;
  readonly authorizedBy: string;
  readonly entityId: string;
  readonly action: string;
  readonly entity: string;
  /** Valor mínimo em centavos — mesma técnica de máscara de moeda de `price-table-page.tsx`. */
  readonly minAmountCents: number;
}

const EMPTY_FORM: FilterFormState = {
  from: '',
  to: '',
  actorId: '',
  authorizedBy: '',
  entityId: '',
  action: '',
  entity: '',
  minAmountCents: 0,
};

export interface AuditLogPageProps {
  /** Injetável para teste — padrão `new AuditApi()`. */
  readonly auditApi?: AuditApi;
}

export function AuditLogPage({ auditApi = new AuditApi() }: Readonly<AuditLogPageProps>) {
  const fromFieldId = useId();
  const toFieldId = useId();
  const actorFieldId = useId();
  const authorizedByFieldId = useId();
  const actionFieldId = useId();
  const entityFieldId = useId();
  const entityIdFieldId = useId();
  const minAmountFieldId = useId();

  const [form, setForm] = useState<FilterFormState>(EMPTY_FORM);
  const [appliedFilters, setAppliedFilters] = useState<AuditLogFilters>({});
  const [entries, setEntries] = useState<readonly AuditLogEntry[]>([]);
  const [meta, setMeta] = useState<{ nextCursor: string | null; hasMore: boolean }>();
  const [hasQueried, setHasQueried] = useState(false);
  const [loading, setLoading] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string>();
  const [expandedId, setExpandedId] = useState<string>();
  const [copiedTargetId, setCopiedTargetId] = useState<string>();

  async function runQuery(nextFilters: AuditLogFilters, append: boolean): Promise<void> {
    if (append) setLoadingMore(true);
    else setLoading(true);
    setError(undefined);
    try {
      const response = await auditApi.list(nextFilters);
      setEntries((current) => (append ? [...current, ...response.data] : response.data));
      setMeta(response.meta);
      setAppliedFilters(nextFilters);
      setHasQueried(true);
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      if (append) setLoadingMore(false);
      else setLoading(false);
    }
  }

  // Busca sob demanda: dispara na montagem da própria tela (quando o gestor abre a seção), não no
  // boot do CloudAdmin — ver nota de integração no topo do arquivo.
  useEffect(() => {
    void runQuery(toApiFilters(EMPTY_FORM), false);
  }, []);

  function applyFilters(): void {
    setExpandedId(undefined);
    void runQuery(toApiFilters(form), false);
  }

  function loadMore(): void {
    if (!meta?.nextCursor) return;
    void runQuery({ ...appliedFilters, cursor: meta.nextCursor }, true);
  }

  async function copyTargetId(id: string): Promise<void> {
    try {
      await navigator.clipboard.writeText(id);
      setCopiedTargetId(id);
    } catch {
      // Área de transferência indisponível (ex.: ambiente de teste) — falha silenciosa, o id
      // continua visível para cópia manual.
    }
  }

  const columns: ReadonlyArray<DataTableColumn<AuditLogEntry>> = [
    {
      key: 'occurredAt',
      header: 'Quando',
      width: '10.5rem',
      render: (row) => <time dateTime={row.occurredAt}>{formatDateTime(row.occurredAt)}</time>,
    },
    {
      key: 'action',
      header: 'Ação',
      render: (row) => <Badge tone={actionTone(row.action)}>{actionLabel(row.action)}</Badge>,
    },
    { key: 'summary', header: 'Resumo' },
    {
      key: 'actor',
      header: 'Autor',
      render: (row) => row.actor?.name ?? 'Sistema',
    },
    {
      key: 'authorizedBy',
      header: 'Autorizado por',
      render: (row) => row.authorizedBy?.name ?? '—',
    },
    {
      key: 'target',
      header: 'Origem',
      render: (row) => row.target?.label ?? '—',
    },
  ];

  const expandedEntry = entries.find((entry) => entry.id === expandedId);

  return (
    <main className="db-page nx-anim-in" aria-labelledby="audit-log-title">
      <header className="db-page__header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Auditoria</p>
          <h1 className="db-page__title" id="audit-log-title">
            Trilha de auditoria
          </h1>
          <p className="db-page__lead">
            Consulte quem fez o quê, quando e com autorização de quem — para investigar uma
            suspeita sem depender de suporte técnico (RF-AUD-03).
          </p>
        </div>
      </header>

      <AlertBanner tone="info" icon="visibility">
        O acesso a esta trilha também fica registrado, com autor, horário e dispositivo (RN-004).
      </AlertBanner>

      {error ? <AlertBanner tone="danger">{error}</AlertBanner> : null}

      <Card
        title="Filtros"
        subtitle="Pensados para as perguntas reais do gestor — quem, quando, o quê e quanto."
        className="db-form-card"
      >
        <div className="audit-filters">
          <Field label="De" htmlFor={fromFieldId}>
            <Input
              id={fromFieldId}
              type="datetime-local"
              value={form.from}
              onChange={(event) => setForm((current) => ({ ...current, from: event.target.value }))}
            />
          </Field>
          <Field label="Até" htmlFor={toFieldId}>
            <Input
              id={toFieldId}
              type="datetime-local"
              value={form.to}
              onChange={(event) => setForm((current) => ({ ...current, to: event.target.value }))}
            />
          </Field>
          <Field label="Operador (ID)" htmlFor={actorFieldId} hint="UUID de quem executou a ação">
            <Input
              id={actorFieldId}
              value={form.actorId}
              onChange={(event) =>
                setForm((current) => ({ ...current, actorId: event.target.value }))
              }
            />
          </Field>
          <Field label="Autorizado por (ID)" htmlFor={authorizedByFieldId}>
            <Input
              id={authorizedByFieldId}
              value={form.authorizedBy}
              onChange={(event) =>
                setForm((current) => ({ ...current, authorizedBy: event.target.value }))
              }
            />
          </Field>
          <Field label="Tipo de ação" htmlFor={actionFieldId}>
            <Select
              id={actionFieldId}
              value={form.action}
              onChange={(event) =>
                setForm((current) => ({ ...current, action: event.target.value }))
              }
              options={ACTION_OPTIONS}
            />
          </Field>
          <Field label="Entidade" htmlFor={entityFieldId}>
            <Select
              id={entityFieldId}
              value={form.entity}
              onChange={(event) =>
                setForm((current) => ({ ...current, entity: event.target.value }))
              }
              options={ENTITY_OPTIONS}
            />
          </Field>
          <Field label="ID da entidade" htmlFor={entityIdFieldId}>
            <Input
              id={entityIdFieldId}
              value={form.entityId}
              onChange={(event) =>
                setForm((current) => ({ ...current, entityId: event.target.value }))
              }
            />
          </Field>
          <Field
            label="Valor mínimo"
            htmlFor={minAmountFieldId}
            hint="Ex.: descontos ou cancelamentos acima deste valor"
          >
            <Input
              id={minAmountFieldId}
              numeric
              inputMode="numeric"
              prefix="R$"
              value={centsToDisplay(form.minAmountCents)}
              onChange={(event) =>
                setForm((current) => ({
                  ...current,
                  minAmountCents: digitsToCents(event.target.value),
                }))
              }
            />
          </Field>
        </div>

        <div className="db-editor__footer">
          <p className="db-hint">
            {entries.length > 0
              ? `${entries.length} registro(s) carregado(s)`
              : 'Ajuste os filtros e consulte a trilha'}
          </p>
          <Button type="button" busy={loading} onClick={applyFilters}>
            <Icon name="search" size={18} />
            Filtrar
          </Button>
        </div>
      </Card>

      <Card
        as="section"
        aria-label="Registros da trilha"
        title="Registros"
        subtitle="Toque em uma linha para ver autor, autorizador, motivo e o antes/depois."
      >
        {!hasQueried && loading ? (
          <output className="db-loading">
            <span className="nx-spinner" aria-hidden="true" />
            Consultando a trilha…
          </output>
        ) : entries.length === 0 ? (
          <EmptyState icon="history" title="Nenhum registro encontrado">
            Ajuste os filtros — período, operador, tipo de ação ou valor mínimo.
          </EmptyState>
        ) : (
          <>
            <DataTable
              columns={columns}
              rows={entries}
              rowKey="id"
              onRowClick={(row) =>
                setExpandedId((current) => (current === row.id ? undefined : row.id))
              }
              className="nx-anim-in"
            />

            {expandedEntry ? (
              <AuditEntryDetail
                key={expandedEntry.id}
                entry={expandedEntry}
                copiedTargetId={copiedTargetId}
                onCopyTargetId={(id) => void copyTargetId(id)}
              />
            ) : null}

            {meta?.hasMore ? (
              <div className="db-editor__footer">
                <p className="db-hint">Há mais registros no período consultado.</p>
                <Button type="button" variant="ghost" busy={loadingMore} onClick={loadMore}>
                  Carregar mais
                </Button>
              </div>
            ) : null}
          </>
        )}
      </Card>
    </main>
  );
}

interface AuditEntryDetailProps {
  readonly entry: AuditLogEntry;
  readonly copiedTargetId: string | undefined;
  readonly onCopyTargetId: (id: string) => void;
}

/**
 * Painel de detalhe de uma linha expandida — autor/autorizador/dispositivo/motivo/origem e o
 * antes/depois SEMPRE como lista de chave→valor (`KeyValueBlock`), nunca `JSON.stringify` cru
 * (Gherkin da US: "não deve exibir JSON bruto ao gestor").
 */
function AuditEntryDetail({ entry, copiedTargetId, onCopyTargetId }: Readonly<AuditEntryDetailProps>) {
  const target = entry.target;
  return (
    <div className="audit-detail nx-anim-in" aria-label={`Detalhes: ${entry.summary}`}>
      <dl className="audit-detail__meta nx-stagger">
        <div className="audit-detail__meta-item">
          <dt>Autor</dt>
          <dd>{entry.actor?.name ?? 'Sistema'}</dd>
        </div>
        <div className="audit-detail__meta-item">
          <dt>Autorizado por</dt>
          <dd>{entry.authorizedBy?.name ?? '—'}</dd>
        </div>
        <div className="audit-detail__meta-item">
          <dt>Dispositivo</dt>
          <dd>{entry.device?.label ?? '—'}</dd>
        </div>
        <div className="audit-detail__meta-item">
          <dt>Motivo</dt>
          <dd>{entry.reason ?? '—'}</dd>
        </div>
        {target ? (
          <div className="audit-detail__meta-item">
            <dt>Origem</dt>
            <dd>
              <Button type="button" variant="ghost" size="sm" onClick={() => onCopyTargetId(target.id)}>
                <Icon name="content_copy" size={16} />
                {target.label}
              </Button>
              {copiedTargetId === target.id ? <span className="db-hint">ID copiado</span> : null}
            </dd>
          </div>
        ) : null}
        {entry.traceId ? (
          <div className="audit-detail__meta-item">
            <dt>Traço</dt>
            <dd className="db-code">{entry.traceId}</dd>
          </div>
        ) : null}
      </dl>

      {entry.before || entry.after ? (
        <div className="audit-detail__changes">
          {entry.before ? <KeyValueBlock title="Antes" data={entry.before} /> : null}
          {entry.after ? <KeyValueBlock title="Depois" data={entry.after} /> : null}
        </div>
      ) : null}
    </div>
  );
}

/** Lista de pares chave→valor — a única forma permitida de mostrar `before`/`after` (nunca um bloco de JSON). */
function KeyValueBlock({ title, data }: Readonly<{ title: string; data: Record<string, unknown> }>) {
  const pairs = Object.entries(data);
  if (pairs.length === 0) {
    return (
      <div className="audit-detail__block">
        <p className="db-hint">{title}</p>
        <p className="db-hint">Nenhum campo registrado.</p>
      </div>
    );
  }
  return (
    <div className="audit-detail__block">
      <p className="db-hint">{title}</p>
      <ul className="audit-detail__kv-list">
        {pairs.map(([key, value]) => (
          <li key={key}>
            <span className="audit-detail__kv-key">{humanizeKey(key)}</span>
            <span className="audit-detail__kv-value">{formatValue(key, value)}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}

const KEY_LABELS: Record<string, string> = {
  discount: 'Desconto',
  amount: 'Valor',
  price: 'Preço',
  reason: 'Motivo',
  permissions: 'Permissões',
  minSelect: 'Seleção mínima',
  maxSelect: 'Seleção máxima',
  name: 'Nome',
  status: 'Status',
  quantity: 'Quantidade',
};

/** "camelCase"/"snake_case" -> rótulo legível em português quando conhecido, senão só um texto separado por espaço. */
function humanizeKey(key: string): string {
  const known = KEY_LABELS[key];
  if (known) return known;
  const spaced = key.replace(/([a-z0-9])([A-Z])/g, '$1 $2').replace(/_/g, ' ');
  return spaced.charAt(0).toUpperCase() + spaced.slice(1).toLowerCase();
}

/**
 * Formata um valor de `before`/`after` como texto — NUNCA como JSON. Números em campos que parecem
 * monetários (nome contendo "amount"/"price"/"discount"/"total"/"valor") são formatados como moeda
 * SEM dividir por 100 — ADR-017 (representação monetária) usa `decimal` de ponta a ponta neste
 * backend (nunca um inteiro em centavos): `Price.Amount` e todo `JsonSerializer.Serialize` ad-hoc
 * nos handlers (ex. `SetVariantPriceCommandHandler`) gravam o valor em reais já com casas decimais
 * (`45.00`, não `4500`). Objetos aninhados viram uma lista "chave: valor" encadeada, também sem
 * chaves/JSON.
 */
function formatValue(key: string, value: unknown): string {
  if (value === null || value === undefined) return '—';
  if (typeof value === 'boolean') return value ? 'Sim' : 'Não';
  if (typeof value === 'number') {
    return /amount|price|discount|total|valor/i.test(key)
      ? formatBRL(value)
      : new Intl.NumberFormat('pt-BR').format(value);
  }
  if (typeof value === 'string') return value;
  if (Array.isArray(value)) {
    return value.length === 0 ? '—' : value.map((item) => formatValue(key, item)).join(', ');
  }
  if (typeof value === 'object') {
    return Object.entries(value as Record<string, unknown>)
      .map(([nestedKey, nestedValue]) => `${humanizeKey(nestedKey)}: ${formatValue(nestedKey, nestedValue)}`)
      .join('; ');
  }
  if (typeof value === 'bigint') return value.toString();
  if (typeof value === 'symbol') return value.description ?? 'Símbolo';
  return '—';
}

function formatBRL(amount: number): string {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(amount);
}

function actionLabel(action: string): string {
  return ACTION_LABEL_BY_VALUE.get(action) ?? action;
}

type BadgeTone = 'neutral' | 'brand' | 'info' | 'success' | 'warning' | 'danger' | 'accent' | 'solid';

function actionTone(action: string): BadgeTone {
  if (/cancel|denied|deny/i.test(action)) return 'danger';
  if (/discount/i.test(action)) return 'warning';
  if (/permission|access/i.test(action)) return 'brand';
  if (/price/i.test(action)) return 'info';
  return 'neutral';
}

/**
 * Monta os filtros a partir do formulário — só inclui uma chave quando ela tem valor (o
 * `tsconfig` do monorepo liga `exactOptionalPropertyTypes`, então uma chave `actorId: undefined`
 * explícita já não é atribuível a `actorId?: string`; por isso o spread condicional abaixo, em vez
 * de `actorId: form.actorId || undefined`).
 */
function toApiFilters(form: FilterFormState, cursor?: string): AuditLogFilters {
  const trimmedActorId = form.actorId.trim();
  const trimmedAuthorizedBy = form.authorizedBy.trim();
  const trimmedEntityId = form.entityId.trim();
  return {
    ...(form.from ? { from: new Date(form.from).toISOString() } : {}),
    ...(form.to ? { to: new Date(form.to).toISOString() } : {}),
    ...(trimmedActorId ? { actorId: trimmedActorId } : {}),
    ...(trimmedAuthorizedBy ? { authorizedBy: trimmedAuthorizedBy } : {}),
    ...(trimmedEntityId ? { entityId: trimmedEntityId } : {}),
    ...(form.action ? { action: form.action } : {}),
    ...(form.entity ? { entity: form.entity } : {}),
    ...(form.minAmountCents > 0 ? { minAmount: centsToDecimalString(form.minAmountCents) } : {}),
    limit: 50,
    ...(cursor ? { cursor } : {}),
  };
}

function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' }).format(
    new Date(value),
  );
}

function toMessage(reason: unknown): string {
  return reason instanceof Error
    ? reason.message
    : 'Não foi possível consultar a trilha de auditoria.';
}

// --- máscara de moeda (duplicada de propósito, mesmo comentário de price-table-page.tsx: não há
// um módulo compartilhado de dinheiro entre pastas de apps/web-admin/src nesta base) ---

function centsToDecimalString(cents: number): string {
  const intPart = Math.floor(cents / 100);
  const decPart = String(cents % 100).padStart(2, '0');
  return `${intPart}.${decPart}`;
}

function centsToDisplay(cents: number): string {
  const intPart = String(Math.floor(cents / 100)).replace(/\B(?=(\d{3})+(?!\d))/g, '.');
  const decPart = String(cents % 100).padStart(2, '0');
  return `${intPart},${decPart}`;
}

function digitsToCents(rawInput: string): number {
  const digits = rawInput.replace(/\D/g, '');
  return digits === '' ? 0 : Number(digits);
}
