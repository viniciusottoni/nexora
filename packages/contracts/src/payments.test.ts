import { describe, expect, it } from 'vitest';
import { applyDiscountRequestSchema, waiveSessionServiceFeeRequestSchema } from './payments.js';

describe('contratos de pagamento/desconto/taxa (US-053/US-054)', () => {
  it('recusa desconto com percentual e valor ao mesmo tempo', () => {
    const parsed = applyDiscountRequestSchema.safeParse({
      percent: 10,
      amount: 5,
      reason: 'ambíguo',
      scope: 'SESSION',
    });

    expect(parsed.success).toBe(false);
  });

  it('recusa desconto sem motivo', () => {
    const parsed = applyDiscountRequestSchema.safeParse({
      percent: 10,
      reason: '',
      scope: 'SESSION',
    });

    expect(parsed.success).toBe(false);
  });

  it('recusa retirada de taxa sem motivo', () => {
    const parsed = waiveSessionServiceFeeRequestSchema.safeParse({
      reason: '',
      scope: 'FULL',
    });

    expect(parsed.success).toBe(false);
  });
});
