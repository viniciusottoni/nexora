import { useId, type HTMLAttributes, type PropsWithChildren, type ReactNode } from 'react';

export interface FieldProps extends PropsWithChildren<HTMLAttributes<HTMLDivElement>> {
  readonly label?: ReactNode;
  readonly hint?: ReactNode;
  readonly error?: ReactNode;
  readonly required?: boolean;
  readonly htmlFor?: string;
}

export function Field({
  label,
  hint,
  error,
  required = false,
  htmlFor,
  className = '',
  children,
  ...props
}: Readonly<FieldProps>) {
  const generatedId = useId();
  const descriptionId = `${htmlFor ?? generatedId}-description`;
  return (
    <div {...props} className={`db-field ${className}`.trim()}>
      {label ? (
        <label className="db-field__label" htmlFor={htmlFor}>
          {label}
          {required ? <span className="db-field__required">*</span> : null}
        </label>
      ) : null}
      {children}
      {error ? (
        <span id={descriptionId} className="db-field__error">
          {error}
        </span>
      ) : hint ? (
        <span id={descriptionId} className="db-field__hint">
          {hint}
        </span>
      ) : null}
    </div>
  );
}
