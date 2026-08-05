import { useEffect, useId, useRef, useState } from 'react';
import { Button, Card, StatusPill } from '@nexora/ui';
import {
  PRODUCT_UNAVAILABLE_REASON_LABELS,
  PRODUCT_UNAVAILABLE_REASONS,
} from '@nexora/contracts';
import {
  AvailabilityApi,
  subscribeToAvailability,
  type AvailabilitySubscription,
  type ProductAvailabilityChangedEvent,
  type ProductUnavailableReason,
} from './availability-api.js';
import './unavailable-toggle.css';

export interface UnavailableToggleProps {
  readonly productId: string;
  readonly productName: string;
  readonly isAvailable: boolean;
  readonly unavailableReason?: string | null;
  /** Injetável para teste — padrão `new AvailabilityApi()`. */
  readonly api?: AvailabilityApi;
  /** JWT do dispositivo/operador autenticado, usado pela assinatura em tempo real. */
  readonly accessToken?: string;
  readonly onChanged?: (isAvailable: boolean, reason: string | null) => void;
  /** Injetável para teste — evita abrir um WebSocket real em ambiente de teste. */
  readonly subscribeFn?: typeof subscribeToAvailability;
}

/**
 * Botão "cabe em um toque" do KDS (US-015 §10, detalhamento fino em US-044) para marcar um
 * produto indisponível/disponível de novo. Um único elemento alterna os dois sentidos: disponível
 * → toque pede o motivo e marca indisponível; indisponível → toque marca disponível direto (sem
 * confirmação extra — a cozinha já sabe que o insumo voltou).
 *
 * Assina `subscribeToAvailability` para refletir em tempo real (WebSocket + fallback de polling a
 * cada 5s, US-015 §9) uma marcação feita por OUTRO dispositivo (garçom, painel do gestor) para o
 * MESMO produto — sem isso, dois toques simultâneos (KDS e painel) divergiriam até a próxima
 * atualização manual da tela.
 */
export function UnavailableToggle({
  productId,
  productName,
  isAvailable: initialIsAvailable,
  unavailableReason: initialReason,
  api,
  accessToken,
  onChanged,
  subscribeFn = subscribeToAvailability,
}: Readonly<UnavailableToggleProps>) {
  const dialogTitleId = useId();
  const [isAvailable, setIsAvailable] = useState(initialIsAvailable);
  const [reason, setReason] = useState<string | null>(initialReason ?? null);
  const [reasonDialogOpen, setReasonDialogOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();
  const infoRef = useRef<HTMLDivElement>(null);

  /** Destaca brevemente nome + selo quando o valor muda sem ação deste dispositivo
   * (outro terminal marcou o mesmo produto) — sem remontar o nó, como pede
   * `.nx-anim-flash` (packages/ui/src/components/motion.css). */
  function flashInfo(): void {
    const el = infoRef.current;
    if (!el) return;
    el.classList.remove('nx-anim-flash');
    void el.offsetWidth; // força reflow para permitir reiniciar a animação no mesmo nó
    el.classList.add('nx-anim-flash');
  }

  useEffect(() => {
    const subscription: AvailabilitySubscription = subscribeFn(
      (event: ProductAvailabilityChangedEvent) => {
        if (event.data.productId !== productId) return;
        if (event.type === 'product.unavailable') {
          setIsAvailable(false);
          setReason(event.data.reason ?? null);
        } else {
          setIsAvailable(true);
          setReason(null);
        }
        flashInfo();
      },
      { ...(accessToken ? { accessToken } : {}), ...(api ? { api } : {}) },
    );

    return () => subscription.close();
  }, [productId, accessToken, api, subscribeFn]);

  async function handleToggle(): Promise<void> {
    setError(undefined);
    const client = api ?? new AvailabilityApi();

    if (isAvailable) {
      setReasonDialogOpen(true);
      return;
    }

    setBusy(true);
    try {
      const updated = await client.markAvailable(productId);
      setIsAvailable(updated.isAvailable);
      setReason(updated.unavailableReason);
      onChanged?.(updated.isAvailable, updated.unavailableReason);
    } catch (cause) {
      setError(toMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  /** US-044 §10 — o toque no motivo JÁ confirma (nenhum passo extra), mantendo a marcação em "um toque" na prática. */
  async function confirmUnavailable(selectedReason: ProductUnavailableReason): Promise<void> {
    setError(undefined);
    setBusy(true);
    try {
      const updated = await (api ?? new AvailabilityApi()).markUnavailable(productId, selectedReason);
      setIsAvailable(updated.isAvailable);
      setReason(updated.unavailableReason);
      setReasonDialogOpen(false);
      onChanged?.(updated.isAvailable, updated.unavailableReason);
    } catch (cause) {
      setError(toMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="kds-availability-toggle" data-product-id={productId}>
      <div className="kds-availability-toggle__info" ref={infoRef}>
        <span className="kds-availability-toggle__name">{productName}</span>
        {!isAvailable ? (
          <StatusPill status="UNAVAILABLE" label={reason ? `Em falta — ${reason}` : 'Em falta'} />
        ) : null}
      </div>
      <Button
        type="button"
        variant={isAvailable ? 'danger' : 'accent'}
        size="touch"
        busy={busy}
        onClick={() => void handleToggle()}
        aria-pressed={!isAvailable}
      >
        {isAvailable ? 'Marcar indisponível' : 'Marcar disponível'}
      </Button>
      {error ? (
        <p className="kds-availability-toggle__error nx-anim-toast-in" role="alert">
          {error}
        </p>
      ) : null}
      {reasonDialogOpen ? (
        <div
          className="kds-availability-dialog"
          role="dialog"
          aria-modal="true"
          aria-labelledby={dialogTitleId}
          onKeyDown={(event) => {
            // US-044 §10 — "motivo escolhido por número (1 acabou, 2 equipamento, 3 qualidade),
            // não por texto": aceita a tecla física, sem exigir toque na grade.
            const index = ['1', '2', '3'].indexOf(event.key);
            if (index === -1 || busy) return;
            const selected = PRODUCT_UNAVAILABLE_REASONS[index];
            if (selected) void confirmUnavailable(selected);
          }}
        >
          <Card className="kds-availability-dialog__card nx-anim-scale-in">
            <h2 id={dialogTitleId}>Marcar produto indisponível</h2>
            <p>Escolha o motivo — a equipe toda vê na hora.</p>
            <div className="kds-availability-dialog__reasons">
              {PRODUCT_UNAVAILABLE_REASONS.map((reasonCode, index) => (
                <Button
                  key={reasonCode}
                  type="button"
                  variant="danger"
                  size="touch"
                  busy={busy}
                  onClick={() => void confirmUnavailable(reasonCode)}
                >
                  <span className="kds-availability-dialog__reason-key">{index + 1}</span>
                  {PRODUCT_UNAVAILABLE_REASON_LABELS[reasonCode]}
                </Button>
              ))}
            </div>
            <div className="kds-availability-dialog__actions">
              <Button
                type="button"
                variant="ghost"
                disabled={busy}
                onClick={() => setReasonDialogOpen(false)}
              >
                Cancelar
              </Button>
            </div>
          </Card>
        </div>
      ) : null}
    </div>
  );
}

function toMessage(cause: unknown): string {
  return cause instanceof Error ? cause.message : 'Não foi possível atualizar a disponibilidade.';
}
