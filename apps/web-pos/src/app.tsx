import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import {
  BrandMark,
  Button,
  Card,
  createNeutralBrandingResponse,
  CreatedByFooter,
  DevicePairingScreen,
  NotificationCenter,
  OperatorBar,
  OperationalAuthClient,
  OperationalAuthError,
  PinScreen,
  readRegisteredDeviceIdentity,
  RuntimeBrandingProvider,
  SideNav,
  SyncStatus,
  TopBar,
  useRuntimeBranding,
  type OperationalSession,
} from '@nexora/ui';
import { BillingPage } from './billing/billing-page.js';
import { CashPanelPage } from './cash-panel/cash-panel-page.js';
import { CashSessionPage } from './cash-session/cash-session-page.js';
import { useNotificationCenter } from './notifications/use-notification-center.js';
import { configurePosOrderQueue, posOrderQueue } from './offline/pos-order-queue.js';
import { OrderCompositionPage } from './order-composition/order-composition-page.js';
import { OpenTablePage } from './tables/open-table-page.js';
import { TableMapPage } from './table-map/table-map-page.js';
import './styles.css';

export interface PosHomeProps {
  readonly tenantName: string;
  readonly logo?: string;
}

type PosOperationalView = 'tables' | 'cash-panel' | 'billing' | 'cash-session';

