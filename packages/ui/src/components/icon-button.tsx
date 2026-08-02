import { forwardRef, type ButtonHTMLAttributes } from 'react';
import { Icon } from './icon.js';

export interface IconButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  /** Nome da ligature do Material Symbols Rounded. */
  readonly icon: string;
  readonly size?: 'sm' | 'md' | 'lg';
  readonly variant?: 'ghost' | 'solid' | 'outline';
  /** Contador sobreposto (alertas, chamados de mesa). */
  readonly badge?: number | string;
  /** Obrigatório — vira aria-label e title. */
  readonly label: string;
}

export const IconButton = forwardRef<HTMLButtonElement, Readonly<IconButtonProps>>(function IconButton(
  { icon, size = 'md', variant = 'ghost', badge, label, className = '', type = 'button', ...props },
  ref,
) {
  const glyphSize = size === 'lg' ? 24 : size === 'sm' ? 18 : 20;
  return (
    <button
      {...props}
      ref={ref}
      type={type}
      aria-label={label}
      title={label}
      className={`db-icon-button db-icon-button--${size} db-icon-button--${variant} ${className}`.trim()}
    >
      <Icon name={icon} size={glyphSize} />
      {badge ? <span className="db-icon-button__dot">{badge}</span> : null}
    </button>
  );
});
