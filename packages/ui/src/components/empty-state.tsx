import type { HTMLAttributes, ReactNode } from 'react';

/** Estado vazio. Diga o que fazer, não só que está vazio. */
export interface EmptyStateProps extends Omit<HTMLAttributes<HTMLDivElement>, 'title'> {
  readonly icon?: string;
  readonly title?: ReactNode;
  readonly action?: ReactNode;
}

export function EmptyState({
  icon = 'inbox',
  title,
  children,
  action,
  className = '',
  ...props
}: Readonly<EmptyStateProps>) {
  return (
    <div {...props} className={`db-empty-state ${className}`.trim()}>
      <span className="db-empty-state__icon">
        <span aria-hidden="true" className="material-symbols-rounded db-empty-state__icon-glyph">
          {icon}
        </span>
      </span>
      {title ? <div className="db-empty-state__title">{title}</div> : null}
      {children ? <div className="db-empty-state__body">{children}</div> : null}
      {action ? <div className="db-empty-state__action">{action}</div> : null}
    </div>
  );
}
