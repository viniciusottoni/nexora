// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ThemeProvider, neutralTheme, type Theme } from './theme-provider.js';

describe('ThemeProvider', () => {
  it('aplica branding em runtime por CSS custom properties', () => {
    const theme: Theme = {
      ...neutralTheme,
      colors: { ...neutralTheme.colors, primary: '#B42318', onPrimary: '#FFFFFF' },
      radius: 16,
    };
    render(
      <ThemeProvider theme={theme}>
        <span>Conteúdo</span>
      </ThemeProvider>,
    );

    const root = screen.getByText('Conteúdo').parentElement;
    expect(root).toHaveStyle('--brand-primary: #B42318');
    expect(root).toHaveStyle('--brand-radius: 16px');
  });

  it('usa tema neutro (marca Nexora) quando branding está ausente', () => {
    render(
      <ThemeProvider>
        <span>Fallback</span>
      </ThemeProvider>,
    );

    expect(screen.getByText('Fallback').parentElement).toHaveStyle(
      `--brand-primary: ${neutralTheme.colors.primary}`,
    );
  });
});
