import { useId, useState, type FormEvent } from 'react';
import { pairDeviceResponseSchema, type DeviceKindDto } from '@nexora/contracts';
import { Button } from '../components/button.js';
import { Field } from '../components/field.js';
import { Input } from '../components/input.js';
import { saveRegisteredDeviceIdentity } from './browser-device-identity.js';

export interface DevicePairingScreenProps {
  readonly kind: DeviceKindDto;
  readonly defaultLabel: string;
  readonly onPaired: (identity: { deviceId: string; deviceSecret: string }) => void;
}

export function DevicePairingScreen({
  kind,
  defaultLabel,
  onPaired,
}: Readonly<DevicePairingScreenProps>) {
  const codeId = useId();
  const [code, setCode] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setError(undefined);
    try {
      const response = await fetch('/v1/devices/pair', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
        body: JSON.stringify({
          code,
          label: defaultLabel,
          kind,
          fingerprint: await browserFingerprint(),
        }),
      });
      if (!response.ok)
        throw new Error('C\u00f3digo inv\u00e1lido, expirado ou j\u00e1 utilizado.');
      const result = pairDeviceResponseSchema.parse(await response.json());
      const identity = { deviceId: result.device.id, deviceSecret: result.deviceSecret };
      await saveRegisteredDeviceIdentity(identity);
      onPaired(identity);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'N\u00e3o foi poss\u00edvel parear.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="db-device-pairing">
      <form onSubmit={(event) => void submit(event)}>
        <p className="db-cloud-login__eyebrow">PRIMEIRO ACESSO</p>
        <h1>Autorizar dispositivo</h1>
        <p>Digite o c&oacute;digo de seis d&iacute;gitos gerado pelo gestor.</p>
        <p className="db-device-pairing__device">
          Este terminal ser&aacute; identificado como <strong>{defaultLabel}</strong>.
        </p>
        {error ? <p role="alert">{error}</p> : null}
        <Field label={'C\u00f3digo de pareamento'} htmlFor={codeId}>
          <Input
            id={codeId}
            inputMode="numeric"
            autoComplete="one-time-code"
            maxLength={6}
            pattern="[0-9]{6}"
            value={code}
            onChange={(event) => setCode(event.target.value.replace(/\D/g, '').slice(0, 6))}
            required
          />
        </Field>
        <Button type="submit" busy={busy} disabled={code.length !== 6}>
          Autorizar
        </Button>
      </form>
    </main>
  );
}

async function browserFingerprint(): Promise<string> {
  const source = `${navigator.userAgent}|${navigator.language}|${screen.width}x${screen.height}`;
  const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(source));
  return [...new Uint8Array(digest)].map((byte) => byte.toString(16).padStart(2, '0')).join('');
}
