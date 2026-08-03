import type { AnchorHTMLAttributes } from 'react';

export type CreatedByFooterProps = Omit<
  AnchorHTMLAttributes<HTMLAnchorElement>,
  'href' | 'children' | 'target' | 'rel'
>;

/**
 * Crédito do fornecedor da plataforma, presente em toda página web do produto —
 * nunca a marca do tenant (isso é `BrandMark`/`NexoraLogo`). Mesmo texto e link em
 * qualquer instância/tenant, sem exceção (ADR-013): não é branding, é atribuição.
 */
export function CreatedByFooter(props: Readonly<CreatedByFooterProps>) {
  const { className = '', ...rest } = props;
  return (
    <a
      {...rest}
      href="https://www.replaystudio.com.br"
      target="_blank"
      rel="noreferrer"
      className={`db-created-by ${className}`.trim()}
    >
      Created by ReplayStudio
    </a>
  );
}
