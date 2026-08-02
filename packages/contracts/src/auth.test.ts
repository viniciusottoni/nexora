import { describe, expect, it } from 'vitest';
import { authorizeRequestSchema, passwordLoginSchema, pinLoginSchema } from './auth.js';

describe('contratos de autenticação', () => {
  it('valida login de senha e OTP opcional', () => {
    expect(
      passwordLoginSchema.parse({
        email: 'gestor@example.com',
        password: 'segredo-longo',
        otp: '123456',
      }),
    ).toMatchObject({ email: 'gestor@example.com' });
  });

  it('exige deviceId UUID no login por PIN', () => {
    expect(() => pinLoginSchema.parse({ pin: '4821', deviceId: 'caixa' })).toThrow();
  });

  it('aceita contexto estruturado na elevação', () => {
    expect(
      authorizeRequestSchema.parse({
        action: 'CANCEL_STARTED_ITEM',
        pin: '9911',
        context: { orderItemId: 'x' },
      }),
    ).toMatchObject({ action: 'CANCEL_STARTED_ITEM' });
  });
});
