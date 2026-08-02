import { describe, expect, it, vi } from 'vitest';
import { ModifierGroupsApi } from './modifier-groups-api.js';

const groupId = '0198aabb-1111-7000-8000-000000000001';
const modifierId = '0198aabb-1111-7000-8000-000000000002';
const productId = '0198aabb-1111-7000-8000-000000000003';

const modifier = {
  id: modifierId,
  groupId,
  name: 'Borda',
  priceDelta: '5.00',
  ingredientId: null,
  quantity: null,
  isAvailable: true,
  sortOrder: 0,
};
const group = {
  id: groupId,
  name: 'Adicionais',
  minSelect: 0,
  maxSelect: 3,
  isRequired: false,
  sortOrder: 0,
  modifiers: [modifier],
  productIds: [productId],
};

describe('ModifierGroupsApi', () => {
  it('cobre CRUD de grupos, modificadores e vínculos com contratos validados', async () => {
    const fetcher = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url =
        typeof input === 'string' ? input : input instanceof URL ? input.toString() : input.url;
      if (init?.method === 'DELETE') return new Response(null, { status: 204 });
      if (url.includes(`/products/${productId}/modifier-groups`)) {
        return json({ productId, groupId, sortOrder: 0 });
      }
      if (url.includes('/modifiers')) return json(modifier);
      if (!init?.method) return json({ items: [group] });
      return json(group);
    });
    const api = new ModifierGroupsApi('/api', fetcher);

    await expect(api.list()).resolves.toEqual({ items: [group] });
    await expect(
      api.createGroup({
        name: 'Adicionais',
        minSelect: 0,
        maxSelect: 3,
        isRequired: false,
        sortOrder: 0,
      }),
    ).resolves.toMatchObject({ id: groupId });
    await expect(api.updateGroup(groupId, { minSelect: 1, maxSelect: 3 })).resolves.toMatchObject({
      id: groupId,
    });
    await expect(
      api.createModifier(groupId, {
        name: 'Borda',
        priceDelta: '5.00',
        ingredientId: null,
        quantity: null,
        sortOrder: 0,
      }),
    ).resolves.toMatchObject({ id: modifierId });
    await expect(
      api.updateModifierPrice(groupId, modifierId, { priceDelta: '6.00' }),
    ).resolves.toMatchObject({ id: modifierId });
    await expect(
      api.setModifierAvailability(groupId, modifierId, { isAvailable: false }),
    ).resolves.toMatchObject({ id: modifierId });
    await expect(api.linkToProduct(productId, { groupId, sortOrder: 0 })).resolves.toEqual({
      productId,
      groupId,
      sortOrder: 0,
    });
    await expect(api.unlinkFromProduct(productId, groupId)).resolves.toBeUndefined();
    await expect(api.deleteGroup(groupId)).resolves.toBeUndefined();

    expect(fetcher).toHaveBeenCalledTimes(9);
    expect(
      fetcher.mock.calls
        .filter((call) => call[1]?.method && call[1]?.method !== 'DELETE')
        .every((call) => new Headers(call[1]?.headers).has('Idempotency-Key')),
    ).toBe(true);
  });

  it('propaga o detail da API', async () => {
    const api = new ModifierGroupsApi(
      '/api',
      async () =>
        new Response(JSON.stringify({ detail: 'Grupo não encontrado.' }), { status: 404 }),
    );

    await expect(api.deleteGroup(groupId)).rejects.toThrow('Grupo não encontrado.');
  });
});

function json(value: unknown): Response {
  return new Response(JSON.stringify(value), {
    status: 200,
    headers: { 'Content-Type': 'application/json' },
  });
}
