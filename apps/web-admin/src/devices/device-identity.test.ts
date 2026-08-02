import { describe, expect, it, vi } from 'vitest';
import { DeviceIdentityVault, type DeviceIdentity } from './device-identity.js';

describe('DeviceIdentityVault', () => {
  it('mantém a mesma identificação persistente entre aberturas', async () => {
    let stored: DeviceIdentity | null = null;
    const storage = {
      read: vi.fn(async () => stored),
      write: vi.fn(async (value: DeviceIdentity) => {
        stored = value;
      }),
    };
    const vault = new DeviceIdentityVault(storage, {
      localId: () => 'local-id',
      fingerprint: async () => 'browser-fingerprint',
    });

    const first = await vault.getOrCreate();
    const second = await vault.getOrCreate();

    expect(first).toEqual({
      localId: 'local-id',
      fingerprint: 'browser-fingerprint',
      deviceId: null,
      deviceSecret: null,
    });
    expect(second).toEqual(first);
    expect(storage.write).toHaveBeenCalledTimes(1);
  });

  it('guarda identificador e segredo recebidos no pareamento', async () => {
    let stored: DeviceIdentity | null = {
      localId: 'local-id',
      fingerprint: 'browser-fingerprint',
      deviceId: null,
      deviceSecret: null,
    };
    const vault = new DeviceIdentityVault(
      {
        read: async () => stored,
        write: async (value) => {
          stored = value;
        },
      },
      { localId: () => 'unused', fingerprint: async () => 'unused' },
    );

    await vault.savePairing('device-id', 'secret-once');

    expect(stored).toEqual(
      expect.objectContaining({ deviceId: 'device-id', deviceSecret: 'secret-once' }),
    );
  });
});
