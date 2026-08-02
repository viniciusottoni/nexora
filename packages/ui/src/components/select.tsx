import { forwardRef, type SelectHTMLAttributes } from 'react';

function Glyph({ name, size }: Readonly<{ name: string; size: number }>) {
  return (
    <span aria-hidden="true" className="material-symbols-rounded" style={{ fontSize: size, lineHeight: 1 }}>
      {name}
    </span>
  );
}

export interface SelectOption {
  readonly value: string;
  readonly label: string;
}

export interface SelectProps extends Omit<SelectHTMLAttributes<HTMLSelectElement>, 'size'> {
  readonly size?: 'md' | 'lg';
  /** Strings ou `{value,label}`. Ignorado se `children` for passado. */
  readonly options?: ReadonlyArray<string | SelectOption>;
}

export const Select = forwardRef<HTMLSelectElement, Readonly<SelectProps>>(function Select(
  { size = 'md', options = [], children, className = '', ...props },
  ref,
) {
  return (
    <span className={`db-select db-select--${size} ${className}`.trim()}>
      <select {...props} ref={ref} className="db-select__control">
        {children ??
          options.map((option) => {
            const value = typeof option === 'string' ? option : option.value;
            const label = typeof option === 'string' ? option : option.label;
            return (
              <option key={value} value={value}>
                {label}
              </option>
            );
          })}
      </select>
      <span className="db-select__chevron">
        <Glyph name="expand_more" size={20} />
      </span>
    </span>
  );
});
