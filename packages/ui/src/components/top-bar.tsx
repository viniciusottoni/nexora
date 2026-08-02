import type { HTMLAttributes, ReactNode } from 'react';

export interface TopBarProps extends Omit<HTMLAttributes<HTMLElement>, 'className' | 'title'> {
  readonly title?: ReactNode;
  readonly subtitle?: ReactNode;
  readonly left?: ReactNode;
  /** Slot da direita — sempre carrega o `SyncStatus`. */
  readonly right?: ReactNode;
  readonly variant?: 'card' | 'sunken' | 'brand';
  readonly className?: string;
}

/**
 * Barra superior fixa de 56px. Um dos dois únicos elementos de chrome fixo do
 * produto — ver Design System readme.md § "Espaço, densidade e layout".
 */
export function TopBar({
  title,
  subtitle,
  left,
  right,
  variant = 'card',
  className = '',
  ...props
}: Readonly<TopBarProps>) {
  let variantClass = '';
  if (variant === 'sunken') variantClass = 'db-topbar--sunken';
  else if (variant === 'brand') variantClass = 'db-topbar--brand';

  return (
    <header {...props} className={`db-topbar ${variantClass} ${className}`.trim()}>
      {left}
      {title || subtitle ? (
        <div className="db-topbar__group">
          <div className="db-topbar__title">{title}</div>
          {subtitle ? <div className="db-topbar__subtitle">{subtitle}</div> : null}
        </div>
      ) : null}
      <div className="db-topbar__spacer" />
      {right ? <div className="db-topbar__right">{right}</div> : null}
    </header>
  );
}
