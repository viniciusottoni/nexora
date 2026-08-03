import { describe, expect, it, vi } from 'vitest';
import { playAlertChime, vibrateAlert } from './alert-sound.js';

describe('vibrateAlert (US-025 §10)', () => {
  it('chama navigator.vibrate com um padrão curto quando disponível', () => {
    const vibrate = vi.fn();
    vibrateAlert({ vibrate } as unknown as Navigator);
    expect(vibrate).toHaveBeenCalledWith([200, 100, 200]);
  });

  it('não lança quando navigator.vibrate não existe (ex.: iOS Safari)', () => {
    expect(() => vibrateAlert({} as Navigator)).not.toThrow();
  });

  it('não lança quando navigator é undefined (ambiente sem DOM)', () => {
    expect(() => vibrateAlert(undefined)).not.toThrow();
  });
});

describe('playAlertChime (US-025 §10)', () => {
  it('constroi dois osciladores curtos via Web Audio API quando disponível', () => {
    const start = vi.fn();
    const stop = vi.fn();
    const connect = vi.fn().mockReturnThis();
    const setValueAtTime = vi.fn();
    const exponentialRampToValueAtTime = vi.fn();
    const close = vi.fn();

    class FakeAudioContext {
      currentTime = 0;
      destination = {};
      createOscillator() {
        return { frequency: { value: 0 }, type: '', connect, start, stop };
      }
      createGain() {
        return { gain: { setValueAtTime, exponentialRampToValueAtTime } };
      }
      close() {
        close();
        return Promise.resolve();
      }
    }

    playAlertChime(FakeAudioContext as unknown as typeof AudioContext);

    expect(start).toHaveBeenCalledTimes(2);
  });

  it('não lança quando AudioContext não existe no ambiente', () => {
    expect(() => playAlertChime(undefined)).not.toThrow();
  });
});
