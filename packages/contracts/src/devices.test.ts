import { describe, expect, it } from 'vitest';
import {
  deviceListResponseSchema,
  pairDeviceRequestSchema,
  pairDeviceResponseSchema,
  pairingCodeResponseSchema,
  renameDeviceRequestSchema,
} from './devices.js';

describe('contratos de dispositivos', () => {
  it('aceita pareamento normativo e não aceita código fora de seis dígitos', () => {
    expect(
      pairDeviceRequestSchema.parse({
        code: '418302',
        label: 'Caixa 1',
        kind: 'CASHIER',
        fingerprint: 'fp-1',
      }),
    ).toEqual({ code: '418302', label: 'Caixa 1', kind: 'CASHIER', fingerprint: 'fp-1' });
    expect(() =>
      pairDeviceRequestSchema.parse({
        code: '12345',
        label: 'Caixa',
        kind: 'CASHIER',
        fingerprint: 'fp',
      }),
    ).toThrow();
  });

  it('serializa validade e segredo retornado uma única vez', () => {
    expect(
      pairingCodeResponseSchema.parse({
        code: '418302',
        expiresAt: '2026-07-31T18:10:00.000Z',
        expiresInSeconds: 600,
      }),
    ).toBeTruthy();
    expect(
      pairDeviceResponseSchema.parse({
        device: {
          id: '0198aabb-1111-7000-8000-000000000001',
          label: 'Caixa 1',
        },
        deviceSecret: 'opaque-secret',
      }),
    ).toBeTruthy();
  });

  it('restringe renomeação e representa revisão por inatividade', () => {
    expect(() => renameDeviceRequestSchema.parse({ label: '   ' })).toThrow();
    expect(
      deviceListResponseSchema.parse({
        items: [
          {
            id: '0198aabb-1111-7000-8000-000000000001',
            label: 'Tablet apoio',
            kind: 'SUPPORT_TABLET',
            active: true,
            lastSeenAt: null,
            needsReview: false,
          },
        ],
      }),
    ).toBeTruthy();
  });
});
