import { forwardRef, type ButtonHTMLAttributes } from 'react';

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  readonly busy?: boolean;
  /** `primary` usa a cor do tenant; `accent` é o verde Nexora de confirmação. */
  readonly variant?: 'primary' | 'accent' | 'secondary' | 'ghost' | 'danger';
  /** `touch` (64px) e `lg` (48px) são obrigatórios em mesa, garçom e KDS. */
  readonly size?: 'sm' | 'md' | 'lg' | 'touch';
  readonly block?: boolean;
}

export const Button = forwardRef<HTMLButtonElement, Readonly<ButtonProps>>(function Button(
  { busy = false, disabled, variant = 'primary', size = 'md', block = false, className = '', children, ...props },
  ref,
) {
  return (
    <button
      {...props}
      ref={ref}
      className={`db-button db-button--${variant} db-button--${size} ${block ? 'db-button--block' : ''} ${className}`.trim()}
      disabled={disabled || busy}
      aria-busy={busy}
    >
      {children}
    </button>
  );
});
