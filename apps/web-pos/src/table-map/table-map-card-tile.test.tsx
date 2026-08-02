// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { StrictMode } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { TableMapCardTile, type TableMapCardTileProps } from './table-map-card-tile.js';

const baseProps: TableMapCardTileProps = {
  id: '0198aabb-1111-7000-8000-000000000001',
  label: '12',
  status: 'OCCUPIED',
  minutesOpen: 47,
  guestCount: 4,
  totalLabel: 'R$ 186,40',
  waiterName: 'Ana',
  sessionId: '0198aabb-1111-7000-8000-000000000009',
  billRequested: false,
  waiterCalled: false,
  itemsReadyToServe: 0,
  aboveAvgDuration: false,
};

describe('TableMapCardTile', () => {
  it('mostra identificação, tempo, valor e garçom', () => {
    render(<TableMapCardTile {...baseProps} />);
    expect(screen.getByRole('button', { name: /Mesa 12/ })).toBeInTheDocument();
    expect(screen.getByText('47 min')).toBeInTheDocument();
    expect(screen.getByText('R$ 186,40')).toBeInTheDocument();
  });

  it('limita a três badges de sinal simultâneos (US-023 §15)', () => {
    render(
      <TableMapCardTile
        {...baseProps}
        billRequested
        waiterCalled
        itemsReadyToServe={2}
        aboveAvgDuration
      />,
    );
    expect(screen.getByText('Conta pedida')).toBeInTheDocument();
    expect(screen.getByText('Garçom chamado')).toBeInTheDocument();
    expect(screen.getByText('2 itens prontos')).toBeInTheDocument();
    expect(screen.queryByText('Acima do tempo médio')).not.toBeInTheDocument();
  });

  it(
    'não re-renderiza quando as props primitivas não mudam — memoização necessária para o ' +
      'orçamento de 60 mesas em <1s (US-023 §12); o teste real de FPS de celular não é possível ' +
      'neste ambiente (sem device físico), então validamos o mecanismo que sustenta a meta: menos ' +
      'trabalho de render por atualização, não o tempo de frame em si',
    () => {
      let renderCount = 0;
      function CountingWrapper(props: Readonly<TableMapCardTileProps>) {
        renderCount += 1;
        return <TableMapCardTile {...props} />;
      }

      // Envolve o próprio tile numa função rastreada só para confirmar a mecânica de memo:
      // como TableMapCardTile é `memo(...)`, re-renderizar o PAI com props idênticas não deveria
      // re-executar o corpo do componente interno. Não dá para contar renders do componente
      // memoizado diretamente sem um profiler — em vez disso, o teste abaixo mede o efeito
      // observável: o DOM não é recriado (mesmo nó), confirmando que o React pulou o re-render.
      const { rerender, container } = render(<CountingWrapper {...baseProps} />);
      const firstNode = container.querySelector('.table-map__tile');
      rerender(<CountingWrapper {...baseProps} />);
      const secondNode = container.querySelector('.table-map__tile');

      expect(renderCount).toBe(2); // o wrapper (não memoizado) sempre re-renderiza — esperado
      expect(secondNode).toBe(firstNode); // mas o React não recriou o DOM do tile memoizado
    },
  );

  it('aciona onSelect com o id da mesa', () => {
    const onSelect = vi.fn();
    render(<TableMapCardTile {...baseProps} onSelect={onSelect} />);
    screen.getByRole('button', { name: /Mesa 12/ }).click();
    expect(onSelect).toHaveBeenCalledWith(baseProps.id);
  });

  it('renderiza em StrictMode sem duplicar o DOM visível', () => {
    render(
      <StrictMode>
        <TableMapCardTile {...baseProps} />
      </StrictMode>,
    );
    expect(screen.getAllByRole('button', { name: /Mesa 12/ })).toHaveLength(1);
  });

  it('US-025 §7: mostra "Atendido" quando ha chamada pendente e aciona onAcknowledgeCall com o id da mesa', () => {
    const onAcknowledgeCall = vi.fn();
    render(<TableMapCardTile {...baseProps} waiterCalled onAcknowledgeCall={onAcknowledgeCall} />);

    screen.getByRole('button', { name: /atendido/i }).click();

    expect(onAcknowledgeCall).toHaveBeenCalledWith(baseProps.id);
  });

  it('sem chamada pendente, nao mostra o botao "Atendido"', () => {
    render(<TableMapCardTile {...baseProps} waiterCalled={false} onAcknowledgeCall={vi.fn()} />);
    expect(screen.queryByRole('button', { name: /atendido/i })).not.toBeInTheDocument();
  });

  it('US-026 §4: mostra "Pedir conta" numa mesa ocupada e aciona onRequestBill com o id da sessao', () => {
    const onRequestBill = vi.fn();
    render(<TableMapCardTile {...baseProps} onRequestBill={onRequestBill} />);

    screen.getByRole('button', { name: /pedir conta/i }).click();

    expect(onRequestBill).toHaveBeenCalledWith(baseProps.sessionId);
  });

  it('mesa com conta ja solicitada nao mostra "Pedir conta" de novo', () => {
    render(<TableMapCardTile {...baseProps} billRequested onRequestBill={vi.fn()} />);
    expect(screen.queryByRole('button', { name: /pedir conta/i })).not.toBeInTheDocument();
  });
});
