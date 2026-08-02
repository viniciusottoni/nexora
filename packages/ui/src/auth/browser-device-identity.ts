const DATABASE_NAME = 'platform-device-identity';
const STORE_NAME = 'identity';
const IDENTITY_KEY = 'current';

export async function readRegisteredDeviceId(): Promise<string | null> {
  return (await readRegisteredDeviceIdentity())?.deviceId ?? null;
}

export async function readRegisteredDeviceIdentity(): Promise<{
  deviceId: string;
  deviceSecret: string;
} | null> {
  if (!('indexedDB' in globalThis)) return null;
  const database = await openDatabase();
  return new Promise((resolve, reject) => {
    const request = database
      .transaction(STORE_NAME, 'readonly')
      .objectStore(STORE_NAME)
      .get(IDENTITY_KEY);
    request.onsuccess = () => {
      const identity = request.result as { deviceId?: unknown; deviceSecret?: unknown } | undefined;
      resolve(
        typeof identity?.deviceId === 'string' && typeof identity.deviceSecret === 'string'
          ? { deviceId: identity.deviceId, deviceSecret: identity.deviceSecret }
          : null,
      );
    };
    request.onerror = () =>
      reject(request.error ?? new Error('Falha ao ler dispositivo registrado.'));
  });
}

export async function saveRegisteredDeviceIdentity(identity: {
  deviceId: string;
  deviceSecret: string;
}): Promise<void> {
  if (!('indexedDB' in globalThis)) throw new Error('IndexedDB indisponível.');
  const database = await openDatabase();
  await new Promise<void>((resolve, reject) => {
    const request = database
      .transaction(STORE_NAME, 'readwrite')
      .objectStore(STORE_NAME)
      .put(identity, IDENTITY_KEY);
    request.onsuccess = () => resolve();
    request.onerror = () => reject(request.error ?? new Error('Falha ao salvar dispositivo.'));
  });
}

function openDatabase(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(DATABASE_NAME, 1);
    request.onupgradeneeded = () => {
      if (!request.result.objectStoreNames.contains(STORE_NAME))
        request.result.createObjectStore(STORE_NAME);
    };
    request.onsuccess = () => resolve(request.result);
    request.onerror = () =>
      reject(request.error ?? new Error('Identidade do dispositivo indisponível.'));
  });
}
