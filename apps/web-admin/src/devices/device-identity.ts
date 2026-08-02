export interface DeviceIdentity {
  readonly localId: string;
  readonly fingerprint: string;
  readonly deviceId: string | null;
  readonly deviceSecret: string | null;
}

export interface DeviceIdentityStorage {
  read(): Promise<DeviceIdentity | null>;
  write(value: DeviceIdentity): Promise<void>;
}

export interface DeviceIdentityGenerators {
  localId(): string;
  fingerprint(): Promise<string>;
}

export class DeviceIdentityVault {
  constructor(
    private readonly storage: DeviceIdentityStorage,
    private readonly generators: DeviceIdentityGenerators,
  ) {}

  async getOrCreate(): Promise<DeviceIdentity> {
    const existing = await this.storage.read();
    if (existing) return existing;
    const created: DeviceIdentity = {
      localId: this.generators.localId(),
      fingerprint: await this.generators.fingerprint(),
      deviceId: null,
      deviceSecret: null,
    };
    await this.storage.write(created);
    return created;
  }

  async savePairing(deviceId: string, deviceSecret: string): Promise<DeviceIdentity> {
    const current = await this.getOrCreate();
    const paired: DeviceIdentity = { ...current, deviceId, deviceSecret };
    await this.storage.write(paired);
    return paired;
  }
}

const DATABASE_NAME = 'platform-device-identity';
const STORE_NAME = 'identity';
const IDENTITY_KEY = 'current';

export function createBrowserDeviceIdentityVault(): DeviceIdentityVault {
  return new DeviceIdentityVault(new IndexedDbDeviceIdentityStorage(), {
    localId: () => crypto.randomUUID(),
    fingerprint: browserFingerprint,
  });
}

export class IndexedDbDeviceIdentityStorage implements DeviceIdentityStorage {
  async read(): Promise<DeviceIdentity | null> {
    const database = await openDatabase();
    return new Promise((resolve, reject) => {
      const request = database
        .transaction(STORE_NAME, 'readonly')
        .objectStore(STORE_NAME)
        .get(IDENTITY_KEY);
      request.onsuccess = () => resolve((request.result as DeviceIdentity | undefined) ?? null);
      request.onerror = () =>
        reject(request.error ?? new Error('Falha ao ler identificação do dispositivo'));
    });
  }

  async write(value: DeviceIdentity): Promise<void> {
    const database = await openDatabase();
    return new Promise((resolve, reject) => {
      const transaction = database.transaction(STORE_NAME, 'readwrite');
      transaction.objectStore(STORE_NAME).put(value, IDENTITY_KEY);
      transaction.oncomplete = () => resolve();
      transaction.onerror = () =>
        reject(transaction.error ?? new Error('Falha ao salvar identificação do dispositivo'));
      transaction.onabort = () =>
        reject(transaction.error ?? new Error('Gravação da identificação cancelada'));
    });
  }
}

async function browserFingerprint(): Promise<string> {
  const material = [
    navigator.userAgent,
    navigator.language,
    navigator.platform,
    `${screen.width}x${screen.height}x${screen.colorDepth}`,
    Intl.DateTimeFormat().resolvedOptions().timeZone,
  ].join('|');
  const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(material));
  return Array.from(new Uint8Array(digest), (byte) => byte.toString(16).padStart(2, '0')).join('');
}

function openDatabase(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(DATABASE_NAME, 1);
    request.onupgradeneeded = () => {
      if (!request.result.objectStoreNames.contains(STORE_NAME))
        request.result.createObjectStore(STORE_NAME);
    };
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error ?? new Error('IndexedDB indisponível'));
  });
}
