import { useCallback, useEffect, useMemo, useState } from 'react';
import type { BillSplitMode, BillSplitPartDto } from '@nexora/contracts';
import { Button, Icon } from '@nexora/ui';
import { ConsumptionApi } from './consumption-api.js';
import { WaiterCallApi, WaiterCallApiError } from './waiter-call-api.js';
import './waiter-call-panel.css';

export interface WaiterCallPanelProps {
  readonly qrToken: string;
  readonly sessionToken: string;
  readonly baseUrl?: string;
  readonly fetcher?: typeof fetch;
}

interface Feedback {
  readonly kind: 'success' | 'info' | 'error';
  readonly message: string;
}

const SPLIT_MODE_OPTIONS: ReadonlyArray<{ readonly value: BillSplitMode; readonly label: string }> = [
  { value: 'SINGLE', label: 'Valor único' },
  { value: 'BY_PERSON', label: 'Dividir por pessoa' },
  { value: 'BY_ITEM', label: 'Cada um paga o que pediu' },
];

/**
 * Botão fixo de "chamar garçom" (US-025 §10: "sempre acessível, fixo, sem precisar navegar") e a
 * tela de "pedir a conta" (US-026 §10) — vive fora das abas Cardápio/Consumo em
 * `table-access-page.tsx`, visível independente de qual aba o cliente está vendo.
 */
