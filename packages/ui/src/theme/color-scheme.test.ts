// @vitest-environment jsdom
import { renderHook, act } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { pickBrandLogo, useColorScheme } from './color-scheme.js';

function stubMatchMedia(initialMatches: boolean) {
  let matches = initialMatches;
  const listeners = new Set<() => void>();
  const media: MediaQueryList = {
    matches,
    media: '(prefers-color-scheme: dark)',
    onchange: null,
    addEventListener: (_event: string, listener: () => void) => listeners.add(listener),
    removeEventListener: (_event: string, listener: () => void) => listeners.delete(listener),
    addListener: () => {},
    removeListener: () => {},
    dispatchEvent: () => true,
  } as unknown as MediaQueryList;

  vi.stubGlobal('matchMedia', vi.fn().mockReturnValue(media));

  return {
    setMatches(next: boolean) {
      matches = next;
      Object.defineProperty(media, 'matches', { value: matches, configurable: true });
      listeners.forEach((listener) => listener());
    },
  };
}

describe('useColorScheme', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('lê o esquema de cor inicial do dispositivo (claro)', () => {
    stubMatchMedia(false);
    const { result } = renderHook(() => useColorScheme());
    expect(result.current).toBe('light');
  });

  it('lê o esquema de cor inicial do dispositivo (escuro)', () => {
    stubMatchMedia(true);
    const { result } = renderHook(() => useColorScheme());
    expect(result.current).toBe('dark');
  });

  it('atualiza quando o usuário alterna o tema do sistema em runtime', () => {
    const stub = stubMatchMedia(false);
    const { result } = renderHook(() => useColorScheme());
    expect(result.current).toBe('light');

    act(() => stub.setMatches(true));

    expect(result.current).toBe('dark');
  });

  it('degrada para claro sem matchMedia (ex.: SSR)', () => {
    vi.stubGlobal('matchMedia', undefined);
    const { result } = renderHook(() => useColorScheme());
    expect(result.current).toBe('light');
  });
});

describe('pickBrandLogo', () => {
  it('escolhe a variante escura no tema escuro', () => {
    expect(pickBrandLogo({ light: 'a.svg', dark: 'b.svg' }, 'dark')).toBe('b.svg');
  });

  it('escolhe a variante clara no tema claro', () => {
    expect(pickBrandLogo({ light: 'a.svg', dark: 'b.svg' }, 'light')).toBe('a.svg');
  });

  it('cai para a variante disponível quando o tenant só configurou uma', () => {
    expect(pickBrandLogo({ light: 'a.svg' }, 'dark')).toBe('a.svg');
    expect(pickBrandLogo({ dark: 'b.svg' }, 'light')).toBe('b.svg');
  });

  it('devolve undefined quando o tenant não configurou nenhuma logo', () => {
    expect(pickBrandLogo({}, 'light')).toBeUndefined();
  });
});
