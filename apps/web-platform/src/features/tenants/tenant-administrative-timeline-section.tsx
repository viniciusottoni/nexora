import { useEffect, useId, useMemo, useState } from 'react';
import {
  AlertBanner,
  Badge,
  Button,
  Card,
  EmptyState,
  Field,
  Icon,
  Input,
  Select,
} from '@nexora/ui';
import type {
  AdministrativeTimelineEntry,
  AdministrativeTimelineEntryType,
  AdministrativeTimelineFilters,
} from '@nexora/contracts';

import {
  createTenantAdministrativeTimelineApi,
  type TenantAdministrativeTimelineApi,
} from './tenant-administrative-timeline-api.js';
import './tenant-administrative-timeline-section.css';

export interface TenantAdministrativeTimelineSectionProps {
  readonly tenantId: string;
  readonly api?: TenantAdministrativeTimelineApi;
}

const TYPE_LABEL: Record<AdministrativeTimelineEntryType, string> = {
  CREATION: 'Criação',
  STATUS_CHANGED: 'Status',
  PLAN_CHANGED: 'Plano',
  OWNER_CHANGED: 'Proprietário',
  CREDENTIALS_REISSUED: 'Credenciais',
  DOMAIN_REGISTERED: 'Domínio',
  SUPPORT_GRANTED: 'Suporte',
  INCIDENT: 'Incidente',
};

const TYPE_ICON: Record<AdministrativeTimelineEntryType, string> = {
  CREATION: 'flag',
  STATUS_CHANGED: 'sync_alt',
  PLAN_CHANGED: 'workspace_premium',
  OWNER_CHANGED: 'person',
  CREDENTIALS_REISSUED: 'vpn_key',
  DOMAIN_REGISTERED: 'public',
  SUPPORT_GRANTED: 'support_agent',
  INCIDENT: 'report',
};

const TYPE_FILTER_OPTIONS: readonly {
  value: AdministrativeTimelineEntryType | '';
  label: string;
}[] = [
  { value: '', label: 'Todos os tipos' },
  { value: 'STATUS_CHANGED', label: TYPE_LABEL.STATUS_CHANGED },
  { value: 'PLAN_CHANGED', label: TYPE_LABEL.PLAN_CHANGED },
  { value: 'OWNER_CHANGED', label: TYPE_LABEL.OWNER_CHANGED },
  { value: 'CREDENTIALS_REISSUED', label: TYPE_LABEL.CREDENTIALS_REISSUED },
  { value: 'DOMAIN_REGISTERED', label: TYPE_LABEL.DOMAIN_REGISTERED },
  { value: 'SUPPORT_GRANTED', label: TYPE_LABEL.SUPPORT_GRANTED },
  { value: 'INCIDENT', label: TYPE_LABEL.INCIDENT },
];

function formatDateTime(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' });
}

function toMessage(reason: unknown): string {
  return reason instanceof Error
    ? reason.message
    : 'Não foi possível carregar a linha do tempo administrativa.';
}

/**
 * US-157 · Central operacional, auditoria e atalhos de suporte — Gherkin "Linha do tempo
 * administrativa": fatos em ordem cronológica com ator/origem/motivo/correlationId. Seção
 * autocontida dentro de `tenant-detail-page.tsx` (mesmo padrão de `TenantPlanSection`/
 * `TenantOwnershipSection`) — faz o PRÓPRIO fetch e isola a própria falha num `AlertBanner` LOCAL
 * (não derruba a ficha inteira do estabelecimento).
 */
