import { describe, expect, it } from 'vitest';
import { catalogImportCommitResponseSchema, catalogImportValidateResponseSchema } from './catalog-import.js';

describe('contratos de importação de cardápio (US-144)', () => {
  it('valida resposta de pré-visualização com erros por linha', () => {
    const parsed = catalogImportValidateResponseSchema.safeParse({
      valid: false,
      errors: [{ row: 12, column: 'preco', message: 'Valor inválido' }],
      preview: {
        toCreate: { categories: 0, products: 0, variants: 0 },
        toUpdate: { categories: 0, products: 0, variants: 0 },
      },
    });

    expect(parsed.success).toBe(true);
  });

  it('valida resultado de importação bem-sucedida', () => {
    const parsed = catalogImportCommitResponseSchema.safeParse({
      valid: true,
      errors: [],
      created: { categories: 6, products: 57, variants: 132 },
      updated: { categories: 0, products: 0, variants: 0 },
      skipped: 0,
    });

    expect(parsed.success).toBe(true);
  });

  it('recusa resposta sem as contagens obrigatórias', () => {
    expect(
      catalogImportCommitResponseSchema.safeParse({
        valid: true,
        errors: [],
        created: { categories: 1 },
        updated: { categories: 0, products: 0, variants: 0 },
        skipped: 0,
      }).success,
    ).toBe(false);
  });
});
