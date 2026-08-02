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
import type {
  CategoryDto,
  DeviceDto,
  ModifierGroup,
  PermissionCatalogItem,
  ProductDto,
  RoleDto,
  StationDto,
} from '@nexora/contracts';
import { UnavailableListPage } from './availability/unavailable-list-page.js';
import { BrandingContainer } from './branding/branding-container.js';
import { CatalogPage } from './catalog/catalog-page.js';
import { CategoriesApi } from './catalog/categories-api.js';
import { PricesApi } from './catalog/prices-api.js';
import { ProductsApi } from './catalog/products-api.js';
import { VariantsApi } from './catalog/variants-api.js';
import { DeviceManagementPage } from './devices/device-management-page.js';
import { DevicesApi } from './devices/devices-api.js';
import { ModifierGroupManagementPage } from './modifiers/modifier-group-management-page.js';
import { ModifierGroupsApi } from './modifiers/modifier-groups-api.js';
import { PrepTimeSection } from './prep-time/prep-time-section.js';
import { PrepTimeApi } from './prep-time/prep-time-api.js';
import { PricingSection } from './pricing/pricing-section.js';
import { PricingApi } from './pricing/pricing-api.js';
import { RoleManagementPage } from './roles/role-management-page.js';
import { RolesApi } from './roles/roles-api.js';
import { StationManagementPage } from './stations/station-management-page.js';
import { StationsApi } from './stations/stations-api.js';
import './app.css';

interface DeviceIdentity {
  readonly deviceId: string;
  readonly deviceSecret: string;
}

const cloudDevicesApi = new DevicesApi();
const cloudRolesApi = new RolesApi();
const cloudStationsApi = new StationsApi();
const cloudCategoriesApi = new CategoriesApi();
const cloudProductsApi = new ProductsApi();
const cloudVariantsApi = new VariantsApi();
const cloudPricesApi = new PricesApi();
const cloudModifierGroupsApi = new ModifierGroupsApi();
const cloudPricingApi = new PricingApi();
const cloudPrepTimeApi = new PrepTimeApi();

export function isLocalEdgeAdminPath(pathname: string): boolean {
  return /^\/admin(?:\/|$)/.test(pathname);
}

export function App() {
  const local = isLocalEdgeAdminPath(globalThis.location?.pathname ?? '/');
  return <ThemeProvider>{local ? <LocalAdmin /> : <CloudAdmin />}</ThemeProvider>;
}

function CloudAdmin() {
  const [authenticated, setAuthenticated] = useState(() => hasCloudSession());
  const [section, setSection] = useState<AdminSection>('devices');
  const [devices, setDevices] = useState<readonly DeviceDto[]>([]);
  const [roles, setRoles] = useState<readonly RoleDto[]>([]);
  const [permissionCatalog, setPermissionCatalog] = useState<readonly PermissionCatalogItem[]>([]);
  const [stations, setStations] = useState<readonly StationDto[]>([]);
  const [categories, setCategories] = useState<readonly CategoryDto[]>([]);
  const [products, setProducts] = useState<readonly ProductDto[]>([]);
  const [modifierGroups, setModifierGroups] = useState<readonly ModifierGroup[]>([]);
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
    cloudStationsApi
      .list()
      .then((result) => {
        if (active) setStations(result.items);
      })
      .catch((reason: unknown) => {
        if (active) setError(toMessage(reason));
      });
    cloudCategoriesApi
      .list()
      .then((result) => {
        if (active) setCategories(result.items);
      })
      .catch((reason: unknown) => {
        if (active) setError(toMessage(reason));
      });
    cloudProductsApi
      .list()
      .then((result) => {
        if (active) setProducts(result.items);
      })
      .catch((reason: unknown) => {
        if (active) setError(toMessage(reason));
      });
    cloudModifierGroupsApi
      .list()
      .then((result) => {
        if (active) setModifierGroups(result.items);
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

  async function refreshStations() {
    setStations((await cloudStationsApi.list()).items);
  }

  async function refreshCategories() {
    setCategories((await cloudCategoriesApi.list()).items);
  }

  async function refreshProducts() {
    setProducts((await cloudProductsApi.list()).items);
  }

  async function refreshModifierGroups() {
    setModifierGroups((await cloudModifierGroupsApi.list()).items);
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
            const updated = await cloudModifierGroupsApi.updateModifierPrice(groupId, modifierId, {
              priceDelta,
            });
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
      {section === 'pricing' ? (
        <PricingSection
          categories={categories}
          products={products}
          pricingApi={cloudPricingApi}
          variantsApi={cloudVariantsApi}
        />
      ) : null}
      {section === 'availability' ? <UnavailableListPage /> : null}
      {section === 'prep-time' ? (
        <PrepTimeSection
          products={products}
          stations={stations}
          prepTimeApi={cloudPrepTimeApi}
          variantsApi={cloudVariantsApi}
        />
      ) : null}
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
  { value: 'stations', label: 'Pra\u00e7as de produ\u00e7\u00e3o' },
  { value: 'catalog', label: 'Card\u00e1pio' },
  { value: 'modifiers', label: 'Grupos de modificadores' },
  { value: 'pricing', label: 'Pre\u00e7os' },
  { value: 'availability', label: 'Indispon\u00edveis' },
  { value: 'prep-time', label: 'Tempo e praça' },
] as const;

type AdminSection =
  | 'devices'
  | 'roles'
  | 'branding'
  | 'stations'
  | 'catalog'
  | 'modifiers'
  | 'pricing'
  | 'availability'
  | 'prep-time';

function AdminNavigation({
  section,
  onSectionChange,
}: Readonly<{
  section: AdminSection;
  onSectionChange: (section: AdminSection) => void;
}>) {
  return (
    <nav className="admin-nav" aria-label={'Administra\u00e7\u00e3o'}>
      <SegmentedControl
        options={ADMIN_SECTIONS}
        value={section}
        onChange={(value) => onSectionChange(value as AdminSection)}
      />
    </nav>
  );
}

function toMessage(reason: unknown): string {
  return reason instanceof Error
    ? reason.message
    : 'N\u00e3o foi poss\u00edvel carregar a administra\u00e7\u00e3o.';
}
