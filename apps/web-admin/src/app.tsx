import { useEffect, useMemo, useState } from 'react';
import {
  Button,
  CloudLoginScreen,
  CreatedByFooter,
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
import type { AreaDto, DeviceDto, PermissionCatalogItem, RoleDto, TableDto } from '@nexora/contracts';
import { BrandingContainer } from './branding/branding-container.js';
import { DeviceManagementPage } from './devices/device-management-page.js';
import { DevicesApi } from './devices/devices-api.js';
import { RoleManagementPage } from './roles/role-management-page.js';
import { RolesApi } from './roles/roles-api.js';
import { TableManagementPage } from './tables/table-management-page.js';
import { AreasApi, TablesApi } from './tables/tables-api.js';
import './app.css';

interface DeviceIdentity {
  readonly deviceId: string;
  readonly deviceSecret: string;
}

const IS_LOCAL_DEV =
  globalThis.location?.hostname === 'localhost' || globalThis.location?.hostname === '127.0.0.1';
const CLOUD_API_BASE_URL = IS_LOCAL_DEV ? '/cloud' : '';
const EDGE_API_BASE_URL = IS_LOCAL_DEV ? '/edge' : '';

const cloudDevicesApi = new DevicesApi(CLOUD_API_BASE_URL);
const cloudRolesApi = new RolesApi(CLOUD_API_BASE_URL);
const cloudAreasApi = new AreasApi(CLOUD_API_BASE_URL);
const cloudTablesApi = new TablesApi(CLOUD_API_BASE_URL);

/** Dispara o download do PDF de QR Codes (US-020, cenário "Exportação para impressão"). */
function downloadPdfBlob(blob: Blob, areaId?: string): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = areaId ? `qr-codes-mesas-${areaId}.pdf` : 'qr-codes-mesas.pdf';
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
}

export function isLocalEdgeAdminPath(pathname: string): boolean {
  return /^\/admin(?:\/|$)/.test(pathname);
}

export function App() {
  const local = isLocalEdgeAdminPath(globalThis.location?.pathname ?? '/');
  return <ThemeProvider>{local ? <LocalAdmin /> : <CloudAdmin />}</ThemeProvider>;
}

function CloudAdmin() {
  const [authenticated, setAuthenticated] = useState(() => hasCloudSession());
  const [section, setSection] = useState<CloudAdminSection>('devices');
  const [devices, setDevices] = useState<readonly DeviceDto[]>([]);
  const [roles, setRoles] = useState<readonly RoleDto[]>([]);
  const [permissionCatalog, setPermissionCatalog] = useState<readonly PermissionCatalogItem[]>([]);
  const [areas, setAreas] = useState<readonly AreaDto[]>([]);
  const [tables, setTables] = useState<readonly TableDto[]>([]);
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
    cloudAreasApi
      .list()
      .then((result) => {
        if (active) setAreas(result.items);
      })
      .catch((reason: unknown) => {
        if (active) setError(toMessage(reason));
      });
    cloudTablesApi
      .list()
      .then((result) => {
        if (active) setTables(result.items);
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

  async function refreshAreas() {
    setAreas((await cloudAreasApi.list()).items);
  }

  async function refreshTables() {
    setTables((await cloudTablesApi.list()).items);
  }

  if (!authenticated)
    return (
      <CloudLoginScreen
        baseUrl={CLOUD_API_BASE_URL}
        onAuthenticated={() => setAuthenticated(true)}
      />
    );

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
      {section === 'tables' ? (
        <TableManagementPage
          areas={areas}
          tables={tables}
          onCreateArea={async (name) => {
            await cloudAreasApi.create({ name, position: areas.length });
            await refreshAreas();
          }}
          onDeactivateArea={async (id) => {
            await cloudAreasApi.deactivate(id);
            await refreshAreas();
          }}
          onActivateArea={async (id) => {
            await cloudAreasApi.activate(id);
            await refreshAreas();
          }}
          onDeleteArea={async (id) => {
            await cloudAreasApi.remove(id);
            await refreshAreas();
          }}
          onCreateTable={async (input) => {
            await cloudTablesApi.create(input);
            await refreshTables();
          }}
          onCreateTablesBulk={async (input) => {
            await cloudTablesApi.createBulk(input);
            await refreshTables();
            await refreshAreas();
          }}
          onRotateToken={async (id) => {
            await cloudTablesApi.rotateQrToken(id);
          }}
          onDeactivateTable={async (id) => {
            await cloudTablesApi.deactivate(id);
            await refreshTables();
          }}
          onActivateTable={async (id) => {
            await cloudTablesApi.activate(id);
            await refreshTables();
          }}
          onDeleteTable={async (id) => {
            await cloudTablesApi.remove(id);
            await refreshTables();
            await refreshAreas();
          }}
          onExportQrCodesPdf={async (areaId) => {
            const blob = await cloudTablesApi.exportQrCodesPdf(areaId);
            downloadPdfBlob(blob, areaId);
          }}
        />
      ) : null}
      {section === 'branding' ? <BrandingContainer baseUrl={CLOUD_API_BASE_URL} /> : null}
      <CreatedByFooter />
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
    () => (device ? new OperationalAuthClient({ baseUrl: EDGE_API_BASE_URL, ...device }) : undefined),
    [device],
  );
  const devicesApi = useMemo(() => {
    if (!device || !session) return undefined;
    return new DevicesApi(EDGE_API_BASE_URL, (input, init) =>
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
        <span className="nx-spinner" aria-hidden="true" />
        Preparando administra&ccedil;&atilde;o local&hellip;
      </p>
    );
  }
  if (device === null) {
    return (
      <DevicePairingScreen
        kind="SUPPORT_TABLET"
        defaultLabel={'Gest\u00e3o local'}
        baseUrl={EDGE_API_BASE_URL}
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
      <CreatedByFooter />
    </>
  );
}

export type CloudAdminSection = 'devices' | 'roles' | 'tables' | 'branding';

const ADMIN_SECTIONS = [
  { value: 'devices', label: 'Dispositivos' },
  { value: 'roles', label: 'Pap\u00e9is e permiss\u00f5es' },
  { value: 'tables', label: 'Ambientes e mesas' },
  { value: 'branding', label: 'Identidade visual' },
] as const;

function AdminNavigation({
  section,
  onSectionChange,
}: Readonly<{
  section: CloudAdminSection;
  onSectionChange: (section: CloudAdminSection) => void;
}>) {
  return (
    <nav className="admin-nav" aria-label={'Administra\u00e7\u00e3o'}>
      <SegmentedControl
        options={ADMIN_SECTIONS}
        value={section}
        onChange={(value) => onSectionChange(value as CloudAdminSection)}
      />
    </nav>
  );
}

function toMessage(reason: unknown): string {
  return reason instanceof Error
    ? reason.message
    : 'N\u00e3o foi poss\u00edvel carregar a administra\u00e7\u00e3o.';
}