export interface PosOperationalWorkAreaProps {
  readonly identity: {
    readonly accessToken: string;
    readonly deviceId: string;
    readonly deviceSecret: string;
  };
  readonly onOrderQueued?: () => void;
  readonly tenantName?: string;
  readonly logo?: string;
  readonly headerActions?: ReactNode;
  readonly queuedOrderCount?: number;
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

export function PosOperationalWorkArea({
  identity,
  onOrderQueued,
  tenantName = 'Estabelecimento',
  headerActions,
  queuedOrderCount = 0,
}: Readonly<PosOperationalWorkAreaProps>) {
  const [activeView, setActiveView] = useState<PosOperationalView>('tables');
  const [openingTableId, setOpeningTableId] = useState<string>();
  const [billingSessionId, setBillingSessionId] = useState<string>();
  const [composingSessionId, setComposingSessionId] = useState<string>();

  function changeView(view: PosOperationalView) {
    setActiveView(view);
    setOpeningTableId(undefined);
    setBillingSessionId(undefined);
    setComposingSessionId(undefined);
  }

  const title = billingSessionId
    ? 'Recebimento'
    : openingTableId
      ? 'Abrir mesa'
      : composingSessionId
        ? 'Lançar pedido'
        : activeView === 'cash-panel'
          ? 'Mesas e comandas abertas'
          : activeView === 'billing'
            ? 'Recebimento'
          : activeView === 'cash-session'
            ? 'Fechamento de caixa'
            : 'Mapa de mesas';

  return (
    <div className="pos-operation-shell">
      <SideNav
        brand={
          <BrandMark inverse subtitle="Caixa · Terminal 1" size={22} />
        }
        activeId={billingSessionId ? 'billing' : activeView}
        onSelect={(view) => {
          if (view === 'tables' || view === 'cash-panel' || view === 'billing' || view === 'cash-session') {
            changeView(view);
          }
        }}
        items={[
          { group: 'Operação' },
          {
            id: 'cash-panel',
            label: <span aria-label="Painel do caixa">Mesas e comandas</span>,
            icon: 'table_restaurant',
          },
          { id: 'billing', label: 'Recebimento', icon: 'point_of_sale' },
          {
            id: 'cash-session',
            label: <span aria-label="Caixa">Fechamento de caixa</span>,
            icon: 'lock_clock',
          },
          { group: 'Salão' },
          { id: 'tables', label: 'Mapa de mesas', icon: 'grid_view' },
        ]}
        footer={
          <SyncStatus
            state={queuedOrderCount > 0 ? 'local' : 'online'}
            {...(queuedOrderCount > 0 ? { queued: queuedOrderCount } : {})}
          />
        }
      />

      <div className="pos-operation-main">
        <TopBar
          title={title}
          subtitle={`${tenantName} · visão de hoje`}
          right={
            <>
              {headerActions}
              <SyncStatus
                state={queuedOrderCount > 0 ? 'local' : 'online'}
                {...(queuedOrderCount > 0 ? { queued: queuedOrderCount } : {})}
              />
            </>
          }
        />

        <div className="pos-operation-content">
          {openingTableId ? (
            <OpenTablePage identity={identity} preselectedTableId={openingTableId} onExit={() => setOpeningTableId(undefined)} />
          ) : billingSessionId ? (
            <BillingPage identity={identity} sessionId={billingSessionId} onExit={() => setBillingSessionId(undefined)} />
          ) : composingSessionId ? (
            <OrderCompositionPage
              identity={identity}
              sessionId={composingSessionId}
              onExit={() => setComposingSessionId(undefined)}
              {...(onOrderQueued ? { onOrderQueued } : {})}
            />
          ) : activeView === 'cash-panel' ? (
            <CashPanelPage identity={identity} onOpenBilling={setBillingSessionId} />
          ) : activeView === 'billing' ? (
            <CashPanelPage identity={identity} onOpenBilling={setBillingSessionId} mode="receiving" />
          ) : activeView === 'cash-session' ? (
            <CashSessionPage identity={identity} onExit={() => changeView('cash-panel')} />
          ) : (
            <TableMapPage
              identity={identity}
              onSelectTable={setOpeningTableId}
              onOpenBilling={setBillingSessionId}
              onComposeOrder={setComposingSessionId}
            />
          )}
        </div>
      </div>
    </div>
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
  // US-034 §8/§10 — contador da fila local de pedidos (queda de LAN), lido no shell autenticado
  // (não numa tela específica) porque o indicador precisa ser PERMANENTE mesmo quando o garçom sai
  // da tela de composição de pedido para o mapa de mesas.
  const [queuedOrderCount, setQueuedOrderCount] = useState(0);
  const refreshQueuedOrderCount = useCallback(() => {
    void posOrderQueue.count().then(setQueuedOrderCount).catch(() => {});
  }, []);
  useEffect(() => {
    void readRegisteredDeviceIdentity()
      .then(setDevice)
      .catch(() => setDevice(null));
  }, []);
  // US-034 §7/§10 — reenvio automático ao reconectar: só `window.addEventListener('online', ...)`
  // (nenhum segundo mecanismo de detecção concorrente) mais uma tentativa já no carregamento
  // (cobre o caso "app reaberto já online, mas com backlog de uma queda anterior"). Nenhuma tela
  // deste app tem uma conexão realtime PERSISTENTE durante a janela vulnerável — TableMapPage e
  // OrderCompositionPage são mutuamente exclusivas (só uma montada por vez), e é justamente na
  // segunda que um pedido pode ficar na fila — por isso a configuração/reenvio moram aqui, no
  // shell autenticado, não em nenhuma tela específica.
  useEffect(() => {
    if (!device || !session) return;
    configurePosOrderQueue({ accessToken: session.accessToken, deviceId: device.deviceId, deviceSecret: device.deviceSecret });

    let active = true;
    async function syncQueue() {
      await posOrderQueue.flush().catch(() => {});
      if (active) refreshQueuedOrderCount();
    }
    void syncQueue();

    function handleOnline() {
      void syncQueue();
    }
    window.addEventListener('online', handleOnline);
    return () => {
      active = false;
      window.removeEventListener('online', handleOnline);
    };
  }, [device, session, refreshQueuedOrderCount]);
  const client = useMemo(
    () => (device ? new OperationalAuthClient({ baseUrl: '', ...device }) : undefined),
    [device],
  );
  // E-08 (US-081/US-083) — central de notificações do shell autenticado, visível de qualquer tela
  // (§10: "acessível de qualquer tela"). O hook precisa ser chamado incondicionalmente em TODO
  // render (regra dos hooks) mesmo antes do login — por isso `identity` é `undefined` até
  // device/session existirem, e é o próprio hook (não esta chamada) que fica ocioso nesse meio-tempo
  // (mesmo padrão de `if (!device || !session) return;` dentro do efeito de reenvio, acima).
  const identity = device && session ? { accessToken: session.accessToken, deviceId: device.deviceId, deviceSecret: device.deviceSecret } : undefined;
  const notificationCenter = useNotificationCenter({ identity });
  const [notificationCenterOpen, setNotificationCenterOpen] = useState(false);
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
      {queuedOrderCount > 0 ? (
        // US-034 §10: indicador discreto e PERMANENTE (nunca modal/pop-up) — usa `SyncStatus`
        // (packages/ui) já existente, mais uma legenda sem jargão técnico com a redação exata da
        // história ("trabalhando sem internet · N registros aguardando envio").
        <div className="pos-offline-banner nx-anim-in" role="status">
          <SyncStatus state="local" queued={queuedOrderCount} />
          <span className="pos-offline-banner__hint">
            {`Trabalhando sem internet · ${queuedOrderCount} ${queuedOrderCount === 1 ? 'registro' : 'registros'} aguardando envio.`}
          </span>
        </div>
      ) : null}
      <PosOperationalWorkArea
        identity={{ accessToken: session.accessToken, deviceId: device.deviceId, deviceSecret: device.deviceSecret }}
        onOrderQueued={refreshQueuedOrderCount}
        tenantName={tenant.name}
        {...(logo ? { logo } : {})}
        queuedOrderCount={queuedOrderCount}
        headerActions={
          <>
            <NotificationCenter
              items={notificationCenter.items}
              open={notificationCenterOpen}
              onOpenChange={setNotificationCenterOpen}
              onAcknowledge={(id) => void notificationCenter.acknowledge(id)}
              loading={notificationCenter.loading}
              pushPermissionPending={notificationCenter.pushPermissionPending}
              onRequestPushPermission={() => void notificationCenter.requestPushPermission()}
            />
            <OperatorBar
              userName={session.user.name}
              onSwitchOperator={() => {
                setSwitching(true);
                setError(undefined);
                setIntentionId(crypto.randomUUID());
              }}
            />
          </>
        }
      />
      <CreatedByFooter />
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
