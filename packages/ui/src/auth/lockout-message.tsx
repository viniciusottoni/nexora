import { useEffect, useState } from 'react';

export interface LockoutMessageProps {
  readonly retryAfterSeconds: number;
  readonly onElapsed?: () => void;
}

export function LockoutMessage({ retryAfterSeconds, onElapsed }: Readonly<LockoutMessageProps>) {
  const [remaining, setRemaining] = useState(Math.max(0, retryAfterSeconds));
  useEffect(() => {
    if (remaining <= 0) {
      onElapsed?.();
      return;
    }
    const timer = window.setTimeout(() => setRemaining((value) => Math.max(0, value - 1)), 1_000);
    return () => window.clearTimeout(timer);
  }, [remaining, onElapsed]);
  const minutes = Math.floor(remaining / 60)
    .toString()
    .padStart(2, '0');
  const seconds = (remaining % 60).toString().padStart(2, '0');
  return (
    <p className="db-lockout" role="status" aria-live="polite">
      Acesso temporariamente bloqueado. Tente novamente em{' '}
      <strong>
        {minutes}:{seconds}
      </strong>
      .
    </p>
  );
}
