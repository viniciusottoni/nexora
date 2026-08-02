import type { CSSProperties, HTMLAttributes } from 'react';

export interface IconProps extends HTMLAttributes<HTMLSpanElement> {
  /** Nome da ligature do Material Symbols Rounded, ex. "timer", "table_restaurant". */
  readonly name: string;
  /** Tamanho em px. Padrão 20. Use 24 em operação e 32+ no KDS. */
  readonly size?: number;
  /** Glyph preenchido (usado para estado ativo/selecionado). */
  readonly fill?: boolean;
  /** Peso do traço: 300 (leve), 400 (padrão), 500/600 (ênfase). */
  readonly weight?: 300 | 400 | 500 | 600;
  readonly color?: string;
  /** Se informado, o ícone deixa de ser decorativo e recebe role="img". */
  readonly label?: string;
}

export function Icon({
  name,
  size = 20,
  fill = false,
  weight = 400,
  color,
  label,
  style,
  className = '',
  ...props
}: Readonly<IconProps>) {
  const computedStyle: CSSProperties = {
    fontSize: `${size}px`,
    lineHeight: 1,
    color: color || 'inherit',
    flex: '0 0 auto',
    fontVariationSettings: `'FILL' ${fill ? 1 : 0}, 'wght' ${weight}, 'GRAD' 0, 'opsz' ${size}`,
    userSelect: 'none',
    ...style,
  };

  return (
    <span
      {...props}
      className={`material-symbols-rounded ${className}`.trim()}
      style={computedStyle}
      aria-hidden={label ? undefined : 'true'}
      aria-label={label}
      role={label ? 'img' : undefined}
    >
      {name}
    </span>
  );
}
