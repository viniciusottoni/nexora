import { useCallback, useEffect, useState } from 'react';
import type { CashMovementDto, CloseCashSessionResponse, GetCurrentCashSessionResponse, OpenTableSessionInfo } from '@nexora/contracts';
import { AlertBanner, Badge, Button, Card } from '@nexora/ui';
import type { OperationalRequestIdentity } from '@nexora/ui';
import { formatMoneyBrl } from '../table-map/table-map-signals.js';
import { CashSessionApi, CashSessionApiError } from './cash-session-api.js';
import './cash-session.css';

export interface CashSessionPageProps {
  readonly identity: OperationalRequestIdentity;
  readonly baseUrl?: string;
  readonly fetcher?: typeof fetch;
  /** Chamado ao voltar para o mapa de mesas. */
  readonly onExit?: () => void;
}

type MovementType = 'WITHDRAWAL' | 'SUPPLY';

const WITHDRAWAL_REASONS = ['Depósito bancário', 'Troca de turno', 'Segurança'];
const SUPPLY_REASONS = ['Reforço de troco', 'Início de turno'];
const JUSTIFICATION_REASONS = ['Erro de troco', 'Nota fiscal não emitida', 'Diferença não identificada'];

/**
 * Tela de caixa (US-055 abertura e fechamento; US-056 sangria e suprimento) — acessada a partir do
 * menu do POS. US-055 §10: "valor esperado com a composição detalhada"; US-056 §10: "dois botões
 * distintos e inequívocos: retirar e suprir" e "histórico do turno acessível na mesma tela".
 */
