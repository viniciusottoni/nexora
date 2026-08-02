import { forwardRef, type InputHTMLAttributes, type ReactNode } from 'react';

export interface InputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'size' | 'prefix'> {
  readonly size?: 'md' | 'lg';
  readonly icon?: ReactNode;
  readonly prefix?: ReactNode;
  readonly suffix?: ReactNode;
  readonly invalid?: boolean;
  readonly numeric?: boolean;
}

export const Input = forwardRef<HTMLInputElement, Readonly<InputProps>>(function Input(
  { size = 'md', icon, prefix, suffix, invalid = false, numeric = false, disabled, className = '', ...props },
  ref,
) {
  return (
    <span
      className={`db-input db-input--${size} ${invalid ? 'db-input--invalid' : ''} ${disabled ? 'db-input--disabled' : ''} ${numeric ? 'db-input--numeric' : ''} ${className}`.trim()}
    >
      {icon}
      {prefix ? <span className="db-input__adornment">{prefix}</span> : null}
      <input {...props} ref={ref} className="db-input__control" disabled={disabled} aria-invalid={invalid} />
      {suffix ? <span className="db-input__adornment">{suffix}</span> : null}
    </span>
  );
});
