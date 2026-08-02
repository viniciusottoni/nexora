import { describe, expect, it } from 'vitest';
import {
  formatMinutesOpen,
  formatMoneyBrl,
  formatRelativeSync,
  selectTopSignals,
  toTableCardStatus,
  urgencyScore,
} from './table-map-signals.js';
import type { TableMapEntry } from '@nexora/contracts';

function buildEntry(overrides: Partial<TableMapEntry['flags']> = {}): TableMapEntry {
  return {
    id: '0198aabb-1111-7000-8000-000000000001',
    label: '12',
    area: 'Salão',
    status: 'OCCUPIED',
    seats: 4,
    session: {
      openedAt: '2026-08-02T18:00:00.000Z',
      minutesOpen: 47,
      total: '187.00',
      guestCount: 4,
      waiter: { id: '0198aabb-1111-7000-8000-000000000002', name: 'Ana' },
      sessionId: '0198aabb-1111-7000-8000-000000000007',
    },
    flags: {
      waiterCalled: false,
      billRequested: false,
      itemsReadyToServe: 0,
      aboveAvgDuration: false,
      ...overrides,
    },
  };
}

describe('table-map-signals', () => {
  it('traduz o status combinado do backend para o vocabulário de TableCard/StatusPill', () => {
    expect(toTableCardStatus('FREE')).toBe('FREE');
    expect(toTableCardStatus('OCCUPIED')).toBe('OPEN');
    expect(toTableCardStatus('BILL_REQUESTED')).toBe('BILL_REQUESTED');
    expect(toTableCardStatus('RESERVED')).toBe('CLOSED');
    expect(toTableCardStatus('BLOCKED')).toBe('CLOSED');
  });

  it('pontua urgência com o mesmo peso do backend (conta pedida > garçom chamado > item pronto > acima da média)', () => {
    const billRequested = urgencyScore(buildEntry({ billRequested: true }));
    const waiterCalled = urgencyScore(buildEntry({ waiterCalled: true }));
    const itemsReady = urgencyScore(buildEntry({ itemsReadyToServe: 3 }));
    const aboveAvg = urgencyScore(buildEntry({ aboveAvgDuration: true }));
    const none = urgencyScore(buildEntry());

    expect(billRequested).toBeGreaterThan(waiterCalled);
    expect(waiterCalled).toBeGreaterThan(itemsReady);
    expect(itemsReady).toBeGreaterThan(aboveAvg);
    expect(aboveAvg).toBeGreaterThan(none);
  });

  it('limita a três sinais simultâneos por cartão (US-023 §15), na ordem de prioridade', () => {
    const entry = buildEntry({ billRequested: true, waiterCalled: true, itemsReadyToServe: 2, aboveAvgDuration: true });
    const signals = selectTopSignals(entry);

    expect(signals).toHaveLength(3);
    expect(signals.map((s) => s.key)).toEqual(['billRequested', 'waiterCalled', 'itemsReady']);
  });

  it('não mostra nenhum sinal quando não há ação pendente', () => {
    expect(selectTopSignals(buildEntry())).toHaveLength(0);
  });

  it('formata minutos e valor monetário', () => {
    expect(formatMinutesOpen(47)).toBe('47 min');
    expect(formatMoneyBrl('186.40')).toContain('186,40');
  });

  it('formata o rótulo de sincronização relativa', () => {
    const now = new Date('2026-08-02T18:05:00.000Z');
    expect(formatRelativeSync(new Date('2026-08-02T18:04:54.000Z'), now)).toBe('há 6 s');
    expect(formatRelativeSync(new Date('2026-08-02T18:00:00.000Z'), now)).toBe('há 5 min');
  });
});
