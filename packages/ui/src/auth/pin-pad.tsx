import { useEffect, useState } from 'react';

export interface PinPadProps {
  readonly onSubmit: (pin: string) => void | Promise<void>;
  readonly submitLabel?: string;
  readonly disabled?: boolean;
  readonly error?: string;
}

const DIGITS = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '0'] as const;

export function PinPad({
  onSubmit,
  submitLabel = 'Entrar',
  disabled = false,
  error,
}: Readonly<PinPadProps>) {
  const [pin, setPin] = useState('');
  const add = (digit: string) =>
    setPin((current) => (current.length < 6 ? `${current}${digit}` : current));
  const remove = () => setPin((current) => current.slice(0, -1));
  const submit = () => {
    if (pin.length >= 4 && !disabled) void onSubmit(pin);
  };
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (disabled) return;
      if (/^\d$/.test(event.key)) add(event.key);
      else if (event.key === 'Backspace') remove();
      else if (event.key === 'Enter') submit();
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  });
  return (
    <div className="db-pin-pad">
      <div
        className="db-pin-dots"
        aria-label={pin.length ? `PIN com ${pin.length} dígitos` : 'PIN vazio'}
      >
        {Array.from({ length: 6 }, (_, index) => (
          <span key={index} className={index < pin.length ? 'is-filled' : ''} aria-hidden="true" />
        ))}
      </div>
      {error && (
        <p className="db-auth-error" role="alert">
          {error}
        </p>
      )}
      <div className="db-pin-grid">
        {DIGITS.slice(0, 9).map((digit, index) => (
          <button
            key={digit}
            type="button"
            onClick={() => add(digit)}
            disabled={disabled}
            autoFocus={index === 0}
          >
            {digit}
          </button>
        ))}
        <button
          type="button"
          className="db-pin-action"
          aria-label="Limpar PIN"
          onClick={() => setPin('')}
          disabled={disabled}
        >
          Limpar
        </button>
        <button type="button" onClick={() => add('0')} disabled={disabled}>
          0
        </button>
        <button
          type="button"
          className="db-pin-action"
          aria-label="Apagar último dígito"
          onClick={remove}
          disabled={disabled}
        >
          Apagar
        </button>
      </div>
      <button
        className="db-pin-submit"
        type="button"
        disabled={disabled || pin.length < 4}
        onClick={submit}
      >
        {submitLabel}
      </button>
    </div>
  );
}
