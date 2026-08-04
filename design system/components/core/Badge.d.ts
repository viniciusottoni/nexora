/** Rótulo compacto para canal, fase, prioridade e contagem. */
export interface BadgeProps {
  children?: React.ReactNode;
  tone?: 'neutral' | 'brand' | 'info' | 'success' | 'warning' | 'danger' | 'accent' | 'solid';
  size?: 'sm' | 'md' | 'lg';
  icon?: string;
  /** Canto reto — use em códigos e siglas (ex. "M12"). */
  square?: boolean;
}
export function Badge(props: BadgeProps): JSX.Element;
