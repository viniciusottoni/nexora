import { useId, useState } from 'react';
import { Button, Card, Icon } from '@nexora/ui';
import { PRODUCT_UNAVAILABLE_REASON_LABELS, PRODUCT_UNAVAILABLE_REASONS } from '@nexora/contracts';
import { AvailabilityApi, type ProductUnavailableReason } from './availability-api.js';
import './mark-unavailable-from-item.css';

export interface MarkUnavailableFromItemProps {
  readonly productId: string;
  readonly productName: string;
  /** US-044 §6 — item da fila que originou a marcação, para o EVT-012 `order.item.unavailable_flagged`. */
  readonly orderItemId: string;
  readonly api?: AvailabilityApi;
  readonly onMarked?: () => void;
}

/**
 * US-044 §3 ("Ação de marcar indisponível a partir do cartão") — gatilho compacto para colocar
 * dentro de cada linha de item do `OrderTicket` da fila do KDS: um ícone que abre o MESMO diálogo
 * de motivo numerado de `UnavailableToggle` (1 acabou, 2 equipamento, 3 qualidade — nunca texto
 * livre), mas sem o selo/estado de "disponível↔indisponível" que o toggle da tela de produtos
 * mostra — na fila, o operador só quer sinalizar rápido e seguir cozinhando.
 */
export function MarkUnavailableFromItem({
  productId,
  productName,
  orderItemId,
  api,
  onMarked,
}: Readonly<MarkUnavailableFromItemProps>) {
  const dialogTitleId = useId();
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();
  const [done, setDone] = useState(false);

  async function confirm(reason: ProductUnavailableReason): Promise<void> {
    setError(undefined);
    setBusy(true);
    try {
      await (api ?? new AvailabilityApi()).markUnavailable(productId, reason, true, orderItemId);
      setDone(true);
      setOpen(false);
      onMarked?.();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Não foi possível marcar indisponível.');
    } finally {
      setBusy(false);
    }
  }

  if (done) {
    return (
      <span className="kds-mark-unavailable-from-item__done nx-anim-in">
        <Icon name="report" size={16} />
        Sinalizado
      </span>
    );
  }

  return (
    <>
      <button
        type="button"
        className="kds-mark-unavailable-from-item__trigger"
        onClick={() => setOpen(true)}
        aria-label={`Marcar ${productName} como indisponível`}
      >
        <Icon name="report" size={16} />
      </button>
      {open ? (
        <div
          className="kds-availability-dialog"
          role="dialog"
          aria-modal="true"
          aria-labelledby={dialogTitleId}
          onKeyDown={(event) => {
            const index = ['1', '2', '3'].indexOf(event.key);
            if (index === -1 || busy) return;
            const selected = PRODUCT_UNAVAILABLE_REASONS[index];
            if (selected) void confirm(selected);
          }}
        >
          <Card className="kds-availability-dialog__card nx-anim-scale-in">
            <h2 id={dialogTitleId}>Sinalizar falta de {productName}</h2>
            <p>Todos os canais deixam de vender em até 2 segundos.</p>
            <div className="kds-availability-dialog__reasons">
              {PRODUCT_UNAVAILABLE_REASONS.map((reasonCode, index) => (
                <Button
                  key={reasonCode}
                  type="button"
                  variant="danger"
                  size="touch"
                  busy={busy}
                  onClick={() => void confirm(reasonCode)}
                >
                  <span className="kds-availability-dialog__reason-key">{index + 1}</span>
                  {PRODUCT_UNAVAILABLE_REASON_LABELS[reasonCode]}
                </Button>
              ))}
            </div>
            {error ? (
              <p className="kds-availability-toggle__error nx-anim-toast-in" role="alert">
                {error}
              </p>
            ) : null}
            <div className="kds-availability-dialog__actions">
              <Button type="button" variant="ghost" disabled={busy} onClick={() => setOpen(false)}>
                Cancelar
              </Button>
            </div>
          </Card>
        </div>
      ) : null}
    </>
  );
}
