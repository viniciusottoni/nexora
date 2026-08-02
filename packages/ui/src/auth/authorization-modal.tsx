import { useEffect } from 'react';
import { PinPad } from './pin-pad.js';

export interface AuthorizationModalProps {
  readonly actionLabel: string;
  readonly onAuthorize: (pin: string) => void | Promise<void>;
  readonly onCancel: () => void;
  readonly busy?: boolean;
  readonly error?: string;
}

export function AuthorizationModal(props: Readonly<AuthorizationModalProps>) {
  useEffect(() => {
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !props.busy) props.onCancel();
    };
    document.addEventListener('keydown', closeOnEscape);
    return () => document.removeEventListener('keydown', closeOnEscape);
  }, [props]);
  return (
    <div className="db-auth-backdrop">
      <section
        className="db-auth-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="authorization-title"
      >
        <button
          type="button"
          className="db-auth-close"
          aria-label="Cancelar autorização"
          onClick={props.onCancel}
          disabled={props.busy}
        >
          ×
        </button>
        <p className="db-auth-kicker">Ação sensível</p>
        <h2 id="authorization-title">Autorização necessária</h2>
        <p>{props.actionLabel}. Peça o PIN de um perfil autorizado.</p>
        <PinPad
          onSubmit={props.onAuthorize}
          submitLabel="Autorizar"
          {...(props.busy === undefined ? {} : { disabled: props.busy })}
          {...(props.error ? { error: props.error } : {})}
        />
      </section>
    </div>
  );
}