export function TenantAdministrativeTimelineSection({
  tenantId,
  api: providedApi,
}: Readonly<TenantAdministrativeTimelineSectionProps>) {
  const api = useMemo(() => providedApi ?? createTenantAdministrativeTimelineApi(), [providedApi]);

  const [entries, setEntries] = useState<AdministrativeTimelineEntry[]>();
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<unknown>();
  const [typeFilter, setTypeFilter] = useState<AdministrativeTimelineEntryType | ''>('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [actorId, setActorId] = useState('');
  const [correlationId, setCorrelationId] = useState('');
  const [appliedFilters, setAppliedFilters] = useState<AdministrativeTimelineFilters>();
  const typeFieldId = useId();
  const fromFieldId = useId();
  const toFieldId = useId();
  const actorFieldId = useId();
  const correlationFieldId = useId();

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(undefined);
    api
      .list(tenantId, appliedFilters)
      .then((result) => {
        if (!cancelled) {
          setEntries([...result.data].reverse());
          setNextCursor(result.nextCursor);
        }
      })
      .catch((reason: unknown) => {
        if (!cancelled) setError(reason);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [api, appliedFilters, tenantId]);

  function applyFilters() {
    const nextFilters: AdministrativeTimelineFilters = {
      ...(typeFilter ? { type: [typeFilter] } : {}),
      ...(fromDate ? { from: `${fromDate}T00:00:00.000Z` } : {}),
      ...(toDate ? { to: `${toDate}T23:59:59.999Z` } : {}),
      ...(actorId.trim() ? { actorId: actorId.trim() } : {}),
      ...(correlationId.trim() ? { correlationId: correlationId.trim() } : {}),
    };
    setAppliedFilters(nextFilters);
  }

  async function loadMore() {
    if (!nextCursor) return;
    setLoadingMore(true);
    setError(undefined);
    try {
      const result = await api.list(tenantId, { ...appliedFilters, cursor: nextCursor });
      setEntries((current) => [...result.data].reverse().concat(current ?? []));
      setNextCursor(result.nextCursor);
    } catch (reason) {
      setError(reason);
    } finally {
      setLoadingMore(false);
    }
  }

  return (
    <Card
      title="Linha do tempo administrativa"
      subtitle="Criação, status, plano, proprietário, credenciais, domínio, suporte e incidentes — em ordem cronológica."
    >
      <div className="tenant-timeline__filters">
        <Field label="Filtrar por tipo" htmlFor={typeFieldId}>
          <Select
            id={typeFieldId}
            value={typeFilter}
            onChange={(event) =>
              setTypeFilter(event.target.value as AdministrativeTimelineEntryType | '')
            }
            options={TYPE_FILTER_OPTIONS}
          />
        </Field>
        <Field label="Data inicial" htmlFor={fromFieldId}>
          <Input
            id={fromFieldId}
            type="date"
            value={fromDate}
            onChange={(event) => setFromDate(event.target.value)}
          />
        </Field>
        <Field label="Data final" htmlFor={toFieldId}>
          <Input
            id={toFieldId}
            type="date"
            value={toDate}
            onChange={(event) => setToDate(event.target.value)}
          />
        </Field>
        <Field label="ID do ator" htmlFor={actorFieldId}>
          <Input
            id={actorFieldId}
            value={actorId}
            onChange={(event) => setActorId(event.target.value)}
          />
        </Field>
        <Field label="ID de correlação" htmlFor={correlationFieldId}>
          <Input
            id={correlationFieldId}
            value={correlationId}
            onChange={(event) => setCorrelationId(event.target.value)}
          />
        </Field>
        <div className="tenant-timeline__filter-actions">
          <Button type="button" variant="secondary" onClick={applyFilters}>
            Aplicar filtros
          </Button>
        </div>
      </div>

      {loading ? (
        <div className="db-loading" role="status">
          <span className="nx-spinner" aria-hidden="true" />
          Carregando linha do tempo…
        </div>
      ) : error !== undefined ? (
        <AlertBanner
          tone="danger"
          title="Não foi possível carregar a linha do tempo administrativa"
        >
          {toMessage(error)}
        </AlertBanner>
      ) : !entries || entries.length === 0 ? (
        <EmptyState icon="history" title="Nenhum fato registrado">
          Nenhum evento administrativo encontrado para o filtro selecionado.
        </EmptyState>
      ) : (
        <ol className="tenant-timeline__list nx-stagger">
          {entries.map((entry, index) => (
            <li
              key={`${entry.type}-${entry.occurredAt}-${index}`}
              className="tenant-timeline__item"
            >
              <span className="tenant-timeline__icon" aria-hidden="true">
                <Icon name={TYPE_ICON[entry.type]} size={18} />
              </span>
              <div className="tenant-timeline__content">
                <div className="tenant-timeline__head">
                  <Badge tone="neutral" size="sm">
                    {TYPE_LABEL[entry.type]}
                  </Badge>
                  <span className="tenant-timeline__when">{formatDateTime(entry.occurredAt)}</span>
                </div>
                <p className="tenant-timeline__summary">{entry.summary}</p>
                <dl className="tenant-timeline__meta">
                  <div>
                    <dt>Ator</dt>
                    <dd>{entry.actor?.name ?? 'Sistema'}</dd>
                  </div>
                  <div>
                    <dt>Origem</dt>
                    <dd>{entry.origin}</dd>
                  </div>
                  <div>
                    <dt>Motivo</dt>
                    <dd>{entry.reason}</dd>
                  </div>
                  {entry.correlationId ? (
                    <div>
                      <dt>Correlação</dt>
                      <dd className="db-code">{entry.correlationId}</dd>
                    </div>
                  ) : null}
                </dl>
              </div>
            </li>
          ))}
        </ol>
      )}

      {nextCursor && !loading && error === undefined ? (
        <div className="tenant-timeline__load-more">
          <Button
            type="button"
            variant="secondary"
            busy={loadingMore}
            onClick={() => void loadMore()}
          >
            Carregar mais fatos
          </Button>
        </div>
      ) : null}
    </Card>
  );
}