export function WaiterCallPanel({ qrToken, sessionToken, baseUrl = '', fetcher = fetch }: Readonly<WaiterCallPanelProps>) {
  const api = useMemo(() => new WaiterCallApi(qrToken, sessionToken, baseUrl, fetcher), [qrToken, sessionToken, baseUrl, fetcher]);
  const consumptionApi = useMemo(() => new ConsumptionApi(sessionToken, baseUrl, fetcher), [sessionToken, baseUrl, fetcher]);

  const [callingWaiter, setCallingWaiter] = useState(false);
  const [callFeedback, setCallFeedback] = useState<Feedback | null>(null);

  const [billPanelOpen, setBillPanelOpen] = useState(false);
  const [splitMode, setSplitMode] = useState<BillSplitMode>('SINGLE');
  const [people, setPeople] = useState(2);
  const [requestingBill, setRequestingBill] = useState(false);
  const [billFeedback, setBillFeedback] = useState<Feedback | null>(null);
  const [billRequested, setBillRequested] = useState(false);

  // US-027 §10 — prévia REAL da divisão (calculada pelo backend, com taxa de serviço e
  // arredondamento corretos), não uma conta de padaria feita no cliente (subtotal / N).
  const [previewParts, setPreviewParts] = useState<readonly BillSplitPartDto[] | null>(null);

  const handleCallWaiter = useCallback(async () => {
    setCallingWaiter(true);
    setCallFeedback(null);
    try {
      const result = await api.callWaiter();
      setCallFeedback(
        result.alreadyPending
          ? { kind: 'info', message: 'O garçom já foi avisado — só um instante.' }
          : { kind: 'success', message: 'Garçom chamado! Ele já está a caminho.' },
      );
    } catch (cause) {
      setCallFeedback({
        kind: 'error',
        message: cause instanceof WaiterCallApiError ? cause.message : 'Não foi possível chamar o garçom agora.',
      });
    } finally {
      setCallingWaiter(false);
    }
  }, [api]);

  const openBillPanel = useCallback(() => {
    setBillPanelOpen(true);
    setBillFeedback(null);
  }, []);

  // Recalcula a prévia real (US-027) sempre que o modo/contagem de pessoas muda, enquanto o
  // painel está aberto — mesmo cálculo que o caixa vai usar de verdade no fechamento.
  useEffect(() => {
    if (!billPanelOpen || splitMode !== 'BY_PERSON') {
      setPreviewParts(null);
      return;
    }
    let active = true;
    void consumptionApi
      .getBillPreview('BY_PERSON', people)
      .then((bill) => {
        if (active) setPreviewParts(bill.split);
      })
      .catch(() => {
        if (active) setPreviewParts(null);
      });
    return () => {
      active = false;
    };
  }, [billPanelOpen, splitMode, people, consumptionApi]);

  const handleRequestBill = useCallback(async () => {
    setRequestingBill(true);
    setBillFeedback(null);
    try {
      const result = await api.requestBill(splitMode, splitMode === 'BY_PERSON' ? people : undefined);
      setBillRequested(true);
      setBillFeedback({
        kind: 'success',
        message: result.alreadyRequested ? 'Preferência de divisão atualizada.' : 'Conta solicitada! O caixa já foi avisado.',
      });
    } catch (cause) {
      setBillFeedback({
        kind: 'error',
        message: cause instanceof WaiterCallApiError ? cause.message : 'Não foi possível pedir a conta agora.',
      });
    } finally {
      setRequestingBill(false);
    }
  }, [api, splitMode, people]);

  // Todas as partes têm o mesmo valor exceto, no máximo, um centavo de diferença (ADR-017: a
  // sobra do arredondamento vai para a primeira parcela) — mostrar a lista completa, não uma
  // média, é o que evita a pessoa 1 achar que pagou "errado" quando comparar com a pessoa 2.
  const previewLabel = previewParts
    ? formatPreviewLabel(previewParts)
    : null;

  return (
    <>
      <div className="waiter-call-bar" role="toolbar" aria-label="Ações da mesa">
        <button
          type="button"
          className="waiter-call-bar__button"
          onClick={() => void handleCallWaiter()}
          disabled={callingWaiter}
        >
          <Icon name="room_service" size={20} />
          Chamar garçom
        </button>
        <button type="button" className="waiter-call-bar__button waiter-call-bar__button--secondary" onClick={openBillPanel}>
          <Icon name="receipt_long" size={20} />
          Pedir a conta
        </button>
      </div>

      {callFeedback ? (
        <output className={`waiter-call-feedback waiter-call-feedback--${callFeedback.kind}`} role="status">
          {callFeedback.message}
        </output>
      ) : null}

      {billPanelOpen ? (
        <div className="bill-request-panel" role="dialog" aria-label="Pedir a conta">
          <header className="bill-request-panel__head">
            <h2>Como vamos dividir?</h2>
            <button
              type="button"
              className="bill-request-panel__close"
              onClick={() => setBillPanelOpen(false)}
              aria-label="Fechar"
            >
              <Icon name="close" size={20} />
            </button>
          </header>

          <div className="bill-request-panel__options">
            {SPLIT_MODE_OPTIONS.map((option) => (
              <label key={option.value} className="bill-request-panel__option">
                <input
                  type="radio"
                  name="splitMode"
                  value={option.value}
                  checked={splitMode === option.value}
                  onChange={() => setSplitMode(option.value)}
                />
                {option.label}
              </label>
            ))}
          </div>

          {splitMode === 'BY_PERSON' ? (
            <div className="bill-request-panel__people">
              <label htmlFor="bill-request-people">Quantas pessoas?</label>
              <input
                id="bill-request-people"
                type="number"
                min={1}
                value={people}
                onChange={(event) => setPeople(Math.max(1, Number.parseInt(event.target.value, 10) || 1))}
              />
              {previewLabel ? (
                <p key={previewLabel} className="bill-request-panel__preview nx-anim-flash">
                  {previewLabel}
                </p>
              ) : null}
            </div>
          ) : null}

          {billFeedback ? (
            <output className={`waiter-call-feedback waiter-call-feedback--${billFeedback.kind}`} role="status">
              {billFeedback.message}
            </output>
          ) : null}

          <Button onClick={() => void handleRequestBill()} disabled={requestingBill || billRequested} block>
            {billRequested ? 'Conta solicitada' : 'Confirmar'}
          </Button>
        </div>
      ) : null}
    </>
  );
}

/**
 * "R$ 33,34 (1ª pessoa) e R$ 33,33 para as demais" quando o resíduo de arredondamento (ADR-017)
 * gera um valor diferente para a primeira parte; "R$ 27,50 por pessoa" quando todas batem.
 */
function formatPreviewLabel(parts: readonly BillSplitPartDto[]): string | null {
  if (parts.length === 0) return null;
  const toBrl = (amount: string) => `R$ ${amount.replace('.', ',')}`;
  const distinctAmounts = new Set(parts.map((part) => part.amount));

  if (distinctAmounts.size === 1) {
    return `${toBrl(parts[0]!.amount)} por pessoa`;
  }

  const [first, ...rest] = parts;
  const restAmounts = new Set(rest.map((part) => part.amount));
  if (restAmounts.size === 1 && first) {
    return `${toBrl(first.amount)} para a 1ª pessoa e ${toBrl(rest[0]!.amount)} para as demais`;
  }

  return parts.map((part) => toBrl(part.amount)).join(' + ');
}
