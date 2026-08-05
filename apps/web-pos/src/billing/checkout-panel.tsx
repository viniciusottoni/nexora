import { useState } from 'react';
import type { BillResponse, PaymentRequest, RegisterPaymentsResponse } from '@nexora/contracts';
import { Badge, Button, Card } from '@nexora/ui';
import type { OperationalRequestIdentity } from '@nexora/ui';
import { formatMoneyBrl } from '../table-map/table-map-signals.js';
import { BillingApi, BillingApiError } from './billing-api.js';
import './checkout-panel.css';

export interface CheckoutPanelProps {
  readonly identity: OperationalRequestIdentity;
  readonly sessionId: string;
  readonly bill: BillResponse;
  readonly api: BillingApi;
  readonly onBillChanged?: (patch: Partial<Pick<BillResponse, 'discount' | 'serviceFee' | 'serviceFeeWaived' | 'total'>>) => void;
  /** Chamado depois de um fechamento bem-sucedido (US-052) — o chamador decide se navega/recarrega. */
  readonly onPaid?: (result: RegisterPaymentsResponse) => void;
}

const PAYMENT_METHODS = [
  { value: 'CASH', label: 'Dinheiro' },
  { value: 'CREDIT', label: 'Crédito' },
  { value: 'DEBIT', label: 'Débito' },
  { value: 'PIX', label: 'Pix' },
  { value: 'VOUCHER', label: 'Voucher' },
] as const;

const MACHINE_PROVIDERS = ['CIELO', 'MERCADO_PAGO', 'STONE', 'GETNET'] as const;

interface PaymentLine {
  readonly key: string;
  method: (typeof PAYMENT_METHODS)[number]['value'];
  amount: string;
  receivedAmount: string;
  provider: string;
  providerRef: string;
  brand: string;
  installments: string;
}

function newLine(): PaymentLine {
  return {
    key: crypto.randomUUID(),
    method: 'CASH',
    amount: '',
    receivedAmount: '',
    provider: '',
    providerRef: '',
    brand: '',
    installments: '',
  };
}

function parseMoneyCents(value: string): number {
  const normalized = value.trim().replace(',', '.');
  const match = /^(-)?(\d+)(?:\.(\d{0,2}))?$/.exec(normalized);
  if (!match) return 0;
  const sign = match[1];
  const reais = match[2] ?? '0';
  const cents = match[3] ?? '';
  const absolute = Number.parseInt(reais, 10) * 100 + Number.parseInt(cents.padEnd(2, '0'), 10);
  return sign ? -absolute : absolute;
}

function centsToDecimal(cents: number): number {
  return cents / 100;
}

function moneyCents(cents: number): string {
  return formatMoneyBrl((cents / 100).toFixed(2));
}

function usesExternalProvider(method: PaymentLine['method']): boolean {
  return method === 'CREDIT' || method === 'DEBIT' || method === 'PIX';
}

/**
 * US-052 (Múltiplas formas de pagamento na mesma conta) + US-058 (Pagamento de maquininha
 * externa) + US-054 (Desconto com autorização) + US-053 (Taxa de serviço com retirada
 * registrada) — painel de fechamento completo da conta, separado de `BillingPage` (US-027) para
 * manter os dois arquivos pequenos: este cuida do FECHAMENTO (fecha a comanda por completo),
 * aquele cuida da DIVISÃO (US-027, sessão continua aberta).
 */
