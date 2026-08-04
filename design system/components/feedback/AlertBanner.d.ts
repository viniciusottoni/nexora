/**
 * Alerta gerencial ou operacional. Regra anti-ruído: só alerte quem precisa
 * agir — cada alerta deve trazer a ação junto em `actions`.
 */
export interface AlertBannerProps {
  tone?: 'info' | 'success' | 'warning' | 'danger' | 'neutral';
  title?: React.ReactNode;
  children?: React.ReactNode;
  /** Botões de resolução — um alerta sem ação é ruído. */
  actions?: React.ReactNode;
  icon?: string;
}
export function AlertBanner(props: AlertBannerProps): JSX.Element;
