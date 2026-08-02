import { describe, expect, it } from 'vitest';
import {
  prepTimeAnalysisResponseSchema,
  productStationResponseSchema,
  reassignStationRequestSchema,
  updatePrepTimeThresholdsRequestSchema,
  variantPrepTimeResponseSchema,
} from './catalog-prep-time.js';

describe('contratos de tempo de preparo e praça (US-016)', () => {
  it('aceita limiares nulos (herança do tenant) e valores ordenados', () => {
    expect(
      updatePrepTimeThresholdsRequestSchema.parse({
        prepMinutes: 12,
        warnMinutes: null,
        criticalMinutes: null,
      }),
    ).toEqual({ prepMinutes: 12, warnMinutes: null, criticalMinutes: null });

    expect(
      updatePrepTimeThresholdsRequestSchema.parse({
        prepMinutes: 12,
        warnMinutes: 15,
        criticalMinutes: 20,
      }),
    ).toEqual({ prepMinutes: 12, warnMinutes: 15, criticalMinutes: 20 });
  });

  it('recusa limiar de atenção menor que o tempo de preparo', () => {
    expect(() =>
      updatePrepTimeThresholdsRequestSchema.parse({
        prepMinutes: 12,
        warnMinutes: 10,
        criticalMinutes: null,
      }),
    ).toThrow();
  });

  it('recusa limiar crítico menor que o limiar de atenção', () => {
    expect(() =>
      updatePrepTimeThresholdsRequestSchema.parse({
        prepMinutes: 10,
        warnMinutes: 15,
        criticalMinutes: 12,
      }),
    ).toThrow();
  });

  it('recusa tempo de preparo negativo', () => {
    expect(() =>
      updatePrepTimeThresholdsRequestSchema.parse({
        prepMinutes: -1,
        warnMinutes: null,
        criticalMinutes: null,
      }),
    ).toThrow();
  });

  it('representa a resposta de atualização de tempo de preparo', () => {
    expect(
      variantPrepTimeResponseSchema.parse({
        variantId: '0198aabb-1111-7000-8000-000000000001',
        prepMinutes: 12,
        warnMinutes: 15,
        criticalMinutes: 20,
      }),
    ).toBeTruthy();
  });

  it('aceita stationId nulo para remover o vínculo do produto com a praça', () => {
    expect(reassignStationRequestSchema.parse({ stationId: null })).toEqual({ stationId: null });
  });

  it('representa a resposta de reatribuição de praça com e sem praça definida', () => {
    expect(
      productStationResponseSchema.parse({
        productId: '0198aabb-1111-7000-8000-000000000002',
        stationId: null,
        stationCode: null,
        stationName: null,
      }),
    ).toBeTruthy();
  });

  it('representa o comparativo estimado versus real, com e sem sugestão de ajuste', () => {
    expect(
      prepTimeAnalysisResponseSchema.parse({
        variantId: '0198aabb-1111-7000-8000-000000000001',
        configuredMinutes: 12,
        effectiveWarnMinutes: 15,
        warnMinutesInherited: true,
        effectiveCriticalMinutes: 25,
        criticalMinutesInherited: true,
        actualAvgMinutes: 16.4,
        actualP90Minutes: null,
        sampleSize: 340,
        suggestion: 16,
        note: null,
      }),
    ).toBeTruthy();

    expect(
      prepTimeAnalysisResponseSchema.parse({
        variantId: '0198aabb-1111-7000-8000-000000000001',
        configuredMinutes: 12,
        effectiveWarnMinutes: 15,
        warnMinutesInherited: true,
        effectiveCriticalMinutes: 25,
        criticalMinutesInherited: true,
        actualAvgMinutes: null,
        actualP90Minutes: null,
        sampleSize: 0,
        suggestion: null,
        note: 'Sem histórico de preparo nos últimos 30 dias.',
      }),
    ).toBeTruthy();
  });
});
