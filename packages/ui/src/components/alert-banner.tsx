import type { HTMLAttributes, ReactNode } from 'react';

/**
 * Alerta gerencial ou operacional. Regra anti-ruído (readme › "Alerta = fato +
 * consequência + ação"): nunca só o alarme — traga `children` com a consequência
 * e `actions` com o caminho de resolução.
 */
export type AlertBannerTone = 'info' | 'success' | 'warning' | 'danger' | 'neutral';

export interface AlertBannerProps extends Omit<HTMLAttributes<HTMLDivElement>, 'title'> {
  readonly tone?: AlertBannerTone;
  readonly title?: ReactNode;
  /** Botões de resolução — um alerta sem ação é ruído. */
  readonly actions?: ReactNode;
  /** Sobrescreve o ícone canônico do tom. */
  readonly icon?: string;
}

const TONE_ICON: Record<AlertBannerTone, string> = {
  info: 'info',
  success: 'check_circle',
  warning: 'warning',
  danger: 'error',
  neutral: 'notifications',
};

export function AlertBanner({
  tone = 'info',
  title,
  children,
  actions,
  icon,
  className = '',
  ...props
}: Readonly<AlertBannerProps>) {
  return (
    <div {...props} role="status" className={`db-alert-banner db-alert-banner--${tone} ${className}`.trim()}>
      <span aria-hidden="true" className="material-symbols-rounded db-alert-banner__icon">
        {icon ?? TONE_ICON[tone]}
      </span>
      <div className="db-alert-banner__content">
        {title ? <div className="db-alert-banner__title">{title}</div> : null}
        {children ? <div className="db-alert-banner__body">{children}</div> : null}
      </div>
      {actions ? <div className="db-alert-banner__actions">{actions}</div> : null}
    </div>
  );
}
