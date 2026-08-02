// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { AlertBanner } from './alert-banner.js';

describe('AlertBanner', () => {
  it('anuncia o fato com role="status" e tom informativo por padrão', () => {
    render(<AlertBanner title="Fato">Consequência do fato.</AlertBanner>);

    const banner = screen.getByRole('status');
    expect(banner).toHaveClass('db-alert-banner--info');
    expect(screen.getByText('Fato')).toBeInTheDocument();
    expect(screen.getByText('Consequência do fato.')).toBeInTheDocument();
  });

  it('nunca é só o alarme: expõe a ação de resolução junto do alerta', () => {
    render(
      <AlertBanner tone="danger" title="Divergência acima de 5%" actions={<button type="button">Abrir contagem</button>}>
        Queijo mussarela: teórico 41,2 kg × real 36,8 kg.
      </AlertBanner>,
    );

    const banner = screen.getByRole('status');
    expect(banner).toHaveClass('db-alert-banner--danger');
    expect(screen.getByRole('button', { name: 'Abrir contagem' })).toBeInTheDocument();
  });
});
