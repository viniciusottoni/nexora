import { forwardRef, type InputHTMLAttributes, type ReactNode } from 'react';

export interface CheckboxProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  readonly label?: ReactNode;
  readonly type?: 'checkbox' | 'radio';
  /** Valor à direita — usado em grupos de modificadores do cardápio. */
  readonly price?: string;
  /** Remove a altura mínima de toque (use só em listas densas de desktop). */
  readonly compact?: boolean;
}

export const Checkbox = forwardRef<HTMLInputElement, Readonly<CheckboxProps>>(function Checkbox(
  { label, type = 'checkbox', price, compact = false, className = '', ...props },
  ref,
) {
  return (
    <label
      className={`db-checkbox ${type === 'radio' ? 'db-checkbox--radio' : ''} ${compact ? 'db-checkbox--compact' : ''} ${className}`.trim()}
    >
      <input {...props} ref={ref} type={type} />
      <span>{label}</span>
      {price ? <span className="db-checkbox__price">{price}</span> : null}
    </label>
  );
});
