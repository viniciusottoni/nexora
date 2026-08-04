import { describe, expect, it } from 'vitest';
import {
  alertEngineTypes,
  alertGroupListResponseSchema,
  alertListResponseSchema,
  alertRoutingConfigSchema,
  alertSchema,
  subscribePushRequestSchema,
  tenantThresholdsSchema,
  updateAlertRoutingRequestSchema,
  updateTenantThresholdsRequestSchema,
} from './alerts.js';

const sampleAlert = {
  id: '0198aabb-1111-7000-8000-000000000001',
  type: 'ORDER_LATE',
  severity: 'HIGH',
  entityType: 'order',
  entityId: '0198aabb-1111-7000-8000-000000000002',
  message: 'Pedido A47 da mesa 12 está há 21 minutos na fila.',
  raisedAt: '2026-08-04T18:00:00.000Z',
  acknowledgedAt: null,
  acknowledgedBy: null,
  resolvedAt: null,
  targetRoles: ['WAITER', 'KITCHEN'],
  targetUserId: null,
  groupKey: null,
};

describe('contrato do motor de alertas (E-08)', () => {
  it('US-080 §7 — aceita um alerta individual', () => {
    expect(() => alertSchema.parse(sampleAlert)).not.toThrow();
  });

  it('GET /v1/alerts?status=open', () => {
    const parsed = alertListResponseSchema.parse({ alerts: [sampleAlert], nextCursor: null });
    expect(parsed.alerts).toHaveLength(1);
  });

  it('US-083 §7 — GET /v1/alerts?grouped=true consolida vários alertas num grupo', () => {
    const parsed = alertGroupListResponseSchema.parse({
      groups: [
        {
          type: 'ORDER_LATE',
          count: 5,
          severity: 'HIGH',
          message: '5 pedidos atrasados',
          firstRaisedAt: '2026-08-04T18:00:00.000Z',
          lastRaisedAt: '2026-08-04T18:00:30.000Z',
          alerts: [sampleAlert],
        },
      ],
    });
    expect(parsed.groups[0]!.count).toBe(5);
  });

  it('US-080 §7 — GET /v1/tenant/thresholds traz o limiar monetário como string (ADR-017)', () => {
    const parsed = tenantThresholdsSchema.parse({
      orderWarnMinutes: 12,
      orderCriticalMinutes: 18,
      itemInWindowMinutes: 2,
      tableIdleMinutes: 10,
      cashDivergenceAlert: '20.00',
      cmvDivergencePercent: 5,
      syncDelayMinutes: 5,
      dineInPromiseMinutes: 10,
      deliveryPromiseMinutes: 25,
      avgTimeAboveTargetPercent: 20,
      cancellationCountThreshold: 5,
      cancellationWindowMinutes: 60,
      discountAboveThresholdPercent: 15,
      discountWindowMinutes: 60,
    });
    expect(typeof parsed.cashDivergenceAlert).toBe('string');
  });

  it('PATCH /v1/tenant/thresholds aceita um corpo parcial', () => {
    expect(() => updateTenantThresholdsRequestSchema.parse({ orderCriticalMinutes: 25 })).not.toThrow();
    expect(() => updateTenantThresholdsRequestSchema.parse({})).not.toThrow();
  });

  it('US-082 §7 — GET /v1/tenant/alert-routing é um dicionário por tipo de alerta', () => {
    const parsed = alertRoutingConfigSchema.parse({
      ORDER_LATE: { roles: ['WAITER', 'KITCHEN', 'MANAGER'], scope: 'RESPONSIBLE', escalateAfterSeconds: 120, groupWindowSeconds: 60 },
      CASH_DIVERGENCE: { roles: ['MANAGER'], scope: 'TENANT', escalateAfterSeconds: null, groupWindowSeconds: null },
    });
    expect(parsed.ORDER_LATE?.scope).toBe('RESPONSIBLE');
  });

  it('US-083 §7 — PATCH /v1/tenant/alert-routing aceita patch parcial de um único campo', () => {
    const parsed = updateAlertRoutingRequestSchema.parse({ ORDER_LATE: { groupWindowSeconds: 60 } });
    expect(parsed.ORDER_LATE?.groupWindowSeconds).toBe(60);
    expect(parsed.ORDER_LATE?.roles).toBeUndefined();
  });

  it('US-081 §7 — POST /v1/notifications/subscribe', () => {
    const parsed = subscribePushRequestSchema.parse({
      endpoint: 'https://push.example/abc',
      keys: { p256dh: 'chave-publica', auth: 'segredo' },
    });
    expect(parsed.keys.p256dh).toBe('chave-publica');
  });

  it('catálogo do motor tem os 7 tipos do MVP (US-080 §2)', () => {
    expect(alertEngineTypes).toHaveLength(7);
    expect(alertEngineTypes).toContain('ORDER_LATE');
    expect(alertEngineTypes).toContain('DISCOUNT_ABOVE_THRESHOLD');
  });
});
