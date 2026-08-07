import { describe, expect, it } from 'vitest';

import { platformSummaryResponseSchema } from './platform-summary.js';

describe('platformSummaryResponseSchema', () => {
  it('aceita o contrato de GET /v1/platform/summary', () => {
    expect(
      platformSummaryResponseSchema.parse({
        tenants: { total: 12, active: 9, attention: 2 },
        installations: { healthy: 8, degraded: 1, offline: 1 },
        pendingInvites: 2,
        generatedAt: '2026-08-05T12:00:00Z',
      }),
    ).toMatchObject({ pendingInvites: 2 });
  });

  it('recusa contadores negativos', () => {
    expect(() =>
      platformSummaryResponseSchema.parse({
        tenants: { total: -1, active: 0, attention: 0 },
        installations: { healthy: 0, degraded: 0, offline: 0 },
        pendingInvites: 0,
        generatedAt: '2026-08-05T12:00:00Z',
      }),
    ).toThrow();
  });

  it('recusa generatedAt que não seja timestamp ISO com fuso', () => {
    expect(() =>
      platformSummaryResponseSchema.parse({
        tenants: { total: 1, active: 1, attention: 0 },
        installations: { healthy: 1, degraded: 0, offline: 0 },
        pendingInvites: 0,
        generatedAt: 'agora',
      }),
    ).toThrow();
  });
});
