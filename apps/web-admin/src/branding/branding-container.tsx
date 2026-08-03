import { useEffect, useState } from 'react';
import type { Branding, UpdateBrandingRequest } from '@nexora/contracts';
import { BrandingApi, LOGO_CONTENT_TYPE_BY_MIME, sha256Hex, type LogoUploadResult } from './branding-api.js';
import { BrandingManagementPage } from './branding-management-page.js';

export interface BrandingContainerProps {
  readonly api?: BrandingApi;
  readonly baseUrl?: string;
}

/**
 * Carrega o branding atual do tenant autenticado (nome do tenant incluso na própria resposta de
 * `GET /v1/tenant/branding` — não precisa vir de outro lugar) e conecta
 * {@link BrandingManagementPage} a {@link BrandingApi} — separado da página em si para a página
 * continuar testável só com props (mesmo padrão de `RoleManagementPage`/`DeviceManagementPage`,
 * que recebem callbacks já prontos em vez de instanciar sua própria Api).
 */
export function BrandingContainer({
  api,
  baseUrl = '',
}: Readonly<BrandingContainerProps>) {
  const brandingApi = api ?? new BrandingApi(baseUrl);
  const [tenantName, setTenantName] = useState<string>();
  const [branding, setBranding] = useState<Branding>();
  const [error, setError] = useState<string>();

  useEffect(() => {
    let active = true;
    brandingApi
      .get()
      .then((response) => {
        if (!active) return;
        setTenantName(response.tenant.name);
        setBranding(response.branding);
      })
      .catch((reason: unknown) => {
        if (active) setError(reason instanceof Error ? reason.message : 'Não foi possível carregar a marca.');
      });
    return () => {
      active = false;
    };
  }, [brandingApi]);

  async function save(patch: UpdateBrandingRequest): Promise<Branding> {
    const result = await brandingApi.update(patch);
    return result.branding;
  }

  async function uploadLogo(kind: 'LOGO_LIGHT' | 'LOGO_DARK', file: File): Promise<LogoUploadResult> {
    const contentType = LOGO_CONTENT_TYPE_BY_MIME[file.type];
    if (!contentType) {
      throw new Error('Formato de imagem não suportado. Use SVG, PNG, JPEG ou WEBP.');
    }
    return brandingApi.uploadLogo(
      { kind, contentType, bytes: file.size, sha256: await sha256Hex(file) },
      file,
    );
  }

  if (error) {
    return (
      <p className="branding-error" role="alert">
        {error}
      </p>
    );
  }

  if (!branding || !tenantName) {
    return (
      <p className="branding-loading" role="status">
        <span className="nx-spinner" aria-hidden="true" />
        Carregando identidade visual…
      </p>
    );
  }

  return (
    <BrandingManagementPage
      tenantName={tenantName}
      branding={branding}
      onSave={save}
      onUploadLogo={uploadLogo}
    />
  );
}
