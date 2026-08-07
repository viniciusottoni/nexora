import { describe, expect, it } from 'vitest';

import {
  DEFAULT_DIRECTORY_FILTERS,
  activeDirectoryFilterCount,
  dateInputToRangeEndUtc,
  dateInputToRangeStartUtc,
  directoryFiltersToSearchParams,
  formatDateInputPtBr,
  hasActiveSearchOrFilters,
  maskOwnerEmail,
  searchParamsToDirectoryFilters,
  tenantDirectoryRowsToCsv,
  type TenantDirectoryFiltersState,
} from './tenants-directory-view-model.js';
import type { TenantDirectoryItem } from '@nexora/contracts';

describe('maskOwnerEmail', () => {
  it('mantém só o primeiro caractere local e o domínio', () => {
    expect(maskOwnerEmail('dono@example.com')).toBe('d***@example.com');
  });

  it('mostra um texto discreto quando o e-mail ainda não existe', () => {
    expect(maskOwnerEmail(null)).toBe('Não informado');
  });

  it('devolve o valor original se não houver "@" (defensivo)', () => {
    expect(maskOwnerEmail('sem-arroba')).toBe('sem-arroba');
  });
});

describe('directoryFiltersToSearchParams / searchParamsToDirectoryFilters', () => {
  it('é uma ida-e-volta estável para filtros compostos', () => {
    const filters: TenantDirectoryFiltersState = {
      query: 'betinha',
      status: ['ACTIVE', 'SUSPENDED'],
      plan: ['COMPLETO'],
      template: ['PIZZERIA'],
      health: ['DEGRADED'],
      createdFrom: '2026-01-01',
      createdTo: '2026-12-31',
      sort: 'name',
    };

    const params = directoryFiltersToSearchParams(filters);
    expect(params.getAll('status')).toEqual(['ACTIVE', 'SUSPENDED']);
    expect(params.get('query')).toBe('betinha');
    expect(params.get('sort')).toBe('name');

    expect(searchParamsToDirectoryFilters(params)).toEqual(filters);
  });

  it('omite `sort` da URL quando é o padrão (attention) — URL mais limpa no caso comum', () => {
    const params = directoryFiltersToSearchParams(DEFAULT_DIRECTORY_FILTERS);
    expect(params.has('sort')).toBe(false);
    expect(searchParamsToDirectoryFilters(params).sort).toBe('attention');
  });

  it('ignora valores de status/saúde fora do enum ao hidratar da URL (defensivo contra URL adulterada)', () => {
    const params = new URLSearchParams('status=NOT_A_STATUS&status=ACTIVE&health=NOT_A_HEALTH');
    const filters = searchParamsToDirectoryFilters(params);
    expect(filters.status).toEqual(['ACTIVE']);
    expect(filters.health).toEqual([]);
  });
});

describe('activeDirectoryFilterCount / hasActiveSearchOrFilters', () => {
  it('conta filtros discretos, sem contar a busca textual', () => {
    const filters: TenantDirectoryFiltersState = {
      ...DEFAULT_DIRECTORY_FILTERS,
      query: 'texto qualquer',
      status: ['ACTIVE'],
      createdFrom: '2026-01-01',
    };
    expect(activeDirectoryFilterCount(filters)).toBe(2);
    expect(hasActiveSearchOrFilters(filters)).toBe(true);
  });

  it('sem busca nem filtro nenhum, ambos ficam neutros', () => {
    expect(activeDirectoryFilterCount(DEFAULT_DIRECTORY_FILTERS)).toBe(0);
    expect(hasActiveSearchOrFilters(DEFAULT_DIRECTORY_FILTERS)).toBe(false);
  });
});

describe('formatDateInputPtBr / dateInputToRangeStartUtc / dateInputToRangeEndUtc', () => {
  it('converte yyyy-mm-dd para DD/MM/AAAA sem deslocar fuso', () => {
    expect(formatDateInputPtBr('2026-08-05')).toBe('05/08/2026');
  });

  it('converte para os limites UTC do dia (início e fim)', () => {
    expect(dateInputToRangeStartUtc('2026-08-05')).toBe('2026-08-05T00:00:00Z');
    expect(dateInputToRangeEndUtc('2026-08-05')).toBe('2026-08-05T23:59:59Z');
  });
});

describe('tenantDirectoryRowsToCsv', () => {
  const row: TenantDirectoryItem = {
    id: crypto.randomUUID(),
    name: 'Dona Betinha',
    slug: 'dona-betinha',
    status: 'ACTIVE',
    plan: 'COMPLETO',
    ownerEmail: 'dono@example.com',
    storesCount: 1,
    installationsCount: 1,
    health: 'OK',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-08-01T00:00:00Z',
  };

  it('inclui só os campos exibidos na tabela, com o e-mail mascarado (DoD: nada fora do escopo administrativo)', () => {
    const csv = tenantDirectoryRowsToCsv([row]);
    const lines = csv.split('\r\n');

    expect(lines[0]).toBe('"Nome","Endereço","Plano","Status","Proprietário","Lojas","Instalações","Última atualização"');
    expect(lines[1]).toBe(
      '"Dona Betinha","dona-betinha","COMPLETO","ACTIVE","d***@example.com","1","1","2026-08-01T00:00:00Z"',
    );
    expect(csv).not.toContain('dono@example.com');
  });

  it('preenche owner ausente com um texto discreto e não quebra o CSV', () => {
    const csv = tenantDirectoryRowsToCsv([{ ...row, ownerEmail: null }]);
    expect(csv).toContain('Não informado');
  });

  it('escapa aspas duplas no valor (CSV RFC 4180)', () => {
    const csv = tenantDirectoryRowsToCsv([{ ...row, name: 'Pizzaria "Dona" Betinha' }]);
    expect(csv).toContain('"Pizzaria ""Dona"" Betinha"');
  });
});
