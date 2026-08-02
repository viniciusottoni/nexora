import type { CSSProperties, HTMLAttributes, ReactNode } from 'react';

export interface BrandMarkProps extends HTMLAttributes<HTMLSpanElement> {
  /** Caminho do arquivo de logo (Nexora ou do tenant). */
  readonly logoSrc?: string;
  /** Nome do estabelecimento — usado quando o tenant ainda não enviou logo. */
  readonly tenantName?: string;
  readonly subtitle?: string;
  /** Altura do logo em px (ou base de escala do wordmark). */
  readonly size?: number;
  readonly inverse?: boolean;
}

/**
 * Assinatura de marca. Sem `logoSrc` renderiza o wordmark em tipo — nunca redesenha o
 * símbolo do logo; aponte `logoSrc` para o arquivo em `assets/`.
 */
export function BrandMark({
  logoSrc,
  tenantName,
  subtitle,
  size = 28,
  inverse = false,
  className = '',
  ...props
}: Readonly<BrandMarkProps>) {
  let inner: ReactNode;

  if (logoSrc) {
    inner = (
      <img
        src={logoSrc}
        alt={tenantName || 'Nexora'}
        className="db-brand-mark__img"
        style={{ height: `${size}px` }}
      />
    );
  } else if (tenantName) {
    const tenantStyle = {
      '--db-brand-mark-size': `${size}px`,
      fontSize: `${size * 0.46}px`,
    } as CSSProperties;
    inner = (
      <>
        <span className="db-brand-mark__tenant" style={tenantStyle}>
          {tenantName.trim().charAt(0).toUpperCase()}
        </span>
        <span>
          <span className="db-brand-mark__word" style={{ fontSize: `${size * 0.62}px` }}>
            {tenantName}
          </span>
          {subtitle ? (
            <span className="db-brand-mark__sub" style={{ display: 'block' }}>
              {subtitle}
            </span>
          ) : null}
        </span>
      </>
    );
  } else {
    inner = (
      <span>
        <span className="db-brand-mark__word" style={{ fontSize: `${size * 0.7}px` }}>
          NEXORA
        </span>
        {subtitle ? (
          <span className="db-brand-mark__sub" style={{ display: 'block' }}>
            {subtitle}
          </span>
        ) : null}
      </span>
    );
  }

  return (
    <span
      {...props}
      className={`db-brand-mark ${inverse ? 'db-brand-mark--inverse' : ''} ${className}`.trim()}
    >
      {inner}
    </span>
  );
}
