import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  configureAlertSound,
  playAlertChime,
  playLateAlertChime,
  previewAlertTone,
  resetAlertSoundConfigForTests,
  vibrateAlert,
  vibrateLateAlert,
} from './alert-sound.js';

/** Dublê mínimo de `AudioContext` — jsdom não implementa Web Audio (mesmo padrão do teste irmão em web-pos). */
function createFakeAudioContext() {
  const start = vi.fn();
  const stop = vi.fn();
  const connect = vi.fn().mockReturnThis();
  const setValueAtTime = vi.fn();
  const exponentialRampToValueAtTime = vi.fn();
  const oscillatorTypes: string[] = [];

  class FakeAudioContext {
    currentTime = 0;
    destination = {};
    createOscillator() {
      const oscillator = { frequency: { value: 0 }, type: '', connect, start, stop };
      // Registra o timbre só depois que `playTone` escreve `oscillator.type` — o teste lê no fim.
      queueMicrotask(() => oscillatorTypes.push(oscillator.type));
      return oscillator;
    }
    createGain() {
      return { gain: { setValueAtTime, exponentialRampToValueAtTime } };
    }
    close() {
      return Promise.resolve();
    }
  }

  return { FakeAudioContext, start, setValueAtTime, oscillatorTypes };
}

describe('alert-sound (US-045 §3.1/§7/§10)', () => {
  afterEach(() => {
    resetAlertSoundConfigForTests();
    vi.useRealTimers();
  });

  it('playAlertChime toca o timbre CHIME (dois pulsos)', () => {
    const { FakeAudioContext, start } = createFakeAudioContext();
    playAlertChime(FakeAudioContext as unknown as typeof AudioContext);
    expect(start).toHaveBeenCalledTimes(2);
  });

  it('playLateAlertChime toca o timbre ALERT (três pulsos), distinto do de pedido novo', () => {
    const { FakeAudioContext, start } = createFakeAudioContext();
    playLateAlertChime(FakeAudioContext as unknown as typeof AudioContext);
    expect(start).toHaveBeenCalledTimes(3);
  });

  it('agrupa em rajada: chamadas repetidas de playAlertChime dentro da janela tocam uma única vez', () => {
    const { FakeAudioContext, start } = createFakeAudioContext();
    for (let i = 0; i < 5; i += 1) {
      playAlertChime(FakeAudioContext as unknown as typeof AudioContext);
    }
    expect(start).toHaveBeenCalledTimes(2); // 1 chime = 2 osciladores, não 5×2
  });

  it('o agrupamento em rajada de pedido novo não suprime o alerta de atraso (janelas por timbre)', () => {
    const { FakeAudioContext, start } = createFakeAudioContext();
    playAlertChime(FakeAudioContext as unknown as typeof AudioContext);
    playLateAlertChime(FakeAudioContext as unknown as typeof AudioContext);
    expect(start).toHaveBeenCalledTimes(2 + 3);
  });

  it('depois da janela de rajada, um novo evento volta a tocar', () => {
    vi.useFakeTimers();
    const { FakeAudioContext, start } = createFakeAudioContext();
    playAlertChime(FakeAudioContext as unknown as typeof AudioContext);
    vi.advanceTimersByTime(2100);
    playAlertChime(FakeAudioContext as unknown as typeof AudioContext);
    expect(start).toHaveBeenCalledTimes(4); // 2 + 2
  });

  it('modo silencioso (configureAlertSound muted) suprime playAlertChime e playLateAlertChime', () => {
    configureAlertSound({ muted: true });
    const { FakeAudioContext, start } = createFakeAudioContext();
    playAlertChime(FakeAudioContext as unknown as typeof AudioContext);
    playLateAlertChime(FakeAudioContext as unknown as typeof AudioContext);
    expect(start).not.toHaveBeenCalled();
  });

  it('modo silencioso também suprime a vibração', () => {
    configureAlertSound({ muted: true });
    const vibrate = vi.fn();
    vibrateAlert({ vibrate } as unknown as Navigator);
    vibrateLateAlert({ vibrate } as unknown as Navigator);
    expect(vibrate).not.toHaveBeenCalled();
  });

  it('fora do modo silencioso, vibrateAlert e vibrateLateAlert usam padrões distintos', () => {
    const vibrate = vi.fn();
    vibrateAlert({ vibrate } as unknown as Navigator);
    vibrateLateAlert({ vibrate } as unknown as Navigator);
    expect(vibrate).toHaveBeenNthCalledWith(1, [200, 100, 200]);
    expect(vibrate).toHaveBeenNthCalledWith(2, [300, 100, 300, 100, 300]);
  });

  it('configureAlertSound aplica o volume configurado ao próximo som', () => {
    configureAlertSound({ volume: 0.4 });
    const { FakeAudioContext, setValueAtTime } = createFakeAudioContext();
    playAlertChime(FakeAudioContext as unknown as typeof AudioContext);
    expect(setValueAtTime).toHaveBeenCalledWith(0.4, expect.any(Number));
  });

  it('previewAlertTone ("testar som") ignora o mudo e o agrupamento em rajada', () => {
    configureAlertSound({ muted: true });
    const { FakeAudioContext, start } = createFakeAudioContext();
    previewAlertTone('CHIME', 0.5, FakeAudioContext as unknown as typeof AudioContext);
    previewAlertTone('CHIME', 0.5, FakeAudioContext as unknown as typeof AudioContext);
    expect(start).toHaveBeenCalledTimes(4); // duas chamadas, nenhuma suprimida
  });

  it('não lança quando AudioContext não existe no ambiente', () => {
    expect(() => playAlertChime(undefined)).not.toThrow();
    expect(() => playLateAlertChime(undefined)).not.toThrow();
    expect(() => previewAlertTone('ALERT', 0.5, undefined)).not.toThrow();
  });
});
