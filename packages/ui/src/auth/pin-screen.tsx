import { useEffect, useState } from 'react';
import { LockoutMessage } from './lockout-message.js';
import { PinPad } from './pin-pad.js';

export interface PinScreenProps {
  readonly tenantName: string;
  readonly onSubmit: (pin: string) => void | Promise<void>;
  readonly logo?: string;
  readonly busy?: boolean;
  readonly error?: string;
  readonly retryAfterSeconds?: number;
  readonly onLockoutElapsed?: () => void;
}

export function PinScreen(props: Readonly<PinScreenProps>) {
  const [lockedFor, setLockedFor] = useState(props.retryAfterSeconds ?? 0);
  useEffect(() => setLockedFor(props.retryAfterSeconds ?? 0), [props.retryAfterSeconds]);
  return (
    <main className="db-pin-screen">
      <section className="db-pin-intro">
        {props.logo ? (
          <img src={props.logo} alt="" />
        ) : (
          <span aria-hidden="true">{props.tenantName.charAt(0)}</span>
        )}
        <p>{props.tenantName}</p>
        <h1>Quem está operando?</h1>
        <small>Digite seu PIN. Credencial fica vinculada a este dispositivo.</small>
      </section>
      <section className="db-pin-entry" aria-label="Entrada de PIN">
        {lockedFor > 0 ? (
          <LockoutMessage
            retryAfterSeconds={lockedFor}
            onElapsed={() => {
              setLockedFor(0);
              props.onLockoutElapsed?.();
            }}
          />
        ) : (
          <PinPad
            onSubmit={props.onSubmit}
            {...(props.busy === undefined ? {} : { disabled: props.busy })}
            {...(props.error ? { error: props.error } : {})}
          />
        )}
      </section>
    </main>
  );
}
