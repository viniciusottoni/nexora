import { useEffect, useMemo, useState } from 'react';
import {
  AlertBanner,
  BrandMark,
  Button,
  clearCloudSession,
  CloudLoginScreen,
  CreatedByFooter,
  DevicePairingScreen,
  hasCloudSession,
  Icon,
  operationalAuthenticatedFetch,
  OperationalAuthClient,
  OperationalAuthError,
  PinScreen,
  readRegisteredDeviceIdentity,
  SideNav,
  ThemeProvider,
  TopBar,
  type OperationalSession,
  type SideNavItem,
} from '@nexora/ui';
import type {
  AreaDto,
  CategoryDto,
  DeviceDto,
  ModifierGroup,
  PermissionCatalogItem,
  ProductDto,
  RoleDto,
  StationDto,
  TableDto,
} from '@nexora/contracts';
import { AlertRoutingApi } from './alerts/alert-routing-api.js';
import { AlertRoutingPage } from './alerts/alert-routing-page.js';
import { NotificationBell } from './alerts/notification-bell.js';
import { NotificationsApi } from './alerts/notifications-api.js';
import { ThresholdConfigPage } from './alerts/threshold-config-page.js';
import { ThresholdsApi } from './alerts/thresholds-api.js';
import { AuditApi } from './audit/audit-api.js';
import { AuditLogPage } from './audit/audit-log-page.js';
import { BrandingContainer } from './branding/branding-container.js';
import { UnavailableListPage } from './availability/unavailable-list-page.js';
import { CatalogPage } from './catalog/catalog-page.js';
import { CategoriesApi } from './catalog/categories-api.js';
import { PricesApi } from './catalog/prices-api.js';
import { ProductsApi } from './catalog/products-api.js';
import { VariantsApi } from './catalog/variants-api.js';
import { DeviceManagementPage } from './devices/device-management-page.js';
import { DevicesApi, DevicesApiError } from './devices/devices-api.js';
import { ModifierGroupManagementPage } from './modifiers/modifier-group-management-page.js';
import { ModifierGroupsApi } from './modifiers/modifier-groups-api.js';
import { PrepTimeApi } from './prep-time/prep-time-api.js';
import { PrepTimeSection } from './prep-time/prep-time-section.js';
import { PricingApi } from './pricing/pricing-api.js';
import { PricingSection } from './pricing/pricing-section.js';
import { RoleManagementPage } from './roles/role-management-page.js';
import { RolesApi } from './roles/roles-api.js';
import { StationManagementPage } from './stations/station-management-page.js';
import { StationsApi } from './stations/stations-api.js';
import { TableManagementPage } from './tables/table-management-page.js';
import { AreasApi, TablesApi } from './tables/tables-api.js';

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
const cloudCategoriesApi = new CategoriesApi(CLOUD_API_BASE_URL);
const cloudProductsApi = new ProductsApi(CLOUD_API_BASE_URL);
const cloudVariantsApi = new VariantsApi(CLOUD_API_BASE_URL);
const cloudPricesApi = new PricesApi(CLOUD_API_BASE_URL);
const cloudStationsApi = new StationsApi(CLOUD_API_BASE_URL);
const cloudModifierGroupsApi = new ModifierGroupsApi(CLOUD_API_BASE_URL);
const cloudPrepTimeApi = new PrepTimeApi(CLOUD_API_BASE_URL);
const cloudPricingApi = new PricingApi(CLOUD_API_BASE_URL);
const cloudAuditApi = new AuditApi(CLOUD_API_BASE_URL);
const cloudThresholdsApi = new ThresholdsApi(CLOUD_API_BASE_URL);
const cloudAlertRoutingApi = new AlertRoutingApi(CLOUD_API_BASE_URL);
const cloudNotificationsApi = new NotificationsApi(CLOUD_API_BASE_URL);

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

/** Recurso carregado no boot do CloudAdmin — chave usada para escopar o erro à(s) seção(ões) que o exibem. */
type BootResource =
  | 'devices'
  | 'roles'
  | 'areas'
  | 'tables'
  | 'categories'
  | 'products'
  | 'stations'
  | 'modifierGroups';

