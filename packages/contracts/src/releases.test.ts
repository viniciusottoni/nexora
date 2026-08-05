import { describe, expect, it } from 'vitest';

import {
  publishReleaseRequestSchema,
  publishReleaseResponseSchema,
  releaseRolloutResponseSchema,
} from './releases.js';

describe('publishReleaseRequestSchema', () => {
  it('aceita o exemplo exato do contrato da US-146 §7', () => {
    const parsed = publishReleaseRequestSchema.parse({
      version: '1.5.0',
      rolloutPercent: 10,
      notes: 'Correção de bug crítico no fechamento de comanda.',
    });

    expect(parsed.version).toBe('1.5.0');
    expect(parsed.rolloutPercent).toBe(10);
  });

  it('recusa rolloutPercent fora de 0-100', () => {
    expect(() =>
      publishReleaseRequestSchema.parse({ version: '1.5.0', rolloutPercent: 101 }),
    ).toThrow();
    expect(() =>
      publishReleaseRequestSchema.parse({ version: '1.5.0', rolloutPercent: -1 }),
    ).toThrow();
  });

  it('recusa versão vazia', () => {
    expect(() => publishReleaseRequestSchema.parse({ version: '', rolloutPercent: 10 })).toThrow();
  });
});

describe('publishReleaseResponseSchema', () => {
  it('aceita a resposta de publicação', () => {
    const parsed = publishReleaseResponseSchema.parse({
      release: {
        id: '018f1a2b-3c4d-7e5f-8a9b-0c1d2e3f4a5b',
        version: '1.5.0',
        rolloutPercent: 10,
        notes: null,
        publishedAt: new Date().toISOString(),
        publishedBy: null,
      },
    });

    expect(parsed.release.version).toBe('1.5.0');
  });
});

describe('releaseRolloutResponseSchema', () => {
  it('aceita o exemplo exato do contrato da US-146 §7', () => {
    const parsed = releaseRolloutResponseSchema.parse({
      total: 12,
      updated: 3,
      failed: 0,
      pending: 9,
    });

    expect(parsed.total).toBe(parsed.updated + parsed.failed + parsed.pending);
  });

  it('recusa contagem negativa', () => {
    expect(() =>
      releaseRolloutResponseSchema.parse({ total: -1, updated: 0, failed: 0, pending: 0 }),
    ).toThrow();
  });
});
