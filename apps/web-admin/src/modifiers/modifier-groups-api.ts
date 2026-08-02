import {
  modifierGroupListResponseSchema,
  modifierGroupSchema,
  modifierSchema,
  productModifierGroupSchema,
  type CreateModifierGroupRequest,
  type CreateModifierRequest,
  type LinkModifierGroupToProductRequest,
  type Modifier,
  type ModifierGroup,
  type ModifierGroupListResponse,
  type ProductModifierGroup,
  type UpdateModifierAvailabilityRequest,
  type UpdateModifierGroupRequest,
  type UpdateModifierRequest,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

/** Cliente HTTP do módulo de grupos de modificadores (US-012) — porta de `/v1/catalog/modifier-groups*`. */
export class ModifierGroupsApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = authenticatedFetch,
  ) {}

  async list(): Promise<ModifierGroupListResponse> {
    const response = await this.get('/v1/catalog/modifier-groups');
    return modifierGroupListResponseSchema.parse(await response.json());
  }

  async createGroup(input: CreateModifierGroupRequest): Promise<ModifierGroup> {
    return this.writeGroup('/v1/catalog/modifier-groups', {
      method: 'POST',
      body: JSON.stringify(input),
    });
  }

  async updateGroup(groupId: string, input: UpdateModifierGroupRequest): Promise<ModifierGroup> {
    return this.writeGroup(`/v1/catalog/modifier-groups/${encodeURIComponent(groupId)}`, {
      method: 'PATCH',
      body: JSON.stringify(input),
    });
  }

  async deleteGroup(groupId: string): Promise<void> {
    const response = await this.fetcher(
      `${this.baseUrl}/v1/catalog/modifier-groups/${encodeURIComponent(groupId)}`,
      {
        method: 'DELETE',
        credentials: 'include',
        headers: { 'Idempotency-Key': crypto.randomUUID() },
      },
    );
    await requireSuccess(response);
  }

  async createModifier(groupId: string, input: CreateModifierRequest): Promise<Modifier> {
    return this.writeModifier(
      `/v1/catalog/modifier-groups/${encodeURIComponent(groupId)}/modifiers`,
      {
        method: 'POST',
        body: JSON.stringify(input),
      },
    );
  }

  async updateModifierPrice(
    groupId: string,
    modifierId: string,
    input: UpdateModifierRequest,
  ): Promise<Modifier> {
    return this.writeModifier(
      `/v1/catalog/modifier-groups/${encodeURIComponent(groupId)}/modifiers/${encodeURIComponent(modifierId)}`,
      { method: 'PATCH', body: JSON.stringify(input) },
    );
  }

  async setModifierAvailability(
    groupId: string,
    modifierId: string,
    input: UpdateModifierAvailabilityRequest,
  ): Promise<Modifier> {
    return this.writeModifier(
      `/v1/catalog/modifier-groups/${encodeURIComponent(groupId)}/modifiers/${encodeURIComponent(modifierId)}/availability`,
      { method: 'PATCH', body: JSON.stringify(input) },
    );
  }

  async linkToProduct(
    productId: string,
    input: LinkModifierGroupToProductRequest,
  ): Promise<ProductModifierGroup> {
    const response = await this.fetcher(
      `${this.baseUrl}/v1/catalog/products/${encodeURIComponent(productId)}/modifier-groups`,
      {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
        body: JSON.stringify(input),
      },
    );
    await requireSuccess(response);
    return productModifierGroupSchema.parse(await response.json());
  }

  async unlinkFromProduct(productId: string, groupId: string): Promise<void> {
    const response = await this.fetcher(
      `${this.baseUrl}/v1/catalog/products/${encodeURIComponent(productId)}/modifier-groups/${encodeURIComponent(groupId)}`,
      {
        method: 'DELETE',
        credentials: 'include',
        headers: { 'Idempotency-Key': crypto.randomUUID() },
      },
    );
    await requireSuccess(response);
  }

  private async get(path: string): Promise<Response> {
    const response = await this.fetcher(`${this.baseUrl}${path}`, { credentials: 'include' });
    await requireSuccess(response);
    return response;
  }

  private async writeGroup(path: string, init: RequestInit): Promise<ModifierGroup> {
    const response = await this.write(path, init);
    return modifierGroupSchema.parse(await response.json());
  }

  private async writeModifier(path: string, init: RequestInit): Promise<Modifier> {
    const response = await this.write(path, init);
    return modifierSchema.parse(await response.json());
  }

  private async write(path: string, init: RequestInit): Promise<Response> {
    const response = await this.fetcher(`${this.baseUrl}${path}`, {
      ...init,
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        'Idempotency-Key': crypto.randomUUID(),
        ...init.headers,
      },
    });
    await requireSuccess(response);
    return response;
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  throw new Error(problem?.detail ?? 'Não foi possível concluir a operação.');
}
