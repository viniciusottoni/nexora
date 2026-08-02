import type { CSSProperties, PropsWithChildren } from 'react';

export interface Theme {
  readonly colors: Readonly<{
    primary: string;
    onPrimary: string;
    secondary: string;
    surface: string;
  }>;
  readonly font: string;
  readonly radius: number;
}

/** Marca institucional da plataforma Nexora (navy/verde) — doc 02, §5.2 e Design System/readme.md. */
export const neutralTheme: Theme = {
  colors: {
    primary: '#10305E',
    onPrimary: '#FFFFFF',
    secondary: '#55A21C',
    surface: '#F6F8FB',
  },
  font: "'Inter', system-ui, -apple-system, 'Segoe UI', sans-serif",
  radius: 12,
};

export interface ThemeProviderProps extends PropsWithChildren {
  readonly theme?: Theme;
  readonly className?: string;
}

export function ThemeProvider({
  children,
  theme = neutralTheme,
  className = 'db-theme',
}: Readonly<ThemeProviderProps>) {
  const style = {
    '--brand-primary': theme.colors.primary,
    '--brand-on-primary': theme.colors.onPrimary,
    '--brand-secondary': theme.colors.secondary,
    '--brand-surface': theme.colors.surface,
    '--brand-font': theme.font,
    '--brand-radius': `${theme.radius}px`,
  } as CSSProperties;

  return (
    <div className={className} style={style}>
      {children}
    </div>
  );
}
