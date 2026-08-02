import { useEffect, useState } from 'react';
import type { Branding } from '@nexora/contracts';

/**
 * Esquema de cor do dispositivo do cliente final (`prefers-color-scheme`) — US-003, gap "logo
 * dark nunca é consumido no frontend": `apps/web-menu` só lia `branding.logo.light`, então um
 * cliente com o celular em modo escuro via a logo clara (baixo contraste sobre fundo escuro,
 * às vezes ilegível). Vive em `theme/` (ao lado de `theme-provider.tsx`) por ser sobre APARÊNCIA
 * do dispositivo, não sobre a marca do tenant em si (isso é `branding/runtime-branding.ts`).
 */
export type ColorScheme = 'light' | 'dark';

const DARK_QUERY = '(prefers-color-scheme: dark)';

function readColorScheme(media: Pick<MediaQueryList, 'matches'> | undefined): ColorScheme {
  return media?.matches ? 'dark' : 'light';
}

function matchDarkScheme(): MediaQueryList | undefined {
  return typeof window === 'undefined' || typeof window.matchMedia !== 'function'
    ? undefined
    : window.matchMedia(DARK_QUERY);
}

/** Esquema de cor atual do dispositivo, atualizado em runtime se o usuário alternar o tema do SO/navegador. */
export function useColorScheme(): ColorScheme {
  const [scheme, setScheme] = useState<ColorScheme>(() => readColorScheme(matchDarkScheme()));

  useEffect(() => {
    const media = matchDarkScheme();
    if (!media) return;

    const listener = () => setScheme(readColorScheme(media));
    listener();
    media.addEventListener('change', listener);
    return () => media.removeEventListener('change', listener);
  }, []);

  return scheme;
}

/**
 * Escolhe a variante de logo pelo esquema de cor ativo, com fallback para a variante que existir
 * — um tenant pode ter configurado só uma das duas (ADR-010, "Ausência de configuração" não deve
 * quebrar a aplicação). Tipa o parâmetro direto por `Branding['logo']` (não uma interface própria
 * equivalente) para nunca divergir do contrato de `@nexora/contracts` — inclusive na forma exata
 * que `exactOptionalPropertyTypes` exige dos campos opcionais inferidos pelo zod.
 */
export function pickBrandLogo(logo: Branding['logo'], scheme: ColorScheme): string | undefined {
  return scheme === 'dark' ? (logo.dark ?? logo.light) : (logo.light ?? logo.dark);
}
