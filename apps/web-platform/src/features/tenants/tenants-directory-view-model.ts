import type {
  TenantDirectoryItem,
  TenantDirectorySort,
  TenantHealth,
  TenantStatus,
} from '@nexora/contracts';

/** Estado de filtros do diretório, já normalizado para uso local (fora de qualquer schema HTTP). */
export interface TenantDirectoryFiltersState {
  readonly query: string;
  readonly status: readonly TenantStatus[];
  readonly plan: readonly string[];
  readonly template: readonly string[];
  readonly health: readonly TenantHealth[];
  /** Datas `yyyy-mm-dd` de `<input type="date">` — string vazia = sem filtro. */
  readonly createdFrom: string;
  readonly createdTo: string;
  readonly sort: TenantDirectorySort;
}

export const DEFAULT_DIRECTORY_SORT: TenantDirectorySort = 'attention';

export const DEFAULT_DIRECTORY_FILTERS: TenantDirectoryFiltersState = {
  query: '',
  status: [],
  plan: [],
  template: [],
  health: [],
  createdFrom: '',
  createdTo: '',
  sort: DEFAULT_DIRECTORY_SORT,
};

/** US-151 §3.1 — serializa o estado local para a URL compartilhável (`?query=...&status=...`). */
export function directoryFiltersToSearchParams(filters: Readonly<TenantDirectoryFiltersState>): URLSearchParams {
  const params = new URLSearchParams();
  if (filters.query) params.set('query', filters.query);
  for (const value of filters.status) params.append('status', value);
  for (const value of filters.plan) params.append('plan', value);
  for (const value of filters.template) params.append('template', value);
  for (const value of filters.health) params.append('health', value);
  if (filters.createdFrom) params.set('createdFrom', filters.createdFrom);
  if (filters.createdTo) params.set('createdTo', filters.createdTo);
  if (filters.sort !== DEFAULT_DIRECTORY_SORT) params.set('sort', filters.sort);
  return params;
}

const VALID_STATUS: readonly TenantStatus[] = ['PROVISIONED', 'INSTALLING', 'ACTIVE', 'SUSPENDED', 'CANCELLED'];
const VALID_HEALTH: readonly TenantHealth[] = ['OK', 'DEGRADED', 'DOWN', 'UNKNOWN'];
const VALID_SORT: readonly TenantDirectorySort[] = ['name', 'createdAt', 'updatedAt', 'attention'];

/** Inverso de `directoryFiltersToSearchParams` — hidrata o estado inicial a partir da URL. */
export function searchParamsToDirectoryFilters(params: URLSearchParams): TenantDirectoryFiltersState {
  const sortParam = params.get('sort');
  return {
    query: params.get('query') ?? '',
    status: params.getAll('status').filter((value): value is TenantStatus => VALID_STATUS.includes(value as TenantStatus)),
    plan: params.getAll('plan'),
    template: params.getAll('template'),
    health: params.getAll('health').filter((value): value is TenantHealth => VALID_HEALTH.includes(value as TenantHealth)),
    createdFrom: params.get('createdFrom') ?? '',
    createdTo: params.get('createdTo') ?? '',
    sort: VALID_SORT.includes(sortParam as TenantDirectorySort) ? (sortParam as TenantDirectorySort) : DEFAULT_DIRECTORY_SORT,
  };
}

export function activeDirectoryFilterCount(filters: Readonly<TenantDirectoryFiltersState>): number {
  return (
    filters.status.length +
    filters.plan.length +
    filters.template.length +
    filters.health.length +
    (filters.createdFrom ? 1 : 0) +
    (filters.createdTo ? 1 : 0)
  );
}

export function hasActiveSearchOrFilters(filters: Readonly<TenantDirectoryFiltersState>): boolean {
  return filters.query.trim() !== '' || activeDirectoryFilterCount(filters) > 0;
}

/** `yyyy-mm-dd` (valor nativo de `<input type="date">`) → `DD/MM/AAAA`, sem conversão de fuso. */
export function formatDateInputPtBr(value: string): string {
  const [year, month, day] = value.split('-');
  if (!year || !month || !day) return value;
  return `${day}/${month}/${year}`;
}

/** `yyyy-mm-dd` → limite inicial/final do dia em UTC, para os params `createdFrom`/`createdTo`. */
export function dateInputToRangeStartUtc(value: string): string {
  return `${value}T00:00:00Z`;
}
export function dateInputToRangeEndUtc(value: string): string {
  return `${value}T23:59:59Z`;
}

/**
 * US-151 §15 "Documento e e-mail são dados pessoais: mascarar quando o perfil não exigir
 * visualização integral" — mantém só o primeiro caractere local e o domínio (ex.:
 * `dono@example.com` → `d***@example.com`). Quando o e-mail não existe ainda, devolve um texto
 * discreto em vez de quebrar a tabela/exportação.
 */
export function maskOwnerEmail(email: string | null | undefined): string {
  if (!email) return 'Não informado';
  const at = email.indexOf('@');
  if (at <= 0) return email;
  return `${email.slice(0, 1)}***${email.slice(at)}`;
}

const CSV_HEADER = [
  'Nome',
  'Endereço',
  'Plano',
  'Status',
  'Proprietário',
  'Lojas',
  'Instalações',
  'Última atualização',
];

function csvEscape(value: string): string {
  return `"${value.replace(/"/g, '""')}"`;
}

/**
 * US-151 §3.1/DoD "Exportação não contém campo fora do escopo administrativo" — client-side,
 * apenas os campos já exibidos na tabela (sem endpoint novo; sem dado de negócio do tenant).
 */
export function tenantDirectoryRowsToCsv(rows: readonly TenantDirectoryItem[]): string {
  const lines = rows.map((row) =>
    [
      row.name,
      row.slug,
      row.plan,
      row.status,
      maskOwnerEmail(row.ownerEmail),
      String(row.storesCount),
      String(row.installationsCount),
      row.updatedAt,
    ]
      .map(csvEscape)
      .join(','),
  );
  return [CSV_HEADER.map(csvEscape).join(','), ...lines].join('\r\n');
}

/** Dispara o download do CSV via Blob — sem endpoint, sem lib externa. */
export function downloadTextFile(filename: string, content: string, mimeType = 'text/csv;charset=utf-8;'): void {
  const byteOrderMark = '﻿';
  const blob = new Blob([byteOrderMark + content], { type: mimeType });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  document.body.appendChild(anchor);
  anchor.click();
  document.body.removeChild(anchor);
  URL.revokeObjectURL(url);
}
