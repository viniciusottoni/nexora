import {
  categoryListResponseSchema,
  categorySchema,
  type CategoryDto,
  type CategoryListResponse,
  type CreateCategoryRequest,
  type ReorderCategoriesRequest,
  type UpdateCategoryRequest,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

export class CategoriesApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = authenticatedFetch,
  ) {}

  async list(): Promise<CategoryListResponse> {
    const response = await this.fetcher(`${this.baseUrl}/v1/catalog/categories`, {
      credentials: 'include',
    });
    await requireSuccess(response);
    return categoryListResponseSchema.parse(await response.json());
  }

  async create(input: CreateCategoryRequest): Promise<CategoryDto> {
    return this.write('/v1/catalog/categories', { method: 'POST', body: JSON.stringify(input) });
  }

  async update(id: string, input: UpdateCategoryRequest): Promise<CategoryDto> {
    return this.write(`/v1/catalog/categories/${encodeURIComponent(id)}`, {
      method: 'PATCH',
      body: JSON.stringify(input),
    });
  }

  async reorder(input: ReorderCategoriesRequest): Promise<void> {
    const response = await this.fetcher(`${this.baseUrl}/v1/catalog/categories/reorder`, {
      method: 'PATCH',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
      body: JSON.stringify(input),
    });
    await requireSuccess(response);
  }

  async deactivate(id: string): Promise<void> {
    const response = await this.fetcher(
      `${this.baseUrl}/v1/catalog/categories/${encodeURIComponent(id)}`,
      {
        method: 'DELETE',
        credentials: 'include',
        headers: { 'Idempotency-Key': crypto.randomUUID() },
      },
    );
    await requireSuccess(response);
  }

  private async write(path: string, init: RequestInit): Promise<CategoryDto> {
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
    return categorySchema.parse(await response.json());
  }
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  throw new Error(problem?.detail ?? 'Não foi possível concluir a operação.');
}
