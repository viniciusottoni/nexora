import { useEffect, useMemo, useState } from 'react';
import {
  Button,
  Card,
  createNeutralBrandingResponse,
  DevicePairingScreen,
  OperatorBar,
  OperationalAuthClient,
  OperationalAuthError,
  PinScreen,
  readRegisteredDeviceIdentity,
  RuntimeBrandingProvider,
  useRuntimeBranding,
  type OperationalSession,
} from '@nexora/ui';
import './styles.css';

export interface PosHomeProps {
  readonly tenantName: string;
  readonly logo?: string;
}

export function PosHome({ tenantName, logo }: Readonly<PosHomeProps>) {
  return (
    <main className="pos-shell">
      <header className="pos-header">
        <div className="pos-identity">
          {logo && <img src={logo} alt="" />}
          <h1>{tenantName}</h1>
        </div>
        <div className="pos-state">
          <span aria-hidden="true" />
          Caixa pronto
        </div>
      </header>
      <Card as="section" className="pos-panel">
        <p className="pos-eyebrow">Início de operação</p>
        <h2>Entre para abrir o caixa</h2>
        <p>Identificação rápida e segura neste dispositivo.</p>
        <Button type="button">Acessar operação</Button>
      </Card>
    </main>
  );
}

function BrandedPos() {
  const { tenant, branding } = useRuntimeBranding();
  const logo = branding.logo.dark ?? branding.logo.light;
  const [device, setDevice] = useState<{ deviceId: string; deviceSecret: string } | null>();
  const [session, setSession] = useState<OperationalSession>();
  const [switching, setSwitching] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();
  const [retryAfterSeconds, setRetryAfterSeconds] = useState<number>();
  const [intentionId, setIntentionId] = useState(() => crypto.randomUUID());
  useEffect(() => {
    void readRegisteredDeviceIdentity()
      .then(setDevice)
      .catch(() => setDevice(null));
  }, []);
  const client = useMemo(
    () => (device ? new OperationalAuthClient({ baseUrl: '', ...device }) : undefined),
    [device],
  );
  const submit = async (pin: string) => {
    if (!client) return;
    setBusy(true);
    setError(undefined);
    try {
      setSession(await client.loginWithPin(pin, intentionId));
      setSwitching(false);
      setIntentionId(crypto.randomUUID());
    } catch (cause) {
      const authError = cause instanceof OperationalAuthError ? cause : undefined;
      setError(
        authError?.code === 'DEVICE_NOT_REGISTERED'
          ? 'Dispositivo não autorizado.'
          : 'PIN inválido. Tente novamente.',
      );
      setRetryAfterSeconds(authError?.retryAfterSeconds);
    } finally {
      setBusy(false);
    }
  };
  if (device === undefined)
    return (
      <p className="pos-loading" role="status">
        Preparando dispositivo…
      </p>
    );
  if (device === null)
    return <DevicePairingScreen kind="CASHIER" defaultLabel="Caixa" onPaired={setDevice} />;
  if (!session || switching)
    return (
      <PinScreen
        tenantName={tenant.name}
        onSubmit={submit}
        busy={busy}
        onLockoutElapsed={() => setRetryAfterSeconds(undefined)}
        {...(error ? { error } : {})}
        {...(retryAfterSeconds === undefined ? {} : { retryAfterSeconds })}
        {...(logo ? { logo } : {})}
      />
    );
  return (
    <div className="pos-authenticated">
      <OperatorBar
        userName={session.user.name}
        onSwitchOperator={() => {
          setSwitching(true);
          setError(undefined);
          setIntentionId(crypto.randomUUID());
        }}
      />
      <PosHome tenantName={tenant.name} {...(logo ? { logo } : {})} />
    </div>
  );
}

export function App() {
  return (
    // US-003, gap "resolução de tenant por host não funciona para web-pos/web-kds": este app
    // roda na LAN da loja (Nexora.Api.Edge), onde o host HTTP nunca bate com Tenant.Domain — por
    // isso usa /v1/local/branding (sem host, tenant único da instalação), não o
    // /v1/public/branding?host=... padrão (que é para web-menu, atrás de domínio público).
    <RuntimeBrandingProvider fallback={createNeutralBrandingResponse()} endpoint="/v1/local/branding">
      <BrandedPos />
    </RuntimeBrandingProvider>
  );
}