export function CashSessionPage({ identity, baseUrl = '', fetcher = fetch, onExit }: Readonly<CashSessionPageProps>) {
  const [api] = useState(() => new CashSessionApi(baseUrl, fetcher));

  const [current, setCurrent] = useState<GetCurrentCashSessionResponse>();
  const [movements, setMovements] = useState<readonly CashMovementDto[]>([]);
  const [noOpenSession, setNoOpenSession] = useState(false);
  const [loadError, setLoadError] = useState<string>();

  // Abertura (US-055 §4, cenário "Abertura com fundo").
  const [openingAmountInput, setOpeningAmountInput] = useState('');
  const [openBusy, setOpenBusy] = useState(false);
  const [openError, setOpenError] = useState<string>();

  // Sangria/suprimento (US-056).
  const [movementType, setMovementType] = useState<MovementType>();
  const [movementAmountInput, setMovementAmountInput] = useState('');
  const [movementReasonInput, setMovementReasonInput] = useState('');
  const [movementBusy, setMovementBusy] = useState(false);
  const [movementFeedback, setMovementFeedback] = useState<{ kind: 'success' | 'error'; message: string }>();
  const [movementRequiresAuth, setMovementRequiresAuth] = useState(false);
  const [movementPinInput, setMovementPinInput] = useState('');
  const [movementAuthorizeBusy, setMovementAuthorizeBusy] = useState(false);
  const [movementAuthorizeError, setMovementAuthorizeError] = useState<string>();
  const [movementAuthorizationToken, setMovementAuthorizationToken] = useState<string>();

  // Fechamento (US-055 §4).
  const [countedAmountInput, setCountedAmountInput] = useState('');
  const [justificationInput, setJustificationInput] = useState('');
  const [closeBusy, setCloseBusy] = useState(false);
  const [closeError, setCloseError] = useState<string>();
  const [closeRequiresJustification, setCloseRequiresJustification] = useState(false);
  const [openTables, setOpenTables] = useState<readonly OpenTableSessionInfo[]>();
  const [closePinInput, setClosePinInput] = useState('');
  const [closeAuthorizeBusy, setCloseAuthorizeBusy] = useState(false);
  const [closeAuthorizeError, setCloseAuthorizeError] = useState<string>();
  const [closeAuthorizationToken, setCloseAuthorizationToken] = useState<string>();
  const [closeResult, setCloseResult] = useState<CloseCashSessionResponse>();

  const load = useCallback(async () => {
    setLoadError(undefined);
    try {
      const response = await api.getCurrent(identity);
      setCurrent(response);
      setNoOpenSession(false);
      const history = await api.listMovements(identity);
      setMovements(history.movements);
    } catch (cause) {
      if (cause instanceof CashSessionApiError && cause.code === 'NO_OPEN_CASH_SESSION') {
        setNoOpenSession(true);
        setCurrent(undefined);
        setMovements([]);
        return;
      }
      setLoadError(cause instanceof Error ? cause.message : 'Não foi possível carregar o caixa.');
    }
  }, [api, identity]);

  useEffect(() => {
    void load();
  }, [load]);

  async function openSession() {
    const amount = Number.parseFloat(openingAmountInput.replace(',', '.'));
    if (!Number.isFinite(amount) || amount < 0) {
      setOpenError('Informe um valor de fundo válido.');
      return;
    }
    setOpenBusy(true);
    setOpenError(undefined);
    try {
      await api.open(identity, { openingAmount: amount });
      setOpeningAmountInput('');
      await load();
    } catch (cause) {
      setOpenError(
        cause instanceof CashSessionApiError && cause.code === 'CASH_SESSION_ALREADY_OPEN'
          ? 'Já existe um caixa aberto para este operador neste turno.'
          : cause instanceof Error
            ? cause.message
            : 'Não foi possível abrir o caixa.',
      );
    } finally {
      setOpenBusy(false);
    }
  }

  function startMovement(type: MovementType) {
    setMovementType(type);
    setMovementAmountInput('');
    setMovementReasonInput('');
    setMovementFeedback(undefined);
    setMovementRequiresAuth(false);
    setMovementAuthorizationToken(undefined);
    setMovementAuthorizeError(undefined);
  }

  async function submitMovement() {
    if (!movementType) return;
    const amount = Number.parseFloat(movementAmountInput.replace(',', '.'));
    if (!Number.isFinite(amount) || amount <= 0) {
      setMovementFeedback({ kind: 'error', message: 'Informe um valor maior que zero.' });
      return;
    }
    if (!movementReasonInput.trim()) {
      setMovementFeedback({ kind: 'error', message: 'Informe o motivo do movimento.' });
      return;
    }
    setMovementBusy(true);
    setMovementFeedback(undefined);
    try {
      const response = await api.registerMovement(
        identity,
        { type: movementType, amount, reason: movementReasonInput.trim() },
        movementAuthorizationToken,
      );
      setMovementFeedback({
        kind: 'success',
        message:
          movementType === 'WITHDRAWAL'
            ? `Sangria de ${formatMoneyBrl(response.movement.amount)} registrada. Novo valor esperado: ${formatMoneyBrl(response.newExpected)}.`
            : `Suprimento de ${formatMoneyBrl(response.movement.amount)} registrado. Novo valor esperado: ${formatMoneyBrl(response.newExpected)}.`,
      });
      setMovementType(undefined);
      setMovementAuthorizationToken(undefined);
      setMovementRequiresAuth(false);
      await load();
    } catch (cause) {
      if (cause instanceof CashSessionApiError && cause.code === 'AUTHORIZATION_REQUIRED') {
        setMovementRequiresAuth(true);
        setMovementFeedback({ kind: 'error', message: 'Sangria acima do limite — autorize com o PIN do gerente abaixo.' });
      } else {
        setMovementFeedback({
          kind: 'error',
          message: cause instanceof Error ? cause.message : 'Não foi possível registrar o movimento.',
        });
      }
    } finally {
      setMovementBusy(false);
    }
  }

  /** US-056 §5 (RN-011) — autoriza sangria acima do limite sem trocar de sessão. */
  async function authorizeMovement() {
    if (!movementPinInput) {
      setMovementAuthorizeError('Informe o PIN do gerente.');
      return;
    }
    setMovementAuthorizeBusy(true);
    setMovementAuthorizeError(undefined);
    try {
      const grant = await api.authorize(identity, { action: 'WITHDRAWAL_ABOVE_LIMIT', pin: movementPinInput });
      setMovementAuthorizationToken(grant.authorizationToken);
      setMovementPinInput('');
      setMovementFeedback(undefined);
    } catch (cause) {
      setMovementAuthorizeError(cause instanceof Error ? cause.message : 'Não foi possível autorizar a sangria.');
    } finally {
      setMovementAuthorizeBusy(false);
    }
  }

  async function submitClose() {
    if (!current) return;
    const amount = Number.parseFloat(countedAmountInput.replace(',', '.'));
    if (!Number.isFinite(amount) || amount < 0) {
      setCloseError('Informe o valor contado.');
      return;
    }
    setCloseBusy(true);
    setCloseError(undefined);
    try {
      const response = await api.close(
        identity,
        current.session.id,
        { countedAmount: amount, justification: justificationInput.trim() || null },
        closeAuthorizationToken,
      );
      setCloseResult(response);
      setOpenTables(undefined);
      setCloseAuthorizationToken(undefined);
    } catch (cause) {
      if (cause instanceof CashSessionApiError && cause.code === 'OPEN_TABLES') {
        const sessions = (cause.meta?.openSessions as readonly OpenTableSessionInfo[] | undefined) ?? [];
        setOpenTables(sessions);
        setCloseError('Existem mesas ainda abertas — feche-as ou autorize o fechamento mesmo assim.');
      } else if (cause instanceof CashSessionApiError && cause.code === 'CASH_JUSTIFICATION_REQUIRED') {
        setCloseRequiresJustification(true);
        setCloseError('A divergência encontrada exige uma justificativa antes de fechar o caixa.');
      } else {
        setCloseError(cause instanceof Error ? cause.message : 'Não foi possível fechar o caixa.');
      }
    } finally {
      setCloseBusy(false);
    }
  }

  /** RN-018 — autoriza o fechamento com mesa aberta sem trocar de sessão. */
  async function authorizeClose() {
    if (!closePinInput) {
      setCloseAuthorizeError('Informe o PIN do gerente.');
      return;
    }
    setCloseAuthorizeBusy(true);
    setCloseAuthorizeError(undefined);
    try {
      const grant = await api.authorize(identity, { action: 'CLOSE_DIVERGENT_CASH', pin: closePinInput });
      setCloseAuthorizationToken(grant.authorizationToken);
      setClosePinInput('');
      setCloseError(undefined);
    } catch (cause) {
      setCloseAuthorizeError(cause instanceof Error ? cause.message : 'Não foi possível autorizar o fechamento.');
    } finally {
      setCloseAuthorizeBusy(false);
    }
  }

  if (closeResult) {
    return <CloseReceipt result={closeResult} onExit={onExit} />;
  }

  if (loadError) {
    return (
      <main className="cash-session-page">
        <p role="alert" className="cash-session-error">
          {loadError}
        </p>
      </main>
    );
  }

  if (noOpenSession) {
    return (
      <main className="cash-session-page">
        <header className="cash-session-page__header">
          <div>
            <p className="cash-session-eyebrow">Caixa</p>
            <h1>Abrir caixa</h1>
          </div>
          {onExit ? (
            <Button type="button" variant="ghost" onClick={onExit}>
              Voltar
            </Button>
          ) : null}
        </header>
        <Card as="section" className="cash-session-open-form">
          <label htmlFor="cash-opening-amount">Fundo de caixa</label>
          <input
            id="cash-opening-amount"
            type="number"
            min="0"
            step="0.01"
            inputMode="decimal"
            className="cash-session-amount-input"
            value={openingAmountInput}
            onChange={(event) => setOpeningAmountInput(event.target.value)}
          />
          <Button type="button" onClick={() => void openSession()} disabled={openBusy}>
            Abrir caixa
          </Button>
          {openError ? (
            <p role="alert" className="cash-session-error">
              {openError}
            </p>
          ) : null}
        </Card>
      </main>
    );
  }

  if (!current) {
    return (
      <p role="status" className="cash-session-loading">
        Carregando o caixa…
      </p>
    );
  }

  const { expected } = current;
  const countedAmount = Number.parseFloat(countedAmountInput.replace(',', '.'));
  const hasCountedAmount = Number.isFinite(countedAmount);
  const previewDivergence = hasCountedAmount ? countedAmount - Number.parseFloat(expected.total) : undefined;

  return (
    <main className="cash-session-page">
      <header className="cash-session-page__header cash-session-page__header--embedded">
        <div>
          <p className="cash-session-eyebrow">Caixa aberto</p>
          <h1>Fechamento e movimentos</h1>
        </div>
        {onExit ? (
          <Button type="button" variant="ghost" onClick={onExit}>
            Voltar ao mapa
          </Button>
        ) : null}
      </header>

      <Card as="section" className="cash-session-expected">
        <div className="cash-session-card-heading">
          <h2>Fechamento de caixa</h2>
          <span>Valor esperado</span>
        </div>
        <dl className="cash-session-expected__list">
          <div>
            <dt>Fundo de abertura</dt>
            <dd>{formatMoneyBrl(expected.opening)}</dd>
          </div>
          <div>
            <dt>Recebido em dinheiro</dt>
            <dd>{formatMoneyBrl(expected.cashPayments)}</dd>
          </div>
          <div>
            <dt>Suprimentos</dt>
            <dd>{formatMoneyBrl(expected.supplies)}</dd>
          </div>
          <div>
            <dt>Sangrias</dt>
            <dd>{formatMoneyBrl(expected.withdrawals)}</dd>
          </div>
          <div className="cash-session-expected__total">
            <dt>Total esperado</dt>
            <dd>{formatMoneyBrl(expected.total)}</dd>
          </div>
        </dl>
      </Card>

      <Card as="section" className="cash-session-movements">
        <div className="cash-session-card-heading">
          <h2>Movimentos</h2>
          <span>Sangria e suprimento</span>
        </div>
        <div className="cash-session-movement-buttons">
          <Button type="button" variant="secondary" onClick={() => startMovement('WITHDRAWAL')}>
            Retirar (sangria)
          </Button>
          <Button type="button" variant="secondary" onClick={() => startMovement('SUPPLY')}>
            Suprir
          </Button>
        </div>

        {movementType ? (
          <div className="cash-session-movement-form">
            <label htmlFor="cash-movement-amount">Valor</label>
            <input
              id="cash-movement-amount"
              type="number"
              min="0.01"
              step="0.01"
              inputMode="decimal"
              className="cash-session-amount-input"
              value={movementAmountInput}
              onChange={(event) => setMovementAmountInput(event.target.value)}
            />
            <label htmlFor="cash-movement-reason">Motivo</label>
            <input
              id="cash-movement-reason"
              type="text"
              list="cash-movement-reason-suggestions"
              value={movementReasonInput}
              onChange={(event) => setMovementReasonInput(event.target.value)}
            />
            <datalist id="cash-movement-reason-suggestions">
              {(movementType === 'WITHDRAWAL' ? WITHDRAWAL_REASONS : SUPPLY_REASONS).map((reason) => (
                <option key={reason} value={reason} />
              ))}
            </datalist>
            <Button type="button" onClick={() => void submitMovement()} disabled={movementBusy}>
              {movementType === 'WITHDRAWAL' ? 'Confirmar sangria' : 'Confirmar suprimento'}
            </Button>

            {movementRequiresAuth ? (
              <div className="cash-session-authorize">
                <label htmlFor="cash-movement-pin">PIN do gerente</label>
                <input
                  id="cash-movement-pin"
                  type="password"
                  inputMode="numeric"
                  value={movementPinInput}
                  onChange={(event) => setMovementPinInput(event.target.value)}
                />
                <Button type="button" disabled={movementAuthorizeBusy} onClick={() => void authorizeMovement()}>
                  Autorizar sangria
                </Button>
                {movementAuthorizationToken ? (
                  <Badge tone="success" size="sm">
                    Autorizado
                  </Badge>
                ) : null}
                {movementAuthorizeError ? (
                  <p role="alert" className="cash-session-error">
                    {movementAuthorizeError}
                  </p>
                ) : null}
              </div>
            ) : null}
          </div>
        ) : null}

        {movementFeedback ? (
          <p
            role={movementFeedback.kind === 'error' ? 'alert' : 'status'}
            className={`cash-session-feedback cash-session-feedback--${movementFeedback.kind}`}
          >
            {movementFeedback.message}
          </p>
        ) : null}

        <h3>Histórico do turno</h3>
        {movements.length === 0 ? (
          <p className="cash-session-hint">Nenhum movimento registrado neste turno.</p>
        ) : (
          <ul className="cash-session-history-list">
            {movements.map((movement) => (
              <li key={movement.id} className="cash-session-history-item">
                <Badge tone={movement.type === 'WITHDRAWAL' ? 'danger' : 'success'} size="sm">
                  {movement.type === 'WITHDRAWAL' ? 'Sangria' : 'Suprimento'}
                </Badge>
                <span className="cash-session-history-item__amount">{formatMoneyBrl(movement.amount)}</span>
                <span className="cash-session-history-item__reason">{movement.reason}</span>
              </li>
            ))}
          </ul>
        )}
      </Card>

      <Card as="section" className="cash-session-close">
        <div className="cash-session-card-heading">
          <h2>Conferência</h2>
          <span>Informe o valor contado para fechar o caixa</span>
        </div>

        {openTables && openTables.length > 0 ? (
          <AlertBanner tone="danger" title="Mesas ainda abertas">
            <ul className="cash-session-open-tables-list">
              {openTables.map((table) => (
                <li key={table.table}>
                  Mesa {table.table} — {formatMoneyBrl(table.total)}
                </li>
              ))}
            </ul>
            <div className="cash-session-authorize">
              <label htmlFor="cash-close-pin">PIN do gerente</label>
              <input
                id="cash-close-pin"
                type="password"
                inputMode="numeric"
                value={closePinInput}
                onChange={(event) => setClosePinInput(event.target.value)}
              />
              <Button type="button" disabled={closeAuthorizeBusy} onClick={() => void authorizeClose()}>
                Autorizar fechamento
              </Button>
              {closeAuthorizationToken ? (
                <Badge tone="success" size="sm">
                  Autorizado
                </Badge>
              ) : null}
              {closeAuthorizeError ? (
                <p role="alert" className="cash-session-error">
                  {closeAuthorizeError}
                </p>
              ) : null}
            </div>
          </AlertBanner>
        ) : null}

        <label htmlFor="cash-counted-amount">Valor contado</label>
        <input
          id="cash-counted-amount"
          type="number"
          min="0"
          step="0.01"
          inputMode="decimal"
          className="cash-session-amount-input cash-session-amount-input--lg"
          value={countedAmountInput}
          onChange={(event) => setCountedAmountInput(event.target.value)}
        />

        {previewDivergence !== undefined ? (
          <p
            className={`cash-session-divergence ${previewDivergence === 0 ? 'cash-session-divergence--zero' : previewDivergence > 0 ? 'cash-session-divergence--positive' : 'cash-session-divergence--negative'}`}
          >
            Divergência: {previewDivergence > 0 ? '+' : previewDivergence < 0 ? '−' : ''}
            {formatMoneyBrl(Math.abs(previewDivergence).toFixed(2))}
          </p>
        ) : null}

        {closeRequiresJustification ? (
          <>
            <label htmlFor="cash-justification">Justificativa</label>
            <input
              id="cash-justification"
              type="text"
              list="cash-justification-suggestions"
              value={justificationInput}
              onChange={(event) => setJustificationInput(event.target.value)}
            />
            <datalist id="cash-justification-suggestions">
              {JUSTIFICATION_REASONS.map((reason) => (
                <option key={reason} value={reason} />
              ))}
            </datalist>
          </>
        ) : null}

        <Button type="button" onClick={() => void submitClose()} disabled={closeBusy}>
          Fechar caixa
        </Button>

        {closeError ? (
          <p role="alert" className="cash-session-error">
            {closeError}
          </p>
        ) : null}
      </Card>
    </main>
  );
}

