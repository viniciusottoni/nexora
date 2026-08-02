import {
  brandingResponseSchema,
  updateBrandingResponseSchema,
  uploadBrandingAssetResponseSchema,
  type BrandingResponse,
  type UpdateBrandingRequest,
  type UploadBrandingAssetRequest,
} from '@nexora/contracts';
import { authenticatedFetch } from '@nexora/ui';

export interface UpdateBrandingResult extends BrandingResponse {
  readonly contrast: {
    readonly valid: boolean;
    readonly minimumRatio: 4.5;
    readonly issues: ReadonlyArray<{ readonly pair: string; readonly ratio: number; readonly suggested: string }>;
  };
}

export interface LogoUploadResult {
  readonly assetId: string;
  readonly publicUrl: string;
}

/** Tipos de imagem aceitos por `UploadBrandingAssetRequest.contentType` — únicos que o backend valida. */
export const LOGO_CONTENT_TYPE_BY_MIME: Readonly<
  Record<string, 'image/svg+xml' | 'image/png' | 'image/jpeg' | 'image/webp'>
> = {
  'image/svg+xml': 'image/svg+xml',
  'image/png': 'image/png',
  'image/jpeg': 'image/jpeg',
  'image/webp': 'image/webp',
};

/**
 * Cliente de administração de marca (US-003, gap "não existe tela de administração de marca") —
 * porta do mesmo padrão de `RolesApi`/`DevicesApi` (fetch autenticado + Idempotency-Key em toda
 * escrita, ADR-020). O upload de logo é em duas etapas: 1) pede uma URL pré-assinada aqui,
 * 2) sobe os bytes DIRETO pro object storage (nunca passam pela API) — ver
 * `Nexora.Infrastructure.Storage.S3BrandingStorage`.
 */
export class BrandingApi {
  constructor(
    private readonly baseUrl = '',
    private readonly fetcher: typeof fetch = authenticatedFetch,
  ) {}

  async get(): Promise<BrandingResponse> {
    const response = await this.fetcher(`${this.baseUrl}/v1/tenant/branding`, {
      credentials: 'include',
    });
    await requireSuccess(response);
    return brandingResponseSchema.parse(await response.json());
  }

  async update(patch: UpdateBrandingRequest): Promise<UpdateBrandingResult> {
    const response = await this.fetcher(`${this.baseUrl}/v1/tenant/branding`, {
      method: 'PATCH',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
      body: JSON.stringify(patch),
    });
    await requireSuccess(response);
    return updateBrandingResponseSchema.parse(await response.json()) as UpdateBrandingResult;
  }

  /** Pede a URL pré-assinada, sobe o arquivo e devolve a URL pública já pronta para salvar em `logo.light`/`logo.dark`. */
  async uploadLogo(request: UploadBrandingAssetRequest, file: File): Promise<LogoUploadResult> {
    const response = await this.fetcher(`${this.baseUrl}/v1/tenant/branding/logo`, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
      body: JSON.stringify(request),
    });
    await requireSuccess(response);
    const prepared = uploadBrandingAssetResponseSchema.parse(await response.json());

    // Upload direto pro object storage — SEM credentials/Idempotency-Key (URL já é a credencial,
    // ADR-020 não se aplica a um PUT pré-assinado de uso único) e sem Content-Type (a assinatura
    // de S3BrandingStorage só cobre o header "host" — um header extra não previsto quebraria a
    // verificação da assinatura em provedores mais estritos).
    const upload = await fetch(prepared.uploadUrl, { method: 'PUT', body: file });
    if (!upload.ok) {
      throw new Error('Não foi possível enviar o arquivo. Tente novamente.');
    }

    return { assetId: prepared.assetId, publicUrl: prepared.publicUrl };
  }
}

export async function sha256Hex(file: File): Promise<string> {
  const digest = await crypto.subtle.digest('SHA-256', await file.arrayBuffer());
  return [...new Uint8Array(digest)].map((byte) => byte.toString(16).padStart(2, '0')).join('');
}

async function requireSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  const problem = (await response.json().catch(() => null)) as { detail?: string } | null;
  throw new Error(problem?.detail ?? 'Não foi possível concluir a operação.');
}
