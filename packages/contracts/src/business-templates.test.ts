import { describe, expect, it } from 'vitest';

import { businessTemplateDetailResponseSchema } from './business-templates.js';

describe('businessTemplateDetailResponseSchema', () => {
  it('aceita timestamps com offset do DateTimeOffset do backend', () => {
    expect(
      businessTemplateDetailResponseSchema.parse({
        code: 'PIZZERIA',
        name: 'Pizzaria',
        version: 1,
        isActive: true,
        configJson: '{}',
        seedsJson: '{}',
        createdAt: '2026-08-01T09:00:00+00:00',
        updatedAt: '2026-08-01T09:00:00+00:00',
      }),
    ).toMatchObject({ code: 'PIZZERIA', version: 1 });
  });
});