/** Relatório de fechamento (US-055 §10: "relatório de fechamento imprimível e exportável" — versão mínima nesta tela). */
function CloseReceipt({
  result,
  onExit,
}: Readonly<{ result: CloseCashSessionResponse; onExit?: (() => void) | undefined }>) {
  const divergence = Number.parseFloat(result.divergence);
  return (
    <main className="cash-session-page cash-session-receipt">
      <header className="cash-session-page__header">
        <div>
          <p className="cash-session-eyebrow">Caixa fechado</p>
          <h1>Relatório de fechamento</h1>
        </div>
        {onExit ? (
          <Button type="button" variant="ghost" onClick={onExit}>
            Voltar ao mapa
          </Button>
        ) : null}
      </header>
      <Card as="section" className="cash-session-receipt__summary">
        <dl>
          <div>
            <dt>Esperado</dt>
            <dd>{formatMoneyBrl(result.expected)}</dd>
          </div>
          <div>
            <dt>Contado</dt>
            <dd>{formatMoneyBrl(result.counted)}</dd>
          </div>
          <div
            className={`cash-session-divergence ${divergence === 0 ? 'cash-session-divergence--zero' : divergence > 0 ? 'cash-session-divergence--positive' : 'cash-session-divergence--negative'}`}
          >
            <dt>Divergência</dt>
            <dd>
              {divergence > 0 ? '+' : divergence < 0 ? '−' : ''}
              {formatMoneyBrl(Math.abs(divergence).toFixed(2))}
            </dd>
          </div>
        </dl>
        {result.session.justification ? <p className="cash-session-hint">Justificativa: {result.session.justification}</p> : null}
        <Button type="button" variant="secondary" onClick={() => window.print()}>
          Imprimir relatório
        </Button>
      </Card>
    </main>
  );
}
