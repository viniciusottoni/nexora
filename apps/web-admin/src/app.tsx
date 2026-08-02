import { useEffect, useMemo, useState } from 'react';
import {
  Button,
  CloudLoginScreen,
  DevicePairingScreen,
  hasCloudSession,
  operationalAuthenticatedFetch,
  OperationalAuthClient,
  OperationalAuthError,
  PinScreen,
  readRegisteredDeviceIdentity,
  SegmentedControl,
  ThemeProvider,
  TopBar,
  type OperationalSession,
} from '@nexora/ui';
import type { DeviceDto, PermissionCatalogItem, RoleDto } from '@nexora/contracts';
import { BrandingContainer } from './branding/branding-container.js';
import { DeviceManagementPage } from './devices/device-management-page.js';
import { DevicesApi } from './devices/devices-api.js';
import { RoleManagementPage } from './roles/role-management-page.js';
import { RolesApi } from './roles/roles-api.js';
import './app.css';

interface DeviceIdentity {
  readonly deviceId: string;
  readonly deviceSecret: string;
}

const cloudDevicesApi = new DevicesApi();
const cloudRolesApi = new RolesApi();

export function isLocalEdgeAdminPath(pathname: string): boolean {
  return /^\/admin(?:\/|$)/.test(pathname);
}

export function App() {
  const local = isLocalEdgeAdminPath(globalThis.location?.pathname ?? '/');
  return <ThemeProvider>{local ? <LocalAdmin /> : <CloudAdmin />}</ThemeProvider>;
}

function CloudAdmin() {
  const [authenticated, setAuthenticated] = useState(() => hasCloudSession());
  const [section, setSection] = useState<'devices' | 'roles' | 'branding'>('devices');
  const [devices, setDevices] = useState<readonly DeviceDto[]>([]);
  const [roles, setRoles] = useState<readonly RoleDto[]>([]);
  const [permissionCatalog, setPermissionCatalog] = useState<readonly PermissionCatalogItem[]>([]);
  const [error, setError] = useState<string>();

  useEffect(() => {
    if (!authenticated) return;
    let active = true;
    cloudDevicesApi
      .list()
      .then((result) => {
        if (active) setDevices(result.items);
      })
      .catch((reason: unknown) => {
        if (active) setError(toMessage(reason));
      });
    cloudRolesApi
      .list()
      .then((result) => {
        if (!active) return;
        setRoles(result.items);
        setPermissionCatalog(result.permissionCatalog);
      })
      .catch((reason: unknown) => {
        if (active) setError(toMessage(reason));
      });
    return () => {
      active = false;
    };
  }, [authenticated]);

  async function refreshDevices() {
    setDevices((await cloudDevicesApi.list()).items);
  }

  async function refreshRoles() {
    const result = await cloudRolesApi.list();
    setRoles(result.items);
    setPermissionCatalog(result.permissionCatalog);
  }

  if (!authenticated) return <CloudLoginScreen onAuthenticated={() => setAuthenticated(true)} />;

  return (
    <>
      <AdminNavigation section={section} onSectionChange={setSection} />
      {error ? (
        <p className="admin-error" role="alert">
          {error}
        </p>
      ) : null}
      {section === 'devices' ? (
        <DeviceManagementPage
          devices={devices}
          onRename={async (id, label) => {
            await cloudDevicesApi.rename(id, label);
            await refreshDevices();
          }}
          onRevoke={async (id) => {
            await cloudDevicesApi.revoke(id);
            await refreshDevices();
          }}
        />
      ) : null}
      {section === 'roles' ? (
        <RoleManagementPage
          roles={roles}
          permissionCatalog={permissionCatalog}
          onCreate={async (input) => {
            const created = await cloudRolesApi.create(input);
            await refreshRoles();
            return created;
          }}
          onUpdate={async (id, input) => {
            const updated = await cloudRolesApi.update(id, input);
            await refreshRoles();
            return updated;
          }}
        />
      ) : null}
      {section === 'branding' ? <BrandingContainer /> : null}
    </>
  );
}

