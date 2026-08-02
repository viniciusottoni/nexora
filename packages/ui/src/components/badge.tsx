import type { HTMLAttributes, PropsWithChildren } from 'react';
import { Icon } from './icon.js';

export interface BadgeProps extends PropsWithChildren<HTMLAttributes<HTMLSpanElement>> {
  readonly tone?: 'neutral' | 'brand' | 'info' | 'success' | 'warning' | 'danger' | 'accent' | 'solid';
  readonly size?: 'sm' | 'md' | 'lg';
  readonly icon?: string;
  /** Canto reto — use em códigos e siglas (ex. "M12"). */
  readonly square?: boolean;
}

export function Badge({
  children,
  tone = 'neutral',
  size = 'md',
  icon,
  square = false,
  className = '',
  ...props
}: Readonly<BadgeProps>) {
  return (
    <span
      {...props}
      className={`db-badge db-badge--${tone} db-badge--${size} ${square ? 'db-badge--square' : ''} ${className}`.trim()}
    >
      {icon ? <Icon name={icon} size={size === 'sm' ? 12 : 14} /> : null}
      {children}
    </span>
  );
}
