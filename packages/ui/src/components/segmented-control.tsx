import type { HTMLAttributes, ReactNode } from 'react';

export interface SegmentedControlOption {
  readonly value: string;
  readonly label: ReactNode;
  readonly icon?: string;
}

export interface SegmentedControlProps extends Omit<HTMLAttributes<HTMLDivElement>, 'className' | 'role' | 'onChange'> {
  readonly options: ReadonlyArray<string | SegmentedControlOption>;
  readonly value?: string;
  readonly onChange?: (value: string) => void;
  /** `lg` para operação de toque. */
  readonly size?: 'md' | 'lg';
  readonly block?: boolean;
  readonly className?: string;
}

function OptionIcon({ name }: Readonly<{ name: string }>) {
  return (
    <span
      aria-hidden="true"
      className="material-symbols-rounded"
      style={{
        fontSize: '18px',
        lineHeight: 1,
        flex: '0 0 auto',
        userSelect: 'none',
        fontVariationSettings: "'FILL' 0, 'wght' 400, 'GRAD' 0, 'opsz' 18",
      }}
    >
      {name}
    </span>
  );
}

function toOption(option: string | SegmentedControlOption): SegmentedControlOption {
  return typeof option === 'string' ? { value: option, label: option } : option;
}

/** Alternador de escopo: período, canal, praça de produção, ambiente do salão. */
export function SegmentedControl({
  options,
  value,
  onChange,
  size = 'md',
  block = false,
  className = '',
  ...props
}: Readonly<SegmentedControlProps>) {
  const cls = [
    'db-segmented-control',
    size === 'lg' ? 'db-segmented-control--lg' : '',
    block ? 'db-segmented-control--block' : '',
    className,
  ]
    .filter(Boolean)
    .join(' ');
  return (
    <div {...props} role="group" className={cls}>
      {options.map((rawOption) => {
        const option = toOption(rawOption);
        return (
          <button
            key={option.value}
            type="button"
            aria-pressed={option.value === value}
            onClick={() => onChange?.(option.value)}
          >
            {option.icon ? <OptionIcon name={option.icon} /> : null}
            {option.label}
          </button>
        );
      })}
    </div>
  );
}
