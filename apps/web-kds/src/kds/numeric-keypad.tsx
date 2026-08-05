import { useCallback, useEffect, useRef, useState } from 'react';
import { Button } from '@nexora/ui';
import './numeric-keypad.css';

export interface NumericKeypadProps {
  /** US-041 §3 — Enter (ou tecla física Enter) envia o código digitado; `batch=false` sempre (o modo lote é uma ação separada e deliberada). */
  readonly onSubmit: (code: string) => void;
  /** US-041 §3 ("Avanço em lote do pedido") — confirmação explícita e separada do Enter comum. */
  readonly onSubmitBatch: (code: string) => void;
  /** US-041 §3/§4 ("Desfazer avanço acidental") — tecla dedicada, só habilitada dentro da janela. */
  readonly onUndo?: () => void;
  readonly undoAvailable?: boolean;
  /** Mensagem de erro breve (ex. "Código não encontrado") — some sozinha, nunca trava a tela (US-041 §10). */
  readonly error?: string | undefined;
  readonly disabled?: boolean;
}

/**
 * US-041 (Avançar estado com um toque via teclado numérico) — campo de entrada SEMPRE focado, zero
 * digitação livre: só dígitos, Enter, um "Lote" e um "Desfazer". Aceita tanto toque na grade quanto
 * um teclado numérico físico USB (dígitos, Enter, Backspace, `*` para lote, `-` para desfazer — o
 * layout padrão de teclado numérico de PDV já usa esses símbolos, então não colide com nada).
 * Resposta visual em menos de 300 ms (US-041 §2): o dígito aparece no mostrador na mesma renderização
 * do clique/tecla, sem esperar nenhuma rede.
 */
export function NumericKeypad({
  onSubmit,
  onSubmitBatch,
  onUndo,
  undoAvailable = false,
  error,
  disabled = false,
}: Readonly<NumericKeypadProps>) {
  const [code, setCode] = useState('');
  const [flash, setFlash] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  // Campo sempre focado — o operador nunca precisa clicar antes de digitar (US-041 §10).
  useEffect(() => {
    inputRef.current?.focus();
  }, []);

  useEffect(() => {
    if (!error) return;
    setFlash(true);
    setCode('');
    const timeout = setTimeout(() => setFlash(false), 600);
    return () => clearTimeout(timeout);
  }, [error]);

  const append = useCallback((digit: string) => {
    if (disabled) return;
    setCode((current) => (current.length >= 6 ? current : current + digit));
  }, [disabled]);

  const backspace = useCallback(() => setCode((current) => current.slice(0, -1)), []);

  const submit = useCallback(() => {
    if (disabled || code.length === 0) return;
    onSubmit(code);
    setCode('');
  }, [code, disabled, onSubmit]);

  const submitBatch = useCallback(() => {
    if (disabled || code.length === 0) return;
    onSubmitBatch(code);
    setCode('');
  }, [code, disabled, onSubmitBatch]);

  const handleKeyDown = useCallback(
    (event: React.KeyboardEvent<HTMLInputElement>) => {
      if (/^[0-9]$/.test(event.key)) {
        event.preventDefault();
        append(event.key);
        return;
      }
      switch (event.key) {
        case 'Enter':
          event.preventDefault();
          submit();
          break;
        case 'Backspace':
          event.preventDefault();
          backspace();
          break;
        case 'Escape':
          event.preventDefault();
          setCode('');
          break;
        case '*':
          event.preventDefault();
          submitBatch();
          break;
        case '-':
        case 'Delete':
          if (undoAvailable && onUndo) {
            event.preventDefault();
            onUndo();
          }
          break;
        default:
          break;
      }
    },
    [append, backspace, onUndo, submit, submitBatch, undoAvailable],
  );

  return (
    <div className="kds-keypad nx-anim-in" data-surface="kds">
      <input
        ref={inputRef}
        className={`kds-keypad__display ${flash ? 'kds-keypad__display--error nx-anim-flash' : ''}`}
        value={code}
        onKeyDown={handleKeyDown}
        onChange={() => {
          /* somente teclas físicas de dígito controlam o valor — texto livre é ignorado (US-041 §10). */
        }}
        inputMode="numeric"
        placeholder="código"
        aria-label="Código do pedido"
        data-testid="kds-keypad-display"
      />
      {error ? (
        <p className="kds-keypad__error" role="alert" data-testid="kds-keypad-error">
          {error}
        </p>
      ) : null}
      <div className="kds-keypad__grid">
        {['7', '8', '9', '4', '5', '6', '1', '2', '3'].map((digit) => (
          <Button
            key={digit}
            type="button"
            variant="secondary"
            size="touch"
            onClick={() => append(digit)}
            disabled={disabled}
          >
            {digit}
          </Button>
        ))}
        <Button type="button" variant="ghost" size="touch" onClick={submitBatch} disabled={disabled}>
          Lote
        </Button>
        <Button type="button" variant="secondary" size="touch" onClick={() => append('0')} disabled={disabled}>
          0
        </Button>
        <Button type="button" variant="ghost" size="touch" onClick={backspace} disabled={disabled}>
          ⌫
        </Button>
      </div>
      <div className="kds-keypad__actions">
        <Button
          type="button"
          variant="danger"
          size="touch"
          onClick={onUndo}
          disabled={!undoAvailable || disabled}
          data-testid="kds-keypad-undo"
        >
          Desfazer
        </Button>
        <Button type="button" variant="accent" size="touch" block onClick={submit} disabled={disabled}>
          Enter
        </Button>
      </div>
    </div>
  );
}
