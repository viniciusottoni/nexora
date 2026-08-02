import { describe, expect, it } from 'vitest';
import { problemDetailsSchema } from './errors.js';

describe('Problem Details', () => {
  it('aceita contrato RFC 7807 estendido', () => {
    const parsed = problemDetailsSchema.parse({
      type: 'https://docs.donabetinha.app/errors/not-found',
      title: 'Recurso não encontrado',
      status: 404,
      detail: 'O recurso solicitado não foi encontrado.',
      instance: '/v1/orders/abc',
      code: 'NOT_FOUND',
      recoverable: false,
      requiresAuthorization: false,
      traceId: '4bf92f3577b34da6a3ce929d0e0e4736',
    });

    expect(parsed.code).toBe('NOT_FOUND');
  });

  it('recusa resposta sem classificação operacional', () => {
    expect(() =>
      problemDetailsSchema.parse({ title: 'Erro', status: 500, code: 'INTERNAL_ERROR' }),
    ).toThrow();
  });
});
