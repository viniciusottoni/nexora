import type { HTMLAttributes, PropsWithChildren, ReactNode } from 'react';

export interface CardProps extends PropsWithChildren<Omit<HTMLAttributes<HTMLElement>, 'title'>> {
  readonly as?: 'article' | 'section' | 'div';
  readonly title?: ReactNode;
  readonly subtitle?: ReactNode;
  readonly actions?: ReactNode;
  readonly footer?: ReactNode;
  readonly elevation?: 'flat' | 'card' | 'raised';
  readonly interactive?: boolean;
  readonly padding?: 'default' | 'tight' | 'none';
}

export function Card({
  as: Element = 'section',
  title,
  subtitle,
  actions,
  footer,
  elevation = 'card',
  interactive = false,
  padding = 'default',
  className = '',
  children,
  ...props
}: Readonly<CardProps>) {
  const cls = [
    'db-card',
    elevation === 'flat' ? 'db-card--flat' : '',
    elevation === 'raised' ? 'db-card--raised' : '',
    interactive ? 'db-card--interactive' : '',
    className,
  ]
    .filter(Boolean)
    .join(' ');
  const bodyCls = [
    'db-card__body',
    padding === 'tight' ? 'db-card__body--tight' : '',
    padding === 'none' ? 'db-card__body--none' : '',
  ]
    .filter(Boolean)
    .join(' ');
  return (
    <Element {...props} className={cls}>
      {title || actions ? (
        <header className="db-card__head">
          <div>
            <div className="db-card__title">{title}</div>
            {subtitle ? <div className="db-card__subtitle">{subtitle}</div> : null}
          </div>
          {actions ? <div className="db-card__actions">{actions}</div> : null}
        </header>
      ) : null}
      <div className={bodyCls}>{children}</div>
      {footer ? <footer className="db-card__foot">{footer}</footer> : null}
    </Element>
  );
}
