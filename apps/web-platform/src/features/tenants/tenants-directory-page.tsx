import { useCallback, useEffect, useId, useMemo, useRef, useState, type ChangeEvent } from 'react';
import { AlertBanner, Badge, Button, Card, DataTable, EmptyState, Field, Icon, IconButton, Input, Select } from '@nexora/ui';
import type { TenantDirectoryItem, TenantDirectorySort, TenantHealth, TenantStatus } from '@nexora/contracts';

import { useSearchParams } from '../../routing/use-search-params.js';
import { createTenantsApi, type TenantsApi } from './tenants-api.js';
import { useDebouncedValue } from './use-debounced-value.js';
import {
  activeDirectoryFilterCount,
  dateInputToRangeEndUtc,
  dateInputToRangeStartUtc,
  directoryFiltersToSearchParams,
  formatDateInputPtBr,
  hasActiveSearchOrFilters,
  maskOwnerEmail,
  searchParamsToDirectoryFilters,
  tenantDirectoryRowsToCsv,
  downloadTextFile,
  type TenantDirectoryFiltersState,
} from './tenants-directory-view-model.js';
import './tenants-directory-page.css';

export interface TenantsDirectoryPageProps {
  readonly api?: TenantsApi;
  readonly onCreateTenant: () => void;
  /** US-151 §4 "cada linha deve abrir o detalhe do estabelecimento" — a rota real fica a cargo de quem hospeda a página. */
  readonly onOpenTenant?: (tenantId: string) => void;
}

const STATUS_LABEL: Record<TenantStatus, string> = {
  PROVISIONED: 'Provisionado',
  INSTALLING: 'Instalando',
  ACTIVE: 'Ativo',
  SUSPENDED: 'Suspenso',
  CANCELLED: 'Cancelado',
};

const STATUS_TONE: Record<TenantStatus, 'success' | 'warning' | 'danger' | 'neutral'> = {
  PROVISIONED: 'neutral',
  INSTALLING: 'neutral',
  ACTIVE: 'success',
  SUSPENDED: 'warning',
  CANCELLED: 'danger',
};

const HEALTH_LABEL: Record<TenantHealth, string> = {
  OK: 'Saudável',
  DEGRADED: 'Degradada',
  DOWN: 'Fora do ar',
  UNKNOWN: 'Desconhecida',
};

const STATUS_OPTIONS: ReadonlyArray<{ value: TenantStatus; label: string }> = [
  { value: 'PROVISIONED', label: STATUS_LABEL.PROVISIONED },
  { value: 'INSTALLING', label: STATUS_LABEL.INSTALLING },
  { value: 'ACTIVE', label: STATUS_LABEL.ACTIVE },
  { value: 'SUSPENDED', label: STATUS_LABEL.SUSPENDED },
  { value: 'CANCELLED', label: STATUS_LABEL.CANCELLED },
];

const HEALTH_OPTIONS: ReadonlyArray<{ value: TenantHealth; label: string }> = [
  { value: 'OK', label: HEALTH_LABEL.OK },
  { value: 'DEGRADED', label: HEALTH_LABEL.DEGRADED },
  { value: 'DOWN', label: HEALTH_LABEL.DOWN },
  { value: 'UNKNOWN', label: HEALTH_LABEL.UNKNOWN },
];

const SORT_OPTIONS: ReadonlyArray<{ value: TenantDirectorySort; label: string }> = [
  { value: 'attention', label: 'Criticidade' },
  { value: 'updatedAt', label: 'Última atividade' },
  { value: 'createdAt', label: 'Criação' },
  { value: 'name', label: 'Nome' },
];

function formatUpdatedAt(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' });
}

function toMessage(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Não foi possível carregar os estabelecimentos.';
}

function Chip({ label, onRemove }: Readonly<{ label: string; onRemove: () => void }>) {
  return (
    <span className="tenants-directory__chip">
      {label}
      <button
        type="button"
        className="tenants-directory__chip-remove"
        onClick={onRemove}
        aria-label={`Remover filtro: ${label}`}
      >
        <Icon name="close" size={14} />
      </button>
    </span>
  );
}

