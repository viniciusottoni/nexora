import {
  areaListResponseSchema,
  areaSchema,
  tableListResponseSchema,
  tablesBulkResponseSchema,
  tableSchema,
  type AreaDto,
  type AreaListResponse,
  type CreateAreaRequest,
  type CreateTableRequest,
  type CreateTablesBulkRequest,
  type TableDto,
  type TableListResponse,
  type TablesBulkResponse,
  type UpdateAreaRequest,
  type UpdateTableRequest,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

/** Cliente de `/v1/areas` (US-020) — CRUD de ambientes do salão. */
export class AreasApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = authenticatedFetch,
  ) {}

  async list(): Promise<AreaListResponse> {
    const response = await this.fetcher(`${this.baseUrl}/v1/areas`, { credentials: 'include' });
    await requireSuccess(response);
    return areaListResponseSchema.parse(await response.json());
  }

  async create(input: CreateAreaRequest): Promise<AreaDto> {
    return this.writeArea('/v1/areas', { method: 'POST', body: JSON.stringify(input) });
  }

  async update(id: string, input: UpdateAreaRequest): Promise<AreaDto> {
    return this.writeArea(`/v1/areas/${encodeURIComponent(id)}`, {
      method: 'PATCH',
      body: JSON.stringify(input),
    });
  }

  async activate(id: string): Promise<void> {
    await write(this.baseUrl, this.fetcher, `/v1/areas/${encodeURIComponent(id)}/activate`, {
      method: 'POST',
    });
  }

  async deactivate(id: string): Promise<void> {
    await write(this.baseUrl, this.fetcher, `/v1/areas/${encodeURIComponent(id)}/deactivate`, {
      method: 'POST',
    });
  }

  async remove(id: string): Promise<void> {
    await write(this.baseUrl, this.fetcher, `/v1/areas/${encodeURIComponent(id)}`, {
      method: 'DELETE',
    });
  }

  private async writeArea(path: string, init: RequestInit): Promise<AreaDto> {
    const response = await write(this.baseUrl, this.fetcher, path, init);
    return areaSchema.parse(await response.json());
  }
}

/** Cliente de `/v1/tables` (US-020) — CRUD de mesas, criação em lote, rotação de token e exportação de QR Codes em PDF. */
export class TablesApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = authenticatedFetch,
  ) {}

  async list(areaId?: string): Promise<TableListResponse> {
    const query = areaId ? `?areaId=${encodeURIComponent(areaId)}` : '';
    const response = await this.fetcher(`${this.baseUrl}/v1/tables${query}`, {
      credentials: 'include',
    });
    await requireSuccess(response);
    return tableListResponseSchema.parse(await response.json());
  }

  async create(input: CreateTableRequest): Promise<TableDto> {
    const response = await write(this.baseUrl, this.fetcher, '/v1/tables', {
      method: 'POST',
      body: JSON.stringify(input),
    });
    return tableSchema.parse(await response.json());
  }

  /** Cenário Gherkin "Criação em lote" — cria mesas com rótulos sequenciais numa única intenção transacional. */
  async createBulk(input: CreateTablesBulkRequest): Promise<TablesBulkResponse> {
    const response = await write(this.baseUrl, this.fetcher, '/v1/tables/bulk', {
      method: 'POST',
      body: JSON.stringify(input),
    });
    return tablesBulkResponseSchema.parse(await response.json());
  }

  async update(id: string, input: UpdateTableRequest): Promise<TableDto> {
    const response = await write(
      this.baseUrl,
      this.fetcher,
      `/v1/tables/${encodeURIComponent(id)}`,
      {
        method: 'PATCH',
        body: JSON.stringify(input),
      },
    );
    return tableSchema.parse(await response.json());
  }

  /** Cenário Gherkin "Rotação de token" — o QR Code impresso precisa ser substituído depois desta chamada. */
  async rotateQrToken(id: string): Promise<void> {
    await write(this.baseUrl, this.fetcher, `/v1/tables/${encodeURIComponent(id)}/rotate-token`, {
      method: 'POST',
    });
  }

  async activate(id: string): Promise<void> {
    await write(this.baseUrl, this.fetcher, `/v1/tables/${encodeURIComponent(id)}/activate`, {
      method: 'POST',
    });
  }

  async deactivate(id: string): Promise<void> {
    await write(this.baseUrl, this.fetcher, `/v1/tables/${encodeURIComponent(id)}/deactivate`, {
      method: 'POST',
    });
  }

  async remove(id: string): Promise<void> {
    await write(this.baseUrl, this.fetcher, `/v1/tables/${encodeURIComponent(id)}`, {
      method: 'DELETE',
    });
  }

  /** Cenário Gherkin "Exportação para impressão" — devolve o PDF pronto para download (um QR Code por página). */
  async exportQrCodesPdf(areaId?: string): Promise<Blob> {
    const query = areaId ? `?areaId=${encodeURIComponent(areaId)}` : '';
    const response = await this.fetcher(`${this.baseUrl}/v1/tables/qr-codes.pdf${query}`, {
      credentials: 'include',
    });
    await requireSuccess(response);
    return response.blob();
  }
}

async function write(
  baseUrl: string,
  fetcher: typeof fetch,
  path: string,
  init: RequestInit,
): Promise<Response> {
  const response = await fetcher(`${baseUrl}${path}`, {
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

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  throw new Error(problem?.detail ?? 'Não foi possível concluir a operação.');
}
