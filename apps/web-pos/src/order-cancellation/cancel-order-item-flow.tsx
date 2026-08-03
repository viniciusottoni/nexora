import { useMemo, useState } from 'react';
import { AuthorizationModal, Button, type OperationalRequestIdentity } from '@nexora/ui';
import { OrderCancellationApiError, PosOrderCancellationApi } from './order-cancellation-api.js';
import { CANCELLATION_REASONS } from './order-cancellation-reasons.js';
import './cancel-order-item-flow.css';

export interface CancelOrderItemFlowProps {
  readonly identity: OperationalRequestIdentity;
  readonly orderId: string;
  readonly itemId: string;
  readonly itemName: string;
  readonly baseUrl?: string;
  readonly fetcher?: typeof fetch;
  /** Chamado depois do cancelamento confirmado pelo servidor — quem usa o componente remove o item da própria lista. */
  readonly onCancelled: () => void;
  readonly onDismiss?: () => void;
}

type Stage = 'reason' | 'authorizing';

/**
 * US-033 (Cancelar item ou pedido com autorização) §7/§10 — fluxo completo do lado do cliente:
 * motivo obrigatório (lista curta, US-033 §10) → tenta cancelar → se o servidor recusar com 403
 * `AUTHORIZATION_REQUIRED` (item já iniciado), abre o {@link AuthorizationModal} do design system
 * (`packages/ui`, ADR-023) → gerente digita o PIN NO MESMO DISPOSITIVO do garçom → `POST
 * /v1/auth/authorize` → repete o cancelamento com `X-Authorization-Token`.
 */
export function CancelOrderItemFlow({
  identity,
  orderId,
  itemId,
  itemName,
  baseUrl = '',
  fetcher,
  onCancelled,
  onDismiss,
}: Readonly<CancelOrderItemFlowProps>) {
  const [stage, setStage] = useState<Stage>('reason');
  const [reason, setReason] = useState('');
  const [notes, setNotes] = useState('');
  const [error, setError] = useState<string>();
  const [authError, setAuthError] = useState<string>();
  const [busy, setBusy] = useState(false);

  const api = useMemo(
    () => new PosOrderCancellationApi(identity, baseUrl, fetcher),
    [identity, baseUrl, fetcher],
  );

  async function attemptCancel(authorizationToken?: string) {
    setBusy(true);
    setError(undefined);
    try {
      await api.cancelItem(orderId, itemId, reason, notes.trim() || undefined, authorizationToken);
      onCancelled();
    } catch (cause) {
      if (cause instanceof OrderCancellationApiError && cause.code === 'AUTHORIZATION_REQUIRED') {
        setStage('authorizing');
        return;
      }
      setError(
        cause instanceof OrderCancellationApiError
          ? cause.message
          : 'Não foi possível cancelar o item agora. Tente novamente.',
      );
    } finally {
      setBusy(false);
    }
  }

  async function handleAuthorize(pin: string) {
    setBusy(true);
    setAuthError(undefined);
    try {
      const grant = await api.authorize('CANCEL_STARTED_ITEM', pin, { orderItemId: itemId });
      await attemptCancel(grant.authorizationToken);
    } catch (cause) {
      setAuthError(
        cause instanceof OrderCancellationApiError ? cause.message : 'PIN inválido. Tente novamente.',
      );
      setBusy(false);
    }
  }

  if (stage === 'authorizing') {
    return (
      <AuthorizationModal
        actionLabel={`Cancelar "${itemName}" — item já em produção`}
        onAuthorize={handleAuthorize}
        onCancel={() => setStage('reason')}
        busy={busy}
        {...(authError ? { error: authError } : {})}
      />
    );
  }

  return (
    <div className="db-cancel-item-backdrop">
      <section className="db-cancel-item-modal" role="dialog" aria-modal="true" aria-label={`Cancelar ${itemName}`}>
        <h3>Cancelar item</h3>
        <p className="db-cancel-item-name">{itemName}</p>

        <label className="db-cancel-item-field">
          Motivo
          <select value={reason} onChange={(event) => setReason(event.target.value)} disabled={busy}>
            <option value="">Selecione…</option>
            {CANCELLATION_REASONS.map((option) => (
              <option key={option.code} value={option.code}>
                {option.label}
              </option>
            ))}
          </select>
        </label>

        <label className="db-cancel-item-field">
          Observação (opcional)
          <textarea
            value={notes}
            onChange={(event) => setNotes(event.target.value)}
            maxLength={500}
            disabled={busy}
            placeholder="Ex.: cliente pediu para retirar"
          />
        </label>

        {error ? (
          <p role="alert" className="db-cancel-item-error">
            {error}
          </p>
        ) : null}

        <footer className="db-cancel-item-footer">
          <Button type="button" variant="ghost" onClick={() => onDismiss?.()} disabled={busy}>
            Voltar
          </Button>
          <Button type="button" onClick={() => void attemptCancel()} disabled={!reason || busy} busy={busy}>
            Confirmar cancelamento
          </Button>
        </footer>
      </section>
    </div>
  );
}