export function CheckoutPanel({ identity, sessionId, bill, api, onBillChanged, onPaid }: Readonly<CheckoutPanelProps>) {
  const [lines, setLines] = useState<readonly PaymentLine[]>([newLine()]);
  const [registering, setRegistering] = useState(false);
  const [registerError, setRegisterError] = useState<string>();
  const [duplicateConfirmationRequired, setDuplicateConfirmationRequired] = useState(false);
  const [registerResult, setRegisterResult] = useState<RegisterPaymentsResponse>();

  const [discountPercent, setDiscountPercent] = useState('');
  const [discountReason, setDiscountReason] = useState('');
  const [discountBusy, setDiscountBusy] = useState(false);
  const [discountError, setDiscountError] = useState<string>();
  const [discountSuccess, setDiscountSuccess] = useState<string>();
  const [discountAuthRequired, setDiscountAuthRequired] = useState(false);
  const [discountPin, setDiscountPin] = useState('');
  const [discountAuthBusy, setDiscountAuthBusy] = useState(false);
  const [discountAuthError, setDiscountAuthError] = useState<string>();

  const [waiveBusy, setWaiveBusy] = useState(false);
  const [waiveError, setWaiveError] = useState<string>();
  const [waiveReason, setWaiveReason] = useState('');

  const totalCents = parseMoneyCents(bill.total);
  const enteredTotalCents = lines.reduce((sum, line) => sum + parseMoneyCents(line.amount), 0);
  const remainingCents = totalCents - enteredTotalCents;

  function updateLine(key: string, patch: Partial<PaymentLine>) {
    setLines((prev) => prev.map((line) => (line.key === key ? { ...line, ...patch } : line)));
    setDuplicateConfirmationRequired(false);
  }

  function addLine() {
    setLines((prev) => [...prev, newLine()]);
  }

  function removeLine(key: string) {
    setLines((prev) => (prev.length > 1 ? prev.filter((line) => line.key !== key) : prev));
  }

  async function submitPayments() {
    setRegistering(true);
    setRegisterError(undefined);
    try {
      const payments: PaymentRequest[] = lines.map((line) => ({
        method: line.method,
        amount: centsToDecimal(parseMoneyCents(line.amount)),
        receivedAmount: line.method === 'CASH' && line.receivedAmount ? centsToDecimal(parseMoneyCents(line.receivedAmount)) : null,
        provider: line.provider || null,
        providerRef: line.providerRef || null,
        brand: line.brand || null,
        installments: line.installments ? Number.parseInt(line.installments, 10) : undefined,
        confirmDuplicate: duplicateConfirmationRequired || undefined,
      }));
      const result = await api.registerPayments(identity, sessionId, { payments });
      setRegisterResult(result);
      setDuplicateConfirmationRequired(false);
      onPaid?.(result);
    } catch (cause) {
      if (cause instanceof BillingApiError && cause.code === 'PAYMENT_SUM_MISMATCH') {
        setRegisterError(`A soma informada não bate com o total — diferença de ${moneyCents(Math.abs(remainingCents))}.`);
      } else if (cause instanceof BillingApiError && cause.code === 'PAYMENT_DUPLICATE_REFERENCE') {
        setDuplicateConfirmationRequired(true);
        setRegisterError('Referência já usada neste turno — confira o comprovante e confirme explicitamente se for outro pagamento.');
      } else {
        setRegisterError(cause instanceof Error ? cause.message : 'Não foi possível registrar os pagamentos.');
      }
    } finally {
      setRegistering(false);
    }
  }

  async function submitDiscount(authorizationToken?: string) {
    const percent = Number.parseFloat(discountPercent.replace(',', '.'));
    if (!Number.isFinite(percent) || percent <= 0) {
      setDiscountError('Informe um percentual de desconto maior que zero.');
      return;
    }
    if (!discountReason.trim()) {
      setDiscountError('Informe o motivo do desconto.');
      return;
    }

    setDiscountBusy(true);
    setDiscountError(undefined);
    try {
      const result = await api.applyDiscount(
        identity,
        sessionId,
        { percent, amount: null, reason: discountReason, scope: 'SESSION' },
        authorizationToken,
      );
      setDiscountSuccess(`Desconto de ${formatMoneyBrl(result.session.discount)} aplicado.`);
      onBillChanged?.({ discount: result.session.discount, total: result.session.total });
      setDiscountAuthRequired(false);
      setDiscountPin('');
    } catch (cause) {
      if (cause instanceof BillingApiError && cause.code === 'AUTHORIZATION_REQUIRED') {
        setDiscountAuthRequired(true);
        setDiscountError('Desconto acima do limite — autorização de um gerente é necessária.');
      } else {
        setDiscountError(cause instanceof Error ? cause.message : 'Não foi possível aplicar o desconto.');
      }
    } finally {
      setDiscountBusy(false);
    }
  }

  async function authorizeAndApplyDiscount() {
    if (!discountPin) {
      setDiscountAuthError('Informe o PIN do gerente.');
      return;
    }
    setDiscountAuthBusy(true);
    setDiscountAuthError(undefined);
    try {
      const grant = await api.authorizeDiscount(identity, { sessionId, pin: discountPin, reason: discountReason });
      await submitDiscount(grant.authorizationToken);
    } catch (cause) {
      setDiscountAuthError(cause instanceof Error ? cause.message : 'Não foi possível autorizar o desconto.');
    } finally {
      setDiscountAuthBusy(false);
    }
  }

  async function waiveFullServiceFee() {
    setWaiveBusy(true);
    setWaiveError(undefined);
    try {
      const result = await api.waiveSessionServiceFee(identity, sessionId, { reason: waiveReason || 'Cliente não concordou com a taxa', scope: 'FULL' });
      onBillChanged?.({ serviceFee: result.session.serviceFee, serviceFeeWaived: true, total: result.session.total });
      setWaiveReason('');
    } catch (cause) {
      setWaiveError(cause instanceof Error ? cause.message : 'Não foi possível retirar a taxa de serviço.');
    } finally {
      setWaiveBusy(false);
    }
  }

  if (registerResult) {
    return (
      <Card as="section" className="checkout-panel checkout-panel--done" aria-label="Conta fechada">
        <h2>Conta fechada</h2>
        <p role="status">Sessão: {registerResult.session.status}</p>
        <p className="checkout-panel__receipt-note">Comprovante não fiscal gerado. Este documento não substitui NFC-e/SAT.</p>
        {parseMoneyCents(registerResult.change) > 0 ? (
          <p className="checkout-panel__change">Troco: {formatMoneyBrl(registerResult.change)}</p>
        ) : null}
        <ul className="checkout-panel__payments-list">
          {registerResult.payments.map((payment) => (
            <li key={payment.id}>
              {payment.method} — {formatMoneyBrl(payment.amount)}
              {payment.provider ? ` · ${payment.provider}` : ''}
              {payment.providerRef ? ` · ref. ${payment.providerRef}` : ''}
              {parseMoneyCents(payment.feeAmount) > 0 ? ` · líquido ${formatMoneyBrl(payment.netAmount)} · taxa ${formatMoneyBrl(payment.feeAmount)}` : ''}
              {payment.reconciliationStatus === 'PENDING' ? ' · conciliação pendente' : ''}
            </li>
          ))}
        </ul>
        <a href={registerResult.receipt.url} target="_blank" rel="noreferrer">
          Ver comprovante não fiscal
        </a>
      </Card>
    );
  }

  return (
    <Card as="section" className="checkout-panel" aria-label="Fechar conta">
      <h2>Fechar conta</h2>

      {bill.serviceFeeOptional && !bill.serviceFeeWaived && parseMoneyCents(bill.serviceFee ?? '0') > 0 ? (
        <div className="checkout-panel__service-fee">
          <label htmlFor="checkout-waive-reason">Motivo da retirada da taxa (opcional)</label>
          <input id="checkout-waive-reason" type="text" value={waiveReason} onChange={(event) => setWaiveReason(event.target.value)} />
          <Button type="button" variant="ghost" disabled={waiveBusy} onClick={() => void waiveFullServiceFee()}>
            Retirar taxa de serviço (conta toda)
          </Button>
          {waiveError ? <p role="alert">{waiveError}</p> : null}
        </div>
      ) : null}

      <div className="checkout-panel__discount">
        <h3>Desconto</h3>
        <label htmlFor="checkout-discount-percent">Percentual</label>
        <input
          id="checkout-discount-percent"
          type="number"
          min="0"
          max="100"
          step="0.01"
          value={discountPercent}
          onChange={(event) => setDiscountPercent(event.target.value)}
        />
        <label htmlFor="checkout-discount-reason">Motivo</label>
        <input id="checkout-discount-reason" type="text" value={discountReason} onChange={(event) => setDiscountReason(event.target.value)} />
        <Button type="button" variant="secondary" disabled={discountBusy} onClick={() => void submitDiscount()}>
          Aplicar desconto
        </Button>
        {discountError ? <p role="alert">{discountError}</p> : null}
        {discountSuccess ? <p role="status">{discountSuccess}</p> : null}

        {discountAuthRequired ? (
          <div className="checkout-panel__discount-authorize">
            <label htmlFor="checkout-discount-pin">PIN do gerente</label>
            <input
              id="checkout-discount-pin"
              type="password"
              inputMode="numeric"
              value={discountPin}
              onChange={(event) => setDiscountPin(event.target.value)}
            />
            <Button type="button" disabled={discountAuthBusy} onClick={() => void authorizeAndApplyDiscount()}>
              Autorizar e aplicar
            </Button>
            {discountAuthError ? <p role="alert">{discountAuthError}</p> : null}
          </div>
        ) : null}
      </div>

      <div className="checkout-panel__payments">
        <h3>Pagamentos</h3>
        <p>Total da conta: {formatMoneyBrl(bill.total)}</p>
        {lines.map((line) => (
          <div key={line.key} className="checkout-panel__payment-line">
            <select
              aria-label="Forma de pagamento"
              value={line.method}
              onChange={(event) => updateLine(line.key, { method: event.target.value as PaymentLine['method'] })}
            >
              {PAYMENT_METHODS.map((method) => (
                <option key={method.value} value={method.value}>
                  {method.label}
                </option>
              ))}
            </select>
            <input
              aria-label="Valor"
              type="number"
              min="0.01"
              step="0.01"
              inputMode="decimal"
              value={line.amount}
              onChange={(event) => updateLine(line.key, { amount: event.target.value })}
            />
            {line.method === 'CASH' ? (
              <input
                aria-label="Valor recebido em dinheiro"
                type="number"
                min="0"
                step="0.01"
                inputMode="decimal"
                placeholder="Valor recebido (troco)"
                value={line.receivedAmount}
                onChange={(event) => updateLine(line.key, { receivedAmount: event.target.value })}
              />
            ) : null}
            {usesExternalProvider(line.method) ? (
              <>
                <select
                  aria-label="Maquininha"
                  value={line.provider}
                  onChange={(event) => updateLine(line.key, { provider: event.target.value })}
                >
                  <option value="">Sem maquininha registrada</option>
                  {MACHINE_PROVIDERS.map((provider) => (
                    <option key={provider} value={provider}>
                      {provider}
                    </option>
                  ))}
                </select>
                <input
                  aria-label="NSU / referência da transação"
                  type="text"
                  inputMode="numeric"
                  placeholder="NSU"
                  value={line.providerRef}
                  onChange={(event) => updateLine(line.key, { providerRef: event.target.value })}
                />
                <input
                  aria-label="Bandeira"
                  type="text"
                  placeholder="Bandeira"
                  value={line.brand}
                  onChange={(event) => updateLine(line.key, { brand: event.target.value })}
                />
                <input
                  aria-label="Parcelas"
                  type="number"
                  min="1"
                  step="1"
                  inputMode="numeric"
                  placeholder="Parcelas"
                  value={line.installments}
                  onChange={(event) => updateLine(line.key, { installments: event.target.value })}
                />
              </>
            ) : null}
            {lines.length > 1 ? (
              <Button type="button" size="sm" variant="ghost" onClick={() => removeLine(line.key)}>
                Remover
              </Button>
            ) : null}
          </div>
        ))}
        <Button type="button" variant="ghost" onClick={addLine}>
          + Adicionar forma de pagamento
        </Button>

        <p className="checkout-panel__remaining">
          {remainingCents === 0 ? (
            <Badge tone="success">Soma confere com o total</Badge>
          ) : (
            <Badge tone={remainingCents > 0 ? 'warning' : 'danger'}>
              {remainingCents > 0 ? `Faltam ${moneyCents(remainingCents)}` : `Excede em ${moneyCents(Math.abs(remainingCents))}`}
            </Badge>
          )}
        </p>

        {registerError ? (
          <p role="alert" className="checkout-panel__error">
            {registerError}
          </p>
        ) : null}

        <Button type="button" disabled={registering || remainingCents !== 0} onClick={() => void submitPayments()}>
          {duplicateConfirmationRequired ? 'Confirmar referência duplicada e fechar' : 'Registrar pagamentos e fechar conta'}
        </Button>
      </div>
    </Card>
  );
}