/**
 * US-151 · Diretório de estabelecimentos com busca e filtros — sucessora do diretório mínimo da
 * US-150 (nav + CTA apenas). Busca com debounce, filtros repetíveis em chips removíveis,
 * ordenação, paginação por cursor e reflexo do estado na URL (§3.1/§10, critérios de aceite).
 */
export function TenantsDirectoryPage({
  api: providedApi,
  onCreateTenant,
  onOpenTenant,
}: Readonly<TenantsDirectoryPageProps>) {
  const api = useMemo(() => providedApi ?? createTenantsApi(), [providedApi]);
  const [, setUrlParams] = useSearchParams();

  const initialFilters = useMemo(
    () => searchParamsToDirectoryFilters(new URLSearchParams(globalThis.location?.search ?? '')),
    [],
  );

  const searchFieldId = useId();
  const sortFieldId = useId();
  const statusFieldId = useId();
  const healthFieldId = useId();
  const planFieldId = useId();
  const templateFieldId = useId();
  const createdFromFieldId = useId();
  const createdToFieldId = useId();

  const [queryInput, setQueryInput] = useState(initialFilters.query);
  const debouncedQuery = useDebouncedValue(queryInput, 350);
  const [statusFilters, setStatusFilters] = useState<TenantStatus[]>([...initialFilters.status]);
  const [healthFilters, setHealthFilters] = useState<TenantHealth[]>([...initialFilters.health]);
  const [planFilters, setPlanFilters] = useState<string[]>([...initialFilters.plan]);
  const [templateFilters, setTemplateFilters] = useState<string[]>([...initialFilters.template]);
  const [planInput, setPlanInput] = useState('');
  const [templateInput, setTemplateInput] = useState('');
  const [createdFrom, setCreatedFrom] = useState(initialFilters.createdFrom);
  const [createdTo, setCreatedTo] = useState(initialFilters.createdTo);
  const [sort, setSort] = useState<TenantDirectorySort>(initialFilters.sort);

  const [loading, setLoading] = useState(false);
  const [rows, setRows] = useState<TenantDirectoryItem[]>();
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [error, setError] = useState<string>();
  const [cursorStack, setCursorStack] = useState<Array<string | undefined>>([]);
  const currentCursorRef = useRef<string | undefined>(undefined);
  const requestIdRef = useRef(0);

  const filtersState: TenantDirectoryFiltersState = useMemo(
    () => ({
      query: debouncedQuery,
      status: statusFilters,
      plan: planFilters,
      template: templateFilters,
      health: healthFilters,
      createdFrom,
      createdTo,
      sort,
    }),
    [debouncedQuery, statusFilters, planFilters, templateFilters, healthFilters, createdFrom, createdTo, sort],
  );
  const activeFilterCount = activeDirectoryFilterCount(filtersState);

  // US-151 §3.1 "preservação de filtros na URL" — reflete o estado (já com debounce da busca) a
  // cada mudança, sem empilhar uma entrada de histórico por tecla (`useSearchParams` usa `replace`).
  useEffect(() => {
    setUrlParams(directoryFiltersToSearchParams(filtersState));
  }, [filtersState, setUrlParams]);

  const fetchPage = useCallback(
    async (cursor: string | undefined) => {
      const requestId = ++requestIdRef.current;
      setLoading(true);
      try {
        const response = await api.search({
          query: filtersState.query || undefined,
          status: filtersState.status.length > 0 ? [...filtersState.status] : undefined,
          plan: filtersState.plan.length > 0 ? [...filtersState.plan] : undefined,
          template: filtersState.template.length > 0 ? [...filtersState.template] : undefined,
          health: filtersState.health.length > 0 ? [...filtersState.health] : undefined,
          createdFrom: filtersState.createdFrom ? dateInputToRangeStartUtc(filtersState.createdFrom) : undefined,
          createdTo: filtersState.createdTo ? dateInputToRangeEndUtc(filtersState.createdTo) : undefined,
          sort: filtersState.sort,
          cursor,
        });
        if (requestId !== requestIdRef.current) return;
        setRows(response.data);
        setNextCursor(response.nextCursor);
        setError(undefined);
        currentCursorRef.current = cursor;
      } catch (reason) {
        if (requestId !== requestIdRef.current) return;
        setError(toMessage(reason));
      } finally {
        if (requestId === requestIdRef.current) setLoading(false);
      }
    },
    [api, filtersState],
  );

  // Toda mudança real de busca/filtro/ordenação volta para a primeira página.
  useEffect(() => {
    setCursorStack([]);
    void fetchPage(undefined);
    // eslint-disable-next-line react-hooks/exhaustive-deps -- fetchPage já inclui os filtros nas suas próprias deps.
  }, [fetchPage]);

  const goToNextPage = () => {
    if (!nextCursor || loading) return;
    setCursorStack((stack) => [...stack, currentCursorRef.current]);
    void fetchPage(nextCursor);
  };

  const goToPreviousPage = () => {
    if (loading) return;
    setCursorStack((stack) => {
      if (stack.length === 0) return stack;
      const previousCursor = stack[stack.length - 1];
      void fetchPage(previousCursor);
      return stack.slice(0, -1);
    });
  };

  const addStatus = (event: ChangeEvent<HTMLSelectElement>) => {
    const value = event.target.value as TenantStatus | '';
    if (value && !statusFilters.includes(value)) setStatusFilters((list) => [...list, value]);
  };
  const addHealth = (event: ChangeEvent<HTMLSelectElement>) => {
    const value = event.target.value as TenantHealth | '';
    if (value && !healthFilters.includes(value)) setHealthFilters((list) => [...list, value]);
  };
  const addPlan = () => {
    const value = planInput.trim().toUpperCase();
    if (value && !planFilters.includes(value)) setPlanFilters((list) => [...list, value]);
    setPlanInput('');
  };
  const addTemplate = () => {
    const value = templateInput.trim().toUpperCase();
    if (value && !templateFilters.includes(value)) setTemplateFilters((list) => [...list, value]);
    setTemplateInput('');
  };

  const clearAllFilters = () => {
    setStatusFilters([]);
    setHealthFilters([]);
    setPlanFilters([]);
    setTemplateFilters([]);
    setCreatedFrom('');
    setCreatedTo('');
  };
  const clearSearchAndFilters = () => {
    setQueryInput('');
    clearAllFilters();
  };

  const handleExportCsv = () => {
    if (!rows || rows.length === 0) return;
    downloadTextFile(
      `estabelecimentos-${new Date().toISOString().slice(0, 10)}.csv`,
      tenantDirectoryRowsToCsv(rows),
    );
  };

  const showInitialLoading = rows === undefined && loading;
  const showInitialError = rows === undefined && !loading && Boolean(error);
  const isBaseEmpty = rows !== undefined && rows.length === 0 && !hasActiveSearchOrFilters(filtersState);
  const isNoResults = rows !== undefined && rows.length === 0 && hasActiveSearchOrFilters(filtersState);

  return (
    <main className="db-page nx-anim-in" aria-labelledby="tenants-title">
      <header className="db-page__header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Plataforma · estabelecimentos</p>
          <h1 className="db-page__title" id="tenants-title">
            Estabelecimentos
          </h1>
          <p className="db-page__lead">Busque, filtre e ordene todos os estabelecimentos provisionados na plataforma.</p>
        </div>
        <div className="db-page__actions">
          <Button type="button" onClick={onCreateTenant}>
            <Icon name="add_business" /> Novo estabelecimento
          </Button>
        </div>
      </header>

      <div className="tenants-directory__search-row">
        <Field label="Buscar" htmlFor={searchFieldId} className="tenants-directory__search">
          <Input
            id={searchFieldId}
            placeholder="Nome, slug, domínio, documento ou e-mail do proprietário"
            value={queryInput}
            onChange={(event) => setQueryInput(event.target.value)}
            suffix={
              queryInput ? (
                <IconButton icon="close" label="Limpar busca" size="sm" onClick={() => setQueryInput('')} />
              ) : (
                <Icon name="search" size={18} />
              )
            }
          />
        </Field>
        <Field label="Ordenar por" htmlFor={sortFieldId}>
          <Select
            id={sortFieldId}
            value={sort}
            options={SORT_OPTIONS}
            onChange={(event) => setSort(event.target.value as TenantDirectorySort)}
          />
        </Field>
      </div>

      <div className="db-toolbar tenants-directory__filters">
        <Field label="Status" htmlFor={statusFieldId}>
          <Select
            id={statusFieldId}
            value=""
            onChange={addStatus}
            options={[
              { value: '', label: 'Adicionar…' },
              ...STATUS_OPTIONS.filter((option) => !statusFilters.includes(option.value)),
            ]}
          />
        </Field>
        <Field label="Saúde" htmlFor={healthFieldId}>
          <Select
            id={healthFieldId}
            value=""
            onChange={addHealth}
            options={[
              { value: '', label: 'Adicionar…' },
              ...HEALTH_OPTIONS.filter((option) => !healthFilters.includes(option.value)),
            ]}
          />
        </Field>
        <Field label="Plano" htmlFor={planFieldId}>
          <Input
            id={planFieldId}
            placeholder="ex.: COMPLETO"
            value={planInput}
            onChange={(event) => setPlanInput(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter') {
                event.preventDefault();
                addPlan();
              }
            }}
            suffix={<IconButton icon="add" label="Adicionar filtro de plano" size="sm" onClick={addPlan} />}
          />
        </Field>
        <Field label="Modelo" htmlFor={templateFieldId}>
          <Input
            id={templateFieldId}
            placeholder="ex.: PIZZERIA"
            value={templateInput}
            onChange={(event) => setTemplateInput(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter') {
                event.preventDefault();
                addTemplate();
              }
            }}
            suffix={<IconButton icon="add" label="Adicionar filtro de modelo" size="sm" onClick={addTemplate} />}
          />
        </Field>
        <Field label="Criado a partir de" htmlFor={createdFromFieldId}>
          <Input
            id={createdFromFieldId}
            type="date"
            value={createdFrom}
            onChange={(event) => setCreatedFrom(event.target.value)}
          />
        </Field>
        <Field label="Criado até" htmlFor={createdToFieldId}>
          <Input id={createdToFieldId} type="date" value={createdTo} onChange={(event) => setCreatedTo(event.target.value)} />
        </Field>
      </div>

      {activeFilterCount > 0 ? (
        <div className="tenants-directory__chips nx-stagger" aria-label="Filtros ativos">
          <Badge tone="brand">{activeFilterCount === 1 ? '1 filtro ativo' : `${activeFilterCount} filtros ativos`}</Badge>
          {statusFilters.map((value) => (
            <Chip
              key={`status-${value}`}
              label={`Status: ${STATUS_LABEL[value]}`}
              onRemove={() => setStatusFilters((list) => list.filter((item) => item !== value))}
            />
          ))}
          {healthFilters.map((value) => (
            <Chip
              key={`health-${value}`}
              label={`Saúde: ${HEALTH_LABEL[value]}`}
              onRemove={() => setHealthFilters((list) => list.filter((item) => item !== value))}
            />
          ))}
          {planFilters.map((value) => (
            <Chip
              key={`plan-${value}`}
              label={`Plano: ${value}`}
              onRemove={() => setPlanFilters((list) => list.filter((item) => item !== value))}
            />
          ))}
          {templateFilters.map((value) => (
            <Chip
              key={`template-${value}`}
              label={`Modelo: ${value}`}
              onRemove={() => setTemplateFilters((list) => list.filter((item) => item !== value))}
            />
          ))}
          {createdFrom ? (
            <Chip label={`Criado a partir de ${formatDateInputPtBr(createdFrom)}`} onRemove={() => setCreatedFrom('')} />
          ) : null}
          {createdTo ? (
            <Chip label={`Criado até ${formatDateInputPtBr(createdTo)}`} onRemove={() => setCreatedTo('')} />
          ) : null}
          <Button type="button" variant="ghost" size="sm" onClick={clearAllFilters}>
            Limpar filtros
          </Button>
        </div>
      ) : null}

      {showInitialLoading ? (
        <Card>
          <div className="db-loading" role="status">
            <span className="nx-spinner" aria-hidden="true" />
            Carregando estabelecimentos…
          </div>
        </Card>
      ) : showInitialError ? (
        <AlertBanner tone="danger" title="Não foi possível carregar os estabelecimentos">
          {error}
        </AlertBanner>
      ) : (
        <>
          {error ? (
            <AlertBanner tone="danger" title="Não foi possível atualizar a lista">
              {error} Os resultados abaixo podem estar desatualizados — os filtros foram mantidos.
            </AlertBanner>
          ) : null}

          {isBaseEmpty ? (
            <EmptyState icon="storefront" title="Nenhum estabelecimento provisionado ainda">
              <Button type="button" variant="secondary" size="sm" onClick={onCreateTenant}>
                Provisionar o primeiro estabelecimento
              </Button>
            </EmptyState>
          ) : isNoResults ? (
            <EmptyState icon="search_off" title="Nenhum resultado para os filtros aplicados">
              <Button type="button" variant="secondary" size="sm" onClick={clearSearchAndFilters}>
                Limpar busca e filtros
              </Button>
            </EmptyState>
          ) : rows ? (
            <Card
              padding="none"
              title="Estabelecimentos"
              subtitle={`${rows.length} nesta página${nextCursor ? ' · há mais resultados' : ''}`}
              actions={
                <Button type="button" variant="ghost" size="sm" onClick={handleExportCsv} disabled={rows.length === 0}>
                  <Icon name="download" /> Exportar CSV
                </Button>
              }
            >
              <DataTable
                rowKey="id"
                rows={rows}
                {...(onOpenTenant ? { onRowClick: (row: TenantDirectoryItem) => onOpenTenant(row.id) } : {})}
                columns={[
                  { key: 'name', header: 'Nome', render: (row) => row.name },
                  {
                    key: 'slug',
                    header: 'Endereço',
                    hideCompact: true,
                    render: (row) => <span className="db-code">{row.slug}</span>,
                  },
                  { key: 'plan', header: 'Plano', hideCompact: true, render: (row) => row.plan },
                  {
                    key: 'status',
                    header: 'Status',
                    render: (row) => <Badge tone={STATUS_TONE[row.status]}>{STATUS_LABEL[row.status]}</Badge>,
                  },
                  {
                    key: 'ownerEmail',
                    header: 'Proprietário',
                    hideCompact: true,
                    render: (row) => maskOwnerEmail(row.ownerEmail),
                  },
                  { key: 'storesCount', header: 'Lojas', numeric: true, hideCompact: true, render: (row) => row.storesCount },
                  {
                    key: 'installationsCount',
                    header: 'Instalações',
                    numeric: true,
                    hideCompact: true,
                    render: (row) => row.installationsCount,
                  },
                  {
                    key: 'updatedAt',
                    header: 'Última atualização',
                    hideCompact: true,
                    render: (row) => formatUpdatedAt(row.updatedAt),
                  },
                ]}
              />
              <div className="db-editor__footer">
                <p className="db-hint">{cursorStack.length > 0 ? `Página ${cursorStack.length + 1}` : 'Primeira página'}</p>
                <div className="tenants-directory__pager-actions">
                  <Button
                    type="button"
                    variant="secondary"
                    size="sm"
                    disabled={cursorStack.length === 0 || loading}
                    onClick={goToPreviousPage}
                  >
                    <Icon name="chevron_left" /> Página anterior
                  </Button>
                  <Button type="button" variant="secondary" size="sm" disabled={!nextCursor || loading} onClick={goToNextPage}>
                    Próxima página <Icon name="chevron_right" />
                  </Button>
                </div>
              </div>
            </Card>
          ) : null}
        </>
      )}
    </main>
  );
}
