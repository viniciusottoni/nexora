import { useCallback, useEffect, useMemo, useState } from 'react';
import type { BillResponse } from '@nexora/contracts';
import { Badge, Button, Card, EmptyState, QuantityStepper, SegmentedControl } from '@nexora/ui';
import type { OperationalRequestIdentity } from '@nexora/ui';
import { formatMoneyBrl } from '../table-map/table-map-signals.js';
import { BillingApi, BillingApiError } from './billing-api.js';
import './billing.css';

export interface BillingPageProps {
  readonly identity: OperationalRequestIdentity;
  readonly sessionId: string;
  readonly baseUrl?: string;
  readonly fetcher?: typeof fetch;
  /** Chamado ao voltar para o mapa de mesas. */
  readonly onExit?: () => void;
}

type SplitMode = 'BY_PERSON' | 'BY_ITEM' | 'BY_AMOUNT';

const MODE_OPTIONS = [
  { value: 'BY_PERSON', label: 'Por pessoa' },
  { value: 'BY_ITEM', label: 'Por item' },
  { value: 'BY_AMOUNT', label: 'Por valor' },
] as const;

const PAYMENT_METHODS = [
  { value: 'CASH', label: 'Dinheiro' },
  { value: 'CREDIT', label: 'Crédito' },
  { value: 'DEBIT', label: 'Débito' },
  { value: 'PIX', label: 'Pix' },
] as const;

/**
 * Tela de fechamento/divisão da conta (US-027) — acessada a partir do card de mesa com
 * `billRequested=true` no mapa (US-023). Padrão "por pessoa" (US-027 §10: "por ser o caso mais
 * comum"), com os outros dois modos a um toque via `SegmentedControl`.
 */
