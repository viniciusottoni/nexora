import { forwardRef, type InputHTMLAttributes, type ReactNode } from 'react';

export interface SwitchProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  readonly label?: ReactNode;
  readonly description?: ReactNode;
}

export const Switch = forwardRef<HTMLInputElement, Readonly<SwitchProps>>(function Switch(
  { label, description, className = '', ...props },
  ref,
) {
  return (
    <label className={`db-switch ${className}`.trim()}>
      <input {...props} ref={ref} type="checkbox" role="switch" />
      {label ? (
        <span>
          {label}
          {description ? <span className="db-switch__description">{description}</span> : null}
        </span>
      ) : null}
    </label>
  );
});
