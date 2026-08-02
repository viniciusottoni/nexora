import type { CSSProperties } from 'react';

export interface BrandingPreviewProps {
  readonly tenantName: string;
  readonly welcome: string;
  readonly primary: string;
  readonly onPrimary: string;
  readonly surface: string;
  readonly radius: number;
  readonly logo?: string;
}

export function BrandingPreview(props: Readonly<BrandingPreviewProps>) {
  const style = {
    '--preview-primary': props.primary,
    '--preview-on-primary': props.onPrimary,
    '--preview-surface': props.surface,
    '--preview-radius': `${props.radius}px`,
  } as CSSProperties;
  return (
    <section
      className="db-branding-preview"
      style={style}
      aria-label="Pré-visualização da identidade visual"
    >
      <header>
        {props.logo ? (
          <img src={props.logo} alt="" />
        ) : (
          <span aria-hidden="true">{props.tenantName.charAt(0)}</span>
        )}
        <strong>{props.tenantName}</strong>
      </header>
      <div>
        <small>Mensagem de boas-vindas</small>
        <p>{props.welcome}</p>
        <button type="button">Ver cardápio</button>
      </div>
    </section>
  );
}
