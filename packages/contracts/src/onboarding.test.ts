import { describe, expect, it } from 'vitest';

import {
  onboardingActivationPendingMetaSchema,
  onboardingStatusResponseSchema,
  ONBOARDING_STEP_ORDER,
} from './onboarding.js';

describe('onboardingStatusResponseSchema', () => {
  it('aceita o exemplo exato do contrato da US-141 §7', () => {
    const parsed = onboardingStatusResponseSchema.parse({
      steps: [
        { key: 'TENANT_CREATED', status: 'DONE' },
        { key: 'BRANDING', status: 'DONE' },
        { key: 'MENU', status: 'IN_PROGRESS', progress: { products: 44, expected: 60 } },
        { key: 'TABLES', status: 'PENDING' },
        { key: 'EDGE_INSTALL', status: 'PENDING' },
        { key: 'PAYMENT_CONFIG', status: 'PENDING' },
        { key: 'TRAINING', status: 'PENDING' },
        { key: 'PILOT', status: 'PENDING' },
        { key: 'ACTIVATION', status: 'PENDING' },
      ],
      startedAt: new Date().toISOString(),
      elapsedBusinessDays: 2,
    });

    expect(parsed.steps).toHaveLength(9);
    expect(parsed.steps.map((step) => step.key)).toEqual(ONBOARDING_STEP_ORDER);
  });

  it('recusa resposta sem os nove passos', () => {
    expect(() =>
      onboardingStatusResponseSchema.parse({
        steps: [{ key: 'TENANT_CREATED', status: 'DONE' }],
        startedAt: null,
        elapsedBusinessDays: null,
      }),
    ).toThrow();
  });

  it('recusa uma chave de passo fora do vocabulario fechado', () => {
    expect(() =>
      onboardingStatusResponseSchema.parse({
        steps: ONBOARDING_STEP_ORDER.map((key) => ({ key, status: 'PENDING' })).map((step, index) =>
          index === 0 ? { ...step, key: 'UNKNOWN_STEP' } : step,
        ),
        startedAt: null,
        elapsedBusinessDays: 0,
      }),
    ).toThrow();
  });
});

describe('onboardingActivationPendingMetaSchema', () => {
  it('aceita meta.pendingItems (formato produzido pelo backend hoje)', () => {
    const parsed = onboardingActivationPendingMetaSchema.parse({
      pendingItems: ['MENU', 'TABLES'],
    });
    expect(parsed.pendingItems).toEqual(['MENU', 'TABLES']);
  });

  it('aceita meta.pending (formato exato do contrato da US-141 §7)', () => {
    const parsed = onboardingActivationPendingMetaSchema.parse({ pending: ['TRAINING'] });
    expect(parsed.pending).toEqual(['TRAINING']);
  });
});