function LocalAdmin() {
  const [device, setDevice] = useState<DeviceIdentity | null>();
  const [session, setSession] = useState<OperationalSession>();
  const [devices, setDevices] = useState<readonly DeviceDto[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();
  const [retryAfterSeconds, setRetryAfterSeconds] = useState<number>();
  const [intentionId, setIntentionId] = useState(() => crypto.randomUUID());

  useEffect(() => {
    void readRegisteredDeviceIdentity()
      .then(setDevice)
      .catch(() => setDevice(null));
  }, []);

  const authClient = useMemo(
    () => (device ? new OperationalAuthClient({ baseUrl: '', ...device }) : undefined),
    [device],
  );
  const devicesApi = useMemo(() => {
    if (!device || !session) return undefined;
    return new DevicesApi('', (input, init) =>
      operationalAuthenticatedFetch(input, init, {
        accessToken: session.accessToken,
        ...device,
      }),
    );
  }, [device, session]);

  useEffect(() => {
    if (!devicesApi) return;
    let active = true;
    devicesApi
      .list()
      .then((result) => {
        if (active) setDevices(result.items);
      })
      .catch((reason: unknown) => {
        if (active) setError(toMessage(reason));
      });
    return () => {
      active = false;
    };
  }, [devicesApi]);

  async function login(pin: string) {
    if (!authClient) return;
    setBusy(true);
    setError(undefined);
    try {
      const nextSession = await authClient.loginWithPin(pin, intentionId);
      if (
        !nextSession.permissions.includes('*') &&
        !nextSession.permissions.includes('device:manage')
      ) {
        setError('Seu perfil n\u00e3o permite administrar dispositivos.');
        return;
      }
      setSession(nextSession);
      setIntentionId(crypto.randomUUID());
    } catch (reason) {
      const authError = reason instanceof OperationalAuthError ? reason : undefined;
      setError(
        authError?.code === 'DEVICE_NOT_REGISTERED'
          ? 'Dispositivo n\u00e3o autorizado.'
          : 'PIN inv\u00e1lido. Tente novamente.',
      );
      setRetryAfterSeconds(authError?.retryAfterSeconds);
    } finally {
      setBusy(false);
    }
  }

  async function refreshDevices() {
    if (devicesApi) setDevices((await devicesApi.list()).items);
  }

  if (device === undefined) {
    return (
      <p className="admin-loading" role="status">
        Preparando administra&ccedil;&atilde;o local&hellip;
      </p>
    );
  }
  if (device === null) {
    return (
      <DevicePairingScreen
        kind="SUPPORT_TABLET"
        defaultLabel={'Gest\u00e3o local'}
        onPaired={setDevice}
      />
    );
  }
  if (!session) {
    return (
      <PinScreen
        tenantName={'Gest\u00e3o local'}
        onSubmit={login}
        busy={busy}
        onLockoutElapsed={() => setRetryAfterSeconds(undefined)}
        {...(error ? { error } : {})}
        {...(retryAfterSeconds === undefined ? {} : { retryAfterSeconds })}
      />
    );
  }

  return (
    <>
      <TopBar
        className="admin-local-header"
        title="Dispositivos da loja"
        subtitle="Painel local"
        right={
          <Button type="button" variant="ghost" onClick={() => setSession(undefined)}>
            Trocar gestor
          </Button>
        }
      />
      {error ? (
        <p className="admin-error" role="alert">
          {error}
        </p>
      ) : null}
      <DeviceManagementPage
        devices={devices}
        onCreatePairingCode={() => devicesApi!.createPairingCode()}
        onRename={async (id, label) => {
          await devicesApi!.rename(id, label);
          await refreshDevices();
        }}
        onRevoke={async (id) => {
          await devicesApi!.revoke(id);
          await refreshDevices();
        }}
      />
    </>
  );
}

const ADMIN_SECTIONS = [
  { value: 'devices', label: 'Dispositivos' },
  { value: 'roles', label: 'Pap\u00e9is e permiss\u00f5es' },
  { value: 'branding', label: 'Identidade visual' },
] as const;

function AdminNavigation({
  section,
  onSectionChange,
}: Readonly<{
  section: 'devices' | 'roles' | 'branding';
  onSectionChange: (section: 'devices' | 'roles' | 'branding') => void;
}>) {
  return (
    <nav className="admin-nav" aria-label={'Administra\u00e7\u00e3o'}>
      <SegmentedControl
        options={ADMIN_SECTIONS}
        value={section}
        onChange={(value) => onSectionChange(value as 'devices' | 'roles' | 'branding')}
      />
    </nav>
  );
}

function toMessage(reason: unknown): string {
  return reason instanceof Error
    ? reason.message
    : 'N\u00e3o foi poss\u00edvel carregar a administra\u00e7\u00e3o.';
}
