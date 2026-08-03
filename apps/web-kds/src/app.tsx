import { useEffect, useMemo, useState } from 'react';
import {
  Card,
  createNeutralBrandingResponse,
  CreatedByFooter,
  DevicePairingScreen,
  OperatorBar,
  OperationalAuthClient,
  OperationalAuthError,
  operationalAuthenticatedFetch,
  PinScreen,
  readRegisteredDeviceIdentity,
  RuntimeBrandingProvider,
  useRuntimeBranding,
  type OperationalSession,
} from '@nexora/ui';
import './styles.css';
import { AvailabilityApi } from './availability/availability-api.js';
import { UnavailablePanel } from './availability/unavailable-panel.js';
import { KdsQueuePage } from './kds/kds-queue-page.js';

export interface KdsHomeProps {
  readonly tenantName: string;
  readonly logo?: string;
}

export function KdsHome({ tenantName, logo }: Readonly<KdsHomeProps>) {
  return (
    <main className="kds-shell" data-surface="kds">
      <header className="kds-header">
        <div>
          {logo && <img src={logo} alt="" />}
          <span>{tenantName}</span>
        </div>
        <time>--:--</time>
      </header>
      <section className="kds-status">
        <span className="kds-pulse" aria-hidden="true" />
        <div>
          <p>Fila atual</p>
          <h1>Cozinha em dia</h1>
          <span>Novos pedidos aparecem automaticamente.</span>
        </div>
      </section>
      {/* nx-stagger é seguro aqui: são só 3 colunas de resumo que montam uma vez por
          sessão (login), não a fila de tickets em si — nunca atrasa a leitura de um
          pedido urgente. */}
      <section className="kds-columns nx-stagger" aria-label="Fluxo da cozinha">
        {['A fazer', 'Preparando', 'Pronto'].map((label) => (
          <Card as="div" key={label} className="kds-column">
            <h2>{label}</h2>
            <strong>0</strong>
          </Card>
        ))}
      </section>
    </main>
  );
}

function BrandedKds() {
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
  const availabilityApi = useMemo(
    () =>
      session && device
        ? new AvailabilityApi('', (input, init) =>
            operationalAuthenticatedFetch(input, init, {
              accessToken: session.accessToken,
              deviceId: device.deviceId,
              deviceSecret: device.deviceSecret,
            }),
          )
        : undefined,
    [device, session],
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
      <p className="kds-loading" role="status">
        Preparando dispositivo…
      </p>
    );
  if (device === null)
    return <DevicePairingScreen kind="KDS" defaultLabel="Cozinha" onPaired={setDevice} />;
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
    <div className="kds-authenticated">
      <OperatorBar
        userName={session.user.name}
        onSwitchOperator={() => {
          setSwitching(true);
          setError(undefined);
          setIntentionId(crypto.randomUUID());
        }}
      />
      <KdsQueuePage
        identity={{
          accessToken: session.accessToken,
          deviceId: device.deviceId,
          deviceSecret: device.deviceSecret,
        }}
      />
      {availabilityApi ? (
        <section className="kds-operational-tools" aria-label="Disponibilidade do cardápio">
          <UnavailablePanel api={availabilityApi} accessToken={session.accessToken} />
        </section>
      ) : null}
      <CreatedByFooter />
    </div>
  );
}
export function App() {
  return (
    // US-003, gap "resolução de tenant por host não funciona para web-pos/web-kds" — ver
    // comentário equivalente em apps/web-pos/src/app.tsx.
    <RuntimeBrandingProvider
      fallback={createNeutralBrandingResponse()}
      endpoint="/v1/local/branding"
    >
      <BrandedKds />
    </RuntimeBrandingProvider>
  );
}
