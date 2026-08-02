import type { HTMLAttributes } from 'react';

function Glyph({ name, size }: Readonly<{ name: string; size: number }>) {
  return (
    <span aria-hidden="true" className="material-symbols-rounded" style={{ fontSize: size, lineHeight: 1 }}>
      {name}
    </span>
  );
}

const DIGIT_KEYS = ['1', '2', '3', '4', '5', '6', '7', '8', '9'] as const;

export interface NumericKeypadProps extends Omit<HTMLAttributes<HTMLDivElement>, 'onChange' | 'onSubmit'> {
  readonly value?: string;
  readonly onChange?: (value: string) => void;
  readonly onSubmit?: (value: string) => void;
  /** Nº máximo de dígitos (4–6 no PIN). */
  readonly length?: number;
  /** Mostra os marcadores de PIN acima do teclado. */
  readonly showDots?: boolean;
  /** Variante para superfície escura do KDS. */
  readonly dark?: boolean;
}

export function NumericKeypad({
  value = '',
  onChange,
  onSubmit,
  length,
  showDots = false,
  dark = false,
  className = '',
  ...props
}: Readonly<NumericKeypadProps>) {
  const push = (digit: string) => {
    if (length && value.length >= length) return;
    onChange?.(value + digit);
  };
  return (
    <div {...props} className={className}>
      {showDots ? (
        <div className={`db-numeric-keypad__dots ${dark ? 'db-numeric-keypad__dots--dark' : ''}`.trim()}>
          {Array.from({ length: length ?? 4 }, (_, index) => (
            <span
              key={index}
              className={`db-numeric-keypad__dot ${index < value.length ? 'db-numeric-keypad__dot--on' : ''}`.trim()}
            />
          ))}
        </div>
      ) : null}
      <div className={`db-numeric-keypad ${dark ? 'db-numeric-keypad--dark' : ''}`.trim()}>
        {DIGIT_KEYS.map((digit) => (
          <button key={digit} type="button" onClick={() => push(digit)}>
            {digit}
          </button>
        ))}
        <button type="button" aria-label="Apagar" onClick={() => onChange?.(value.slice(0, -1))}>
          <Glyph name="backspace" size={24} />
        </button>
        <button type="button" onClick={() => push('0')}>
          0
        </button>
        <button
          type="button"
          className="db-numeric-keypad__confirm"
          aria-label="Confirmar"
          onClick={() => onSubmit?.(value)}
        >
          <Glyph name="check" size={28} />
        </button>
      </div>
    </div>
  );
}
