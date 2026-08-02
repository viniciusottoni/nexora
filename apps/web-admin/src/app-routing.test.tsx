// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';

const readDevice = vi.hoisted(() => vi.fn());
vi.mock('@nexora/ui', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@nexora/ui')>()),
  readRegisteredDeviceIdentity: readDevice,
}));

import { App, isLocalEdgeAdminPath } from './app.js';

afterEach(() => {
  localStorage.clear();
  readDevice.mockReset();
});

describe('isLocalEdgeAdminPath', () => {
  it('ativa autenticaÃ§Ã£o local somente sob /admin', () => {
    expect(isLocalEdgeAdminPath('/admin/')).toBe(true);
    expect(isLocalEdgeAdminPath('/admin/dispositivos')).toBe(true);
    expect(isLocalEdgeAdminPath('/')).toBe(false);
    expect(isLocalEdgeAdminPath('/platform/admin')).toBe(false);
  });
});

describe('App local', () => {
  it('solicita pareamento quando navegador ainda nÃ£o estÃ¡ registrado', async () => {
    window.history.pushState({}, '', '/admin/');
    readDevice.mockResolvedValueOnce(null);

    render(<App />);

    expect(
      await screen.findByRole('heading', { name: 'Autorizar dispositivo' }),
    ).toBeInTheDocument();
    expect(screen.getAllByRole('textbox')).toHaveLength(1);
  });

  it('solicita PIN quando dispositivo local jÃ¡ estÃ¡ registrado', async () => {
    window.history.pushState({}, '', '/admin/');
    readDevice.mockResolvedValueOnce({ deviceId: 'device-id', deviceSecret: 'device-secret' });

    render(<App />);

    expect(await screen.findByRole('heading', { name: /Quem.*operando/ })).toBeInTheDocument();
    expect(screen.getByText(/vinculada a este dispositivo/i)).toBeInTheDocument();
  });
});