/** Em quais seções cada recurso aparece — determina onde seu erro de carregamento deve ser mostrado. */
const RESOURCE_SECTIONS: Record<BootResource, readonly CloudAdminSection[]> = {
  devices: ['devices'],
  roles: ['roles'],
  areas: ['tables'],
  tables: ['tables'],
  categories: ['catalog', 'pricing'],
  products: ['catalog', 'prep-time', 'pricing'],
  stations: ['catalog', 'stations', 'prep-time'],
  modifierGroups: ['modifiers'],
};

function CloudAdmin() {
  const [authenticated, setAuthenticated] = useState(() => hasCloudSession());
  const [section, setSection] = useState<CloudAdminSection>('devices');
  const [devices, setDevices] = useState<readonly DeviceDto[]>([]);
  const [roles, setRoles] = useState<readonly RoleDto[]>([]);
  const [permissionCatalog, setPermissionCatalog] = useState<readonly PermissionCatalogItem[]>([]);
  const [areas, setAreas] = useState<readonly AreaDto[]>([]);
  const [tables, setTables] = useState<readonly TableDto[]>([]);
  const [categories, setCategories] = useState<readonly CategoryDto[]>([]);
  const [products, setProducts] = useState<readonly ProductDto[]>([]);
  const [stations, setStations] = useState<readonly StationDto[]>([]);
  const [modifierGroups, setModifierGroups] = useState<readonly ModifierGroup[]>([]);
  const [bootErrors, setBootErrors] = useState<Partial<Record<BootResource, string>>>({});

  function setBootError(resource: BootResource, message: string) {
    setBootErrors((prev) => ({ ...prev, [resource]: message }));
  }

  function clearBootError(resource: BootResource) {
    setBootErrors((prev) =>
      prev[resource] === undefined ? prev : { ...prev, [resource]: undefined },
    );
  }

  const activeBootError = (
    Object.entries(bootErrors) as Array<[BootResource, string | undefined]>
  ).find(([resource, message]) => message && RESOURCE_SECTIONS[resource].includes(section))?.[1];

  useEffect(() => {
    if (!authenticated) return;
    let active = true;
    cloudDevicesApi
      .list()
      .then((result) => {
        if (active) setDevices(result.items);
      })
      .catch((reason: unknown) => {
        if (active) setBootError('devices', toMessage(reason));
      });
    cloudRolesApi
      .list()
      .then((result) => {
        if (!active) return;
        setRoles(result.items);
        setPermissionCatalog(result.permissionCatalog);
      })
      .catch((reason: unknown) => {
        if (active) setBootError('roles', toMessage(reason));
      });
    cloudAreasApi
      .list()
      .then((result) => {
        if (active) setAreas(result.items);
      })
      .catch((reason: unknown) => {
        if (active) setBootError('areas', toMessage(reason));
      });
    cloudTablesApi
      .list()
      .then((result) => {
        if (active) setTables(result.items);
      })
      .catch((reason: unknown) => {
        if (active) setBootError('tables', toMessage(reason));
      });
    cloudCategoriesApi
      .list()
      .then((result) => {
        if (active) setCategories(result.items);
      })
      .catch((reason: unknown) => {
        if (active) setBootError('categories', toMessage(reason));
      });
    cloudProductsApi
      .list()
      .then((result) => {
        if (active) setProducts(result.items);
      })
      .catch((reason: unknown) => {
        if (active) setBootError('products', toMessage(reason));
      });
    cloudStationsApi
      .list()
      .then((result) => {
        if (active) setStations(result.items);
      })
      .catch((reason: unknown) => {
        if (active) setBootError('stations', toMessage(reason));
      });
    cloudModifierGroupsApi
      .list()
      .then((result) => {
        if (active) setModifierGroups(result.items);
      })
      .catch((reason: unknown) => {
        if (active) setBootError('modifierGroups', toMessage(reason));
      });
    return () => {
      active = false;
    };
  }, [authenticated]);

  async function refreshDevices() {
    setDevices((await cloudDevicesApi.list()).items);
    clearBootError('devices');
  }

  async function refreshRoles() {
    const result = await cloudRolesApi.list();
    setRoles(result.items);
    setPermissionCatalog(result.permissionCatalog);
    clearBootError('roles');
  }

  async function refreshAreas() {
    setAreas((await cloudAreasApi.list()).items);
    clearBootError('areas');
  }

  async function refreshTables() {
    setTables((await cloudTablesApi.list()).items);
    clearBootError('tables');
  }

  async function refreshCategories() {
    setCategories((await cloudCategoriesApi.list()).items);
    clearBootError('categories');
  }

  async function refreshProducts() {
    setProducts((await cloudProductsApi.list()).items);
    clearBootError('products');
  }

  async function refreshStations() {
    setStations((await cloudStationsApi.list()).items);
    clearBootError('stations');
  }

  async function refreshModifierGroups() {
    setModifierGroups((await cloudModifierGroupsApi.list()).items);
    clearBootError('modifierGroups');
  }

  if (!authenticated)
    return (
      <CloudLoginScreen
        baseUrl={CLOUD_API_BASE_URL}
        onAuthenticated={() => setAuthenticated(true)}
      />
    );

  return (
    <div className="db-console">
      <SideNav
        variant="dark"
        brand={<BrandMark inverse subtitle="Administração" />}
        items={ADMIN_NAV_ITEMS}
        activeId={section}
        onSelect={(id) => setSection(id as CloudAdminSection)}
        footer={<span className="db-sidenav__foot-note">Nexora · configuração da loja</span>}
      />
      <div className="db-console__main">
        <TopBar
          left={
            <nav className="db-crumbs" aria-label="Trilha de navegação">
              <span>Administração</span>
              <Icon name="chevron_right" size={16} />
              <span className="db-crumbs__current">{sectionLabel(section)}</span>
            </nav>
          }
          right={
            <div className="admin-account">
              <NotificationBell notificationsApi={cloudNotificationsApi} />
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => {
                  clearCloudSession();
                  setAuthenticated(false);
                }}
              >
                Sair
              </Button>
            </div>
          }
        />
        <div className="db-console__content">
          <div className="db-console__column">
            {activeBootError ? (
              <AlertBanner tone="danger" title="Falha ao carregar dados">
                {activeBootError}
              </AlertBanner>
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
                onDelete={async (id) => {
                  await cloudDevicesApi.remove(id);
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
            {section === 'catalog' ? (
              <CatalogPage
                categories={categories}
                products={products}
                stations={stations}
                onCreateCategory={async (input) => {
                  const created = await cloudCategoriesApi.create(input);
                  await refreshCategories();
                  return created;
                }}
                onUpdateCategory={async (id, input) => {
                  const updated = await cloudCategoriesApi.update(id, input);
                  await refreshCategories();
                  return updated;
                }}
                onReorderCategories={async (order) => {
                  await cloudCategoriesApi.reorder({ order: [...order] });
                  await refreshCategories();
                }}
                onDeactivateCategory={async (id) => {
                  await cloudCategoriesApi.deactivate(id);
                  await refreshCategories();
                }}
                onCreateProduct={async (input) => {
                  const created = await cloudProductsApi.create(input);
                  await refreshProducts();
                  return created;
                }}
                onUpdateProduct={async (id, input) => {
                  const updated = await cloudProductsApi.update(id, input);
                  await refreshProducts();
                  return updated;
                }}
                onReorderProducts={async (categoryId, order) => {
                  await cloudProductsApi.reorder({ categoryId, order: [...order] });
                  await refreshProducts();
                }}
                onActivateProduct={async (id) => {
                  const updated = await cloudProductsApi.activate(id);
                  await refreshProducts();
                  return updated;
                }}
                onDeactivateProduct={async (id) => {
                  const updated = await cloudProductsApi.deactivate(id);
                  await refreshProducts();
                  return updated;
                }}
                onUploadProductImage={async (productId, blob, contentType, dimensions) => {
                  await cloudProductsApi.uploadImage(productId, blob, contentType, dimensions);
                  await refreshProducts();
                }}
                onLoadVariants={(productId) => cloudVariantsApi.listForProduct(productId)}
                onCreateVariant={(productId, input) => cloudVariantsApi.create(productId, input)}
                onUpdateVariant={(id, input) => cloudVariantsApi.update(id, input)}
                onSetVariantPrice={(id, input) => cloudPricesApi.setVariantPrice(id, input)}
                onActivateVariant={(id) => cloudVariantsApi.activate(id)}
                onDeactivateVariant={(id) => cloudVariantsApi.deactivate(id)}
                onMarkVariantDefault={(id) => cloudVariantsApi.markAsDefault(id)}
              />
            ) : null}
            {section === 'availability' ? <UnavailableListPage /> : null}
            {section === 'modifiers' ? (
              <ModifierGroupManagementPage
                groups={modifierGroups}
                onCreateGroup={async (input) => {
                  const created = await cloudModifierGroupsApi.createGroup(input);
                  await refreshModifierGroups();
                  return created;
                }}
                onUpdateGroup={async (groupId, minSelect, maxSelect) => {
                  const updated = await cloudModifierGroupsApi.updateGroup(groupId, {
                    minSelect,
                    maxSelect,
                  });
                  await refreshModifierGroups();
                  return updated;
                }}
                onDeleteGroup={async (groupId) => {
                  await cloudModifierGroupsApi.deleteGroup(groupId);
                  await refreshModifierGroups();
                }}
                onCreateModifier={async (groupId, input) => {
                  const created = await cloudModifierGroupsApi.createModifier(groupId, input);
                  await refreshModifierGroups();
                  return created;
                }}
                onUpdateModifierPrice={async (groupId, modifierId, priceDelta) => {
                  const updated = await cloudModifierGroupsApi.updateModifierPrice(
                    groupId,
                    modifierId,
                    {
                      priceDelta,
                    },
                  );
                  await refreshModifierGroups();
                  return updated;
                }}
                onSetModifierAvailability={async (groupId, modifierId, isAvailable) => {
                  const updated = await cloudModifierGroupsApi.setModifierAvailability(
                    groupId,
                    modifierId,
                    { isAvailable },
                  );
                  await refreshModifierGroups();
                  return updated;
                }}
                onLinkToProduct={async (productId, groupId) => {
                  await cloudModifierGroupsApi.linkToProduct(productId, { groupId, sortOrder: 0 });
                  await refreshModifierGroups();
                }}
                onUnlinkFromProduct={async (productId, groupId) => {
                  await cloudModifierGroupsApi.unlinkFromProduct(productId, groupId);
                  await refreshModifierGroups();
                }}
              />
            ) : null}
            {section === 'stations' ? (
              <StationManagementPage
                stations={stations}
                onCreate={async (input) => {
                  const created = await cloudStationsApi.create(input);
                  await refreshStations();
                  return created;
                }}
                onUpdate={async (id, input) => {
                  const updated = await cloudStationsApi.update(id, input);
                  await refreshStations();
                  return updated;
                }}
                onDelete={async (id) => {
                  await cloudStationsApi.remove(id);
                  await refreshStations();
                }}
              />
            ) : null}
            {section === 'prep-time' ? (
              <PrepTimeSection
                products={products}
                stations={stations}
                prepTimeApi={cloudPrepTimeApi}
                variantsApi={cloudVariantsApi}
              />
            ) : null}
            {section === 'pricing' ? (
              <PricingSection
                categories={categories}
                products={products}
                pricingApi={cloudPricingApi}
                variantsApi={cloudVariantsApi}
              />
            ) : null}
            {section === 'audit' ? <AuditLogPage auditApi={cloudAuditApi} /> : null}
            {section === 'alert-thresholds' ? (
              <ThresholdConfigPage thresholdsApi={cloudThresholdsApi} />
            ) : null}
            {section === 'alert-routing' ? (
              <AlertRoutingPage roles={roles} alertRoutingApi={cloudAlertRoutingApi} />
            ) : null}
            <CreatedByFooter />
          </div>
        </div>
      </div>
    </div>
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
    () =>
      device ? new OperationalAuthClient({ baseUrl: EDGE_API_BASE_URL, ...device }) : undefined,
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
        if (!active) return;
        if (reason instanceof DevicesApiError && reason.status === 401) {
          setDevices([]);
          setSession(undefined);
          setError('Sua sess\u00e3o expirou. Informe o PIN novamente.');
          return;
        }
        setError(toMessage(reason));
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
      <p className="db-loading" role="status">
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
    <div className="db-console db-console--local">
      <div className="db-console__main">
        <TopBar
          left={
            <nav className="db-crumbs" aria-label="Trilha de navegação">
              <span>Painel local</span>
              <Icon name="chevron_right" size={16} />
              <span className="db-crumbs__current">Dispositivos da loja</span>
            </nav>
          }
          right={
            <Button type="button" variant="ghost" size="sm" onClick={() => setSession(undefined)}>
              Trocar gestor
            </Button>
          }
        />
        <div className="db-console__content">
          <div className="db-console__column">
            {error ? (
              <AlertBanner tone="danger" title="Falha ao carregar dispositivos">
                {error}
              </AlertBanner>
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
              onDelete={async (id) => {
                await devicesApi!.remove(id);
                await refreshDevices();
              }}
            />
            <CreatedByFooter />
          </div>
        </div>
      </div>
    </div>
  );
}

export type CloudAdminSection =
  | 'devices'
  | 'roles'
  | 'tables'
  | 'branding'
  | 'catalog'
  | 'availability'
  | 'modifiers'
  | 'stations'
  | 'prep-time'
  | 'pricing'
  | 'audit'
  | 'alert-thresholds'
  | 'alert-routing';

/* Agrupado por assunto \u2014 a navega\u00e7\u00e3o de dez itens corridos n\u00e3o dizia o que era opera\u00e7\u00e3o da
   loja, o que era card\u00e1pio e o que era cozinha (SideNav aceita `group` para isso). */
const ADMIN_NAV_ITEMS: readonly SideNavItem[] = [
  { group: 'Estabelecimento' },
  { id: 'devices', label: 'Dispositivos', icon: 'devices' },
  { id: 'roles', label: 'Pap\u00e9is e permiss\u00f5es', icon: 'shield_person' },
  { id: 'tables', label: 'Ambientes e mesas', icon: 'table_bar' },
  { id: 'branding', label: 'Identidade visual', icon: 'palette' },
  { group: 'Card\u00e1pio' },
  { id: 'catalog', label: 'Card\u00e1pio', icon: 'restaurant_menu' },
  { id: 'modifiers', label: 'Grupos de modificadores', icon: 'tune' },
  { id: 'pricing', label: 'Pre\u00e7os', icon: 'sell' },
  { id: 'availability', label: 'Indispon\u00edveis', icon: 'block' },
  { group: 'Cozinha' },
  { id: 'stations', label: 'Pra\u00e7as de produ\u00e7\u00e3o', icon: 'soup_kitchen' },
  { id: 'prep-time', label: 'Tempo e pra\u00e7a', icon: 'schedule' },
  { group: 'Alertas' },
  { id: 'alert-thresholds', label: 'Limiares de alerta', icon: 'rule' },
  { id: 'alert-routing', label: 'Direcionamento de alertas', icon: 'alt_route' },
  { group: 'Auditoria' },
  { id: 'audit', label: 'Trilha de auditoria', icon: 'fact_check' },
];

/** R\u00f3tulo da se\u00e7\u00e3o ativa para a trilha da barra superior \u2014 mesma fonte da navega\u00e7\u00e3o. */
function sectionLabel(section: CloudAdminSection): string {
  const item = ADMIN_NAV_ITEMS.find((candidate) => candidate.id === section);
  return typeof item?.label === 'string' ? item.label : 'Administra\u00e7\u00e3o';
}

function toMessage(reason: unknown): string {
  return reason instanceof Error
    ? reason.message
    : 'N\u00e3o foi poss\u00edvel carregar a administra\u00e7\u00e3o.';
}
