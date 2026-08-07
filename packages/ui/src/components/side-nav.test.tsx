// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { SideNav } from './side-nav.js';

describe('SideNav', () => {
  it('marca o item ativo e aciona a seleção pelo id', () => {
    const onSelect = vi.fn();
    render(
      <SideNav
        activeId="mesas"
        onSelect={onSelect}
        items={[
          { group: 'Operação' },
          { id: 'pulso', label: 'Pulso', icon: 'monitor_heart' },
          { id: 'mesas', label: 'Mesas', icon: 'table_restaurant', count: 12 },
        ]}
      />,
    );

    expect(screen.getByText('Operação')).toBeInTheDocument();

    const active = screen.getByRole('button', { name: /Mesas/ });
    expect(active.className).toContain('db-sidenav__item--on');
    expect(active).toHaveAttribute('aria-current', 'page');

    const inactive = screen.getByRole('button', { name: /Pulso/ });
    expect(inactive).not.toHaveAttribute('aria-current');
    inactive.click();
    expect(onSelect).toHaveBeenCalledWith('pulso');
  });

  it('usa a variante clara quando solicitado', () => {
    render(<SideNav variant="light" items={[{ id: 'x', label: 'X' }]} />);
    expect(screen.getByRole('navigation').className).toContain('db-sidenav--light');
  });
});
