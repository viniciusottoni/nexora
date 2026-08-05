// @vitest-environment jsdom
import { act, renderHook } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { DevicePreferencesApi } from './device-preferences-api.js';
import { usePeakMode } from './use-peak-mode.js';

const identity = { accessToken: 'token-abc', deviceId: 'device-1', deviceSecret: 'secret-1' };
const THRESHOLD = 20;
const HYSTERESIS = 5;

function fakeApi(overrides: Partial<DevicePreferencesApi> = {}): DevicePreferencesApi {
  return {
    updateKdsPreferences: vi.fn().mockResolvedValue({ deviceId: identity.deviceId, preferences: { kds: {} } }),
    ...overrides,
  } as unknown as DevicePreferencesApi;
}

describe('usePeakMode (US-047)', () => {
  it('começa inativo com fila pequena', () => {
    const { result } = renderHook(() =>
      usePeakMode({ orderCount: 5, identity, api: fakeApi(), thresholds: { thresholdItems: THRESHOLD, hysteresisItems: HYSTERESIS } }),
    );
    expect(result.current.active).toBe(false);
  });

  it('ativa automaticamente ao atingir o limiar (Cenário "Ativação automática")', () => {
    const { result, rerender } = renderHook(
      ({ orderCount }) =>
        usePeakMode({ orderCount, identity, api: fakeApi(), thresholds: { thresholdItems: THRESHOLD, hysteresisItems: HYSTERESIS } }),
      { initialProps: { orderCount: 19 } },
    );
    expect(result.current.active).toBe(false);

    rerender({ orderCount: 20 });
    expect(result.current.active).toBe(true);
  });

  it('não oscila ao cair para dentro da faixa de histerese (Cenário "Desativação automática")', () => {
    const { result, rerender } = renderHook(
      ({ orderCount }) =>
        usePeakMode({ orderCount, identity, api: fakeApi(), thresholds: { thresholdItems: THRESHOLD, hysteresisItems: HYSTERESIS } }),
      { initialProps: { orderCount: 20 } },
    );
    expect(result.current.active).toBe(true);

    // Cai para 16 (dentro de 15–19, a faixa de histerese) — continua ativo.
    rerender({ orderCount: 16 });
    expect(result.current.active).toBe(true);

    rerender({ orderCount: 18 });
    expect(result.current.active).toBe(true);

    // Só desativa ao cruzar para baixo de 15.
    rerender({ orderCount: 14 });
    expect(result.current.active).toBe(false);
  });

  it('desativação manual tem prioridade sobre o automático e persiste no dispositivo (Cenário "Sobreposição manual")', () => {
    const api = fakeApi();
    const { result, rerender } = renderHook(
      ({ orderCount }) =>
        usePeakMode({ orderCount, identity, api, thresholds: { thresholdItems: THRESHOLD, hysteresisItems: HYSTERESIS } }),
      { initialProps: { orderCount: 25 } },
    );
    expect(result.current.active).toBe(true);

    act(() => result.current.toggle());
    rerender({ orderCount: 25 });
    expect(result.current.active).toBe(false);
    expect(result.current.manuallyDisabled).toBe(true);
    expect(api.updateKdsPreferences).toHaveBeenCalledWith(
      identity,
      expect.objectContaining({ peakMode: expect.objectContaining({ manuallyDisabled: true }) }),
    );

    // A fila continua grande (30) — permanece desativado, o automático não retoma o controle.
    rerender({ orderCount: 30 });
    expect(result.current.active).toBe(false);

    // A fila esvazia e enche de novo — ainda assim, permanece desativado (a decisão manual venceu).
    rerender({ orderCount: 2 });
    rerender({ orderCount: 40 });
    expect(result.current.active).toBe(false);
  });

  it('ativação manual liga o modo mesmo com a fila abaixo do limiar', () => {
    const api = fakeApi();
    const { result, rerender } = renderHook(
      ({ orderCount }) =>
        usePeakMode({ orderCount, identity, api, thresholds: { thresholdItems: THRESHOLD, hysteresisItems: HYSTERESIS } }),
      { initialProps: { orderCount: 3 } },
    );
    expect(result.current.active).toBe(false);

    act(() => result.current.toggle());
    rerender({ orderCount: 3 });
    expect(result.current.active).toBe(true);
    expect(result.current.manuallyDisabled).toBe(false);
    expect(api.updateKdsPreferences).toHaveBeenCalledWith(
      identity,
      expect.objectContaining({ peakMode: expect.objectContaining({ manuallyDisabled: false }) }),
    );
  });

  it('não persiste preferência (mas continua funcionando localmente) sem identidade — pré-login', () => {
    const api = fakeApi();
    const { result } = renderHook(() =>
      usePeakMode({ orderCount: 25, identity: undefined, api, thresholds: { thresholdItems: THRESHOLD, hysteresisItems: HYSTERESIS } }),
    );
    expect(result.current.active).toBe(true);

    act(() => result.current.toggle());
    expect(api.updateKdsPreferences).not.toHaveBeenCalled();
  });

  it('não quebra quando a persistência falha (US-047 §9: não depende de rede)', () => {
    const api = fakeApi({ updateKdsPreferences: vi.fn().mockRejectedValue(new Error('offline')) });
    const { result } = renderHook(() =>
      usePeakMode({ orderCount: 25, identity, api, thresholds: { thresholdItems: THRESHOLD, hysteresisItems: HYSTERESIS } }),
    );

    expect(() => act(() => result.current.toggle())).not.toThrow();
    expect(result.current.active).toBe(false);
  });

  it('usa os limiares padrão do contrato (20/5) quando nenhum é informado', () => {
    const { result } = renderHook(() => usePeakMode({ orderCount: 20, identity, api: fakeApi() }));
    expect(result.current.thresholdItems).toBe(20);
    expect(result.current.hysteresisItems).toBe(5);
    expect(result.current.active).toBe(true);
  });
});
