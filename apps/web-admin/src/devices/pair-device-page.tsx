import { useId, useState, type FormEvent } from 'react';
import { Button, Card, Field, Input } from '@nexora/ui';
import type { DeviceKindDto, PairDeviceRequest } from '@nexora/contracts';
import './devices.css';

export interface PairDevicePageProps {
  readonly profile: Readonly<{
    label: string;
    kind: DeviceKindDto;
    fingerprint: string;
  }>;
  readonly onPair: (input: PairDeviceRequest) => Promise<void>;
}

export function PairDevicePage({ profile, onPair }: Readonly<PairDevicePageProps>) {
  const codeId = useId();
  const [code, setCode] = useState('');
  const [error, setError] = useState<string>();
  const [busy, setBusy] = useState(false);
  const [paired, setPaired] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!/^\d{6}$/.test(code)) {
      setError('Digite os 6 dígitos exibidos no painel do gestor.');
      return;
    }
    setBusy(true);
    setError(undefined);
    try {
      await onPair({ code, ...profile });
      setPaired(true);
    } catch {
      setError('Código inválido ou expirado. Peça um novo código ao gestor.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="devices-shell devices-shell--pairing">
      <Card className="pairing-card" aria-labelledby="pairing-title">
        <div className="pairing-card__mark" aria-hidden="true">
          06
        </div>
        <p className="devices-eyebrow">TERMINAL SEGURO</p>
        <h1 id="pairing-title">Autorizar dispositivo</h1>
        <p className="devices-lead">
          Digite o código mostrado no painel do gestor. Ele vale por 10 minutos e só pode ser usado
          uma vez.
        </p>

        {paired ? (
          <div className="pairing-success" role="status">
            <strong>Dispositivo autorizado</strong>
            <span>{profile.label} já pode operar nesta loja.</span>
          </div>
        ) : (
          <form className="pairing-form" onSubmit={(event) => void submit(event)} noValidate>
            <Field label="Código de pareamento" htmlFor={codeId} className="pairing-code-field" {...(error ? { error } : {})}>
              <Input
                id={codeId}
                value={code}
                onChange={(event) => setCode(event.target.value.replace(/\D/g, '').slice(0, 6))}
                inputMode="numeric"
                autoComplete="one-time-code"
                maxLength={6}
                pattern="[0-9]{6}"
                placeholder="000000"
                invalid={Boolean(error)}
              />
            </Field>
            <Button type="submit" busy={busy} className="pairing-submit">
              Autorizar este dispositivo
            </Button>
          </form>
        )}
        <p className="pairing-card__footnote">
          Nenhuma senha ou PIN de operador é solicitado nesta etapa.
        </p>
      </Card>
    </main>
  );
}
