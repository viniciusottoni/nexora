import { describe, expect, it } from 'vitest';
import { readStationIdFromAccessToken } from './decode-station-claim.js';

function fakeToken(payload: Record<string, unknown>): string {
  const encode = (value: object) =>
    btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${encode({ alg: 'HS256' })}.${encode(payload)}.signature-nao-verificada-aqui`;
}

describe('readStationIdFromAccessToken (US-031/ADR-011)', () => {
  it('lê a claim stn do payload do token', () => {
    const token = fakeToken({ sub: 'user-1', stn: '0198aabb-1111-7000-8000-000000000099' });
    expect(readStationIdFromAccessToken(token)).toBe('0198aabb-1111-7000-8000-000000000099');
  });

  it('devolve null quando o dispositivo não tem praça associada (ex.: caixa, garçom)', () => {
    const token = fakeToken({ sub: 'user-1', roles: ['CASHIER'] });
    expect(readStationIdFromAccessToken(token)).toBeNull();
  });

  it('devolve null para um token malformado, sem lançar', () => {
    expect(readStationIdFromAccessToken('nao-e-um-jwt')).toBeNull();
    expect(readStationIdFromAccessToken('')).toBeNull();
  });
});
