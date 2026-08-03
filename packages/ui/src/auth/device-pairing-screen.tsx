import { useId, useState, type FormEvent } from 'react';
import { pairDeviceResponseSchema, type DeviceKindDto } from '@nexora/contracts';
import { NexoraSplash } from '../brand/nexora-loader.js';
import { NexoraLogo } from '../brand/nexora-logo.js';
import { Button } from '../components/button.js';
import { CreatedByFooter } from '../components/created-by-footer.js';
import { Field } from '../components/field.js';
import { Input } from '../components/input.js';
import { saveRegisteredDeviceIdentity } from './browser-device-identity.js';

export interface DevicePairingScreenProps {
  readonly kind: DeviceKindDto;
  readonly defaultLabel: string;
  readonly onPaired: (identity: { deviceId: string; deviceSecret: string }) => void;
  readonly baseUrl?: string;
}

export function DevicePairingScreen({
  kind,
  defaultLabel,
  onPaired,
  baseUrl = '',
}: Readonly<DevicePairingScreenProps>) {
  const codeId = useId();
  const [code, setCode] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();
  const [cardOpened, setCardOpened] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setError(undefined);
    try {
      const response = await fetch(`${baseUrl}/v1/devices/pair`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
        body: JSON.stringify({
          code,
          label: defaultLabel,
          kind,
          fingerprint: await browserFingerprint(),
        }),
      });
      if (!response.ok) throw new Error(await pairingFailureMessage(response));
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
      <NexoraSplash label="Preparando o primeiro acesso" onOpened={() => setCardOpened(true)}>
        <form onSubmit={(event) => void submit(event)}>
          <NexoraLogo className="db-auth-logo" variant="lockup" height={42} shine />
          <p className="db-cloud-login__eyebrow">PRIMEIRO ACESSO</p>
          <h1>Autorizar dispositivo</h1>
          <p>Digite o c&oacute;digo de seis d&iacute;gitos gerado pelo gestor.</p>
          <p className="db-device-pairing__device">
            Este terminal ser&aacute; identificado como <strong>{defaultLabel}</strong>.
          </p>
          {error ? (
            <p className="db-auth-error" role="alert">
              {error}
            </p>
          ) : null}
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
      </NexoraSplash>
      <CreatedByFooter
        className={`db-created-by--inline ${cardOpened ? 'is-visible' : ''}`.trim()}
      />
    </main>
  );
}

export async function pairingFailureMessage(response: Response): Promise<string> {
  const problem = (await response.json().catch(() => null)) as {
    code?: unknown;
    detail?: unknown;
    title?: unknown;
  } | null;
  const code = typeof problem?.code === 'string' ? problem.code : undefined;

  const knownMessages: Readonly<Record<string, string>> = {
    DEVICE_PAIRING_CODE_INVALID: 'C\u00f3digo de pareamento inv\u00e1lido.',
    DEVICE_PAIRING_CODE_EXPIRED: 'C\u00f3digo de pareamento expirado. Gere um novo c\u00f3digo.',
    DEVICE_PAIRING_CODE_CONSUMED:
      'C\u00f3digo de pareamento j\u00e1 utilizado. Gere um novo c\u00f3digo.',
    DEVICE_PAIRING_RATE_LIMITED:
      'Muitas tentativas de pareamento. Aguarde alguns minutos e tente novamente.',
    REQUEST_IN_PROGRESS:
      'A solicita\u00e7\u00e3o anterior ainda est\u00e1 sendo processada. Aguarde alguns segundos e tente novamente.',
  };

  if (code && knownMessages[code]) return knownMessages[code];
  if (typeof problem?.detail === 'string' && problem.detail.trim()) return problem.detail;
  if (typeof problem?.title === 'string' && problem.title.trim()) return problem.title;
  return 'N\u00e3o foi poss\u00edvel autorizar o dispositivo.';
}

async function browserFingerprint(): Promise<string> {
  const source = `${navigator.userAgent}|${navigator.language}|${screen.width}x${screen.height}`;
  const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(source));
  return [...new Uint8Array(digest)].map((byte) => byte.toString(16).padStart(2, '0')).join('');
}
