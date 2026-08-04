// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { NotificationCenter, type NotificationCenterItem } from './notification-center.js';

const items: NotificationCenterItem[] = [
  {
    id: '1',
    type: 'ORDER_LATE',
    severity: 'HIGH',
    message: 'Pedido A47 da mesa 12 está há 21 minutos na fila.',
    raisedAt: new Date().toISOString(),
    acknowledgedAt: null,
  },
  {
    id: '2',
    type: 'PRODUCT_UNAVAILABLE',
    severity: 'WARNING',
    message: 'Produto "Refrigerante" está indisponível.',
    raisedAt: new Date().toISOString(),
    acknowledgedAt: new Date().toISOString(),
  },
];

describe('NotificationCenter', () => {
  it('mostra a contagem de pendentes no sino', () => {
    render(<NotificationCenter items={items} open={false} onOpenChange={vi.fn()} />);

    expect(screen.getByRole('button', { name: /1 pendentes/ })).toHaveTextContent('1');
  });

  it('painel fechado não renderiza a lista', () => {
    render(<NotificationCenter items={items} open={false} onOpenChange={vi.fn()} />);

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('painel aberto lista os itens e reconhece um pendente', () => {
    const onAcknowledge = vi.fn();
    render(<NotificationCenter items={items} open onOpenChange={vi.fn()} onAcknowledge={onAcknowledge} />);

    expect(screen.getByText(/Pedido A47/)).toBeInTheDocument();
    expect(screen.getByText(/Refrigerante/)).toBeInTheDocument();

    screen.getByRole('button', { name: 'Reconhecer' }).click();
    expect(onAcknowledge).toHaveBeenCalledWith('1');
  });

  it('estado vazio quando não há notificações', () => {
    render(<NotificationCenter items={[]} open onOpenChange={vi.fn()} />);

    expect(screen.getByText('Nenhuma notificação')).toBeInTheDocument();
  });

  it('US-081 §4 "Permissão não concedida" — mostra convite discreto para ativar push', () => {
    const onRequestPushPermission = vi.fn();
    render(
      <NotificationCenter
        items={[]}
        open
        onOpenChange={vi.fn()}
        pushPermissionPending
        onRequestPushPermission={onRequestPushPermission}
      />,
    );

    const invite = screen.getByText(/Ativar notificações no navegador/);
    invite.click();
    expect(onRequestPushPermission).toHaveBeenCalledOnce();
  });

  it('US-083 §4 grupo exibe a contagem consolidada', () => {
    const grouped: NotificationCenterItem[] = [
      {
        id: 'group-order-late',
        type: 'ORDER_LATE',
        severity: 'HIGH',
        message: '5 pedidos atrasados',
        raisedAt: new Date().toISOString(),
        acknowledgedAt: null,
        count: 5,
      },
    ];

    render(<NotificationCenter items={grouped} open onOpenChange={vi.fn()} />);

    expect(screen.getByText(/5×/)).toBeInTheDocument();
  });
});