export function BillingPage({
  identity,
  sessionId,
  baseUrl = '',
  fetcher = fetch,
  onExit,
}: Readonly<BillingPageProps>) {
  const api = useMemo(() => new BillingApi(baseUrl, fetcher), [baseUrl, fetcher]);

  const [mode, setMode] = useState<SplitMode>('BY_PERSON');
  const [people, setPeople] = useState(2);
  const [bill, setBill] = useState<BillResponse>();
  const [loadError, setLoadError] = useState<string>();
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState<string>();

  // BY_ITEM — atribuição não é persistida no servidor (ver docstring de AssignBillItemsCommand);
  // o estado vive só aqui, no cliente, e é reenviado inteiro a cada chamada de assign-items.
  const [assignments, setAssignments] = useState<ReadonlyMap<string, number>>(new Map());

  // Retirada de taxa (US-027 §4) — conjunto acumulado, reenviado no próximo GET para o servidor
  // manter a mesma prévia consistente (o cálculo em si não é persistido).
  const [waivedPersons, setWaivedPersons] = useState<readonly number[]>([]);

  // BY_AMOUNT
  const [amountInput, setAmountInput] = useState('');
  const [paymentMethod, setPaymentMethod] = useState<(typeof PAYMENT_METHODS)[number]['value']>('CASH');
  const [paymentFeedback, setPaymentFeedback] = useState<{ kind: 'success' | 'error'; message: string }>();

  // US-035 (Bloquear fechamento com item pendente) — só usado no modo BLOCK: o caixa autoriza o
  // fechamento mesmo com pendência sem trocar de sessão (ADR-023, elevação pontual). O token fica
  // válido só para ESTA sessão (bill.hasPendingItems some quando o item é cancelado/servido — o
  // próximo `load()` já reflete isso, sem precisar de estado adicional aqui).
  const [pinInput, setPinInput] = useState('');
  const [authorizeReason, setAuthorizeReason] = useState('');
  const [authorization, setAuthorization] = useState<{ token: string; reason?: string | undefined }>();
  const [authorizeBusy, setAuthorizeBusy] = useState(false);
  const [authorizeError, setAuthorizeError] = useState<string>();

  const load = useCallback(
    async (overrides: { mode?: SplitMode; people?: number; waived?: readonly number[] } = {}) => {
      setLoadError(undefined);
      const effectiveMode = overrides.mode ?? mode;
      try {
        const response = await api.getBill(identity, sessionId, {
          split: effectiveMode,
          ...(effectiveMode === 'BY_PERSON' ? { people: overrides.people ?? people } : {}),
          ...(effectiveMode === 'BY_PERSON' ? { waived: overrides.waived ?? waivedPersons } : {}),
        });
        setBill(response);
      } catch (cause) {
        setLoadError(cause instanceof Error ? cause.message : 'Não foi possível carregar a divisão da conta.');
      }
    },
    [api, identity, sessionId, mode, people, waivedPersons],
  );

  // Recarrega só quando o MODO muda (troca de aba) — `load` já depende de `people`/`waivedPersons`
  // e é chamada explicitamente com overrides por `changePeople`/`toggleWaive` nesses casos, para
  // não disparar duas requisições concorrentes com valores diferentes (este projeto não tem o
  // plugin `react-hooks` configurado, então nenhum lint bloqueia a lista de deps restrita aqui).
  useEffect(() => {
    void load();
  }, [mode]);

  function changeMode(value: string) {
    setMode(value as SplitMode);
    setWaivedPersons([]);
    setAssignments(new Map());
    setPaymentFeedback(undefined);
    setActionError(undefined);
  }

  async function changePeople(next: number) {
    setPeople(next);
    await load({ people: next });
  }

  async function toggleWaive(person: number) {
    if (waivedPersons.includes(person)) return; // retirada não é reversível nesta tela — é um fato registrado (RN-010)
    setBusy(true);
    setActionError(undefined);
    try {
      const response = await api.waiveServiceFee(identity, sessionId, {
        people,
        person,
        alreadyWaivedPersons: [...waivedPersons],
      });
      setBill(response);
      setWaivedPersons([...waivedPersons, person]);
    } catch (cause) {
      setActionError(cause instanceof Error ? cause.message : 'Não foi possível registrar a retirada da taxa.');
    } finally {
      setBusy(false);
    }
  }

  function assignItem(itemId: string, person: number) {
    setAssignments((prev) => {
      const next = new Map(prev);
      next.set(itemId, person);
      return next;
    });
  }

  async function calculateByItem() {
    if (!bill) return;
    const byPerson = new Map<number, string[]>();
    for (const [itemId, person] of assignments) {
      const list = byPerson.get(person) ?? [];
      list.push(itemId);
      byPerson.set(person, list);
    }
    setBusy(true);
    setActionError(undefined);
    try {
      const response = await api.assignItems(identity, sessionId, {
        assignments: [...byPerson.entries()].map(([person, itemIds]) => ({ person, itemIds })),
      });
      setBill(response);
    } catch (cause) {
      setActionError(
        cause instanceof BillingApiError && cause.code === 'BILL_ITEM_NOT_ASSIGNED'
          ? 'Ainda há itens sem pessoa atribuída — atribua todos antes de calcular.'
          : cause instanceof Error
            ? cause.message
            : 'Não foi possível calcular a divisão por item.',
      );
    } finally {
      setBusy(false);
    }
  }

  async function registerPayment() {
    const amount = Number.parseFloat(amountInput.replace(',', '.'));
    if (!Number.isFinite(amount) || amount <= 0) {
      setPaymentFeedback({ kind: 'error', message: 'Informe um valor maior que zero.' });
      return;
    }
    setBusy(true);
    setPaymentFeedback(undefined);
    try {
      const response = await api.registerPartialPayment(
        identity,
        sessionId,
        { amount, method: paymentMethod, reason: authorization?.reason },
        authorization?.token,
      );
      setPaymentFeedback({
        kind: 'success',
        message: `Pagamento de ${formatMoneyBrl(response.amountPaid)} registrado. Restam ${formatMoneyBrl(response.remainingAmount)} em aberto.`,
      });
      setAmountInput('');
      setAuthorization(undefined);
      await load();
    } catch (cause) {
      setPaymentFeedback({
        kind: 'error',
        message:
          cause instanceof BillingApiError && cause.code === 'PENDING_ITEMS'
            ? 'Há itens que ainda não foram entregues — cancele-os ou autorize o fechamento acima.'
            : cause instanceof Error
              ? cause.message
              : 'Não foi possível registrar o pagamento.',
      });
    } finally {
      setBusy(false);
    }
  }

  /**
   * US-035 §10 — autoriza o fechamento com item pendente (modo BLOCK) sem trocar de sessão: o
   * gerente informa o próprio PIN no mesmo terminal. Em sucesso, o token fica guardado para a
   * próxima chamada de `registerPayment` (o botão "Registrar pagamento" é reabilitado).
   */
  async function authorizeCloseWithPending() {
    if (!pinInput) {
      setAuthorizeError('Informe o PIN do gerente.');
      return;
    }
    setAuthorizeBusy(true);
    setAuthorizeError(undefined);
    try {
      const grant = await api.authorizeCloseWithPending(identity, { sessionId, pin: pinInput, reason: authorizeReason || undefined });
      setAuthorization({ token: grant.authorizationToken, reason: authorizeReason || undefined });
      setPinInput('');
    } catch (cause) {
      setAuthorizeError(cause instanceof Error ? cause.message : 'Não foi possível autorizar o fechamento.');
    } finally {
      setAuthorizeBusy(false);
    }
  }

  if (loadError && !bill) {
    return <EmptyState icon="wifi_off" title="Não foi possível carregar a divisão da conta">{loadError}</EmptyState>;
  }

  if (!bill) {
    return (
      <p role="status" className="billing-loading">
        Carregando a conta…
      </p>
    );
  }

  // US-035 (Bloquear fechamento com item pendente) — `pendingItemsMode` ausente na resposta
  // (fixture antiga/tenant sem configuração) cai no mesmo default seguro do backend (WARN).
  const pendingItemsMode = bill.pendingItemsMode ?? 'WARN';
  const blockedByPendingItems = bill.hasPendingItems && pendingItemsMode === 'BLOCK' && !authorization;

  return (
    <main className="billing-page">
      <header className="billing-page__header">
        <div>
          <p className="billing-eyebrow">Fechamento da comanda</p>
          <h1>Dividir a conta</h1>
        </div>
        {onExit ? (
          <Button type="button" variant="ghost" onClick={onExit}>
            Voltar ao mapa
          </Button>
        ) : null}
      </header>

      <SegmentedControl options={MODE_OPTIONS} value={mode} onChange={changeMode} size="lg" block />

      {bill.hasPendingItems && pendingItemsMode === 'WARN' ? (
        <p role="alert" className="billing-pending-warning">
          Há itens ainda em produção — o caixa deve confirmar antes de concluir o recebimento (RN-017).
        </p>
      ) : null}

      {blockedByPendingItems ? (
        <Card as="section" className="billing-pending-block" aria-label="Fechamento bloqueado por item pendente">
          <p role="alert" className="billing-pending-warning">
            Fechamento bloqueado — há itens que ainda não foram entregues (RN-017).
          </p>
          <ul className="billing-pending-items-list">
            {bill.pendingItems.map((item) => (
              <li key={item.id}>
                {item.name} — <Badge tone="warning" size="sm">{item.status}</Badge>
              </li>
            ))}
          </ul>
          <p className="billing-pending-block__hint">
            Cancele os itens pendentes na comanda ou autorize o fechamento mesmo assim.
          </p>
          <div className="billing-pending-authorize">
            <label htmlFor="billing-authorize-pin">PIN do gerente</label>
            <input
              id="billing-authorize-pin"
              type="password"
              inputMode="numeric"
              value={pinInput}
              onChange={(event) => setPinInput(event.target.value)}
            />
            <label htmlFor="billing-authorize-reason">Motivo</label>
            <input
              id="billing-authorize-reason"
              type="text"
              value={authorizeReason}
              onChange={(event) => setAuthorizeReason(event.target.value)}
            />
            <Button type="button" disabled={authorizeBusy} onClick={() => void authorizeCloseWithPending()}>
              Autorizar fechamento
            </Button>
          </div>
          {authorizeError ? (
            <p role="alert" className="billing-error">
              {authorizeError}
            </p>
          ) : null}
        </Card>
      ) : null}

      {actionError ? (
        <p role="alert" className="billing-error">
          {actionError}
        </p>
      ) : null}

      <Card as="section" className="billing-summary">
        <dl>
          <div>
            <dt>Subtotal</dt>
            <dd>{formatMoneyBrl(bill.subtotal)}</dd>
          </div>
          <div>
            <dt>Taxa de serviço</dt>
            <dd>{formatMoneyBrl(bill.serviceFee)}</dd>
          </div>
          <div className="billing-summary__total">
            <dt>Total</dt>
            <dd>{formatMoneyBrl(bill.total)}</dd>
          </div>
        </dl>
      </Card>

      {mode === 'BY_PERSON' ? (
        <Card as="section" className="billing-mode-panel">
          <h2>Quantas pessoas dividem a conta?</h2>
          <QuantityStepper value={people} min={1} max={50} onChange={(value) => void changePeople(value)} />
          <ul className="billing-split-list">
            {bill.split.map((part) => (
              <li key={part.person} className="billing-split-item">
                <span className="billing-split-item__person">Pessoa {part.person}</span>
                <span className="billing-split-item__amount">{formatMoneyBrl(part.amount)}</span>
                <span className="billing-split-item__fee">
                  {part.serviceFeeWaived ? (
                    <Badge tone="neutral" size="sm">
                      Sem taxa
                    </Badge>
                  ) : (
                    <>
                      Taxa: {formatMoneyBrl(part.serviceFeeAmount)}
                      <Button
                        type="button"
                        size="sm"
                        variant="ghost"
                        disabled={busy}
                        onClick={() => void toggleWaive(part.person)}
                      >
                        Retirar taxa
                      </Button>
                    </>
                  )}
                </span>
              </li>
            ))}
          </ul>
        </Card>
      ) : null}

      {mode === 'BY_ITEM' ? (
        <Card as="section" className="billing-mode-panel">
          <h2>Quantas pessoas dividem a conta?</h2>
          <QuantityStepper value={people} min={1} max={50} onChange={setPeople} />
          <ul className="billing-item-list">
            {bill.items.map((item) => (
              <li key={item.id} className="billing-item-row">
                <span className="billing-item-row__name">
                  {item.name}
                  {item.pending ? (
                    <Badge tone="warning" size="sm">
                      Em produção
                    </Badge>
                  ) : null}
                </span>
                <span className="billing-item-row__amount">{formatMoneyBrl(item.total)}</span>
                <span className="billing-item-row__assign">
                  {Array.from({ length: people }, (_, index) => index + 1).map((person) => (
                    <button
                      key={person}
                      type="button"
                      aria-pressed={assignments.get(item.id) === person}
                      onClick={() => assignItem(item.id, person)}
                    >
                      {person}
                    </button>
                  ))}
                </span>
              </li>
            ))}
          </ul>
          {bill.items.length > assignments.size ? (
            <p className="billing-item-list__hint">
              {bill.items.length - assignments.size} item(ns) ainda sem pessoa atribuída.
            </p>
          ) : null}
          <Button type="button" onClick={() => void calculateByItem()} disabled={busy || bill.items.length === 0}>
            Calcular divisão
          </Button>

          {bill.split.length > 0 ? (
            <ul className="billing-split-list">
              {bill.split.map((part) => (
                <li key={part.person} className="billing-split-item">
                  <span className="billing-split-item__person">Pessoa {part.person}</span>
                  <span className="billing-split-item__amount">{formatMoneyBrl(part.amount)}</span>
                </li>
              ))}
            </ul>
          ) : null}
        </Card>
      ) : null}

      {mode === 'BY_AMOUNT' ? (
        <Card as="section" className="billing-mode-panel">
          <h2>Pagamento por valor</h2>
          <p>Saldo em aberto: {formatMoneyBrl(bill.remainingAmount ?? bill.total)}</p>
          <label htmlFor="billing-amount-input">Valor pago agora</label>
          <input
            id="billing-amount-input"
            type="number"
            min="0.01"
            step="0.01"
            inputMode="decimal"
            value={amountInput}
            onChange={(event) => setAmountInput(event.target.value)}
          />
          <SegmentedControl
            options={PAYMENT_METHODS}
            value={paymentMethod}
            onChange={(value) => setPaymentMethod(value as typeof paymentMethod)}
          />
          <Button type="button" onClick={() => void registerPayment()} disabled={busy || blockedByPendingItems}>
            Registrar pagamento
          </Button>
          {blockedByPendingItems ? (
            <p className="billing-pending-block__hint">
              Cancele os itens pendentes ou autorize o fechamento acima antes de registrar o pagamento.
            </p>
          ) : null}
          {paymentFeedback ? (
            <p role={paymentFeedback.kind === 'error' ? 'alert' : 'status'} className={`billing-payment-feedback billing-payment-feedback--${paymentFeedback.kind}`}>
              {paymentFeedback.message}
            </p>
          ) : null}
        </Card>
      ) : null}
    </main>
  );
}
