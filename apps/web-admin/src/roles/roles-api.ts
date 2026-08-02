import {
  roleListResponseSchema,
  roleSchema,
  type CreateRoleRequest,
  type RoleDto,
  type RoleListResponse,
  type UpdateRoleRequest,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

export class RolesApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = authenticatedFetch,
  ) {}

  async list(): Promise<RoleListResponse> {
    const response = await this.fetcher(`${this.baseUrl}/v1/roles`, { credentials: 'include' });
    await requireSuccess(response);
    return roleListResponseSchema.parse(await response.json());
  }

  async create(input: CreateRoleRequest): Promise<RoleDto> {
    return this.write('/v1/roles', { method: 'POST', body: JSON.stringify(input) });
  }

  async update(id: string, input: UpdateRoleRequest): Promise<RoleDto> {
    return this.write(`/v1/roles/${encodeURIComponent(id)}`, {
      method: 'PATCH',
      body: JSON.stringify(input),
    });
  }

  private async write(path: string, init: RequestInit): Promise<RoleDto> {
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
    return roleSchema.parse(await response.json());
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  throw new Error(problem?.detail ?? 'Não foi possível concluir a operação.');
}
