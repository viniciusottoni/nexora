import type { HTMLAttributes } from 'react';

function Glyph({ name, size }: Readonly<{ name: string; size: number }>) {
  return (
    <span aria-hidden="true" className="material-symbols-rounded" style={{ fontSize: size, lineHeight: 1 }}>
      {name}
    </span>
  );
}

export interface QuantityStepperProps extends Omit<HTMLAttributes<HTMLDivElement>, 'onChange'> {
  readonly value?: number;
  readonly min?: number;
  readonly max?: number;
  readonly onChange?: (value: number) => void;
  readonly size?: 'sm' | 'md';
}

export function QuantityStepper({
  value = 1,
  min = 0,
  max = 99,
  onChange,
  size = 'md',
  className = '',
  ...props
}: Readonly<QuantityStepperProps>) {
  const set = (next: number) => onChange?.(Math.min(max, Math.max(min, next)));
  return (
    <div
      {...props}
      className={`db-quantity-stepper ${size === 'sm' ? 'db-quantity-stepper--sm' : ''} ${className}`.trim()}
    >
      <button type="button" aria-label="Diminuir" disabled={value <= min} onClick={() => set(value - 1)}>
        <Glyph name="remove" size={size === 'sm' ? 16 : 20} />
      </button>
      <span className="db-quantity-stepper__value">{value}</span>
      <button type="button" aria-label="Aumentar" disabled={value >= max} onClick={() => set(value + 1)}>
        <Glyph name="add" size={size === 'sm' ? 16 : 20} />
      </button>
    </div>
  );
}
